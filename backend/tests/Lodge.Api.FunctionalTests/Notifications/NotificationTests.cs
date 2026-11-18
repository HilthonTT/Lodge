using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lodge.Api.FunctionalTests.Abstractions;
using Lodge.Application.Notifications;
using Lodge.Contracts.Bookings;
using Lodge.Contracts.Common;
using Lodge.Contracts.Emails;
using Lodge.Contracts.Messaging;
using Lodge.Contracts.Notifications;
using Lodge.Contracts.Reviews;
using Lodge.Domain.Notifications;

namespace Lodge.Api.FunctionalTests.Notifications;

public sealed class NotificationTests(FunctionalTestWebAppFactory factory) : HostingFunctionalTest(factory)
{
    private static readonly NotificationChannelDto AllOn = new(true, true, true, true, true);

    [Fact]
    public async Task BookingEvents_Should_CreateNotifications_ForTheRightPeople()
    {
        (HttpClient host, Guid hostId, Guid listingId) = await CreateHostWithListingAsync(listing => listing with { InstantBook = false });
        DateOnly start = NextFreeDate();
        (HttpClient guest, Guid guestId, Guid bookingId) = await BookAsGuestAsync(listingId, start, 3);
        await ProcessOutboxAsync();

        NotificationResponse requested = (await NotificationsAsync(host)).Should().ContainSingle().Subject;
        requested.Type.Should().Be(NotificationTypes.BookingRequested);
        requested.Link.Should().Be($"/hosting/reservations?bookingId={bookingId}");
        requested.ReadOnUtc.Should().BeNull();
        requested.Data.GetProperty("bookingId").GetGuid().Should().Be(bookingId);
        requested.Data.GetProperty("apartmentId").GetGuid().Should().Be(listingId);
        requested.Data.GetProperty("checkIn").GetString().Should().Be(start.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        requested.Data.GetProperty("guestFirstName").GetString().Should().Be("First");
        requested.Data.GetProperty("listingName").GetString().Should().StartWith("Zz Test listing");
        (await NotificationsAsync(guest)).Should().BeEmpty();

        (await host.PutAsync($"api/v1/hosting/bookings/{bookingId}/accept", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        await ProcessOutboxAsync();
        await PayAsync(guest, guestId, bookingId);
        await ProcessOutboxAsync();

        (await NotificationsAsync(guest)).Select(n => n.Type).Should().BeEquivalentTo(
            [NotificationTypes.BookingAccepted, NotificationTypes.BookingConfirmed]);
        (await NotificationsAsync(host)).Select(n => n.Type).Should().BeEquivalentTo(
            [NotificationTypes.BookingRequested, NotificationTypes.BookingConfirmed]);

        NotificationResponse hostConfirmed = (await NotificationsAsync(host)).Single(n => n.Type == NotificationTypes.BookingConfirmed);
        hostConfirmed.Data.GetProperty("role").GetString().Should().Be("Host");

        // The host cancels: the guest is told (with the refund), and the refund is notified once it succeeded.
        (await host.PutAsync($"api/v1/hosting/bookings/{bookingId}/cancel", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        await ProcessOutboxAsync();

        List<NotificationResponse> guestNotifications = await NotificationsAsync(guest);
        NotificationResponse cancelled = guestNotifications.Single(n => n.Type == NotificationTypes.BookingCancelled);
        cancelled.Data.GetProperty("cancelledBy").GetString().Should().Be("Host");
        cancelled.Data.GetProperty("refundAmount").GetDecimal().Should().BeGreaterThan(0);
        guestNotifications.Should().Contain(n => n.Type == NotificationTypes.RefundSucceeded);
        (await NotificationsAsync(host)).Should().NotContain(n => n.Type == NotificationTypes.BookingCancelled);

        // Newest first.
        guestNotifications.Select(n => n.CreatedOnUtc).Should().BeInDescendingOrder();
        hostId.Should().NotBe(guestId);
    }

    [Fact]
    public async Task Reviews_Replies_And_Payouts_Should_NotifyTheirRecipients()
    {
        (HttpClient host, _, Guid listingId) = await CreateHostWithListingAsync();
        (HttpClient guest, Guid guestId) = await CreateAuthenticatedUserAsync();
        Guid stayId = await CreateCompletedStayAsync(listingId, guestId, checkedOutDaysAgo: 2);

        var categories = new CategoryRatingsDto { Cleanliness = 5, Accuracy = 5, CheckIn = 5, Communication = 5, Location = 4, Value = 5 };
        using (var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/reviews")
        {
            Content = JsonContent.Create(new CreateReviewRequest(guestId, stayId, 5, "Lovely flat.", categories)),
        })
        {
            request.Headers.Add("X-Idempotency-Key", Guid.NewGuid().ToString());
            (await guest.SendAsync(request)).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        await ProcessOutboxAsync();
        (await NotificationsAsync(host)).Should().NotContain(n => n.Type == NotificationTypes.ReviewReceived, "the review is not published yet");

        (await host.PostAsJsonAsync($"api/v1/hosting/bookings/{stayId}/guest-review", new GuestReviewRequest(5, "A great guest.")))
            .IsSuccessStatusCode.Should().BeTrue();
        await ProcessOutboxAsync();

        NotificationResponse hostReview = (await NotificationsAsync(host)).Should().ContainSingle(n => n.Type == NotificationTypes.ReviewReceived).Subject;
        hostReview.Data.GetProperty("kind").GetString().Should().Be("Listing");
        hostReview.Data.GetProperty("rating").GetInt32().Should().Be(5);
        hostReview.Link.Should().Be("/hosting/reviews");

        NotificationResponse guestReview = (await NotificationsAsync(guest)).Should().ContainSingle(n => n.Type == NotificationTypes.ReviewReceived).Subject;
        guestReview.Data.GetProperty("kind").GetString().Should().Be("Guest");
        guestReview.Link.Should().Be($"/users/{guestId}");

        Guid reviewId = hostReview.Data.GetProperty("reviewId").GetGuid();
        (await host.PutAsJsonAsync($"api/v1/hosting/reviews/{reviewId}/reply", new ReviewReplyRequest("Thank you!"))).IsSuccessStatusCode.Should().BeTrue();
        (await host.PutAsJsonAsync($"api/v1/hosting/reviews/{reviewId}/reply", new ReviewReplyRequest("Thank you again!"))).IsSuccessStatusCode.Should().BeTrue();
        await ProcessOutboxAsync();

        (await NotificationsAsync(guest)).Should().ContainSingle(n => n.Type == NotificationTypes.ReviewReplyReceived)
            .Which.Link.Should().Be($"/apartments/{listingId}#reviews");

        // A paid payout.
        (HttpClient payer, Guid payerId, Guid bookingId) = await BookAsGuestAsync(listingId, NextFreeDate(), 2);
        await PayAsync(payer, payerId, bookingId);
        await ExecuteSqlAsync($"UPDATE payouts SET scheduled_for = CURRENT_DATE - 1 WHERE booking_id = {bookingId}");
        (await host.PostAsync("api/v1/hosting/payouts/onboarding", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        await ProcessOutboxAsync();

        NotificationResponse payout = (await NotificationsAsync(host)).Should().ContainSingle(n => n.Type == NotificationTypes.PayoutPaid).Subject;
        payout.Data.GetProperty("bookingId").GetGuid().Should().Be(bookingId);
        payout.Data.GetProperty("amount").GetDecimal().Should().BeGreaterThan(0);
        payout.Link.Should().Be("/hosting/payouts");
    }

    [Fact]
    public async Task Notifications_Should_BeIdempotent_PerSourceEvent()
    {
        (HttpClient host, _, Guid listingId) = await CreateHostWithListingAsync(listing => listing with { InstantBook = false });
        (_, _, Guid bookingId) = await BookAsGuestAsync(listingId, NextFreeDate(), 2);

        // The same event delivered three times (e.g. redelivered by the broker).
        for (int delivery = 0; delivery < 3; delivery++)
        {
            await RunAsync<IUserNotifier, bool>(async notifier =>
            {
                await notifier.BookingRequestedAsync(bookingId);
                return true;
            });
        }

        await ProcessOutboxAsync();

        (await NotificationsAsync(host)).Should().ContainSingle(n => n.Type == NotificationTypes.BookingRequested);
    }

    [Fact]
    public async Task Notifications_Should_BeMarkedRead_OneByOneOrAll_AndBePrivate()
    {
        (HttpClient host, _, Guid listingId) = await CreateHostWithListingAsync(listing => listing with { InstantBook = false });
        (HttpClient guest, _, _) = await BookAsGuestAsync(listingId, NextFreeDate(), 2);
        await BookAsGuestAsync(listingId, NextFreeDate(), 2);
        await BookAsGuestAsync(listingId, NextFreeDate(), 2);
        await ProcessOutboxAsync();

        (await host.GetFromJsonAsync<UnreadCountResponse>("api/v1/notifications/unread-count"))!.Count.Should().Be(3);

        List<NotificationResponse> notifications = await NotificationsAsync(host);
        Guid first = notifications[0].Id;

        await ShouldFailWithAsync(await guest.PutAsync($"api/v1/notifications/{first}/read", null), HttpStatusCode.NotFound, "Notification.NotFound");

        (await host.PutAsync($"api/v1/notifications/{first}/read", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await host.GetFromJsonAsync<UnreadCountResponse>("api/v1/notifications/unread-count"))!.Count.Should().Be(2);

        PagedList<NotificationResponse>? unread = await host.GetFromJsonAsync<PagedList<NotificationResponse>>(
            "api/v1/notifications?unreadOnly=true&page=1&pageSize=1");
        unread!.TotalCount.Should().Be(2);
        unread.Items.Should().ContainSingle().Which.Id.Should().NotBe(first);

        (await host.GetFromJsonAsync<PagedList<NotificationResponse>>("api/v1/notifications"))!.Items
            .Single(n => n.Id == first).ReadOnUtc.Should().NotBeNull();

        (await host.PutAsync("api/v1/notifications/read-all", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await host.GetFromJsonAsync<UnreadCountResponse>("api/v1/notifications/unread-count"))!.Count.Should().Be(0);
        (await host.GetFromJsonAsync<PagedList<NotificationResponse>>("api/v1/notifications?unreadOnly=true"))!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Preferences_Should_DefaultToAllOn_BeSaved_AndBePrivate()
    {
        (HttpClient user, Guid userId) = await CreateAuthenticatedUserAsync();
        (HttpClient other, _) = await CreateAuthenticatedUserAsync();

        NotificationPreferencesDto? defaults = await user.GetFromJsonAsync<NotificationPreferencesDto>(
            $"api/v1/users/{userId}/notification-preferences");
        defaults.Should().BeEquivalentTo(new NotificationPreferencesDto(AllOn, AllOn));

        var updated = new NotificationPreferencesDto(
            new NotificationChannelDto(Messages: false, Reservations: true, Reviews: false, Payouts: true, Reminders: false),
            new NotificationChannelDto(Messages: true, Reservations: false, Reviews: true, Payouts: true, Reminders: true));

        HttpResponseMessage response = await user.PutAsJsonAsync($"api/v1/users/{userId}/notification-preferences", updated);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await response.Content.ReadFromJsonAsync<NotificationPreferencesDto>()).Should().BeEquivalentTo(updated);

        (await user.GetFromJsonAsync<NotificationPreferencesDto>($"api/v1/users/{userId}/notification-preferences"))
            .Should().BeEquivalentTo(updated);

        await ShouldFailWithAsync(
            await other.GetAsync($"api/v1/users/{userId}/notification-preferences"),
            HttpStatusCode.Forbidden,
            "User.InvalidPermissions");
        await ShouldFailWithAsync(
            await other.PutAsJsonAsync($"api/v1/users/{userId}/notification-preferences", updated),
            HttpStatusCode.Forbidden,
            "User.InvalidPermissions");

        // The raw JSON uses the documented camelCase keys.
        using JsonDocument json = JsonDocument.Parse(await user.GetStringAsync($"api/v1/users/{userId}/notification-preferences"));
        json.RootElement.GetProperty("email").GetProperty("reminders").GetBoolean().Should().BeFalse();
        json.RootElement.GetProperty("inApp").GetProperty("reservations").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Preferences_Should_BeHonoured_ForEmails_AndInAppNotifications()
    {
        (HttpClient host, Guid hostId, Guid listingId) = await CreateHostWithListingAsync();

        // The guest turns off reservation e-mails; the host turns off in-app reservation notifications.
        (HttpClient guest, Guid guestId, string guestEmail) = await RegisterAsync();
        await SetPreferencesAsync(guest, guestId, email: AllOn with { Reservations = false }, inApp: AllOn);
        await SetPreferencesAsync(host, hostId, email: AllOn, inApp: AllOn with { Reservations = false });

        HttpResponseMessage booked = await PostBookingAsync(guest, guestId, listingId, NextFreeDate(), 2);
        booked.StatusCode.Should().Be(HttpStatusCode.OK);
        Guid bookingId = await booked.Content.ReadFromJsonAsync<Guid>();

        await PayAsync(guest, guestId, bookingId);
        await ProcessOutboxAsync();

        Factory.Emails.Sent<BookingReservedEmail>().Should().NotContain(e => e.EmailTo == guestEmail);
        Factory.Emails.Sent<BookingConfirmedEmail>().Should().NotContain(e => e.EmailTo == guestEmail);

        // The guest still gets in-app notifications; the host's are hidden.
        (await NotificationsAsync(guest)).Should().Contain(n => n.Type == NotificationTypes.BookingConfirmed);
        (await NotificationsAsync(host)).Should().BeEmpty();

        // Security e-mails ignore the preferences: the password reset e-mail is still sent.
        await SetPreferencesAsync(guest, guestId, email: new NotificationChannelDto(false, false, false, false, false), inApp: AllOn);
        (await HttpClient.PostAsJsonAsync("api/v1/authentication/forgot-password", new Lodge.Contracts.Authentication.ForgotPasswordRequest(guestEmail)))
            .IsSuccessStatusCode.Should().BeTrue();
        Factory.Emails.PasswordResetEmails(guestEmail).Should().NotBeEmpty();
    }

    [Fact]
    public async Task NewMessageEmails_Should_BeThrottledPerConversation_AndRespectThePreferences()
    {
        (HttpClient host, Guid hostId, Guid listingId) = await CreateHostWithListingAsync();
        string hostEmail = await EmailOfAsync(host);
        (HttpClient guest, Guid guestId, string guestEmail) = await RegisterAsync();

        HttpResponseMessage started = await guest.PostAsJsonAsync("api/v1/conversations", new StartConversationRequest(listingId, "Hello host!"));
        Guid conversationId = (await started.Content.ReadFromJsonAsync<SentMessageResponse>())!.ConversationId;
        await guest.PostAsJsonAsync($"api/v1/conversations/{conversationId}/messages", new SendMessageRequest("Are you there?"));
        await guest.PostAsJsonAsync($"api/v1/conversations/{conversationId}/messages", new SendMessageRequest("One more question."));
        await ProcessOutboxAsync();

        // Three messages within 15 minutes: one e-mail.
        NewMessageEmail email = Factory.Emails.Sent<NewMessageEmail>().Should().ContainSingle(e => e.EmailTo == hostEmail).Subject;
        email.SenderFirstName.Should().Be("First");
        email.Preview.Should().Be("Hello host!");
        email.Link.Should().Be($"{FunctionalTestWebAppFactory.FrontendBaseUrl}/messages/{conversationId}");

        // The guest turned off message e-mails: the host's reply sends none.
        await SetPreferencesAsync(guest, guestId, email: AllOn with { Messages = false }, inApp: AllOn);
        await host.PostAsJsonAsync($"api/v1/conversations/{conversationId}/messages", new SendMessageRequest("Hi! Yes."));
        await ProcessOutboxAsync();

        Factory.Emails.Sent<NewMessageEmail>().Should().NotContain(e => e.EmailTo == guestEmail);
        hostId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ReminderJob_Should_SendTripArrivalAndReviewReminders_Once()
    {
        (HttpClient host, Guid hostId, Guid listingId) = await CreateHostWithListingAsync();
        string hostEmail = await EmailOfAsync(host);
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);

        // A paid stay starting in two days (trip reminder) and one starting tomorrow (arrival reminder).
        (HttpClient traveller, Guid travellerId, string travellerEmail) = await RegisterAsync();
        Guid tripId = await BookAndPayAsync(traveller, travellerId, listingId, today.AddDays(2), 2);

        (HttpClient arriving, Guid arrivingId) = await CreateAuthenticatedUserAsync();
        Guid arrivalId = await BookAndPayAsync(arriving, arrivingId, listingId, today.AddDays(1), 1);

        // A stay that checked out yesterday, not reviewed by either side yet.
        (HttpClient reviewer, Guid reviewerId, string reviewerEmail) = await RegisterAsync();
        Guid stayId = await CreateCompletedStayAsync(listingId, reviewerId, checkedOutDaysAgo: 1);
        await ProcessOutboxAsync();

        int created = await RunAsync<ISendRemindersJob, int>(job => job.SendAsync());
        created.Should().BeGreaterThanOrEqualTo(4);
        (await RunAsync<ISendRemindersJob, int>(job => job.SendAsync())).Should().Be(0);

        (await NotificationsAsync(traveller)).Should().ContainSingle(n => n.Type == NotificationTypes.TripReminder)
            .Which.Link.Should().Be($"/bookings?bookingId={tripId}");
        Factory.Emails.Sent<TripReminderEmail>().Should().ContainSingle(e => e.EmailTo == travellerEmail)
            .Which.CheckIn.Should().Be(today.AddDays(2));

        List<NotificationResponse> hostNotifications = await NotificationsAsync(host);
        hostNotifications.Should().ContainSingle(n => n.Type == NotificationTypes.ArrivalReminder)
            .Which.Data.GetProperty("bookingId").GetGuid().Should().Be(arrivalId);
        hostNotifications.Should().ContainSingle(n => n.Type == NotificationTypes.ReviewReminder)
            .Which.Data.GetProperty("role").GetString().Should().Be("Host");

        NotificationResponse guestReminder = (await NotificationsAsync(reviewer))
            .Should().ContainSingle(n => n.Type == NotificationTypes.ReviewReminder).Subject;
        guestReminder.Data.GetProperty("role").GetString().Should().Be("Guest");
        guestReminder.Data.GetProperty("bookingId").GetGuid().Should().Be(stayId);

        Factory.Emails.Sent<ReviewReminderEmail>().Should().ContainSingle(e => e.EmailTo == reviewerEmail && !e.ForHost);
        Factory.Emails.Sent<ReviewReminderEmail>().Should().ContainSingle(e => e.EmailTo == hostEmail && e.ForHost);

        // Turning reminder e-mails off stops the e-mails (not the in-app reminders).
        (HttpClient quiet, Guid quietId, string quietEmail) = await RegisterAsync();
        await SetPreferencesAsync(quiet, quietId, email: AllOn with { Reminders = false }, inApp: AllOn);
        (_, _, Guid otherListingId) = await CreateHostWithListingAsync();
        await BookAndPayAsync(quiet, quietId, otherListingId, today.AddDays(2), 1);
        await RunAsync<ISendRemindersJob, int>(job => job.SendAsync());

        (await NotificationsAsync(quiet)).Should().Contain(n => n.Type == NotificationTypes.TripReminder);
        Factory.Emails.Sent<TripReminderEmail>().Should().NotContain(e => e.EmailTo == quietEmail);
        hostId.Should().NotBeEmpty();
    }

    private async Task<Guid> BookAndPayAsync(HttpClient guest, Guid guestId, Guid listingId, DateOnly start, int nights)
    {
        HttpResponseMessage response = await PostBookingAsync(guest, guestId, listingId, start, nights);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        Guid bookingId = await response.Content.ReadFromJsonAsync<Guid>();

        await PayAsync(guest, guestId, bookingId);

        return bookingId;
    }

    private static async Task<string> EmailOfAsync(HttpClient client)
    {
        using JsonDocument me = JsonDocument.Parse(await client.GetStringAsync("api/v1/users/auth"));

        return me.RootElement.GetProperty("email").GetString()!;
    }

    private static async Task SetPreferencesAsync(
        HttpClient client,
        Guid userId,
        NotificationChannelDto email,
        NotificationChannelDto inApp)
    {
        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"api/v1/users/{userId}/notification-preferences",
            new NotificationPreferencesDto(email, inApp));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    internal static async Task<List<NotificationResponse>> NotificationsAsync(HttpClient client) =>
        [.. (await client.GetFromJsonAsync<PagedList<NotificationResponse>>("api/v1/notifications?pageSize=100"))!.Items];
}
