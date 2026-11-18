using System.Data;
using Dapper;
using Lodge.Application.Abstractions.Assets;
using Lodge.Contracts.Bookings;
using Lodge.Domain.Bookings;

namespace Lodge.Application.Bookings;

/// <summary>
/// Contains the Dapper read queries for bookings.
/// </summary>
internal static class BookingQueries
{
    // The apartment image is the cover photo (position 0) of the listing.
    private const string SelectBooking =
        """
        SELECT
            b.id AS Id,
            b.apartment_id AS ApartmentId,
            b.user_id AS UserId,
            b.status AS Status,
            b.price_for_period_amount AS PriceAmount,
            b.price_for_period_currency AS PriceCurrency,
            b.cleaning_fee_amount AS CleaningFeeAmount,
            b.cleaning_fee_currency AS CleaningFeeCurrency,
            b.amenities_up_charge_amount AS AmenitiesUpChargeAmount,
            b.amenities_up_charge_currency AS AmenitiesUpChargeCurrency,
            b.total_price_amount AS TotalPriceAmount,
            b.total_price_currency AS TotalPriceCurrency,
            b.discount_amount AS DiscountAmount,
            b.service_fee_amount AS ServiceFeeAmount,
            b.taxes_amount AS TaxAmount,
            b.tax_rate_percent AS TaxRatePercent,
            b.cancellation_policy AS CancellationPolicy,
            b.amount_paid AS AmountPaid,
            b.paid_on_utc AS PaidOnUtc,
            b.refund_amount AS RefundAmount,
            b.refund_status AS RefundStatus,
            b.cancelled_by_host AS CancelledByHost,
            b.duration_start AS DurationStart,
            b.duration_end AS DurationEnd,
            b.created_on_utc AS CreatedOnUtc,
            b.guest_count AS GuestCount,
            b.message_to_host AS MessageToHost,
            a.name AS ApartmentName,
            COALESCE((
                SELECT COALESCE(i.path, '/api/v1/files/' || i.id::text)
                FROM apartment_images AS i
                WHERE i.apartment_id = a.id
                ORDER BY i.position
                LIMIT 1), '') AS ApartmentImageUrl,
            a.owner_id AS ListingHostId,
            a.check_in_instructions AS CheckInInstructions,
            u.first_name || ' ' || u.last_name AS GuestName,
            u.email AS GuestEmail,
            EXISTS (SELECT 1 FROM reviews AS r WHERE r.booking_id = b.id) AS HasReview,
            EXISTS (SELECT 1 FROM guest_reviews AS g WHERE g.booking_id = b.id) AS HasGuestReview
        FROM bookings AS b
        INNER JOIN apartments AS a ON b.apartment_id = a.id
        INNER JOIN users AS u ON b.user_id = u.id
        """;

    public static Task<BookingResponse?> GetByIdAsync(IDbConnection connection, Guid bookingId) =>
        connection.QueryFirstOrDefaultAsync<BookingResponse>(
            $"""
            {SelectBooking}
            WHERE b.id = @BookingId
            """,
            new { BookingId = bookingId });

    public static async Task<List<BookingResponse>> GetByUserIdAsync(IDbConnection connection, Guid userId)
    {
        IEnumerable<BookingResponse> bookings = await connection.QueryAsync<BookingResponse>(
            $"""
            {SelectBooking}
            WHERE b.user_id = @UserId
            ORDER BY b.duration_start DESC, b.created_on_utc DESC
            """,
            new { UserId = userId });

        return bookings.ToList();
    }

    /// <summary>
    /// Returns one page of bookings, newest first, optionally filtered by status, host and listing.
    /// </summary>
    public static async Task<(List<BookingResponse> Items, int TotalCount)> GetPageAsync(
        IDbConnection connection,
        int? status,
        int page,
        int pageSize,
        Guid? hostId = null,
        Guid? apartmentId = null)
    {
        List<string> conditions = [];

        if (status.HasValue)
        {
            conditions.Add("b.status = @Status");
        }

        if (hostId.HasValue)
        {
            conditions.Add("a.owner_id = @HostId");
        }

        if (apartmentId.HasValue)
        {
            conditions.Add("b.apartment_id = @ApartmentId");
        }

        string where = conditions.Count == 0 ? string.Empty : $"WHERE {string.Join(" AND ", conditions)}";

        string sql =
            $"""
            SELECT COUNT(*)
            FROM bookings AS b
            INNER JOIN apartments AS a ON b.apartment_id = a.id
            {where};

            {SelectBooking}
            {where}
            ORDER BY b.created_on_utc DESC, b.id
            LIMIT @PageSize OFFSET @Offset;
            """;

        using SqlMapper.GridReader grid = await connection.QueryMultipleAsync(
            sql,
            new
            {
                Status = status ?? 0,
                HostId = hostId ?? Guid.Empty,
                ApartmentId = apartmentId ?? Guid.Empty,
                PageSize = pageSize,
                Offset = (page - 1) * pageSize,
            });

        int totalCount = await grid.ReadSingleAsync<int>();
        List<BookingResponse> items = (await grid.ReadAsync<BookingResponse>()).ToList();

        return (items, totalCount);
    }

    /// <summary>
    /// When an unanswered request expires, by the rules of the expiry job: <paramref name="expiryHours"/> after it was
    /// made, or once its check-in day has passed (the host can still accept on the check-in day), whichever is first.
    /// </summary>
    public static DateTime RequestExpiresOnUtc(DateTime createdOnUtc, DateOnly checkIn, int expiryHours)
    {
        DateTime byAge = DateTime.SpecifyKind(createdOnUtc, DateTimeKind.Utc).AddHours(expiryHours);
        DateTime byCheckIn = checkIn.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        return byAge < byCheckIn ? byAge : byCheckIn;
    }

    /// <summary>
    /// Resolves the image URL, hides the check-in instructions from everyone except the listing's host and the guest
    /// of a confirmed booking, and works out whether the caller can review the stay (the guest reviews the listing,
    /// the host reviews the guest; completed stays only, within the review window).
    /// </summary>
    public static BookingResponse Present(
        BookingResponse booking,
        Guid? callerId,
        IAssetUrlResolver assetUrlResolver,
        DateOnly today,
        int requestExpiryHours)
    {
        booking.RequestExpiresOnUtc = booking.Status == (int)BookingStatus.PendingApproval
            ? RequestExpiresOnUtc(booking.CreatedOnUtc, DateOnly.FromDateTime(booking.DurationStart), requestExpiryHours)
            : null;

        booking.ApartmentImageUrl = assetUrlResolver.Resolve(booking.ApartmentImageUrl);

        bool isHost = callerId == booking.ListingHostId;
        bool isGuest = callerId == booking.UserId;
        bool isConfirmedGuest = isGuest && booking.Status == (int)BookingStatus.Confirmed;

        if (!isHost && !isConfirmedGuest)
        {
            booking.CheckInInstructions = null;
        }

        if (booking.Status == (int)BookingStatus.Completed)
        {
            DateOnly deadline = DateOnly.FromDateTime(booking.DurationEnd).AddDays(Booking.ReviewWindowDays);
            bool windowOpen = today <= deadline;

            booking.ReviewDeadline = deadline;
            booking.CanReview = windowOpen && ((isGuest && !booking.HasReview) || (isHost && !booking.HasGuestReview));
        }

        return booking;
    }
}
