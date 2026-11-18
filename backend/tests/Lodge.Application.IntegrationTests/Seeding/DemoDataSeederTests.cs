using System.Data;
using Lodge.Api.Seeding;
using Lodge.Application;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Hosting.Superhost;
using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Lodge.Domain.Experiences;
using Lodge.Domain.Messaging;
using Lodge.Domain.Notifications;
using Lodge.Domain.Payouts;
using Lodge.Domain.Reviews;
using Lodge.Domain.Users;
using Lodge.Domain.Wishlists;
using Lodge.Infrastructure;
using Lodge.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Lodge.Application.IntegrationTests.Seeding;

/// <summary>
/// Runs the demo seeder against its own, fresh PostgreSQL container.
/// </summary>
public sealed class DemoDataSeederTests : IAsyncLifetime
{
    private static readonly string[] SeededEmails =
    [
        DemoDataSeeder.DemoEmail,
        DemoDataSeeder.AdminEmail,
        DemoDataSeeder.HostEmail,
        "jamie.lee@lodge.dev",
        "priya.patel@lodge.dev",
        "lucas.martin@lodge.dev",
    ];

    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("lodge-seed")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private ServiceProvider _services = null!;

    public async ValueTask InitializeAsync()
    {
        await _dbContainer.StartAsync();

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = _dbContainer.GetConnectionString(),
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddLogging();
        services.AddApplication().AddInfrastructure().AddPersistence();
        services.AddScoped<DemoDataSeeder>();

        _services = services.BuildServiceProvider();

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<LodgeDbContext>().Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }

    [Fact]
    public async Task Seeder_Should_SeedVerifiedAccountsWithBios_AndBeIdempotent()
    {
        await SeedAsync();
        await SeedAsync();

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        LodgeDbContext dbContext = scope.ServiceProvider.GetRequiredService<LodgeDbContext>();
        IPasswordHashChecker passwordHashChecker = scope.ServiceProvider.GetRequiredService<IPasswordHashChecker>();

        List<User> users = await dbContext.Users.AsNoTracking().ToListAsync();

        users.Select(u => u.Email.Value).Should().BeEquivalentTo(SeededEmails);
        users.Should().OnlyContain(u => u.IsEmailVerified && u.HasPassword && !u.TwoFactorEnabled);
        users.Should().OnlyContain(u => !string.IsNullOrWhiteSpace(u.Bio) && u.Bio!.Length <= User.BioMaxLength);
        users.Single(u => u.Email.Value == DemoDataSeeder.AdminEmail).Role.Should().Be(UserRole.Admin);

        User demo = users.Single(u => u.Email.Value == DemoDataSeeder.DemoEmail);
        demo.VerifyPasswordHash(DemoDataSeeder.DemoPassword, passwordHashChecker).Should().BeTrue();

        (await dbContext.ExternalLogins.CountAsync()).Should().Be(0);
        (await dbContext.UserTokens.CountAsync()).Should().Be(0);
        (await dbContext.RecoveryCodes.CountAsync()).Should().Be(0);
        (await dbContext.Reviews.CountAsync(r => r.UserId == demo.Id)).Should().Be(2);
    }

    [Fact]
    public async Task Seeder_Should_BackfillBiosAndVerification_ForAnExistingDemoDatabase()
    {
        await SeedAsync();

        await using (AsyncServiceScope scope = _services.CreateAsyncScope())
        {
            // What a database seeded before this release looks like.
            await scope.ServiceProvider.GetRequiredService<LodgeDbContext>().Database.ExecuteSqlRawAsync(
                "UPDATE users SET bio = NULL, email_verified_on_utc = NULL");
        }

        await SeedAsync();

        await using AsyncServiceScope verifyScope = _services.CreateAsyncScope();
        List<User> users = await verifyScope.ServiceProvider.GetRequiredService<LodgeDbContext>().Users.AsNoTracking().ToListAsync();

        users.Should().HaveCount(SeededEmails.Length);
        users.Should().OnlyContain(u => u.IsEmailVerified && u.Bio != null);
    }

    [Fact]
    public async Task Seeder_Should_SeedHostsListingsCalendarsRequestsAndAWishlist()
    {
        await SeedAsync();
        await SeedAsync();

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        LodgeDbContext dbContext = scope.ServiceProvider.GetRequiredService<LodgeDbContext>();
        IPasswordHashChecker passwordHashChecker = scope.ServiceProvider.GetRequiredService<IPasswordHashChecker>();

        List<User> users = await dbContext.Users.AsNoTracking().ToListAsync();
        User sam = users.Single(u => u.Email.Value == DemoDataSeeder.HostEmail);
        User priya = users.Single(u => u.Email.Value == DemoDataSeeder.PriyaEmail);
        User demo = users.Single(u => u.Email.Value == DemoDataSeeder.DemoEmail);

        sam.FullName.Should().Be("Sam Rivera");
        sam.IsSuperhost.Should().BeTrue();
        sam.IsEmailVerified.Should().BeTrue();
        sam.Bio.Should().NotBeNullOrWhiteSpace();
        sam.VerifyPasswordHash(DemoDataSeeder.HostPassword, passwordHashChecker).Should().BeTrue();
        users.Where(u => u.Id != sam.Id).Should().OnlyContain(u => !u.IsSuperhost);

        List<Apartment> apartments = await dbContext.Apartments.AsNoTracking().ToListAsync();
        apartments.Should().HaveCount(DemoApartments.All.Count);
        apartments.Count(a => a.OwnerId == sam.Id).Should().Be(8);
        apartments.Where(a => a.OwnerId == priya.Id).Select(a => a.Name.Value)
            .Should().BeEquivalentTo(DemoApartments.CanalHouseboat, DemoApartments.MinimalistLakeHouse);

        apartments.Should().OnlyContain(a =>
            a.IsListed &&
            a.Latitude != 0 && a.Longitude != 0 &&
            a.Beds >= a.Bedrooms && a.Bathrooms >= 1 &&
            a.CheckInInstructions != null && a.HouseRules != null &&
            a.Images.Count == 1 && a.Images.Single().Path!.StartsWith("/images/", StringComparison.Ordinal));
        apartments.Should().OnlyContain(a => a.Amenities.All(amenity => Enum.IsDefined(amenity)));
        apartments.Count(a => !a.InstantBook).Should().BeGreaterThanOrEqualTo(3);
        apartments.Select(a => a.PropertyType).Distinct().Should().HaveCountGreaterThanOrEqualTo(6);
        apartments.Count(a => a.MinNights > 1).Should().BeGreaterThanOrEqualTo(3);
        apartments.Count(a => a.WeekendPrice != null).Should().BeGreaterThanOrEqualTo(2);
        apartments.Count(a => a.WeeklyDiscountPercent > 0 || a.MonthlyDiscountPercent > 0).Should().BeGreaterThanOrEqualTo(2);

        Apartment igloo = apartments.Single(a => a.Name.Value == DemoApartments.AuroraGlassIgloo);
        igloo.Latitude.Should().BeApproximately(66.5, 0.1);
        igloo.PropertyType.Should().Be(PropertyType.Igloo);

        // Sam blocked a few nights of the cabin and raised the price of a few nights at the villa.
        Guid cabinId = apartments.Single(a => a.Name.Value == DemoApartments.PineRidgeCabin).Id;
        (await dbContext.CalendarDays.CountAsync(c => c.ApartmentId == cabinId && c.IsBlocked)).Should().Be(4);
        (await dbContext.CalendarDays.CountAsync(c => c.Price != null)).Should().Be(3);

        // Every booking follows its listing's rules; two requests wait for Sam's answer.
        List<Booking> bookings = await dbContext.Bookings.AsNoTracking().ToListAsync();
        bookings.Should().OnlyContain(b => b.GuestCount >= 1);
        bookings.Where(b => b.Status == BookingStatus.PendingApproval).Should().HaveCount(2)
            .And.OnlyContain(b => b.MessageToHost != null);
        bookings.Select(b => b.Status).Distinct().Should().Contain(
        [
            BookingStatus.Reserved,
            BookingStatus.Confirmed,
            BookingStatus.Rejected,
            BookingStatus.Cancelled,
            BookingStatus.Completed,
            BookingStatus.PendingApproval,
        ]);
        bookings.Should().NotContain(b => apartments.Single(a => a.Id == b.ApartmentId).OwnerId == b.UserId);

        // Sam qualifies as a Superhost, Priya does not.
        Dictionary<Guid, HostPerformance> performances;
        using (IDbConnection connection = await scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>().GetOpenConnectionAsync())
        {
            performances = await HostPerformanceQueries.GetAsync(connection);
        }

        performances[sam.Id].Meets(SuperhostRequirements.Default).Should().BeTrue();
        performances[sam.Id].CompletedStays.Should().Be(10);
        performances[priya.Id].Meets(SuperhostRequirements.Default).Should().BeFalse();

        Wishlist wishlist = await dbContext.Wishlists.AsNoTracking().SingleAsync(w => w.UserId == demo.Id);
        wishlist.Name.Should().Be(DemoDataSeeder.DreamTripsWishlist);
        wishlist.Items.Select(i => i.ApartmentId).Should().BeEquivalentTo(
            apartments.Where(a => DemoDataSeeder.DreamTrips.Contains(a.Name.Value)).Select(a => a.Id));
    }

    [Fact]
    public async Task Seeder_Should_SeedPoliciesPaymentsPayoutsAndReviews()
    {
        await SeedAsync();
        await SeedAsync();

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        LodgeDbContext dbContext = scope.ServiceProvider.GetRequiredService<LodgeDbContext>();
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);

        List<User> users = await dbContext.Users.AsNoTracking().ToListAsync();
        User sam = users.Single(u => u.Email.Value == DemoDataSeeder.HostEmail);
        User priya = users.Single(u => u.Email.Value == DemoDataSeeder.PriyaEmail);
        User demo = users.Single(u => u.Email.Value == DemoDataSeeder.DemoEmail);

        // Varied cancellation policies, snapshotted on the bookings.
        List<Apartment> apartments = await dbContext.Apartments.AsNoTracking().ToListAsync();
        apartments.Select(a => a.CancellationPolicy).Distinct().Should().BeEquivalentTo(
            [CancellationPolicy.Flexible, CancellationPolicy.Moderate, CancellationPolicy.Strict]);

        List<Booking> bookings = await dbContext.Bookings.AsNoTracking().ToListAsync();
        bookings.Should().OnlyContain(b =>
            b.CancellationPolicy == apartments.Single(a => a.Id == b.ApartmentId).CancellationPolicy);
        bookings.Should().OnlyContain(b => b.ServiceFee.Amount > 0 && b.TotalPrice.Amount > b.ServiceFee.Amount);
        bookings.Should().Contain(b => b.Taxes.Amount > 0);

        // Paid bookings have a simulated payment.
        bookings.Where(b => b.Status is BookingStatus.Confirmed or BookingStatus.Completed)
            .Should().OnlyContain(b => b.PaymentIntentId!.StartsWith("sim_", StringComparison.Ordinal) &&
                                       b.AmountPaid == b.TotalPrice.Amount && b.PaidOnUtc != null);

        // The demo user cancelled a paid stay and got 50% of the nights back (Strict, 10 days before check-in).
        Booking refunded = bookings.Single(b => b.UserId == demo.Id && b.RefundStatus == RefundStatus.Succeeded);
        refunded.Status.Should().Be(BookingStatus.Cancelled);
        decimal refundableStay = refunded.PriceForPeriod.Amount - refunded.Discount.Amount + refunded.AmenitiesUpCharge.Amount;
        refunded.RefundAmount.Should().Be(
            PricingService.Round(refundableStay / 2) +
            PricingService.Round(refunded.Taxes.Amount / 2) +
            refunded.CleaningFee.Amount);
        refunded.RefundId.Should().StartWith("sim_");

        // A finished stay the demo user can still review.
        bookings.Should().Contain(b =>
            b.UserId == demo.Id && b.Status == BookingStatus.Completed && b.ReviewDeadline >= today &&
            !dbContext.Reviews.Any(r => r.BookingId == b.Id));

        // Every seeded review is published, with category ratings; Sam replied to two of them and reviewed two guests.
        List<Review> reviews = await dbContext.Reviews.AsNoTracking().ToListAsync();
        reviews.Should().HaveCount(12).And.OnlyContain(r =>
            r.IsPublished &&
            new[] { r.Categories.Cleanliness, r.Categories.Accuracy, r.Categories.CheckIn, r.Categories.Communication,
                r.Categories.Location, r.Categories.Value }.All(rating => rating >= 1 && rating <= 5));
        reviews.Count(r => r.HostReply != null).Should().Be(2);

        List<GuestReview> guestReviews = await dbContext.GuestReviews.AsNoTracking().ToListAsync();
        guestReviews.Should().HaveCount(2).And.OnlyContain(r => r.IsPublished && r.HostId == sam.Id);

        // Sam has an active simulated payout account and a payout history; Priya has none, so hers are on hold.
        List<PayoutAccount> accounts = await dbContext.PayoutAccounts.AsNoTracking().ToListAsync();
        accounts.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            UserId = sam.Id,
            Status = PayoutAccountStatus.Active,
            PayoutsEnabled = true,
        });

        List<Payout> payouts = await dbContext.Payouts.AsNoTracking().ToListAsync();
        payouts.Where(p => p.BookingId != null).Should().HaveCount(bookings.Count(b => b.PaymentIntentId != null));
        payouts.Where(p => p.ExperienceBookingId != null).Should().HaveCount(
            await dbContext.ExperienceBookings.CountAsync(b => b.PaymentIntentId != null));
        payouts.Where(p => p.HostId == sam.Id && p.ScheduledFor <= today)
            .Should().NotBeEmpty().And.OnlyContain(p => p.Status == PayoutStatus.Paid && p.PaidOnUtc != null);
        payouts.Where(p => p.HostId == sam.Id && p.ScheduledFor > today)
            .Should().NotBeEmpty().And.OnlyContain(p => p.Status == PayoutStatus.Scheduled);
        payouts.Where(p => p.HostId == priya.Id && p.ScheduledFor <= today)
            .Should().NotBeEmpty().And.OnlyContain(p => p.Status == PayoutStatus.OnHold);
        payouts.Single(p => p.BookingId == refunded.Id).Amount.Amount.Should().BeLessThan(
            refunded.PriceForPeriod.Amount - refunded.Discount.Amount + refunded.AmenitiesUpCharge.Amount);
    }

    [Fact]
    public async Task Seeder_Should_SeedExperiencesConversationsAndNotifications()
    {
        await SeedAsync();
        await SeedAsync();

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        LodgeDbContext dbContext = scope.ServiceProvider.GetRequiredService<LodgeDbContext>();
        DateTime utcNow = DateTime.UtcNow;

        List<User> users = await dbContext.Users.AsNoTracking().ToListAsync();
        User sam = users.Single(u => u.Email.Value == DemoDataSeeder.HostEmail);
        User priya = users.Single(u => u.Email.Value == DemoDataSeeder.PriyaEmail);
        User demo = users.Single(u => u.Email.Value == DemoDataSeeder.DemoEmail);
        User jamie = users.Single(u => u.Email.Value == DemoDataSeeder.JamieEmail);

        // Six listed experiences by Sam and Priya, without photos, with sessions over the next 30 days.
        List<Experience> experiences = await dbContext.Experiences.AsNoTracking().ToListAsync();
        experiences.Should().HaveCount(6).And.OnlyContain(e =>
            e.IsListed && e.ImageIds.Count == 0 && (e.HostId == sam.Id || e.HostId == priya.Id) &&
            e.Languages.Count > 0 && e.Included.Count > 0);
        experiences.Select(e => e.City).Should().BeEquivalentTo(
            ["Rovaniemi", "Amsterdam", "Bordeaux", "Lisbon", "Queenstown", "Aspen"]);

        List<ExperienceSession> sessions = await dbContext.ExperienceSessions.AsNoTracking().ToListAsync();
        foreach (Experience experience in experiences)
        {
            sessions.Count(s => s.ExperienceId == experience.Id && s.StartsAtUtc > utcNow && s.StartsAtUtc <= utcNow.AddDays(31))
                .Should().BeGreaterThanOrEqualTo(9);
        }

        sessions.Should().OnlyContain(s => s.BookedGuests <= s.Capacity);

        // A few reviews of completed bookings, and the demo user's confirmed, paid booking with a scheduled payout.
        (await dbContext.ExperienceReviews.CountAsync()).Should().BeGreaterThanOrEqualTo(3);
        List<ExperienceBooking> experienceBookings = await dbContext.ExperienceBookings.AsNoTracking().ToListAsync();
        ExperienceBooking demoBooking = experienceBookings.Single(b =>
            b.GuestId == demo.Id && b.Status == ExperienceBookingStatus.Confirmed);
        demoBooking.PaymentIntentId.Should().StartWith("sim_");
        demoBooking.AmountPaid.Should().Be(demoBooking.TotalPrice);
        demoBooking.SessionStartsAtUtc.Should().BeAfter(utcNow);
        (await dbContext.Payouts.SingleAsync(p => p.ExperienceBookingId == demoBooking.Id)).Status
            .Should().Be(PayoutStatus.Scheduled);
        experienceBookings.Where(b => b.Status == ExperienceBookingStatus.Completed).Should().NotBeEmpty();

        // Conversations: three between the demo user and Sam (one with unread messages), Jamie's request to Sam.
        List<Conversation> conversations = await dbContext.Conversations.AsNoTracking().ToListAsync();
        List<Message> messages = await dbContext.Messages.AsNoTracking().ToListAsync();

        List<Conversation> demoWithSam = [.. conversations.Where(c => c.GuestId == demo.Id && c.HostId == sam.Id)];
        demoWithSam.Should().HaveCount(3);
        demoWithSam.Should().Contain(c => messages.Any(m =>
            m.ConversationId == c.Id && m.SenderId != demo.Id && m.SentOnUtc > (c.GuestLastReadOnUtc ?? DateTime.MinValue)));
        messages.Should().Contain(m => m.Type == MessageType.System && m.SystemEvent == SystemEvents.BookingConfirmed);

        Conversation jamieRequest = conversations.Single(c => c.GuestId == jamie.Id && c.HostId == sam.Id);
        messages.Where(m => m.ConversationId == jamieRequest.Id).Should().Contain(m =>
            m.Type == MessageType.System && m.SystemEvent == SystemEvents.BookingRequested && m.BookingId != null);
        messages.Where(m => m.ConversationId == jamieRequest.Id).Should().Contain(m => m.SenderId == jamie.Id);
        jamieRequest.HostLastReadOnUtc.Should().BeNull();

        messages.Should().OnlyContain(m =>
            (m.Type == MessageType.System && m.SenderId == null && m.SystemEvent != null) ||
            (m.Type == MessageType.User && m.SenderId != null && m.SystemEvent == null));

        // A handful of notifications for each guest and host account, some unread.
        List<Notification> notifications = await dbContext.Notifications.AsNoTracking().ToListAsync();

        foreach (User user in users.Where(u => u.Email.Value != DemoDataSeeder.AdminEmail))
        {
            notifications.Count(n => n.UserId == user.Id).Should().BeGreaterThanOrEqualTo(1, user.Email.Value);
        }

        notifications.Count(n => n.UserId == demo.Id).Should().BeGreaterThanOrEqualTo(4);
        notifications.Count(n => n.UserId == sam.Id).Should().BeGreaterThanOrEqualTo(4);
        notifications.Should().Contain(n => n.ReadOnUtc == null).And.Contain(n => n.ReadOnUtc != null);
        notifications.Should().OnlyContain(n => n.InApp && NotificationTypes.All.Contains(n.Type) && n.Link.StartsWith('/'));
    }

    [Fact]
    public async Task Migration_Should_KeepTheDataOfAnOlderDatabase_AndTheSeederShouldAdoptItsListings()
    {
        Guid adminId = Guid.NewGuid();
        Guid apartmentId = Guid.NewGuid();
        Guid bookingId = Guid.NewGuid();
        string cabin = DemoApartments.PineRidgeCabin;

        await using (AsyncServiceScope scope = _services.CreateAsyncScope())
        {
            LodgeDbContext dbContext = scope.ServiceProvider.GetRequiredService<LodgeDbContext>();
            IMigrator migrator = dbContext.GetService<IMigrator>();

            // Back to the previous release's schema (this also runs the Down migration).
            await migrator.MigrateAsync("20261118152926_AccountsAndTrust");

            await dbContext.Database.ExecuteSqlAsync(
                $"""
                INSERT INTO users (id, first_name, last_name, email, role, created_on_utc, deleted, password_hash, email_verified_on_utc)
                VALUES ({adminId}, 'Old', 'Admin', 'old-admin@lodge.test', 'Admin', now(), FALSE, 'x', now())
                """);

            await dbContext.Database.ExecuteSqlAsync(
                $"""
                INSERT INTO apartments (id, name, description, address_country, address_state, address_zip_code, address_city,
                    address_street, price_amount, price_currency, cleaning_fee_amount, cleaning_fee_currency,
                    maximum_room_count, maximum_guest_count, image_url, amenities, created_on_utc)
                VALUES ({apartmentId}, {cabin}, 'Old description', 'United States', 'Colorado', '81611',
                    'Aspen', '112 Pine Ridge Rd', 185, 'USD', 60, 'USD', 3, 4, '/images/cabin.png', ARRAY[1, 11, 5, 13], now())
                """);

            await dbContext.Database.ExecuteSqlAsync(
                $"""
                INSERT INTO bookings (id, apartment_id, user_id, duration_start, duration_end, price_for_period_amount,
                    price_for_period_currency, cleaning_fee_amount, cleaning_fee_currency, amenities_up_charge_amount,
                    amenities_up_charge_currency, total_price_amount, total_price_currency, status, created_on_utc)
                VALUES ({bookingId}, {apartmentId}, {adminId}, DATE '2031-01-01', DATE '2031-01-04', 555, 'USD', 60, 'USD', 0,
                    'USD', 615, 'USD', 2, now())
                """);

            await migrator.MigrateAsync();
        }

        await using (AsyncServiceScope scope = _services.CreateAsyncScope())
        {
            LodgeDbContext dbContext = scope.ServiceProvider.GetRequiredService<LodgeDbContext>();

            Apartment migrated = await dbContext.Apartments.AsNoTracking().SingleAsync(a => a.Id == apartmentId);
            migrated.OwnerId.Should().Be(adminId);
            migrated.Bedrooms.Should().Be(3);
            migrated.Beds.Should().Be(3);
            migrated.Amenities.Should().Equal(Amenity.WiFi, Amenity.SwimmingPool);
            migrated.Images.Should().ContainSingle().Which.Path.Should().Be("/images/cabin.png");
            migrated.Should().BeEquivalentTo(new
            {
                IsListed = true,
                InstantBook = true,
                MinNights = 1,
                MaxNights = 365,
                PropertyType = PropertyType.Apartment,
                CheckInTime = new TimeOnly(15, 0),
                CheckOutTime = new TimeOnly(11, 0),
                Deleted = false,
            });

            Booking booking = await dbContext.Bookings.AsNoTracking().SingleAsync(b => b.Id == bookingId);
            booking.GuestCount.Should().Be(1);
            booking.Discount.Amount.Should().Be(0);
            booking.Discount.Currency.Code.Should().Be("USD");
            booking.Status.Should().Be(BookingStatus.Confirmed);
        }

        // The seeder hands the demo listing (still named like one) to Sam and fills in the new details.
        await SeedAsync();

        await using (AsyncServiceScope scope = _services.CreateAsyncScope())
        {
            LodgeDbContext dbContext = scope.ServiceProvider.GetRequiredService<LodgeDbContext>();
            User sam = await dbContext.Users.AsNoTracking().SingleAsync(u => u.Email.Value == DemoDataSeeder.HostEmail);

            Apartment adopted = await dbContext.Apartments.AsNoTracking().SingleAsync(a => a.Id == apartmentId);
            adopted.OwnerId.Should().Be(sam.Id);
            adopted.Latitude.Should().BeApproximately(39.19, 0.01);
            adopted.PropertyType.Should().Be(PropertyType.Cabin);
            adopted.Images.Should().ContainSingle();
            (await dbContext.Apartments.CountAsync()).Should().Be(DemoApartments.All.Count);
        }
    }

    private async Task SeedAsync()
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedAsync();
    }
}
