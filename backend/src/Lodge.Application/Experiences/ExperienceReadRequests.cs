using System.Data;
using FluentValidation;
using Lodge.Application.Abstractions.App;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Core.Extensions;
using Lodge.Application.Hosting;
using Lodge.Application.Payments;
using Lodge.Contracts.Common;
using Lodge.Contracts.Experiences;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Experiences;
using Microsoft.Extensions.Options;

namespace Lodge.Application.Experiences;

/// <summary>
/// Searches the listed experiences that have a future session with enough spots for <paramref name="Guests"/> (on
/// <paramref name="Date"/>, a UTC date, if given). Prices are compared in the base currency (USD). Ordered by the next
/// session.
/// </summary>
public sealed record SearchExperiencesQuery(
    string? Q,
    string? City,
    string? Category,
    DateOnly? Date,
    int? Guests,
    decimal? MinPrice,
    decimal? MaxPrice,
    int Page,
    int PageSize) : IQuery<PagedList<ExperienceSummaryResponse>>;

internal sealed class SearchExperiencesQueryValidator : AbstractValidator<SearchExperiencesQuery>
{
    public SearchExperiencesQueryValidator()
    {
        RuleFor(x => x.Page).ValidPage();
        RuleFor(x => x.PageSize).ValidPageSize();

        RuleFor(x => x.Category)
            .Must(category => string.IsNullOrWhiteSpace(category) || EnumNames.TryParse<ExperienceCategory>(category, out _))
            .WithError(ExperienceErrors.InvalidCategory);

        RuleFor(x => x.Guests)
            .InclusiveBetween(1, Experience.MaxGuestsLimit)
            .When(x => x.Guests is not null)
            .WithError(ExperienceBookingErrors.InvalidGuestCount);

        RuleFor(x => x)
            .Must(x => x.MinPrice is null or >= 0 && x.MaxPrice is null or >= 0 &&
                       (x.MinPrice is null || x.MaxPrice is null || x.MinPrice <= x.MaxPrice))
            .WithError(Core.Errors.ValidationErrors.SearchApartments.InvalidPriceRange);
    }
}

internal sealed class SearchExperiencesQueryHandler(
    IDbConnectionFactory factory,
    CurrencyConverter currencyConverter,
    IDateTimeProvider dateTimeProvider,
    IOptions<AppOptions> appOptions) : IQueryHandler<SearchExperiencesQuery, PagedList<ExperienceSummaryResponse>>
{
    public async Task<Result<PagedList<ExperienceSummaryResponse>>> Handle(
        SearchExperiencesQuery request,
        CancellationToken cancellationToken)
    {
        SearchExperiencesQuery query = request with
        {
            Category = string.IsNullOrWhiteSpace(request.Category)
                ? null
                : EnumNames.Parse<ExperienceCategory>(request.Category).ToString(),
        };

        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        (List<ExperienceSummaryResponse> items, int totalCount) = await ExperienceQueries.SearchAsync(
            connection,
            query,
            dateTimeProvider.UtcNow,
            currencyConverter.ToBaseSql("e.price_per_guest", "e.currency"),
            appOptions.Value);

        return new PagedList<ExperienceSummaryResponse>(items, request.Page, request.PageSize, totalCount);
    }
}

/// <summary>
/// Gets a listed experience; unlisted ones are only visible to their host (<paramref name="CallerId"/>).
/// </summary>
public sealed record GetExperienceByIdQuery(Guid ExperienceId, Guid? CallerId) : IQuery<ExperienceResponse>;

internal sealed class GetExperienceByIdQueryHandler(IDbConnectionFactory factory, IOptions<AppOptions> appOptions)
    : IQueryHandler<GetExperienceByIdQuery, ExperienceResponse>
{
    public async Task<Result<ExperienceResponse>> Handle(GetExperienceByIdQuery request, CancellationToken cancellationToken)
    {
        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        ExperienceResponse? experience = await ExperienceQueries.GetByIdAsync(connection, request.ExperienceId, appOptions.Value);

        if (experience is null || (!experience.IsListed && experience.Host.Id != request.CallerId))
        {
            return Result.Failure<ExperienceResponse>(ExperienceErrors.NotFound(request.ExperienceId));
        }

        return experience;
    }
}

/// <summary>
/// Gets the bookable sessions of a listed experience between <paramref name="From"/> (default: now) and
/// <paramref name="To"/> (default: 60 days later, at most 366 days).
/// </summary>
public sealed record GetExperienceSessionsQuery(Guid ExperienceId, DateTime? From, DateTime? To)
    : IQuery<List<ExperienceSessionResponse>>;

internal sealed class GetExperienceSessionsQueryHandler(
    IDbConnectionFactory factory,
    IDateTimeProvider dateTimeProvider,
    IOptions<AppOptions> appOptions) : IQueryHandler<GetExperienceSessionsQuery, List<ExperienceSessionResponse>>
{
    public async Task<Result<List<ExperienceSessionResponse>>> Handle(
        GetExperienceSessionsQuery request,
        CancellationToken cancellationToken)
    {
        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        ExperienceResponse? experience = await ExperienceQueries.GetByIdAsync(connection, request.ExperienceId, appOptions.Value);

        if (experience is not { IsListed: true })
        {
            return Result.Failure<List<ExperienceSessionResponse>>(ExperienceErrors.NotFound(request.ExperienceId));
        }

        DateTime utcNow = dateTimeProvider.UtcNow;
        (DateTime from, DateTime to) = SessionRange.Resolve(request.From, request.To, utcNow);

        // Past sessions cannot be booked.
        return await ExperienceQueries.GetSessionsAsync(connection, request.ExperienceId, from < utcNow ? utcNow : from, to);
    }
}

/// <summary>
/// Gets one page of an experience's reviews, newest first.
/// </summary>
public sealed record GetExperienceReviewsQuery(Guid ExperienceId, int Page, int PageSize)
    : IQuery<PagedList<ExperienceReviewResponse>>;

internal sealed class GetExperienceReviewsQueryValidator : AbstractValidator<GetExperienceReviewsQuery>
{
    public GetExperienceReviewsQueryValidator()
    {
        RuleFor(x => x.Page).ValidPage();
        RuleFor(x => x.PageSize).ValidPageSize();
    }
}

internal sealed class GetExperienceReviewsQueryHandler(IDbConnectionFactory factory, IOptions<AppOptions> appOptions)
    : IQueryHandler<GetExperienceReviewsQuery, PagedList<ExperienceReviewResponse>>
{
    public async Task<Result<PagedList<ExperienceReviewResponse>>> Handle(
        GetExperienceReviewsQuery request,
        CancellationToken cancellationToken)
    {
        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        if (await ExperienceQueries.GetByIdAsync(connection, request.ExperienceId, appOptions.Value) is null)
        {
            return Result.Failure<PagedList<ExperienceReviewResponse>>(ExperienceErrors.NotFound(request.ExperienceId));
        }

        (List<ExperienceReviewResponse> items, int totalCount) = await ExperienceQueries.GetReviewsAsync(
            connection,
            request.ExperienceId,
            request.Page,
            request.PageSize,
            appOptions.Value);

        return new PagedList<ExperienceReviewResponse>(items, request.Page, request.PageSize, totalCount);
    }
}

/// <summary>
/// Gets one page of the caller's experiences, newest first (<c>GET /hosting/experiences</c>).
/// </summary>
public sealed record GetHostExperiencesQuery(int Page, int PageSize) : IQuery<PagedList<HostExperienceSummaryResponse>>;

internal sealed class GetHostExperiencesQueryValidator : AbstractValidator<GetHostExperiencesQuery>
{
    public GetHostExperiencesQueryValidator()
    {
        RuleFor(x => x.Page).ValidPage();
        RuleFor(x => x.PageSize).ValidPageSize();
    }
}

internal sealed class GetHostExperiencesQueryHandler(
    IHostAccess hostAccess,
    IDbConnectionFactory factory,
    IDateTimeProvider dateTimeProvider,
    IOptions<AppOptions> appOptions) : IQueryHandler<GetHostExperiencesQuery, PagedList<HostExperienceSummaryResponse>>
{
    public async Task<Result<PagedList<HostExperienceSummaryResponse>>> Handle(
        GetHostExperiencesQuery request,
        CancellationToken cancellationToken)
    {
        Result<HostCaller> caller = await hostAccess.GetCallerAsync(cancellationToken);

        if (caller.IsFailure)
        {
            return Result.Failure<PagedList<HostExperienceSummaryResponse>>(caller.Error);
        }

        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        (List<HostExperienceSummaryResponse> items, int totalCount) = await ExperienceQueries.GetHostExperiencesAsync(
            connection,
            caller.Value.UserId,
            request.Page,
            request.PageSize,
            dateTimeProvider.UtcNow,
            appOptions.Value);

        return new PagedList<HostExperienceSummaryResponse>(items, request.Page, request.PageSize, totalCount);
    }
}

/// <summary>
/// Gets one of the caller's experiences (listed or not).
/// </summary>
public sealed record GetHostExperienceQuery(Guid ExperienceId) : IQuery<ExperienceResponse>;

internal sealed class GetHostExperienceQueryHandler(
    IHostAccess hostAccess,
    IDbConnectionFactory factory,
    IOptions<AppOptions> appOptions) : IQueryHandler<GetHostExperienceQuery, ExperienceResponse>
{
    public async Task<Result<ExperienceResponse>> Handle(GetHostExperienceQuery request, CancellationToken cancellationToken)
    {
        Result<HostCaller> caller = await hostAccess.GetCallerAsync(cancellationToken);

        if (caller.IsFailure)
        {
            return Result.Failure<ExperienceResponse>(caller.Error);
        }

        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        ExperienceResponse? experience = await ExperienceQueries.GetByIdAsync(connection, request.ExperienceId, appOptions.Value);

        if (experience is null)
        {
            return Result.Failure<ExperienceResponse>(ExperienceErrors.NotFound(request.ExperienceId));
        }

        if (!caller.Value.IsAdmin && experience.Host.Id != caller.Value.UserId)
        {
            return Result.Failure<ExperienceResponse>(ExperienceErrors.NotHost);
        }

        return experience;
    }
}

/// <summary>
/// Gets the sessions of one of the caller's experiences between <paramref name="From"/> (default: now) and
/// <paramref name="To"/> (default: 60 days later), including cancelled ones.
/// </summary>
public sealed record GetHostExperienceSessionsQuery(Guid ExperienceId, DateTime? From, DateTime? To)
    : IQuery<List<HostExperienceSessionResponse>>;

internal sealed class GetHostExperienceSessionsQueryHandler(
    IHostAccess hostAccess,
    IExperienceRepository experienceRepository,
    IDbConnectionFactory factory,
    IDateTimeProvider dateTimeProvider) : IQueryHandler<GetHostExperienceSessionsQuery, List<HostExperienceSessionResponse>>
{
    public async Task<Result<List<HostExperienceSessionResponse>>> Handle(
        GetHostExperienceSessionsQuery request,
        CancellationToken cancellationToken)
    {
        var managed = await HostExperiences.GetManagedAsync(hostAccess, experienceRepository, request.ExperienceId, cancellationToken);

        if (managed.IsFailure)
        {
            return Result.Failure<List<HostExperienceSessionResponse>>(managed.Error);
        }

        (DateTime from, DateTime to) = SessionRange.Resolve(request.From, request.To, dateTimeProvider.UtcNow);

        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        return await ExperienceQueries.GetHostSessionsAsync(connection, request.ExperienceId, from, to);
    }
}

/// <summary>
/// Gets one page of the bookings of the caller's experiences (optionally of one experience), latest session first.
/// </summary>
public sealed record GetHostExperienceBookingsQuery(Guid? ExperienceId, int Page, int PageSize)
    : IQuery<PagedList<ExperienceBookingResponse>>;

internal sealed class GetHostExperienceBookingsQueryValidator : AbstractValidator<GetHostExperienceBookingsQuery>
{
    public GetHostExperienceBookingsQueryValidator()
    {
        RuleFor(x => x.Page).ValidPage();
        RuleFor(x => x.PageSize).ValidPageSize();
    }
}

internal sealed class GetHostExperienceBookingsQueryHandler(
    IHostAccess hostAccess,
    IDbConnectionFactory factory,
    IDateTimeProvider dateTimeProvider,
    IOptions<AppOptions> appOptions) : IQueryHandler<GetHostExperienceBookingsQuery, PagedList<ExperienceBookingResponse>>
{
    public async Task<Result<PagedList<ExperienceBookingResponse>>> Handle(
        GetHostExperienceBookingsQuery request,
        CancellationToken cancellationToken)
    {
        Result<HostCaller> caller = await hostAccess.GetCallerAsync(cancellationToken);

        if (caller.IsFailure)
        {
            return Result.Failure<PagedList<ExperienceBookingResponse>>(caller.Error);
        }

        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        (List<ExperienceBookingResponse> items, int totalCount) = await ExperienceQueries.GetHostBookingsAsync(
            connection,
            caller.Value.UserId,
            request.ExperienceId,
            request.Page,
            request.PageSize,
            dateTimeProvider.UtcNow,
            appOptions.Value);

        return new PagedList<ExperienceBookingResponse>(items, request.Page, request.PageSize, totalCount);
    }
}

internal static class SessionRange
{
    public const int DefaultDays = 60;

    public const int MaxDays = 366;

    public static (DateTime From, DateTime To) Resolve(DateTime? from, DateTime? to, DateTime utcNow)
    {
        DateTime start = ToUtc(from) ?? utcNow;
        DateTime end = ToUtc(to) ?? start.AddDays(DefaultDays);

        if (end < start)
        {
            end = start;
        }

        if (end > start.AddDays(MaxDays))
        {
            end = start.AddDays(MaxDays);
        }

        return (start, end);
    }

    private static DateTime? ToUtc(DateTime? value) => value switch
    {
        null => null,
        { Kind: DateTimeKind.Local } local => local.ToUniversalTime(),
        DateTime other => DateTime.SpecifyKind(other, DateTimeKind.Utc),
    };
}
