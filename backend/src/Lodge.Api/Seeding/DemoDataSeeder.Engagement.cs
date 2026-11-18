using System.Text.Json;
using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Lodge.Domain.Experiences;
using Lodge.Domain.Messaging;
using Lodge.Domain.Notifications;
using Lodge.Domain.Payouts;
using Lodge.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Lodge.Api.Seeding;

/// <summary>
/// Demo experiences, conversations and notifications (phase 4).
/// </summary>
/// <remarks>
/// Six listed experiences hosted by Sam and Priya with sessions over the next 30 days, a few past sessions with
/// completed, reviewed bookings, and one confirmed, paid experience booking for the demo user. Three conversations
/// between the demo user and Sam (a booking thread with an unread reply, the pending igloo request, an inquiry) and
/// Jamie's pending request to Sam (unread by Sam), with system messages. A handful of notifications per guest and host
/// account, some unread. Each part is only added when missing, so older demo databases are upgraded too.
/// </remarks>
internal sealed partial class DemoDataSeeder
{
    public const string AuroraHunt = "Northern Lights Hunt by Snowmobile";
    public const string CanalFoodTour = "Amsterdam Canal Food Tour";
    public const string WineTasting = "Bordeaux Grand Cru Wine Tasting";
    public const string SurfLesson = "Learn to Surf in Lisbon";
    public const string QueenstownHike = "Ben Lomond Summit Hike";
    public const string SkiClinic = "Aspen Ski Clinic with a Pro";

    private static readonly JsonSerializerOptions NotificationDataOptions = new(JsonSerializerDefaults.Web);

    private sealed record DemoExperience(string Title, DemoHost Host, ExperienceDetails Details, int StartHourUtc);

    private static IReadOnlyList<DemoExperience> DemoExperiences =>
    [
        new(AuroraHunt, DemoHost.Sam, Details(
            AuroraHunt,
            "Ride a snowmobile deep into the Lapland wilderness, away from the city lights, and chase the aurora borealis with a local guide. We stop at a wilderness hut for a campfire, hot berry juice and stories about the northern lights.",
            ExperienceCategory.Outdoors, "Rovaniemi", "Finland", 66.5039, 25.7294,
            "Lobby of the Arctic Circle Snow Hotel, Arctic Circle Way 7", 240, 145, "EUR", 8,
            ["en", "fi"], ["Thermal overalls, boots and gloves", "Campfire snacks and hot berry juice", "Hotel pick-up"]), 18),
        new(CanalFoodTour, DemoHost.Priya, Details(
            CanalFoodTour,
            "Taste your way through the Jordaan along the canals: warm stroopwafels, aged Gouda, raw herring for the brave, bitterballen in a brown café and apple pie at the city's most famous bakery.",
            ExperienceCategory.Food, "Amsterdam", "Netherlands", 52.3752, 4.8840,
            "In front of the Westerkerk, Prinsengracht 279", 180, 79, "EUR", 10,
            ["en", "nl"], ["Eight tastings", "Two drinks", "A map of our favourite spots"]), 11),
        new(WineTasting, DemoHost.Sam, Details(
            WineTasting,
            "Visit two family-run châteaux of the Médoc, walk the vineyards with the winemakers and taste six grands crus paired with local cheeses. Finish with a blind tasting challenge.",
            ExperienceCategory.Food, "Bordeaux", "France", 44.8378, -0.5792,
            "Place de la Bourse, by the Miroir d'eau", 150, 95, "EUR", 12,
            ["en", "fr"], ["Six wine tastings", "Cheese pairing", "Transport between the châteaux"]), 13),
        new(SurfLesson, DemoHost.Sam, Details(
            SurfLesson,
            "Catch your first wave on Carcavelos beach. A certified instructor teaches you to read the ocean, paddle and pop up, in small groups so that everyone gets attention.",
            ExperienceCategory.Sports, "Lisbon", "Portugal", 38.6779, -9.3355,
            "Carcavelos beach, next to the lifeguard tower", 120, 60, "EUR", 8,
            ["en", "pt"], ["Surfboard and wetsuit", "Beach safety briefing", "Photos of your session"]), 10),
        new(QueenstownHike, DemoHost.Priya, Details(
            QueenstownHike,
            "Climb from the gondola to the summit of Ben Lomond for a 360° view over Lake Wakatipu and the Remarkables. A steady pace, plenty of breaks and a picnic lunch at the saddle.",
            ExperienceCategory.Outdoors, "Queenstown", "New Zealand", -45.0270, 168.6591,
            "Skyline Gondola base station, Brecon Street", 360, 120, "USD", 8,
            ["en"], ["Picnic lunch", "Hiking poles", "Gondola ticket down"]), 21),
        new(SkiClinic, DemoHost.Sam, Details(
            SkiClinic,
            "Spend a morning on Aspen Mountain with a former racer: carve cleaner turns, ski steeper runs with confidence and get personal video feedback over hot chocolate.",
            ExperienceCategory.Sports, "Aspen", "United States", 39.1911, -106.8175,
            "Silver Queen Gondola, 601 E Dean St", 180, 180, "USD", 6,
            ["en", "es"], ["Video analysis", "Hot chocolate", "Small group of at most six"]), 16),
    ];

    /// <summary>
    /// Adds the missing phase-4 demo data (experiences, conversations, notifications). Changes are saved by the caller.
    /// </summary>
    private async Task SeedEngagementAsync(
        User demo,
        User jamie,
        User priya,
        User lucas,
        User sam,
        Dictionary<string, Apartment> apartments,
        CancellationToken cancellationToken)
    {
        DateTime utcNow = dateTimeProvider.UtcNow;

        Dictionary<string, Experience> experiences = [];

        if (!await dbContext.Experiences.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            experiences = SeedExperiences(demo, jamie, priya, lucas, sam, utcNow);
        }

        if (!await dbContext.Conversations.AnyAsync(cancellationToken))
        {
            await SeedConversationsAsync(demo, jamie, sam, apartments, utcNow, cancellationToken);
        }

        if (!await dbContext.Notifications.AnyAsync(cancellationToken))
        {
            await SeedNotificationsAsync(demo, jamie, priya, lucas, sam, apartments, experiences, utcNow, cancellationToken);
        }
    }

    private Dictionary<string, Experience> SeedExperiences(
        User demo,
        User jamie,
        User priya,
        User lucas,
        User sam,
        DateTime utcNow)
    {
        DateOnly today = DateOnly.FromDateTime(utcNow);
        Dictionary<string, Experience> experiences = [];
        IReadOnlyList<DemoExperience> demoExperiences = DemoExperiences;

        for (int index = 0; index < demoExperiences.Count; index++)
        {
            DemoExperience demoExperience = demoExperiences[index];
            User host = demoExperience.Host == DemoHost.Sam ? sam : priya;

            // No photos are bundled for experiences: the frontend shows category placeholders.
            Experience experience = Experience.Create(host.Id, demoExperience.Details, []).Value;
            experience.CreatedOnUtc = utcNow.AddDays(-60 + index);
            dbContext.Experiences.Add(experience);
            experiences[demoExperience.Title] = experience;

            // A session every third day over the next 30 days.
            for (int day = 1 + (index % 3); day <= 30; day += 3)
            {
                DateTime startsAt = today.AddDays(day).ToDateTime(new TimeOnly(demoExperience.StartHourUtc, 0), DateTimeKind.Utc);
                dbContext.ExperienceSessions.Add(
                    ExperienceSession.Create(experience, startsAt, experience.MaxGuests, utcNow).Value);
            }
        }

        // Past sessions with completed (paid) bookings, most of them reviewed.
        (string Title, User Guest, int DaysAgo, int Guests, int? Rating, string? Comment)[] pastBookings =
        [
            (AuroraHunt, jamie, 12, 2, 5, "We saw the aurora dance for almost an hour. Sam's stories at the campfire were the cherry on top."),
            (AuroraHunt, lucas, 26, 3, 5, "Cold, magical and perfectly organised. The thermal suits really work!"),
            (CanalFoodTour, lucas, 9, 2, 4, "Delicious stops and Priya knows every baker in the Jordaan. Bring an appetite."),
            (WineTasting, priya, 15, 2, 5, "Two wonderful châteaux and a blind tasting I will never forget."),
            (SurfLesson, demo, 20, 1, null, null),
        ];

        foreach ((string title, User guest, int daysAgo, int guests, int? rating, string? comment) in pastBookings)
        {
            Experience experience = experiences[title];
            DemoExperience demoExperience = demoExperiences.Single(e => e.Title == title);
            DateTime startsAt = today.AddDays(-daysAgo).ToDateTime(new TimeOnly(demoExperience.StartHourUtc, 0), DateTimeKind.Utc);
            DateTime bookedOn = startsAt.AddDays(-14);

            ExperienceSession session = ExperienceSession.Create(experience, startsAt, experience.MaxGuests, bookedOn).Value;
            dbContext.ExperienceSessions.Add(session);

            ExperienceBooking booking = PayExperience(experience, session, guest, guests, bookedOn, sam, payoutDue: true);
            booking.Complete(startsAt.AddMinutes(experience.DurationMinutes + 30), experience.DurationMinutes);

            if (rating is not null)
            {
                dbContext.ExperienceReviews.Add(
                    ExperienceReview.Create(booking, rating.Value, comment, startsAt.AddDays(1)).Value);
            }
        }

        // The demo user's upcoming, paid canal food tour.
        Experience foodTour = experiences[CanalFoodTour];
        ExperienceSession upcoming = ExperienceSession.Create(
            foodTour,
            today.AddDays(5).ToDateTime(new TimeOnly(15, 0), DateTimeKind.Utc),
            foodTour.MaxGuests,
            utcNow).Value;
        dbContext.ExperienceSessions.Add(upcoming);

        PayExperience(foodTour, upcoming, demo, 2, utcNow.AddDays(-2), sam, payoutDue: false);

        return experiences;
    }

    /// <summary>
    /// Books and pays spots of a session (simulated payment) and schedules the host payout: past payouts are paid for
    /// Sam (active payout account) and on hold for Priya.
    /// </summary>
    private ExperienceBooking PayExperience(
        Experience experience,
        ExperienceSession session,
        User guest,
        int guests,
        DateTime bookedOn,
        User sam,
        bool payoutDue)
    {
        ExperienceBooking booking = ExperienceBooking.Reserve(
            experience,
            session,
            guest.Id,
            guests,
            paymentsOptions.Value.GuestServiceFeePercent,
            bookedOn).Value;
        booking.CreatedOnUtc = bookedOn;
        booking.ConfirmPayment($"sim_pi_seed_{booking.Id:N}", booking.TotalPrice, bookedOn.AddMinutes(5));
        dbContext.ExperienceBookings.Add(booking);

        var payout = Payout.ScheduleExperience(booking, experience.HostId, bookingPricing.HostServiceFeePercent);

        if (payoutDue)
        {
            if (experience.IsHostedBy(sam.Id))
            {
                payout.MarkPaid($"sim_tr_seed_{payout.Id:N}", payout.ScheduledFor.ToDateTime(new TimeOnly(5, 0), DateTimeKind.Utc));
            }
            else
            {
                payout.PutOnHold();
            }
        }

        dbContext.Payouts.Add(payout);

        return booking;
    }

    private async Task SeedConversationsAsync(
        User demo,
        User jamie,
        User sam,
        Dictionary<string, Apartment> apartments,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        // 1. The upcoming villa stay: booked, paid, a few questions, and Sam's last reply is unread.
        Apartment villa = apartments[DemoApartments.HillsideLuxuryVilla];
        Booking? villaBooking = await FindBookingAsync(villa, demo, BookingStatus.Confirmed, cancellationToken);
        Conversation villaThread = StartConversation(villa, demo, utcNow.AddDays(-8));

        Post(villaThread, SystemEvents.BookingReserved, villaBooking, "Booked. The reservation is confirmed once paid.", utcNow.AddDays(-8));
        Send(villaThread, demo, "Hi Sam! We're so excited about the villa. Is the pool heated in this season?", utcNow.AddDays(-8).AddMinutes(2));
        Send(villaThread, sam, "Hi Alex, welcome! Yes, the pool is heated to 28 °C all year round.", utcNow.AddDays(-8).AddMinutes(40));
        Post(villaThread, SystemEvents.BookingConfirmed, villaBooking, "The booking is paid and confirmed.", utcNow.AddDays(-7));
        Send(villaThread, demo, "Perfect, thank you! Could we check in a little earlier, around 1 pm?", utcNow.AddDays(-2));
        villaThread.MarkRead(demo.Id, utcNow.AddDays(-2));
        Send(villaThread, sam, "Of course! The cleaners will be done by noon. I'll leave a bottle of local wine for you.", utcNow.AddHours(-3));

        // 2. The pending igloo request: the request and the demo user's message, waiting for Sam.
        Apartment igloo = apartments[DemoApartments.AuroraGlassIgloo];
        Booking? iglooRequest = await FindBookingAsync(igloo, demo, BookingStatus.PendingApproval, cancellationToken);
        Conversation iglooThread = StartConversation(igloo, demo, utcNow.AddHours(-20));

        Post(iglooThread, SystemEvents.BookingRequested, iglooRequest, "Booking request sent.", utcNow.AddHours(-20));
        Message request = Send(
            iglooThread,
            demo,
            iglooRequest?.MessageToHost ?? "Hi Sam! We are celebrating our anniversary and would love to see the northern lights.",
            utcNow.AddHours(-20).AddMilliseconds(1));

        if (iglooRequest is not null)
        {
            request.AttachToBooking(iglooRequest.Id);
        }

        iglooThread.MarkRead(sam.Id, utcNow.AddHours(-19));

        // 3. An inquiry without a booking, answered by Sam.
        Apartment seaside = apartments[DemoApartments.SeasideFamilyRetreat];
        Conversation inquiry = StartConversation(seaside, demo, utcNow.AddDays(-15));

        Send(inquiry, demo, "Hello! Is the retreat suitable for a toddler? Do you have a cot and a high chair?", utcNow.AddDays(-15));
        Send(inquiry, sam, "Hi Alex, absolutely: there's a cot, a high chair and a gate for the stairs. The beach is a 5-minute stroll.", utcNow.AddDays(-15).AddHours(1));
        Send(inquiry, demo, "Wonderful, thanks Sam. We'll look at dates for next summer!", utcNow.AddDays(-14));
        inquiry.MarkRead(sam.Id, utcNow.AddDays(-14).AddHours(2));

        // 4. Jamie's pending request for the château: unread by Sam.
        Apartment chateau = apartments[DemoApartments.ChateauDesVignes];
        Booking? jamieRequest = await FindBookingAsync(chateau, jamie, BookingStatus.PendingApproval, cancellationToken);
        Conversation jamieThread = StartConversation(chateau, jamie, utcNow.AddHours(-6));

        Post(jamieThread, SystemEvents.BookingRequested, jamieRequest, "Booking request sent.", utcNow.AddHours(-6));
        Message jamieMessage = Send(
            jamieThread,
            jamie,
            jamieRequest?.MessageToHost ?? "We would like to celebrate my parents' 40th wedding anniversary with the family.",
            utcNow.AddHours(-6).AddMilliseconds(1));

        if (jamieRequest is not null)
        {
            jamieMessage.AttachToBooking(jamieRequest.Id);
        }
    }

    private async Task SeedNotificationsAsync(
        User demo,
        User jamie,
        User priya,
        User lucas,
        User sam,
        Dictionary<string, Apartment> apartments,
        Dictionary<string, Experience> experiences,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        DateTime read = utcNow.AddDays(-1);

        async Task BookingAsync(
            User recipient,
            string type,
            string apartmentName,
            User guest,
            BookingStatus status,
            string link,
            string dedupe,
            DateTime createdOn,
            bool isRead,
            object? extra = null)
        {
            Apartment apartment = apartments[apartmentName];
            Booking? booking = await FindBookingAsync(apartment, guest, status, cancellationToken);

            if (booking is null)
            {
                return;
            }

            var data = new Dictionary<string, object?>
            {
                ["bookingId"] = booking.Id,
                ["apartmentId"] = apartment.Id,
                ["listingName"] = apartment.Name.Value,
                ["checkIn"] = booking.Duration.Start,
                ["checkOut"] = booking.Duration.End,
                ["guestCount"] = booking.GuestCount,
                ["guestFirstName"] = guest.FirstName.Value,
                ["hostFirstName"] = apartment.IsOwnedBy(sam.Id) ? sam.FirstName.Value : priya.FirstName.Value,
            };

            foreach (var property in extra?.GetType().GetProperties() ?? [])
            {
                data[char.ToLowerInvariant(property.Name[0]) + property.Name[1..]] = property.GetValue(extra);
            }

            AddNotification(
                recipient,
                type,
                data,
                link.Replace("{id}", booking.Id.ToString(), StringComparison.Ordinal),
                dedupe.Replace("{id}", booking.Id.ToString(), StringComparison.Ordinal),
                createdOn,
                isRead ? read : null);
        }

        // The demo user (guest).
        await BookingAsync(demo, NotificationTypes.BookingConfirmed, DemoApartments.HillsideLuxuryVilla, demo, BookingStatus.Confirmed,
            "/bookings?bookingId={id}", "booking:{id}:confirmed", utcNow.AddDays(-7), isRead: true, new { Role = "Guest" });
        await BookingAsync(demo, NotificationTypes.BookingDeclined, DemoApartments.ChateauDesVignes, demo, BookingStatus.Rejected,
            "/bookings?bookingId={id}", "booking:{id}:declined", utcNow.AddDays(-3), isRead: true);
        await BookingAsync(demo, NotificationTypes.ReviewReminder, DemoApartments.RedwoodTreehouse, demo, BookingStatus.Completed,
            "/bookings?bookingId={id}", "booking:{id}:review-reminder", utcNow.AddDays(-6), isRead: false,
            new { Role = "Guest", ReviewDeadline = DateOnly.FromDateTime(utcNow).AddDays(-7 + Booking.ReviewWindowDays) });

        Booking? refunded = await FindBookingAsync(apartments[DemoApartments.AuroraGlassIgloo], demo, BookingStatus.Cancelled, cancellationToken);

        if (refunded is not null)
        {
            AddNotification(demo, NotificationTypes.RefundSucceeded, new
            {
                BookingId = refunded.Id,
                ApartmentId = refunded.ApartmentId,
                ListingName = DemoApartments.AuroraGlassIgloo,
                CheckIn = refunded.Duration.Start,
                CheckOut = refunded.Duration.End,
                Amount = refunded.RefundAmount,
                Currency = refunded.TotalPrice.Currency.Code,
            }, $"/bookings?bookingId={refunded.Id}", $"booking:{refunded.Id}:refund", utcNow.AddHours(-2), null);
        }

        ExperienceBooking? demoExperience = dbContext.ExperienceBookings.Local
            .FirstOrDefault(b => b.GuestId == demo.Id && b.Status == ExperienceBookingStatus.Confirmed);

        if (demoExperience is not null && experiences.TryGetValue(CanalFoodTour, out Experience? foodTour))
        {
            object ExperienceData(string role) => new
            {
                ExperienceBookingId = demoExperience.Id,
                ExperienceId = foodTour.Id,
                ExperienceTitle = foodTour.Title,
                StartsAtUtc = demoExperience.SessionStartsAtUtc,
                demoExperience.GuestCount,
                foodTour.MeetingPoint,
                GuestFirstName = demo.FirstName.Value,
                HostFirstName = priya.FirstName.Value,
                Role = role,
            };

            AddNotification(demo, NotificationTypes.ExperienceBooked, ExperienceData("Guest"),
                $"/experience-bookings/{demoExperience.Id}", $"experience-booking:{demoExperience.Id}:confirmed",
                utcNow.AddDays(-2), null);
            AddNotification(priya, NotificationTypes.ExperienceBooked, ExperienceData("Host"),
                $"/hosting/experiences/{foodTour.Id}/bookings", $"experience-booking:{demoExperience.Id}:confirmed",
                utcNow.AddDays(-2), null);
        }

        // Sam (host).
        await BookingAsync(sam, NotificationTypes.BookingRequested, DemoApartments.ChateauDesVignes, jamie, BookingStatus.PendingApproval,
            "/hosting/reservations?bookingId={id}", "booking:{id}:requested", utcNow.AddHours(-6), isRead: false);
        await BookingAsync(sam, NotificationTypes.BookingRequested, DemoApartments.AuroraGlassIgloo, demo, BookingStatus.PendingApproval,
            "/hosting/reservations?bookingId={id}", "booking:{id}:requested", utcNow.AddHours(-20), isRead: false);
        await BookingAsync(sam, NotificationTypes.BookingConfirmed, DemoApartments.HillsideLuxuryVilla, demo, BookingStatus.Confirmed,
            "/hosting/reservations?bookingId={id}", "booking:{id}:confirmed", utcNow.AddDays(-7), isRead: true, new { Role = "Host" });
        await BookingAsync(sam, NotificationTypes.ArrivalReminder, DemoApartments.PineRidgeCabin, jamie, BookingStatus.Confirmed,
            "/hosting/reservations?bookingId={id}", "booking:{id}:arrival-reminder", utcNow.AddHours(-4), isRead: false);

        Payout? lastPaid = dbContext.Payouts.Local
            .Where(p => p.HostId == sam.Id && p.Status == PayoutStatus.Paid && p.BookingId != null)
            .OrderByDescending(p => p.PaidOnUtc)
            .FirstOrDefault()
            ?? await dbContext.Payouts
                .Where(p => p.HostId == sam.Id && p.Status == PayoutStatus.Paid && p.BookingId != null)
                .OrderByDescending(p => p.PaidOnUtc)
                .FirstOrDefaultAsync(cancellationToken);

        if (lastPaid is not null)
        {
            Booking? paidBooking = dbContext.Bookings.Local.FirstOrDefault(b => b.Id == lastPaid.BookingId)
                ?? await dbContext.Bookings.FirstOrDefaultAsync(b => b.Id == lastPaid.BookingId, cancellationToken);
            string listingName = apartments.Values.FirstOrDefault(a => a.Id == paidBooking?.ApartmentId)?.Name.Value ?? string.Empty;

            AddNotification(sam, NotificationTypes.PayoutPaid, new
            {
                PayoutId = lastPaid.Id,
                lastPaid.Amount.Amount,
                Currency = lastPaid.Amount.Currency.Code,
                lastPaid.BookingId,
                ExperienceBookingId = (Guid?)null,
                ListingName = listingName,
            }, "/hosting/payouts", $"payout:{lastPaid.Id}:paid", lastPaid.PaidOnUtc ?? read, read);
        }

        // Jamie (guest).
        await BookingAsync(jamie, NotificationTypes.BookingConfirmed, DemoApartments.PineRidgeCabin, jamie, BookingStatus.Confirmed,
            "/bookings?bookingId={id}", "booking:{id}:confirmed", utcNow.AddDays(-10), isRead: true, new { Role = "Guest" });
        await BookingAsync(jamie, NotificationTypes.TripReminder, DemoApartments.PineRidgeCabin, jamie, BookingStatus.Confirmed,
            "/bookings?bookingId={id}", "booking:{id}:trip-reminder", utcNow.AddHours(-4), isRead: false);

        // Lucas (guest).
        await BookingAsync(lucas, NotificationTypes.BookingConfirmed, DemoApartments.CanalHouseboat, lucas, BookingStatus.Confirmed,
            "/bookings?bookingId={id}", "booking:{id}:confirmed", utcNow.AddDays(-12), isRead: true, new { Role = "Guest" });
        Apartment chateau = apartments[DemoApartments.ChateauDesVignes];
        Booking? lucasStay = await FindBookingAsync(chateau, lucas, BookingStatus.Completed, cancellationToken);
        Domain.Reviews.Review? repliedReview = lucasStay is null
            ? null
            : dbContext.Reviews.Local.FirstOrDefault(r => r.BookingId == lucasStay.Id)
              ?? await dbContext.Reviews.FirstOrDefaultAsync(r => r.BookingId == lucasStay.Id, cancellationToken);

        if (repliedReview is not null)
        {
            AddNotification(lucas, NotificationTypes.ReviewReplyReceived, new
            {
                ReviewId = repliedReview.Id,
                BookingId = repliedReview.BookingId,
                ApartmentId = chateau.Id,
                ListingName = chateau.Name.Value,
                HostFirstName = sam.FirstName.Value,
            }, $"/apartments/{chateau.Id}#reviews", $"review:{repliedReview.Id}:reply", utcNow.AddDays(-1), null);
        }

        // Priya (host and guest).
        await BookingAsync(priya, NotificationTypes.BookingConfirmed, DemoApartments.CanalHouseboat, lucas, BookingStatus.Confirmed,
            "/hosting/reservations?bookingId={id}", "booking:{id}:confirmed", utcNow.AddDays(-12), isRead: true, new { Role = "Host" });
    }

    private void AddNotification(
        User recipient,
        string type,
        object data,
        string link,
        string dedupeKey,
        DateTime createdOnUtc,
        DateTime? readOnUtc)
    {
        var notification = Notification.Create(
            recipient.Id,
            type,
            JsonSerializer.Serialize(data, NotificationDataOptions),
            link,
            dedupeKey,
            inApp: true,
            createdOnUtc);

        if (readOnUtc is not null)
        {
            notification.MarkRead(readOnUtc.Value);
        }

        dbContext.Notifications.Add(notification);
    }

    private async Task<Booking?> FindBookingAsync(
        Apartment apartment,
        User guest,
        BookingStatus status,
        CancellationToken cancellationToken) =>
        dbContext.Bookings.Local.FirstOrDefault(b => b.ApartmentId == apartment.Id && b.UserId == guest.Id && b.Status == status)
        ?? await dbContext.Bookings.FirstOrDefaultAsync(
            b => b.ApartmentId == apartment.Id && b.UserId == guest.Id && b.Status == status,
            cancellationToken);

    private Conversation StartConversation(Apartment apartment, User guest, DateTime startedOnUtc)
    {
        Conversation conversation = Conversation.Start(apartment.Id, guest.Id, apartment.OwnerId, startedOnUtc).Value;
        dbContext.Conversations.Add(conversation);

        return conversation;
    }

    private Message Send(Conversation conversation, User sender, string body, DateTime sentOnUtc)
    {
        Message message = conversation.Send(sender.Id, body, sentOnUtc).Value;
        dbContext.Messages.Add(message);

        return message;
    }

    private void Post(Conversation conversation, string systemEvent, Booking? booking, string fallback, DateTime sentOnUtc)
    {
        if (booking is null)
        {
            return;
        }

        dbContext.Messages.Add(conversation.Post(systemEvent, booking.Id, fallback, sentOnUtc));
    }

    private static ExperienceDetails Details(
        string title,
        string description,
        ExperienceCategory category,
        string city,
        string country,
        double latitude,
        double longitude,
        string meetingPoint,
        int durationMinutes,
        decimal pricePerGuest,
        string currency,
        int maxGuests,
        string[] languages,
        string[] included) => new()
    {
        Title = title,
        Description = description,
        Category = category,
        City = city,
        Country = country,
        Latitude = latitude,
        Longitude = longitude,
        MeetingPoint = meetingPoint,
        DurationMinutes = durationMinutes,
        PricePerGuest = pricePerGuest,
        Currency = currency,
        MaxGuests = maxGuests,
        Languages = languages,
        Included = included,
    };
}
