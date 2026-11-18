using System.Net;
using System.Net.Http.Json;
using Lodge.Api.FunctionalTests.Abstractions;
using Lodge.Api.FunctionalTests.Notifications;
using Lodge.Application.Experiences;
using Lodge.Contracts.Common;
using Lodge.Contracts.Experiences;
using Lodge.Contracts.Payments;
using Lodge.Domain.Notifications;

namespace Lodge.Api.FunctionalTests.Experiences;

public sealed class ExperienceTests(FunctionalTestWebAppFactory factory) : HostingFunctionalTest(factory)
{
    [Fact]
    public async Task Hosts_Should_CreateUpdateListAndDeleteExperiences()
    {
        (HttpClient host, Guid hostId) = await CreateAuthenticatedUserAsync();
        Guid photoId = await UploadPhotoIdAsync(host);

        ExperienceRequest request = NewExperience() with { ImageIds = [photoId] };
        Guid experienceId = await CreateExperienceAsync(host, request);

        ExperienceResponse? created = await host.GetFromJsonAsync<ExperienceResponse>($"api/v1/hosting/experiences/{experienceId}");
        created.Should().BeEquivalentTo(new
        {
            request.Title,
            Category = "Workshops",
            request.City,
            DurationMinutes = 120,
            PricePerGuest = 50m,
            Currency = "USD",
            MaxGuests = 6,
            Languages = new[] { "en", "fr" },
            IsListed = true,
            FreeCancellationHours = 24,
            ReviewCount = 0,
            AverageRating = (decimal?)null,
        });
        created!.Images.Should().ContainSingle().Which.Id.Should().Be(photoId);
        created.Host.Should().BeEquivalentTo(new { Id = hostId, FirstName = "First", IsSuperhost = false });

        PagedList<HostExperienceSummaryResponse>? mine = await host.GetFromJsonAsync<PagedList<HostExperienceSummaryResponse>>(
            "api/v1/hosting/experiences");
        mine!.Items.Should().ContainSingle(e => e.Id == experienceId).Which.ImageUrl.Should().EndWith($"/api/v1/files/{photoId}");

        // Validation.
        await ShouldFailWithAsync(
            await host.PostAsJsonAsync("api/v1/hosting/experiences", NewExperience() with { Category = "Karaoke" }),
            HttpStatusCode.BadRequest,
            "Experience.InvalidCategory");
        await ShouldFailWithAsync(
            await host.PostAsJsonAsync("api/v1/hosting/experiences", NewExperience() with { Languages = ["english"] }),
            HttpStatusCode.BadRequest,
            "Experience.InvalidLanguages");
        await ShouldFailWithAsync(
            await host.PostAsJsonAsync("api/v1/hosting/experiences", NewExperience() with { DurationMinutes = 20 }),
            HttpStatusCode.BadRequest,
            "Experience.InvalidDuration");
        await ShouldFailWithAsync(
            await host.PostAsJsonAsync("api/v1/hosting/experiences", NewExperience() with { Title = new string('t', 121) }),
            HttpStatusCode.BadRequest,
            "Experience.InvalidTitle");
        await ShouldFailWithAsync(
            await host.PostAsJsonAsync("api/v1/hosting/experiences", NewExperience() with { ImageIds = [Guid.NewGuid()] }),
            HttpStatusCode.BadRequest,
            "Experience.InvalidImage");

        // Access.
        (HttpClient otherHost, _) = await CreateAuthenticatedUserAsync();
        await ShouldFailWithAsync(
            await otherHost.PutAsJsonAsync($"api/v1/hosting/experiences/{experienceId}", request),
            HttpStatusCode.Forbidden,
            "Experience.NotHost");
        (HttpClient unverified, _) = await CreateAuthenticatedUserAsync(verifyEmail: false);
        await ShouldFailWithAsync(
            await unverified.PostAsJsonAsync("api/v1/hosting/experiences", NewExperience()),
            HttpStatusCode.Forbidden,
            "User.EmailNotVerified");

        // Update, unlist (hidden from others), delete.
        (await host.PutAsJsonAsync($"api/v1/hosting/experiences/{experienceId}", request with { PricePerGuest = 65, ImageIds = [] }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await HttpClient.GetFromJsonAsync<ExperienceResponse>($"api/v1/experiences/{experienceId}"))!
            .Should().BeEquivalentTo(new { PricePerGuest = 65m, Images = Array.Empty<ExperienceImageResponse>() });

        (await host.PutAsJsonAsync($"api/v1/hosting/experiences/{experienceId}/status", new UpdateExperienceStatusRequest(false)))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        await ShouldFailWithAsync(await HttpClient.GetAsync($"api/v1/experiences/{experienceId}"), HttpStatusCode.NotFound, "Experience.NotFound");
        (await host.GetAsync($"api/v1/experiences/{experienceId}")).StatusCode.Should().Be(HttpStatusCode.OK);

        (await host.DeleteAsync($"api/v1/hosting/experiences/{experienceId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        await ShouldFailWithAsync(await host.GetAsync($"api/v1/hosting/experiences/{experienceId}"), HttpStatusCode.NotFound, "Experience.NotFound");
    }

    [Fact]
    public async Task RecurringSessions_Should_BeGenerated_InTheHostsTimeZone()
    {
        (HttpClient host, _) = await CreateAuthenticatedUserAsync();
        Guid experienceId = await CreateExperienceAsync(host, NewExperience());

        DateOnly from = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(3);
        DateOnly until = from.AddDays(27);
        var recurring = new RecurringSessionsRequest
        {
            DaysOfWeek = ["Monday", "thursday"],
            StartTime = "14:00",
            TimeZone = "Europe/Amsterdam",
            From = from,
            Until = until,
        };

        HttpResponseMessage response = await host.PostAsJsonAsync(
            $"api/v1/hosting/experiences/{experienceId}/sessions",
            new CreateSessionsRequest { Recurring = recurring, Capacity = 4 });
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        CreatedSessionsResponse created = (await response.Content.ReadFromJsonAsync<CreatedSessionsResponse>())!;
        created.Ids.Should().HaveCount(8);

        List<HostExperienceSessionResponse>? sessions = await host.GetFromJsonAsync<List<HostExperienceSessionResponse>>(
            $"api/v1/hosting/experiences/{experienceId}/sessions?from={from:yyyy-MM-dd}&to={until.AddDays(1):yyyy-MM-dd}");

        TimeZoneInfo amsterdam = TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");
        sessions.Should().HaveCount(8).And.OnlyContain(s => s.Capacity == 4 && s.SpotsLeft == 4 && !s.IsCancelled);
        sessions!.Select(s => TimeZoneInfo.ConvertTimeFromUtc(s.StartsAtUtc, amsterdam)).Should().OnlyContain(local =>
            local.Hour == 14 && local.Minute == 0 &&
            (local.DayOfWeek == DayOfWeek.Monday || local.DayOfWeek == DayOfWeek.Thursday));

        // Existing start times are skipped: nothing left to create.
        await ShouldFailWithAsync(
            await host.PostAsJsonAsync($"api/v1/hosting/experiences/{experienceId}/sessions", new CreateSessionsRequest { Recurring = recurring }),
            HttpStatusCode.BadRequest,
            "ExperienceSession.NoSessions");

        // A daily schedule over a year is more than 200 sessions.
        await ShouldFailWithAsync(
            await host.PostAsJsonAsync($"api/v1/hosting/experiences/{experienceId}/sessions", new CreateSessionsRequest
            {
                Recurring = recurring with
                {
                    DaysOfWeek = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"],
                    StartTime = "09:30",
                    Until = from.AddDays(300),
                },
            }),
            HttpStatusCode.BadRequest,
            "ExperienceSession.TooManySessions");

        await ShouldFailWithAsync(
            await host.PostAsJsonAsync($"api/v1/hosting/experiences/{experienceId}/sessions",
                new CreateSessionsRequest { Recurring = recurring with { TimeZone = "Mars/Olympus" } }),
            HttpStatusCode.BadRequest,
            "ExperienceSession.InvalidTimeZone");
        await ShouldFailWithAsync(
            await host.PostAsJsonAsync($"api/v1/hosting/experiences/{experienceId}/sessions",
                new CreateSessionsRequest { Recurring = recurring, StartsAtUtc = DateTime.UtcNow.AddDays(2) }),
            HttpStatusCode.BadRequest,
            "ExperienceSession.InvalidSchedule");
        await ShouldFailWithAsync(
            await host.PostAsJsonAsync($"api/v1/hosting/experiences/{experienceId}/sessions",
                new CreateSessionsRequest { StartsAtUtc = DateTime.UtcNow.AddHours(-1) }),
            HttpStatusCode.BadRequest,
            "ExperienceSession.StartsInPast");
        await ShouldFailWithAsync(
            await host.PostAsJsonAsync($"api/v1/hosting/experiences/{experienceId}/sessions",
                new CreateSessionsRequest { StartsAtUtc = DateTime.UtcNow.AddDays(2), Capacity = 7 }),
            HttpStatusCode.BadRequest,
            "ExperienceSession.InvalidCapacity");

        // A single session; the public list shows bookable sessions only.
        Guid single = await CreateSessionAsync(host, experienceId, DateTime.UtcNow.AddDays(1), capacity: 2);
        List<ExperienceSessionResponse>? publicSessions = await HttpClient.GetFromJsonAsync<List<ExperienceSessionResponse>>(
            $"api/v1/experiences/{experienceId}/sessions");
        publicSessions!.Select(s => s.Id).Should().Contain(single).And.Contain(created.Ids);
        publicSessions!.Single(s => s.Id == single).Should().BeEquivalentTo(new { Capacity = 2, SpotsLeft = 2 });
    }

    [Fact]
    public async Task Search_Should_ReturnListedExperiences_WithAFutureSessionWithEnoughSpots()
    {
        (HttpClient host, _) = await CreateAuthenticatedUserAsync();
        string city = $"Searchville {Guid.NewGuid():N}"[..20];

        ExperienceRequest bookable = NewExperience() with { City = city, Category = "Food", PricePerGuest = 40 };
        Guid bookableId = await CreateExperienceAsync(host, bookable);
        DateTime startsAt = DateTime.UtcNow.Date.AddDays(5).AddHours(10);
        await CreateSessionAsync(host, bookableId, startsAt, capacity: 2);

        Guid unlistedId = await CreateExperienceAsync(host, NewExperience() with { City = city, IsListed = false });
        await CreateSessionAsync(host, unlistedId, startsAt, capacity: 2);

        await CreateExperienceAsync(host, NewExperience() with { City = city });

        async Task<List<ExperienceSummaryResponse>> SearchAsync(string query) =>
            [.. (await HttpClient.GetFromJsonAsync<PagedList<ExperienceSummaryResponse>>(
                $"api/v1/experiences?city={Uri.EscapeDataString(city)}{query}"))!.Items];

        ExperienceSummaryResponse found = (await SearchAsync(string.Empty)).Should().ContainSingle().Subject;
        found.Id.Should().Be(bookableId);
        found.NextSessionStartsAtUtc.Should().Be(startsAt);
        found.Category.Should().Be("Food");
        found.ImageUrl.Should().BeNull();
        found.HostFirstName.Should().Be("First");

        (await SearchAsync("&guests=2")).Should().ContainSingle();
        (await SearchAsync("&guests=3")).Should().BeEmpty();
        (await SearchAsync($"&date={startsAt:yyyy-MM-dd}")).Should().ContainSingle();
        (await SearchAsync($"&date={startsAt.AddDays(1):yyyy-MM-dd}")).Should().BeEmpty();
        (await SearchAsync("&category=food")).Should().ContainSingle();
        (await SearchAsync("&category=Sports")).Should().BeEmpty();
        (await SearchAsync($"&q={Uri.EscapeDataString(bookable.Title[..12])}")).Should().ContainSingle();
        (await SearchAsync("&minPrice=30&maxPrice=45")).Should().ContainSingle();
        (await SearchAsync("&minPrice=41")).Should().BeEmpty();

        await ShouldFailWithAsync(await HttpClient.GetAsync("api/v1/experiences?category=Karaoke"), HttpStatusCode.BadRequest, "Experience.InvalidCategory");

        // The seeded experiences are searchable too.
        PagedList<ExperienceSummaryResponse>? seeded = await HttpClient.GetFromJsonAsync<PagedList<ExperienceSummaryResponse>>(
            "api/v1/experiences?city=Rovaniemi");
        seeded!.Items.Should().ContainSingle().Which.Title.Should().Be("Northern Lights Hunt by Snowmobile");
    }

    [Fact]
    public async Task Capacity_Should_NeverBeExceeded_UnderConcurrentBookings()
    {
        (HttpClient host, _) = await CreateAuthenticatedUserAsync();
        Guid experienceId = await CreateExperienceAsync(host, NewExperience());
        Guid sessionId = await CreateSessionAsync(host, experienceId, DateTime.UtcNow.AddDays(4), capacity: 3);

        List<(HttpClient Client, Guid UserId)> guests = [];

        for (int index = 0; index < 8; index++)
        {
            guests.Add(await CreateAuthenticatedUserAsync());
        }

        HttpResponseMessage[] responses = await Task.WhenAll(guests.Select(guest => ReserveAsync(guest.Client, sessionId, 1)));

        responses.Count(r => r.StatusCode == HttpStatusCode.Created).Should().Be(3);
        HttpResponseMessage[] rejected = [.. responses.Where(r => r.StatusCode != HttpStatusCode.Created)];
        rejected.Should().HaveCount(5);

        foreach (HttpResponseMessage response in rejected)
        {
            await ShouldFailWithAsync(response, HttpStatusCode.Conflict, "ExperienceSession.NotEnoughSpots");
        }

        List<HostExperienceSessionResponse>? sessions = await host.GetFromJsonAsync<List<HostExperienceSessionResponse>>(
            $"api/v1/hosting/experiences/{experienceId}/sessions");
        sessions!.Single(s => s.Id == sessionId).Should().BeEquivalentTo(new { BookedGuests = 3, SpotsLeft = 0, BookingCount = 3 });

        // A full session no longer appears in the bookable sessions.
        (await HttpClient.GetFromJsonAsync<List<ExperienceSessionResponse>>($"api/v1/experiences/{experienceId}/sessions"))!
            .Single(s => s.Id == sessionId).SpotsLeft.Should().Be(0);
    }

    [Fact]
    public async Task Payment_Should_ConfirmTheBooking_ScheduleThePayout_AndNotify()
    {
        (HttpClient host, _) = await CreateAuthenticatedUserAsync();
        Guid experienceId = await CreateExperienceAsync(host, NewExperience());
        DateTime startsAt = DateTime.UtcNow.AddDays(6);
        Guid sessionId = await CreateSessionAsync(host, experienceId, startsAt, capacity: 6);

        (HttpClient guest, Guid guestId) = await CreateAuthenticatedUserAsync();

        // Business rules first: verified e-mail, not one's own experience, guest count.
        (HttpClient unverified, _) = await CreateAuthenticatedUserAsync(verifyEmail: false);
        await ShouldFailWithAsync(await ReserveAsync(unverified, sessionId, 1), HttpStatusCode.Forbidden, "User.EmailNotVerified");
        await ShouldFailWithAsync(await ReserveAsync(host, sessionId, 1), HttpStatusCode.BadRequest, "ExperienceBooking.OwnExperience");
        await ShouldFailWithAsync(await ReserveAsync(guest, sessionId, 7), HttpStatusCode.BadRequest, "ExperienceBooking.InvalidGuestCount");

        Guid bookingId = await ReserveIdAsync(guest, sessionId, 2);

        ExperienceBookingResponse reserved = await GetExperienceBookingAsync(guest, bookingId);
        reserved.Should().BeEquivalentTo(new
        {
            Status = "Reserved",
            GuestCount = 2,
            PriceTotal = 100m,
            ServiceFee = 12m,
            TotalPrice = 112m,
            Currency = "USD",
            SessionId = sessionId,
            ExperienceId = experienceId,
            CanCancel = true,
            CanReview = false,
        });

        HttpResponseMessage checkout = await guest.PostAsync($"api/v1/experience-bookings/{bookingId}/checkout", null);
        checkout.StatusCode.Should().Be(HttpStatusCode.OK);
        (await checkout.Content.ReadFromJsonAsync<CheckoutResponse>())!.Url.Should().Be(
            $"{FunctionalTestWebAppFactory.FrontendBaseUrl}/checkout/simulated?experienceBookingId={bookingId}");

        (HttpClient stranger, _) = await CreateAuthenticatedUserAsync();
        await ShouldFailWithAsync(
            await stranger.PostAsync($"api/v1/payments/simulated/experience-bookings/{bookingId}/complete", null),
            HttpStatusCode.NotFound,
            "ExperienceBooking.NotFound");
        await ShouldFailWithAsync(await stranger.GetAsync($"api/v1/experience-bookings/{bookingId}"), HttpStatusCode.NotFound, "ExperienceBooking.NotFound");

        await PayExperienceAsync(guest, bookingId);
        // Paying twice is harmless.
        await PayExperienceAsync(guest, bookingId);

        ExperienceBookingResponse confirmed = await GetExperienceBookingAsync(guest, bookingId);
        confirmed.Status.Should().Be("Confirmed");
        confirmed.AmountPaid.Should().Be(112m);
        confirmed.PaidOnUtc.Should().NotBeNull();
        await ShouldFailWithAsync(await guest.PostAsync($"api/v1/experience-bookings/{bookingId}/checkout", null), HttpStatusCode.BadRequest, "ExperienceBooking.NotReserved");

        // The host sees the booking and a payout scheduled the day after the session (price minus the 3% host fee).
        ExperienceBookingResponse hostView = await GetExperienceBookingAsync(host, bookingId);
        hostView.CanCancel.Should().BeFalse();
        hostView.GuestId.Should().Be(guestId);

        PagedList<ExperienceBookingResponse>? hostBookings = await host.GetFromJsonAsync<PagedList<ExperienceBookingResponse>>(
            $"api/v1/hosting/experiences/bookings?experienceId={experienceId}");
        hostBookings!.Items.Should().ContainSingle().Which.Id.Should().Be(bookingId);

        PayoutResponse payout = (await host.GetFromJsonAsync<PagedList<PayoutResponse>>("api/v1/hosting/payouts"))!.Items
            .Should().ContainSingle(p => p.ExperienceBookingId == bookingId).Subject;
        payout.Should().BeEquivalentTo(new
        {
            BookingId = (Guid?)null,
            Amount = 97m,
            Status = "Scheduled",
            ScheduledFor = DateOnly.FromDateTime(startsAt).AddDays(1),
        });
        payout.ListingName.Should().StartWith("Zz Test experience");

        List<ExperienceBookingResponse>? mine = await guest.GetFromJsonAsync<List<ExperienceBookingResponse>>(
            $"api/v1/users/{guestId}/experience-bookings");
        mine.Should().ContainSingle().Which.Id.Should().Be(bookingId);
        await ShouldFailWithAsync(await stranger.GetAsync($"api/v1/users/{guestId}/experience-bookings"), HttpStatusCode.Forbidden, "User.InvalidPermissions");

        await ProcessOutboxAsync();

        (await NotificationTests.NotificationsAsync(guest)).Should().ContainSingle(n => n.Type == NotificationTypes.ExperienceBooked)
            .Which.Data.GetProperty("role").GetString().Should().Be("Guest");
        (await NotificationTests.NotificationsAsync(host)).Should().ContainSingle(n => n.Type == NotificationTypes.ExperienceBooked)
            .Which.Link.Should().Be($"/hosting/experiences/{experienceId}/bookings");
    }

    [Fact]
    public async Task Cancellation_Should_RefundInFull_AtLeast24HoursBefore_AndNotLater()
    {
        (HttpClient host, _) = await CreateAuthenticatedUserAsync();
        Guid experienceId = await CreateExperienceAsync(host, NewExperience());
        Guid earlySession = await CreateSessionAsync(host, experienceId, DateTime.UtcNow.AddDays(3), capacity: 4);
        Guid lateSession = await CreateSessionAsync(host, experienceId, DateTime.UtcNow.AddHours(10), capacity: 4);

        (HttpClient guest, _) = await CreateAuthenticatedUserAsync();

        // Three days ahead: full refund (sent by the refund pipeline), spots released, payout cancelled.
        Guid early = await ReserveIdAsync(guest, earlySession, 2);
        await PayExperienceAsync(guest, early);

        (await guest.PutAsync($"api/v1/experience-bookings/{early}/cancel", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        ExperienceBookingResponse cancelled = await GetExperienceBookingAsync(guest, early);
        cancelled.Should().BeEquivalentTo(new { Status = "Cancelled", RefundAmount = 112m, RefundStatus = "Pending", CancelledByHost = false, CanCancel = false });

        await ProcessOutboxAsync();

        (await GetExperienceBookingAsync(guest, early)).RefundStatus.Should().Be("Succeeded");
        (await NotificationTests.NotificationsAsync(guest)).Should().Contain(n =>
            n.Type == NotificationTypes.RefundSucceeded && n.Data.GetProperty("experienceBookingId").GetGuid() == early);
        (await host.GetFromJsonAsync<PagedList<PayoutResponse>>("api/v1/hosting/payouts"))!.Items
            .Single(p => p.ExperienceBookingId == early).Status.Should().Be("Cancelled");
        (await host.GetFromJsonAsync<List<HostExperienceSessionResponse>>($"api/v1/hosting/experiences/{experienceId}/sessions"))!
            .Single(s => s.Id == earlySession).BookedGuests.Should().Be(0);

        await ShouldFailWithAsync(await guest.PutAsync($"api/v1/experience-bookings/{early}/cancel", null), HttpStatusCode.BadRequest, "ExperienceBooking.NotCancellable");

        // Ten hours ahead: no refund, the host keeps the payout.
        Guid late = await ReserveIdAsync(guest, lateSession, 1);
        await PayExperienceAsync(guest, late);

        (await guest.PutAsync($"api/v1/experience-bookings/{late}/cancel", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await GetExperienceBookingAsync(guest, late)).Should().BeEquivalentTo(new { Status = "Cancelled", RefundAmount = 0m, RefundStatus = "None" });
        (await host.GetFromJsonAsync<PagedList<PayoutResponse>>("api/v1/hosting/payouts"))!.Items
            .Single(p => p.ExperienceBookingId == late).Should().BeEquivalentTo(new { Status = "Scheduled", Amount = 48.5m });

        // An unpaid reservation is simply released.
        Guid unpaid = await ReserveIdAsync(guest, earlySession, 1);
        (await guest.PutAsync($"api/v1/experience-bookings/{unpaid}/cancel", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await GetExperienceBookingAsync(guest, unpaid)).Should().BeEquivalentTo(new { Status = "Cancelled", RefundAmount = (decimal?)null });
    }

    [Fact]
    public async Task Hosts_Should_RemoveEmptySessions_AndCancelBookedOnes_WithFullRefunds()
    {
        (HttpClient host, _) = await CreateAuthenticatedUserAsync();
        Guid experienceId = await CreateExperienceAsync(host, NewExperience());
        Guid empty = await CreateSessionAsync(host, experienceId, DateTime.UtcNow.AddDays(8), capacity: 4);
        Guid booked = await CreateSessionAsync(host, experienceId, DateTime.UtcNow.AddHours(12), capacity: 4);

        (HttpClient guest, _) = await CreateAuthenticatedUserAsync();
        Guid bookingId = await ReserveIdAsync(guest, booked, 2);
        await PayExperienceAsync(guest, bookingId);

        (await host.DeleteAsync($"api/v1/hosting/experiences/{experienceId}/sessions/{empty}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await host.DeleteAsync($"api/v1/hosting/experiences/{experienceId}/sessions/{booked}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        List<HostExperienceSessionResponse>? sessions = await host.GetFromJsonAsync<List<HostExperienceSessionResponse>>(
            $"api/v1/hosting/experiences/{experienceId}/sessions");
        sessions!.Should().NotContain(s => s.Id == empty);
        sessions.Single(s => s.Id == booked).IsCancelled.Should().BeTrue();

        // Less than 24 hours before the start, but the host cancelled: full refund.
        (await GetExperienceBookingAsync(guest, bookingId)).Should().BeEquivalentTo(new
        {
            Status = "Cancelled",
            CancelledByHost = true,
            RefundAmount = 112m,
            RefundStatus = "Pending",
        });

        // An experience with upcoming bookings cannot be deleted; without them it can.
        Guid other = await CreateSessionAsync(host, experienceId, DateTime.UtcNow.AddDays(9), capacity: 4);
        await ReserveIdAsync(guest, other, 1);
        await ShouldFailWithAsync(await host.DeleteAsync($"api/v1/hosting/experiences/{experienceId}"), HttpStatusCode.Conflict, "Experience.HasUpcomingBookings");
    }

    [Fact]
    public async Task Reviews_Should_OnlyBeAllowed_OnceForCompletedBookings()
    {
        (HttpClient host, _) = await CreateAuthenticatedUserAsync();
        Guid experienceId = await CreateExperienceAsync(host, NewExperience());
        Guid sessionId = await CreateSessionAsync(host, experienceId, DateTime.UtcNow.AddDays(2), capacity: 4);

        (HttpClient guest, _) = await CreateAuthenticatedUserAsync();
        Guid bookingId = await ReserveIdAsync(guest, sessionId, 1);
        await PayExperienceAsync(guest, bookingId);

        await ShouldFailWithAsync(
            await guest.PostAsJsonAsync($"api/v1/experience-bookings/{bookingId}/review", new ExperienceReviewRequest(5, "Great!")),
            HttpStatusCode.Forbidden,
            "ExperienceBooking.NotCompleted");

        // The session took place: the daily job completes the booking.
        await ExecuteSqlAsync($"UPDATE experience_sessions SET starts_at_utc = now() - interval '1 day' WHERE id = {sessionId}");
        await ExecuteSqlAsync($"UPDATE experience_bookings SET session_starts_at_utc = now() - interval '1 day' WHERE id = {bookingId}");
        (await RunAsync<IExperienceJobs, int>(jobs => jobs.CompletePastSessionsAsync())).Should().BeGreaterThanOrEqualTo(1);

        ExperienceBookingResponse completed = await GetExperienceBookingAsync(guest, bookingId);
        completed.Status.Should().Be("Completed");
        completed.CanReview.Should().BeTrue();

        (HttpClient stranger, _) = await CreateAuthenticatedUserAsync();
        await ShouldFailWithAsync(
            await stranger.PostAsJsonAsync($"api/v1/experience-bookings/{bookingId}/review", new ExperienceReviewRequest(1, "Nope")),
            HttpStatusCode.NotFound,
            "ExperienceBooking.NotFound");
        await ShouldFailWithAsync(
            await guest.PostAsJsonAsync($"api/v1/experience-bookings/{bookingId}/review", new ExperienceReviewRequest(6, "Too good")),
            HttpStatusCode.BadRequest,
            "Rating.Invalid");

        HttpResponseMessage reviewed = await guest.PostAsJsonAsync(
            $"api/v1/experience-bookings/{bookingId}/review",
            new ExperienceReviewRequest(4, "  Fun and well organised.  "));
        reviewed.StatusCode.Should().Be(HttpStatusCode.Created, await reviewed.Content.ReadAsStringAsync());

        await ShouldFailWithAsync(
            await guest.PostAsJsonAsync($"api/v1/experience-bookings/{bookingId}/review", new ExperienceReviewRequest(5, "Again")),
            HttpStatusCode.BadRequest,
            "ExperienceBooking.AlreadyReviewed");

        PagedList<ExperienceReviewResponse>? reviews = await HttpClient.GetFromJsonAsync<PagedList<ExperienceReviewResponse>>(
            $"api/v1/experiences/{experienceId}/reviews");
        reviews!.Items.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            Rating = 4,
            Comment = "Fun and well organised.",
            Guest = new { FirstName = "First" },
        });

        (await HttpClient.GetFromJsonAsync<ExperienceResponse>($"api/v1/experiences/{experienceId}"))!
            .Should().BeEquivalentTo(new { ReviewCount = 1, AverageRating = 4m });
        (await GetExperienceBookingAsync(guest, bookingId)).Should().BeEquivalentTo(new { HasReview = true, CanReview = false });
    }

    [Fact]
    public async Task ReminderJob_Should_RemindGuestsOfTomorrowsExperiences_Once()
    {
        (HttpClient host, _) = await CreateAuthenticatedUserAsync();
        Guid experienceId = await CreateExperienceAsync(host, NewExperience());
        DateTime tomorrow = DateTime.UtcNow.Date.AddDays(1).AddHours(12);
        Guid sessionId = await CreateSessionAsync(host, experienceId, tomorrow, capacity: 4);

        (HttpClient guest, _) = await CreateAuthenticatedUserAsync();
        Guid bookingId = await ReserveIdAsync(guest, sessionId, 2);
        await PayExperienceAsync(guest, bookingId);

        await RunAsync<Lodge.Application.Notifications.ISendRemindersJob, int>(job => job.SendAsync());
        await RunAsync<Lodge.Application.Notifications.ISendRemindersJob, int>(job => job.SendAsync());

        NotificationResponseAssertion(await NotificationTests.NotificationsAsync(guest), bookingId);
    }

    [Fact]
    public async Task Reserve_Should_ReplayTheFirstResult_ForTheSameIdempotencyKey()
    {
        (HttpClient host, _) = await CreateAuthenticatedUserAsync();
        Guid experienceId = await CreateExperienceAsync(host, NewExperience());
        Guid sessionId = await CreateSessionAsync(host, experienceId, DateTime.UtcNow.AddDays(5), capacity: 4);
        (HttpClient guest, _) = await CreateAuthenticatedUserAsync();
        string key = Guid.NewGuid().ToString();

        HttpResponseMessage first = await ReserveAsync(guest, sessionId, 2, key);
        HttpResponseMessage replay = await ReserveAsync(guest, sessionId, 2, key);

        first.StatusCode.Should().Be(HttpStatusCode.Created, await first.Content.ReadAsStringAsync());
        replay.StatusCode.Should().Be(HttpStatusCode.Created, await replay.Content.ReadAsStringAsync());
        Guid bookingId = (await first.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;
        (await replay.Content.ReadFromJsonAsync<CreatedResponse>())!.Id.Should().Be(bookingId);
        replay.Headers.Location.Should().Be(first.Headers.Location);

        // Only two spots were taken.
        (await ReserveAsync(guest, sessionId, 3)).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReserveAsync(guest, sessionId, 2)).StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private static void NotificationResponseAssertion(
        List<Lodge.Contracts.Notifications.NotificationResponse> notifications,
        Guid bookingId)
    {
        var reminder = notifications.Should().ContainSingle(n => n.Type == NotificationTypes.ExperienceReminder).Subject;
        reminder.Link.Should().Be($"/experience-bookings/{bookingId}");
        reminder.Data.GetProperty("experienceBookingId").GetGuid().Should().Be(bookingId);
        reminder.Data.GetProperty("meetingPoint").GetString().Should().Be("The garden gate, 1 Test Street");
    }

    [Fact]
    public async Task UnpaidHolds_Should_BeReleased_AfterThePaymentWindow()
    {
        (HttpClient host, _) = await CreateAuthenticatedUserAsync();
        Guid experienceId = await CreateExperienceAsync(host, NewExperience());
        Guid sessionId = await CreateSessionAsync(host, experienceId, DateTime.UtcNow.AddDays(5), capacity: 2);

        (HttpClient guest, _) = await CreateAuthenticatedUserAsync();
        Guid bookingId = await ReserveIdAsync(guest, sessionId, 2);

        await ExecuteSqlAsync($"UPDATE experience_bookings SET created_on_utc = now() - interval '3 hours' WHERE id = {bookingId}");
        (await RunAsync<IExperienceJobs, int>(jobs => jobs.ExpireUnpaidHoldsAsync())).Should().BeGreaterThanOrEqualTo(1);

        (await GetExperienceBookingAsync(guest, bookingId)).Status.Should().Be("Cancelled");
        (await HttpClient.GetFromJsonAsync<List<ExperienceSessionResponse>>($"api/v1/experiences/{experienceId}/sessions"))!
            .Single(s => s.Id == sessionId).SpotsLeft.Should().Be(2);
    }

    private static ExperienceRequest NewExperience() => new()
    {
        Title = $"Zz Test experience {Guid.NewGuid():N}",
        Description = "Learn to make tea the slow way, in a quiet garden.",
        Category = "Workshops",
        City = "Testville",
        Country = "Testland",
        Latitude = -40,
        Longitude = -20,
        MeetingPoint = "The garden gate, 1 Test Street",
        DurationMinutes = 120,
        PricePerGuest = 50,
        Currency = "USD",
        MaxGuests = 6,
        Languages = ["en", "FR"],
        Included = ["Tea", "Biscuits"],
    };

    private static async Task<Guid> CreateExperienceAsync(HttpClient host, ExperienceRequest request)
    {
        HttpResponseMessage response = await host.PostAsJsonAsync("api/v1/hosting/experiences", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;
    }

    private static async Task<Guid> CreateSessionAsync(HttpClient host, Guid experienceId, DateTime startsAtUtc, int capacity)
    {
        HttpResponseMessage response = await host.PostAsJsonAsync(
            $"api/v1/hosting/experiences/{experienceId}/sessions",
            new CreateSessionsRequest { StartsAtUtc = startsAtUtc, Capacity = capacity });
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<CreatedSessionsResponse>())!.Ids.Single();
    }

    private static async Task<HttpResponseMessage> ReserveAsync(
        HttpClient guest,
        Guid sessionId,
        int guestCount,
        string? idempotencyKey = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/experience-bookings")
        {
            Content = JsonContent.Create(new ReserveExperienceRequest(sessionId, guestCount)),
        };
        request.Headers.Add("X-Idempotency-Key", idempotencyKey ?? Guid.NewGuid().ToString());

        return await guest.SendAsync(request);
    }

    private static async Task<Guid> ReserveIdAsync(HttpClient guest, Guid sessionId, int guestCount)
    {
        HttpResponseMessage response = await ReserveAsync(guest, sessionId, guestCount);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;
    }

    private static async Task PayExperienceAsync(HttpClient guest, Guid bookingId)
    {
        HttpResponseMessage completed = await guest.PostAsync(
            $"api/v1/payments/simulated/experience-bookings/{bookingId}/complete",
            null);
        completed.StatusCode.Should().Be(HttpStatusCode.NoContent, await completed.Content.ReadAsStringAsync());
    }

    private static async Task<ExperienceBookingResponse> GetExperienceBookingAsync(HttpClient client, Guid bookingId) =>
        (await client.GetFromJsonAsync<ExperienceBookingResponse>($"api/v1/experience-bookings/{bookingId}"))!;
}
