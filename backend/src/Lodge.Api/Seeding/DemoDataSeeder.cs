using Lodge.Application.Abstractions.Cryptography;
using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Reviews;
using Lodge.Domain.Shared;
using Lodge.Domain.Users;
using Lodge.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lodge.Api.Seeding;

/// <summary>
/// Seeds realistic demo data for local development and screenshots. Idempotent: nothing is written when the
/// demo user already exists. Booking dates are relative to "today", so the calendar always looks alive.
/// </summary>
/// <remarks>
/// Demo login: <see cref="DemoEmail"/> / <see cref="DemoPassword"/>. The other guests use the same password.
/// Admin login: <see cref="AdminEmail"/> / <see cref="AdminPassword"/>.
/// Images are stored as relative paths (<c>/images/...</c>, served from <c>wwwroot</c>) and resolved to absolute
/// URLs with <c>Assets:PublicBaseUrl</c> when returned by the API.
/// </remarks>
internal sealed partial class DemoDataSeeder(
    LodgeDbContext dbContext,
    IPasswordHasher passwordHasher,
    PricingService pricingService,
    IDateTimeProvider dateTimeProvider,
    ILogger<DemoDataSeeder> logger)
{
    public const string DemoEmail = "demo@lodge.dev";
    public const string DemoPassword = "Demo123!";
    public const string AdminEmail = "admin@lodge.dev";
    public const string AdminPassword = "Admin123!";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedAdminAsync(cancellationToken);

        Email demoEmail = Email.Create(DemoEmail).Value;

        if (await dbContext.Users.AnyAsync(u => u.Email.Value == demoEmail.Value, cancellationToken))
        {
            return;
        }

        DateTime utcNow = dateTimeProvider.UtcNow;
        DateOnly today = DateOnly.FromDateTime(utcNow);

        Dictionary<string, Apartment> apartments = await SeedApartmentsAsync(cancellationToken);

        User demo = CreateUser("Alex", "Morgan", DemoEmail);
        User jamie = CreateUser("Jamie", "Lee", "jamie.lee@lodge.dev");
        User priya = CreateUser("Priya", "Patel", "priya.patel@lodge.dev");
        User lucas = CreateUser("Lucas", "Martin", "lucas.martin@lodge.dev");
        dbContext.Users.AddRange(demo, jamie, priya, lucas);

        // Past, completed stays with reviews.
        (string Apartment, User Guest, int StartOffset, int Nights, int Rating, string Comment)[] pastStays =
        [
            ("Pine Ridge Cabin", demo, -64, 5, 5, "Waking up to the mountains with a coffee on the porch was magical. Spotless and cosy."),
            ("Canal Houseboat", demo, -31, 3, 4, "A unique way to see Amsterdam. Compact, but very well thought out."),
            ("Pine Ridge Cabin", jamie, -40, 4, 5, "The wood stove and the hot tub made our winter getaway perfect."),
            ("Hillside Luxury Villa", jamie, -90, 6, 5, "Unbelievable ocean views and a pool we never wanted to leave."),
            ("Aurora Glass Igloo", priya, -120, 2, 5, "We saw the northern lights from bed. Once-in-a-lifetime experience!"),
            ("Lavender Hill Cottage", priya, -75, 4, 4, "Charming village, lovely garden. The kitchen is a bit small."),
            ("Redwood Treehouse", priya, -20, 3, 5, "Fell asleep listening to the forest. The kids still talk about it."),
            ("Château des Vignes", lucas, -150, 5, 5, "Perfect for our family reunion: wine tasting, long dinners and space for everyone."),
            ("Minimalist Lake House", lucas, -45, 4, 4, "Beautiful design and a stunning lake. Bring warm clothes in the evening."),
            ("Seaside Family Retreat", lucas, -100, 7, 5, "Walking distance to the beach, great for kids, host was very responsive."),
            ("Maple Grove Family Home", jamie, -12, 3, 4, "Plenty of room for the whole family and a big backyard."),
            ("Canal Houseboat", lucas, -200, 2, 3, "Great location, but the bedroom gets warm in summer."),
        ];

        foreach ((string apartmentName, User guest, int startOffset, int nights, int rating, string comment) in pastStays)
        {
            Booking booking = Reserve(apartments[apartmentName], guest, today.AddDays(startOffset), nights, utcNow);
            booking.Confirm(utcNow);
            booking.Complete(utcNow);

            Review review = Review.Create(booking, Rating.Create(rating).Value, Comment.Create(comment).Value).Value;
            dbContext.Reviews.Add(review);
        }

        // A finished stay the demo user has not reviewed yet.
        Booking unreviewed = Reserve(apartments["Redwood Treehouse"], demo, today.AddDays(-9), 2, utcNow);
        unreviewed.Confirm(utcNow);
        unreviewed.Complete(utcNow);

        // The demo user's current and upcoming bookings (one per status shown in the UI).
        Booking confirmed = Reserve(apartments["Hillside Luxury Villa"], demo, today.AddDays(14), 5, utcNow);
        confirmed.Confirm(utcNow);

        Reserve(apartments["Aurora Glass Igloo"], demo, today.AddDays(30), 3, utcNow);

        Booking cancelled = Reserve(apartments["Lavender Hill Cottage"], demo, today.AddDays(7), 3, utcNow);
        cancelled.Cancel(utcNow);

        Booking rejected = Reserve(apartments["Château des Vignes"], demo, today.AddDays(45), 4, utcNow);
        rejected.Reject(utcNow);

        // Other guests' upcoming stays, so the availability calendars have blocked dates.
        Reserve(apartments["Pine Ridge Cabin"], jamie, today.AddDays(3), 4, utcNow).Confirm(utcNow);
        Reserve(apartments["Canal Houseboat"], priya, today.AddDays(10), 5, utcNow).Confirm(utcNow);
        Reserve(apartments["Seaside Family Retreat"], lucas, today.AddDays(21), 7, utcNow);

        // Demo data must not trigger welcome/booking e-mails through the outbox.
        foreach (Entity entity in dbContext.ChangeTracker.Entries<Entity>().Select(e => e.Entity))
        {
            entity.ClearDomainEvents();
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        LogSeeded(logger, apartments.Count, DemoEmail);
    }

    private async Task SeedAdminAsync(CancellationToken cancellationToken)
    {
        Email adminEmail = Email.Create(AdminEmail).Value;

        if (await dbContext.Users.AnyAsync(u => u.Email.Value == adminEmail.Value, cancellationToken))
        {
            return;
        }

        User admin = CreateUser("Avery", "Admin", AdminEmail, AdminPassword);
        admin.ChangeRole(UserRole.Admin);
        admin.ClearDomainEvents();

        dbContext.Users.Add(admin);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Dictionary<string, Apartment>> SeedApartmentsAsync(CancellationToken cancellationToken)
    {
        List<Apartment> existing = await dbContext.Apartments.ToListAsync(cancellationToken);

        Dictionary<string, Apartment> byName = existing
            .GroupBy(a => a.Name.Value)
            .ToDictionary(g => g.Key, g => g.First());

        foreach (Apartment apartment in DemoApartments.Create())
        {
            if (byName.TryAdd(apartment.Name.Value, apartment))
            {
                dbContext.Apartments.Add(apartment);
            }
        }

        return byName;
    }

    private User CreateUser(string firstName, string lastName, string email, string password = DemoPassword) =>
        User.Create(
            FirstName.Create(firstName).Value,
            LastName.Create(lastName).Value,
            Email.Create(email).Value,
            passwordHasher.HashPassword(Password.Create(password).Value));

    private Booking Reserve(Apartment apartment, User guest, DateOnly start, int nights, DateTime utcNow)
    {
        DateRange duration = DateRange.Create(start, start.AddDays(nights)).Value;

        var booking = Booking.Reserve(apartment, guest.Id, duration, utcNow, pricingService);

        dbContext.Bookings.Add(booking);

        return booking;
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Seeded demo data: {ApartmentCount} apartments, demo user {DemoEmail}")]
    private static partial void LogSeeded(ILogger logger, int apartmentCount, string demoEmail);
}
