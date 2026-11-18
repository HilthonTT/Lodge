using Lodge.Application.Abstractions.Cryptography;
using Lodge.Application.Payments;
using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Payouts;
using Lodge.Domain.Reviews;
using Lodge.Domain.Users;
using Lodge.Domain.Wishlists;
using Lodge.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lodge.Api.Seeding;

/// <summary>
/// Seeds realistic demo data for local development and screenshots. Idempotent: when the demo user already exists,
/// only missing pieces of newer releases are added. Booking dates are relative to "today", so the calendar always
/// looks alive.
/// </summary>
/// <remarks>
/// Demo login: <see cref="DemoEmail"/> / <see cref="DemoPassword"/>. The other guests use the same password.
/// Host login: <see cref="HostEmail"/> / <see cref="HostPassword"/> (a Superhost with most listings).
/// Admin login: <see cref="AdminEmail"/> / <see cref="AdminPassword"/>.
/// Every seeded account has a verified e-mail address and a short bio, and none has two-factor authentication.
/// Payments are simulated: confirmed bookings have a simulated payment and a payout; Sam has an active simulated payout
/// account (past payouts paid), Priya has none (her due payouts are on hold). Seeded reviews are published, with
/// category ratings, two host replies and two host-to-guest reviews; the demo user has a cancelled, refunded booking
/// and a finished stay that can still be reviewed. Experiences, conversations and notifications are seeded too (see
/// <c>DemoDataSeeder.Engagement.cs</c>).
/// Images are stored as relative paths (<c>/images/...</c>, served from <c>wwwroot</c>) and resolved to absolute
/// URLs with <c>Assets:PublicBaseUrl</c> when returned by the API.
/// </remarks>
internal sealed partial class DemoDataSeeder(
    LodgeDbContext dbContext,
    IPasswordHasher passwordHasher,
    BookingPricing bookingPricing,
    IOptions<PaymentsOptions> paymentsOptions,
    IDateTimeProvider dateTimeProvider,
    ILogger<DemoDataSeeder> logger)
{
    public const string DemoEmail = "demo@lodge.dev";
    public const string DemoPassword = "Demo123!";
    public const string HostEmail = "host@lodge.dev";
    public const string HostPassword = "Host123!";
    public const string AdminEmail = "admin@lodge.dev";
    public const string AdminPassword = "Admin123!";
    public const string PriyaEmail = "priya.patel@lodge.dev";
    public const string JamieEmail = "jamie.lee@lodge.dev";
    public const string LucasEmail = "lucas.martin@lodge.dev";
    public const string DreamTripsWishlist = "Dream trips";

    /// <summary>
    /// The demo user's wishlist "Dream trips".
    /// </summary>
    internal static readonly string[] DreamTrips =
    [
        DemoApartments.AuroraGlassIgloo,
        DemoApartments.HillsideLuxuryVilla,
        DemoApartments.CanalHouseboat,
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedAdminAsync(cancellationToken);

        if (await dbContext.Users.AnyAsync(u => u.Email.Value == DemoEmail, cancellationToken))
        {
            await UpgradeAsync(cancellationToken);
            return;
        }

        DateTime utcNow = dateTimeProvider.UtcNow;
        DateOnly today = DateOnly.FromDateTime(utcNow);

        User demo = CreateUser("Alex", "Morgan", DemoEmail, Bios.Demo);
        User jamie = CreateUser("Jamie", "Lee", JamieEmail, Bios.Jamie);
        User priya = CreateUser("Priya", "Patel", PriyaEmail, Bios.Priya);
        User lucas = CreateUser("Lucas", "Martin", LucasEmail, Bios.Lucas);
        User sam = CreateHost();
        dbContext.Users.AddRange(demo, jamie, priya, lucas, sam);

        Dictionary<string, Apartment> apartments = await SeedApartmentsAsync(sam, priya, cancellationToken);

        // Sam gets paid through a (simulated) payout account; Priya has not connected one, so her payouts are on hold.
        PayoutAccount samPayoutAccount = CreateSimulatedPayoutAccount(sam);

        // Past, completed stays with reviews (the request-to-book ones were accepted by the host back then).
        (string Apartment, User Guest, int StartOffset, int Nights, int Rating, string Comment)[] pastStays =
        [
            (DemoApartments.PineRidgeCabin, demo, -64, 5, 5, "Waking up to the mountains with a coffee on the porch was magical. Spotless and cosy."),
            (DemoApartments.CanalHouseboat, demo, -31, 3, 4, "A unique way to see Amsterdam. Compact, but very well thought out."),
            (DemoApartments.PineRidgeCabin, jamie, -40, 4, 5, "The wood stove and the hot tub made our winter getaway perfect."),
            (DemoApartments.HillsideLuxuryVilla, jamie, -90, 6, 5, "Unbelievable ocean views and a pool we never wanted to leave."),
            (DemoApartments.AuroraGlassIgloo, priya, -120, 2, 5, "We saw the northern lights from bed. Once-in-a-lifetime experience!"),
            (DemoApartments.LavenderHillCottage, priya, -75, 4, 4, "Charming village, lovely garden. The kitchen is a bit small."),
            (DemoApartments.RedwoodTreehouse, priya, -20, 3, 5, "Fell asleep listening to the forest. The kids still talk about it."),
            (DemoApartments.ChateauDesVignes, lucas, -150, 5, 5, "Perfect for our family reunion: wine tasting, long dinners and space for everyone."),
            (DemoApartments.MinimalistLakeHouse, lucas, -45, 4, 4, "Beautiful design and a stunning lake. Bring warm clothes in the evening."),
            (DemoApartments.SeasideFamilyRetreat, lucas, -100, 7, 5, "Walking distance to the beach, great for kids, host was very responsive."),
            (DemoApartments.MapleGroveFamilyHome, jamie, -12, 3, 5, "Plenty of room for the whole family and a big backyard. Sam thought of everything."),
            (DemoApartments.CanalHouseboat, lucas, -200, 2, 3, "Great location, but the bedroom gets warm in summer."),
        ];

        Dictionary<(string Apartment, User Guest), (Booking Booking, Review Review)> pastBookings = [];

        for (int index = 0; index < pastStays.Length; index++)
        {
            (string apartmentName, User guest, int startOffset, int nights, int rating, string comment) = pastStays[index];
            Apartment apartment = apartments[apartmentName];

            Booking booking = Book(apartment, guest, today.AddDays(startOffset), nights, utcNow);
            Pay(booking, apartment, samPayoutAccount, sam, today, utcNow);
            booking.Complete(utcNow);

            Review review = Review.CreatePublished(
                booking,
                Rating.Create(rating).Value,
                SeedCategories(rating, index),
                Comment.Create(comment).Value,
                booking.Duration.End.AddDays(2).ToDateTime(new TimeOnly(18, 0), DateTimeKind.Utc));
            dbContext.Reviews.Add(review);

            pastBookings[(apartmentName, guest)] = (booking, review);
        }

        // Sam answers a couple of reviews and reviews two of his guests.
        pastBookings[(DemoApartments.PineRidgeCabin, jamie)].Review.Reply(
            "Thank you Jamie! The hot tub is waiting for your next winter getaway.",
            utcNow);
        pastBookings[(DemoApartments.ChateauDesVignes, lucas)].Review.Reply(
            "Merci Lucas, it was a pleasure to host your family. Come back for the harvest!",
            utcNow);

        dbContext.GuestReviews.Add(GuestReview.CreatePublished(
            pastBookings[(DemoApartments.MapleGroveFamilyHome, jamie)].Booking,
            sam.Id,
            Rating.Create(5).Value,
            Comment.Create("Jamie and family left the house spotless and were a joy to host. Welcome back anytime!").Value,
            utcNow));
        dbContext.GuestReviews.Add(GuestReview.CreatePublished(
            pastBookings[(DemoApartments.ChateauDesVignes, lucas)].Booking,
            sam.Id,
            Rating.Create(5).Value,
            Comment.Create("Lucas communicated clearly and treated the château with great care.").Value,
            utcNow));

        // A finished stay the demo user has not reviewed yet (the review window is still open).
        Booking unreviewed = Book(apartments[DemoApartments.RedwoodTreehouse], demo, today.AddDays(-9), 2, utcNow);
        Pay(unreviewed, apartments[DemoApartments.RedwoodTreehouse], samPayoutAccount, sam, today, utcNow);
        unreviewed.Complete(utcNow);

        // The demo user's upcoming bookings, one per status shown in the UI.
        Pay(Book(apartments[DemoApartments.HillsideLuxuryVilla], demo, today.AddDays(14), 5, utcNow),
            apartments[DemoApartments.HillsideLuxuryVilla], samPayoutAccount, sam, today, utcNow);
        Book(apartments[DemoApartments.MapleGroveFamilyHome], demo, today.AddDays(30), 3, utcNow);
        Request(apartments[DemoApartments.AuroraGlassIgloo], demo, today.AddDays(30), 3, utcNow,
            "Hi Sam! We are celebrating our anniversary and would love to see the northern lights.");
        Request(apartments[DemoApartments.LavenderHillCottage], demo, today.AddDays(7), 3, utcNow).Cancel(utcNow);
        Request(apartments[DemoApartments.ChateauDesVignes], demo, today.AddDays(45), 4, utcNow).Decline(utcNow);

        // A paid stay the demo user cancelled 10 days before check-in (Strict policy: 50% back, already refunded).
        SeedRefundedCancellation(apartments[DemoApartments.AuroraGlassIgloo], demo, samPayoutAccount, sam, today, utcNow);

        // Other guests' upcoming stays and requests, so calendars and the host dashboard are busy.
        Pay(Book(apartments[DemoApartments.PineRidgeCabin], jamie, today.AddDays(3), 4, utcNow),
            apartments[DemoApartments.PineRidgeCabin], samPayoutAccount, sam, today, utcNow);
        Pay(Book(apartments[DemoApartments.CanalHouseboat], lucas, today.AddDays(10), 5, utcNow),
            apartments[DemoApartments.CanalHouseboat], samPayoutAccount, sam, today, utcNow);
        Book(apartments[DemoApartments.SeasideFamilyRetreat], lucas, today.AddDays(21), 7, utcNow);
        Request(apartments[DemoApartments.ChateauDesVignes], jamie, today.AddDays(60), 4, utcNow,
            "We would like to celebrate my parents' 40th wedding anniversary with the family.");

        SeedCalendar(apartments, today);

        SeedWishlist(demo, apartments, utcNow);

        // Experiences, conversations and notifications.
        await SeedEngagementAsync(demo, jamie, priya, lucas, sam, apartments, cancellationToken);

        // Demo data must not trigger welcome/booking e-mails through the outbox.
        foreach (Entity entity in dbContext.ChangeTracker.Entries<Entity>().Select(e => e.Entity))
        {
            entity.ClearDomainEvents();
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        LogSeeded(logger, apartments.Count, DemoEmail);
    }

    /// <summary>
    /// Brings a database seeded by an older version up to date: bios and verified e-mail addresses, the host account,
    /// the listing details (owners, coordinates, house rules, …) and the demo wishlist.
    /// </summary>
    private async Task UpgradeAsync(CancellationToken cancellationToken)
    {
        await BackfillProfilesAsync(cancellationToken);

        User? sam = await dbContext.Users.FirstOrDefaultAsync(u => u.Email.Value == HostEmail, cancellationToken);

        if (sam is null)
        {
            sam = CreateHost();
            dbContext.Users.Add(sam);
        }

        User priya = await dbContext.Users.FirstAsync(u => u.Email.Value == PriyaEmail, cancellationToken);
        User demo = await dbContext.Users.FirstAsync(u => u.Email.Value == DemoEmail, cancellationToken);

        Dictionary<string, Apartment> apartments = await SeedApartmentsAsync(sam, priya, cancellationToken);

        if (!await dbContext.Wishlists.AnyAsync(w => w.UserId == demo.Id, cancellationToken))
        {
            SeedWishlist(demo, apartments, dateTimeProvider.UtcNow);
        }

        if (!await dbContext.PayoutAccounts.AnyAsync(a => a.UserId == sam.Id, cancellationToken))
        {
            CreateSimulatedPayoutAccount(sam);
        }

        User jamie = await dbContext.Users.FirstAsync(u => u.Email.Value == JamieEmail, cancellationToken);
        User lucas = await dbContext.Users.FirstAsync(u => u.Email.Value == LucasEmail, cancellationToken);

        await SeedEngagementAsync(demo, jamie, priya, lucas, sam, apartments, cancellationToken);

        foreach (Entity entity in dbContext.ChangeTracker.Entries<Entity>().Select(e => e.Entity))
        {
            entity.ClearDomainEvents();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Gives demo accounts seeded by an older version their bio and a verified e-mail address.
    /// </summary>
    private async Task BackfillProfilesAsync(CancellationToken cancellationToken)
    {
        Dictionary<string, string> bios = new(StringComparer.Ordinal)
        {
            [DemoEmail] = Bios.Demo,
            ["jamie.lee@lodge.dev"] = Bios.Jamie,
            [PriyaEmail] = Bios.Priya,
            ["lucas.martin@lodge.dev"] = Bios.Lucas,
            [AdminEmail] = Bios.Admin,
            [HostEmail] = Bios.Sam,
        };

        string[] emails = [.. bios.Keys];

        List<User> users = await dbContext.Users
            .Where(u => emails.Contains(u.Email.Value) && (u.Bio == null || u.EmailVerifiedOnUtc == null))
            .ToListAsync(cancellationToken);

        foreach (User user in users)
        {
            user.VerifyEmail(dateTimeProvider.UtcNow);

            if (user.Bio is null)
            {
                user.ChangeBio(bios[user.Email.Value]);
            }
        }
    }

    private async Task SeedAdminAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.Users.AnyAsync(u => u.Email.Value == AdminEmail, cancellationToken))
        {
            return;
        }

        User admin = CreateUser("Avery", "Admin", AdminEmail, Bios.Admin, AdminPassword);
        admin.ChangeRole(UserRole.Admin);
        admin.ClearDomainEvents();

        dbContext.Users.Add(admin);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Adds the missing demo listings. Existing listings with a demo name that are not hosted by Sam or Priya yet
    /// (i.e. seeded by an older version) get the current demo details and their host.
    /// </summary>
    private async Task<Dictionary<string, Apartment>> SeedApartmentsAsync(
        User sam,
        User priya,
        CancellationToken cancellationToken)
    {
        Dictionary<string, Apartment> byName = (await dbContext.Apartments.ToListAsync(cancellationToken))
            .GroupBy(a => a.Name.Value)
            .ToDictionary(g => g.Key, g => g.First());

        foreach (DemoListing listing in DemoApartments.All)
        {
            User host = listing.Host == DemoHost.Sam ? sam : priya;

            if (byName.TryGetValue(listing.Name, out Apartment? existing))
            {
                if (!existing.IsOwnedBy(sam.Id) && !existing.IsOwnedBy(priya.Id))
                {
                    existing.Update(listing.ToDetails());
                    existing.ChangeOwner(host.Id);
                }

                continue;
            }

            Apartment apartment = Apartment.Create(
                host.Id,
                listing.ToDetails(),
                [ApartmentImage.FromPath(listing.ImagePath)]).Value;

            dbContext.Apartments.Add(apartment);
            byName.Add(listing.Name, apartment);
        }

        return byName;
    }

    private PayoutAccount CreateSimulatedPayoutAccount(User host)
    {
        PayoutAccount account = PayoutAccount.Create(host.Id, $"sim_acct_seed_{host.Id:N}");
        account.Update(payoutsEnabled: true);

        dbContext.PayoutAccounts.Add(account);

        return account;
    }

    /// <summary>
    /// Records a simulated payment for a reserved booking (confirming it) and schedules the host payout: payouts that
    /// are due (check-in + 1 day has passed) are paid when the host has an active payout account, else on hold.
    /// </summary>
    private Payout Pay(
        Booking booking,
        Apartment apartment,
        PayoutAccount samPayoutAccount,
        User sam,
        DateOnly today,
        DateTime utcNow)
    {
        DateTime paidOnUtc = booking.Duration.Start.AddDays(-7).ToDateTime(new TimeOnly(10, 0), DateTimeKind.Utc);

        booking.ConfirmPayment($"sim_pi_seed_{booking.Id:N}", booking.TotalPrice.Amount, paidOnUtc < utcNow ? paidOnUtc : utcNow);

        var payout = Payout.Schedule(booking, apartment.OwnerId, bookingPricing.HostServiceFeePercent);

        if (payout.ScheduledFor <= today)
        {
            if (apartment.IsOwnedBy(sam.Id) && samPayoutAccount.CanReceivePayouts)
            {
                payout.MarkPaid($"sim_tr_seed_{payout.Id:N}", payout.ScheduledFor.ToDateTime(new TimeOnly(5, 0), DateTimeKind.Utc));
            }
            else
            {
                payout.PutOnHold();
            }
        }

        dbContext.Payouts.Add(payout);

        return payout;
    }

    /// <summary>
    /// A paid booking that the guest cancelled today, 10 days before check-in, with its refund already sent.
    /// </summary>
    private void SeedRefundedCancellation(
        Apartment apartment,
        User guest,
        PayoutAccount samPayoutAccount,
        User sam,
        DateOnly today,
        DateTime utcNow)
    {
        Booking booking = Book(apartment, guest, today.AddDays(10), 2, utcNow);
        Payout payout = Pay(booking, apartment, samPayoutAccount, sam, today, utcNow);

        RefundQuote quote = booking.Cancel(utcNow).Value;
        payout.AdjustForCancellation(quote.RetainedStayAmount);
        booking.MarkRefundSucceeded($"sim_re_seed_{booking.Id:N}", utcNow);
    }

    /// <summary>
    /// Category ratings around the overall rating, varied a little per review.
    /// </summary>
    private static CategoryRatings SeedCategories(int rating, int index)
    {
        int Vary(int offset) => Math.Clamp(rating - ((index + offset) % 4 == 0 ? 1 : 0), 1, 5);

        return CategoryRatings.Create(
            Vary(0),
            Vary(1),
            rating,
            Vary(2),
            Math.Min(5, rating + 1),
            Vary(3)).Value;
    }

    /// <summary>
    /// Sam blocks a few nights of the cabin for himself and charges more for a few nights at the villa.
    /// </summary>
    private void SeedCalendar(Dictionary<string, Apartment> apartments, DateOnly today)
    {
        Guid cabinId = apartments[DemoApartments.PineRidgeCabin].Id;

        for (int offset = 20; offset <= 23; offset++)
        {
            CalendarDay blocked = CalendarDay.Create(cabinId, today.AddDays(offset));
            blocked.SetBlocked(true);
            dbContext.CalendarDays.Add(blocked);
        }

        Guid villaId = apartments[DemoApartments.HillsideLuxuryVilla].Id;

        for (int offset = 60; offset <= 62; offset++)
        {
            CalendarDay peak = CalendarDay.Create(villaId, today.AddDays(offset));
            peak.SetPrice(850);
            dbContext.CalendarDays.Add(peak);
        }
    }

    private void SeedWishlist(User demo, Dictionary<string, Apartment> apartments, DateTime utcNow)
    {
        Wishlist wishlist = Wishlist.Create(demo.Id, DreamTripsWishlist, utcNow).Value;

        for (int index = 0; index < DreamTrips.Length; index++)
        {
            wishlist.AddItem(apartments[DreamTrips[index]].Id, utcNow.AddMinutes(index));
        }

        dbContext.Wishlists.Add(wishlist);
    }

    private User CreateHost()
    {
        User sam = CreateUser("Sam", "Rivera", HostEmail, Bios.Sam, HostPassword);
        sam.SetSuperhost(true);

        return sam;
    }

    private User CreateUser(string firstName, string lastName, string email, string bio, string password = DemoPassword)
    {
        var user = User.Create(
            FirstName.Create(firstName).Value,
            LastName.Create(lastName).Value,
            Email.Create(email).Value,
            passwordHasher.HashPassword(Password.Create(password).Value));

        user.VerifyEmail(dateTimeProvider.UtcNow);
        user.ChangeBio(bio);

        return user;
    }

    /// <summary>
    /// Books the apartment; requests to book are accepted by the host (before the stay started).
    /// </summary>
    private Booking Book(Apartment apartment, User guest, DateOnly start, int nights, DateTime utcNow)
    {
        Booking booking = Request(apartment, guest, start, nights, utcNow);

        if (booking.Status == BookingStatus.PendingApproval)
        {
            DateTime acceptedOnUtc = start.AddDays(-2).ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc);
            booking.Accept(acceptedOnUtc < utcNow ? acceptedOnUtc : utcNow);
        }

        return booking;
    }

    /// <summary>
    /// Books the apartment: a reservation for instant-book listings, otherwise a request to book.
    /// </summary>
    private Booking Request(
        Apartment apartment,
        User guest,
        DateOnly start,
        int nights,
        DateTime utcNow,
        string? messageToHost = null)
    {
        DateRange duration = DateRange.Create(start, start.AddDays(nights)).Value;

        Booking booking = Booking.Reserve(
            apartment,
            guest.Id,
            duration,
            Math.Min(2, apartment.MaximumGuestCount),
            messageToHost,
            utcNow,
            bookingPricing.Calculate(apartment, duration)).Value;

        dbContext.Bookings.Add(booking);

        return booking;
    }

    internal static class Bios
    {
        public const string Demo =
            "Designer from Seattle who plans every trip around good coffee and a long hike. Always leaves the place tidier than I found it.";

        public const string Jamie =
            "Parent of two and a big fan of family road trips. Happiest with a backyard, a barbecue and a board game.";

        public const string Priya =
            "Travel writer chasing northern lights, treehouses and anything a little unusual. I also host two places I love in Amsterdam and Queenstown.";

        public const string Lucas =
            "Food and wine lover from Lyon. I travel with friends and family and love a big kitchen table.";

        public const string Sam =
            "Hi, I'm Sam! I restore special places, from a log cabin in Aspen to a château in Bordeaux, and love helping guests feel at home. I usually answer within an hour.";

        public const string Admin = "Keeping Lodge running smoothly. Reach out if anything looks off.";
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Seeded demo data: {ApartmentCount} apartments, demo user {DemoEmail}")]
    private static partial void LogSeeded(ILogger logger, int apartmentCount, string demoEmail);
}
