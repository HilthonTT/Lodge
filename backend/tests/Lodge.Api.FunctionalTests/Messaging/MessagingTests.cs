using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Lodge.Api.FunctionalTests.Abstractions;
using Lodge.Application.Bookings.Expire;
using Lodge.Contracts.Bookings;
using Lodge.Contracts.Common;
using Lodge.Contracts.Messaging;
using Lodge.Domain.Messaging;

namespace Lodge.Api.FunctionalTests.Messaging;

public sealed class MessagingTests(FunctionalTestWebAppFactory factory) : HostingFunctionalTest(factory)
{
    [Fact]
    public async Task Inquiry_Should_StartOrReuseTheConversation_AndOnlyParticipantsCanSeeIt()
    {
        (HttpClient host, Guid hostId, Guid listingId) = await CreateHostWithListingAsync();
        (HttpClient guest, Guid guestId) = await CreateAuthenticatedUserAsync();
        (HttpClient stranger, _) = await CreateAuthenticatedUserAsync();

        SentMessageResponse first = await InquireAsync(guest, listingId, "  Is the flat quiet at night?  ");
        first.Message.Body.Should().Be("Is the flat quiet at night?");
        first.Message.Type.Should().Be("User");
        first.Message.SenderId.Should().Be(guestId);

        // A second inquiry about the same listing goes to the same conversation.
        SentMessageResponse second = await InquireAsync(guest, listingId, "And is there a lift?");
        second.ConversationId.Should().Be(first.ConversationId);

        Guid conversationId = first.ConversationId;

        ConversationResponse? hostView = await host.GetFromJsonAsync<ConversationResponse>($"api/v1/conversations/{conversationId}");
        hostView!.OtherParticipant.Id.Should().Be(guestId);
        hostView.OtherParticipant.Role.Should().Be("Guest");
        hostView.OtherParticipant.FirstName.Should().Be("First");
        hostView.Apartment.Id.Should().Be(listingId);
        hostView.Apartment.ImageUrl.Should().NotBeNullOrEmpty();
        hostView.LastMessage!.Body.Should().Be("And is there a lift?");
        hostView.LastMessage.Type.Should().Be("User");
        hostView.UnreadCount.Should().Be(2);
        hostView.LatestBooking.Should().BeNull();

        ConversationResponse? guestView = await guest.GetFromJsonAsync<ConversationResponse>($"api/v1/conversations/{conversationId}");
        guestView!.OtherParticipant.Id.Should().Be(hostId);
        guestView.OtherParticipant.Role.Should().Be("Host");
        guestView.UnreadCount.Should().Be(0);

        // Strangers get "not found" everywhere.
        await ShouldFailWithAsync(await stranger.GetAsync($"api/v1/conversations/{conversationId}"), HttpStatusCode.NotFound, "Conversation.NotFound");
        await ShouldFailWithAsync(await stranger.GetAsync($"api/v1/conversations/{conversationId}/messages"), HttpStatusCode.NotFound, "Conversation.NotFound");
        await ShouldFailWithAsync(
            await stranger.PostAsJsonAsync($"api/v1/conversations/{conversationId}/messages", new SendMessageRequest("Hi")),
            HttpStatusCode.NotFound,
            "Conversation.NotFound");
        await ShouldFailWithAsync(await stranger.PutAsync($"api/v1/conversations/{conversationId}/read", null), HttpStatusCode.NotFound, "Conversation.NotFound");
        (await stranger.GetFromJsonAsync<PagedList<ConversationResponse>>("api/v1/conversations"))!.Items
            .Should().NotContain(c => c.Id == conversationId);

        // Anonymous callers are rejected.
        (await HttpClient.GetAsync("api/v1/conversations")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Messages_Should_BeValidated_AndInquiriesAboutOwnListingsRejected()
    {
        (HttpClient host, _, Guid listingId) = await CreateHostWithListingAsync();
        (HttpClient guest, _) = await CreateAuthenticatedUserAsync();

        await ShouldFailWithAsync(
            await host.PostAsJsonAsync("api/v1/conversations", new StartConversationRequest(listingId, "Hello me")),
            HttpStatusCode.BadRequest,
            "Conversation.OwnListing");

        await ShouldFailWithAsync(
            await guest.PostAsJsonAsync("api/v1/conversations", new StartConversationRequest(listingId, "   ")),
            HttpStatusCode.BadRequest,
            "Message.Empty");

        await ShouldFailWithAsync(
            await guest.PostAsJsonAsync("api/v1/conversations", new StartConversationRequest(listingId, new string('a', 2001))),
            HttpStatusCode.BadRequest,
            "Message.TooLong");

        await ShouldFailWithAsync(
            await guest.PostAsJsonAsync("api/v1/conversations", new StartConversationRequest(Guid.NewGuid(), "Hello")),
            HttpStatusCode.NotFound,
            "Apartment.NotFound");

        SentMessageResponse sent = await InquireAsync(guest, listingId, "Hello");

        await ShouldFailWithAsync(
            await host.PostAsJsonAsync($"api/v1/conversations/{sent.ConversationId}/messages", new SendMessageRequest("")),
            HttpStatusCode.BadRequest,
            "Message.Empty");
    }

    [Fact]
    public async Task Messages_Should_BePaginatedWithACursor_NewestPageFirst_InChronologicalOrder()
    {
        (HttpClient host, _, Guid listingId) = await CreateHostWithListingAsync();
        (HttpClient guest, _) = await CreateAuthenticatedUserAsync();

        SentMessageResponse first = await InquireAsync(guest, listingId, "Message 1");
        Guid conversationId = first.ConversationId;

        for (int number = 2; number <= 35; number++)
        {
            HttpClient author = number % 2 == 0 ? host : guest;
            HttpResponseMessage response = await author.PostAsJsonAsync(
                $"api/v1/conversations/{conversationId}/messages",
                new SendMessageRequest($"Message {number}"));
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        MessagePageResponse? latest = await guest.GetFromJsonAsync<MessagePageResponse>(
            $"api/v1/conversations/{conversationId}/messages");

        latest!.Items.Should().HaveCount(30);
        latest.Items.Select(m => m.Body).Should().Equal(Enumerable.Range(6, 30).Select(n => $"Message {n}"));
        latest.HasMore.Should().BeTrue();
        latest.NextBefore.Should().Be(latest.Items[0].Id);

        MessagePageResponse? older = await guest.GetFromJsonAsync<MessagePageResponse>(
            $"api/v1/conversations/{conversationId}/messages?before={latest.NextBefore}&limit=30");

        older!.Items.Select(m => m.Body).Should().Equal(Enumerable.Range(1, 5).Select(n => $"Message {n}"));
        older.HasMore.Should().BeFalse();
        older.NextBefore.Should().BeNull();

        // A date cursor and a custom limit.
        string before = Uri.EscapeDataString(latest.Items[0].SentOnUtc.ToString("O", CultureInfo.InvariantCulture));
        MessagePageResponse? byDate = await guest.GetFromJsonAsync<MessagePageResponse>(
            $"api/v1/conversations/{conversationId}/messages?before={before}&limit=2");

        byDate!.Items.Select(m => m.Body).Should().Equal("Message 4", "Message 5");
        byDate.HasMore.Should().BeTrue();

        await ShouldFailWithAsync(
            await guest.GetAsync($"api/v1/conversations/{conversationId}/messages?before=yesterday"),
            HttpStatusCode.BadRequest,
            "Message.InvalidCursor");
        await ShouldFailWithAsync(
            await guest.GetAsync($"api/v1/conversations/{conversationId}/messages?limit=101"),
            HttpStatusCode.BadRequest,
            "Message.InvalidLimit");
    }

    [Fact]
    public async Task ReadMarkers_Should_DriveTheUnreadCounts_AndTheInbox_ShouldBeNewestFirst()
    {
        (HttpClient host, _, Guid listingId) = await CreateHostWithListingAsync();
        Guid otherListingId = await CreateListingAsync(host);
        (HttpClient guest, _) = await CreateAuthenticatedUserAsync();

        SentMessageResponse older = await InquireAsync(guest, listingId, "First listing?");
        SentMessageResponse newer = await InquireAsync(guest, otherListingId, "Second listing?");
        await guest.PostAsJsonAsync($"api/v1/conversations/{newer.ConversationId}/messages", new SendMessageRequest("Hello again"));

        (await host.GetFromJsonAsync<UnreadCountResponse>("api/v1/conversations/unread-count"))!.Count.Should().Be(3);

        PagedList<ConversationResponse>? inbox = await host.GetFromJsonAsync<PagedList<ConversationResponse>>(
            "api/v1/conversations?page=1&pageSize=10");
        inbox!.Items.Select(c => c.Id).Should().Equal(newer.ConversationId, older.ConversationId);
        inbox.Items[0].UnreadCount.Should().Be(2);

        (await host.PutAsync($"api/v1/conversations/{newer.ConversationId}/read", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await host.GetFromJsonAsync<UnreadCountResponse>("api/v1/conversations/unread-count"))!.Count.Should().Be(1);

        // Replying marks the thread as read for the author and makes it unread for the other side.
        await host.PostAsJsonAsync($"api/v1/conversations/{older.ConversationId}/messages", new SendMessageRequest("Yes!"));
        (await host.GetFromJsonAsync<UnreadCountResponse>("api/v1/conversations/unread-count"))!.Count.Should().Be(0);
        (await guest.GetFromJsonAsync<UnreadCountResponse>("api/v1/conversations/unread-count"))!.Count.Should().Be(1);

        PagedList<ConversationResponse>? guestInbox = await guest.GetFromJsonAsync<PagedList<ConversationResponse>>(
            "api/v1/conversations?pageSize=1");
        guestInbox!.Items.Should().ContainSingle().Which.Id.Should().Be(older.ConversationId);
        guestInbox.TotalCount.Should().Be(2);
        guestInbox.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task Bookings_Should_PostSystemMessages_OnEveryTransition()
    {
        (HttpClient host, _, Guid listingId) = await CreateHostWithListingAsync(listing => listing with { InstantBook = false });
        DateOnly start = NextFreeDate();

        (HttpClient guest, Guid guestId, Guid bookingId) =
            await BookAsGuestAsync(listingId, start, 3, "Hi! We are two quiet hikers.");

        // The request opens the conversation right away, with the guest's message.
        ConversationResponse conversation = await SingleConversationAsync(guest);
        conversation.LatestBooking!.Id.Should().Be(bookingId);
        conversation.LatestBooking.Status.Should().Be("PendingApproval");
        conversation.LatestBooking.DurationStart.Should().Be(start);

        List<MessageResponse> messages = await AllMessagesAsync(guest, conversation.Id);
        messages.Select(m => (m.Type, m.SystemEvent)).Should().Equal(
            ("System", SystemEvents.BookingRequested),
            ("User", (string?)null));
        messages[0].SenderId.Should().BeNull();
        messages[0].BookingId.Should().Be(bookingId);
        messages[0].Body.Should().NotBeNullOrWhiteSpace();
        messages[1].Body.Should().Be("Hi! We are two quiet hikers.");
        messages[1].BookingId.Should().Be(bookingId);
        messages[1].SenderId.Should().Be(guestId);

        // Accepted → dates changed → paid → cancelled by the guest.
        (await host.PutAsync($"api/v1/hosting/bookings/{bookingId}/accept", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        await ProcessOutboxAsync();

        (await guest.PutAsJsonAsync($"api/v1/bookings/{bookingId}/dates", new ChangeBookingDatesRequest(start.AddDays(1), start.AddDays(4))))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        await ProcessOutboxAsync();

        await PayAsync(guest, guestId, bookingId);
        await ProcessOutboxAsync();

        (await guest.PutAsJsonAsync($"api/v1/bookings/{bookingId}/cancel", new CancelBookingRequest(guestId)))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        await ProcessOutboxAsync();

        messages = await AllMessagesAsync(host, conversation.Id);
        messages.Where(m => m.Type == "System").Select(m => m.SystemEvent).Should().Equal(
            SystemEvents.BookingRequested,
            SystemEvents.BookingAccepted,
            SystemEvents.BookingDatesChanged,
            SystemEvents.BookingConfirmed,
            SystemEvents.BookingCancelledByGuest);
        messages.Where(m => m.Type == "System").Should().OnlyContain(m => m.BookingId == bookingId && m.SenderId == null);

        // A second booking by the same guest reuses the conversation.
        HttpResponseMessage again = await PostBookingAsync(guest, guestId, listingId, start.AddDays(10), 2);
        again.StatusCode.Should().Be(HttpStatusCode.OK, await again.Content.ReadAsStringAsync());
        Guid secondBookingId = await again.Content.ReadFromJsonAsync<Guid>();

        (await host.PutAsync($"api/v1/hosting/bookings/{secondBookingId}/decline", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        await ProcessOutboxAsync();

        ConversationResponse reused = await SingleConversationAsync(guest);
        reused.Id.Should().Be(conversation.Id);
        reused.LatestBooking!.Id.Should().Be(secondBookingId);
        reused.LatestBooking.Status.Should().Be("Rejected");
        reused.LastMessage!.Type.Should().Be("System");

        (await AllMessagesAsync(guest, conversation.Id)).Last().SystemEvent.Should().Be(SystemEvents.BookingDeclined);
    }

    [Fact]
    public async Task HostCancellations_And_Expiries_Should_PostTheirSystemMessages()
    {
        (HttpClient host, _, Guid listingId) = await CreateHostWithListingAsync();
        DateOnly start = NextFreeDate();
        (HttpClient guest, Guid guestId, Guid bookingId) = await BookAsGuestAsync(listingId, start, 2);

        await PayAsync(guest, guestId, bookingId);
        (await host.PutAsync($"api/v1/hosting/bookings/{bookingId}/cancel", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        await ProcessOutboxAsync();

        ConversationResponse conversation = await SingleConversationAsync(guest);
        (await AllMessagesAsync(guest, conversation.Id)).Select(m => m.SystemEvent).Should().Equal(
            SystemEvents.BookingReserved,
            SystemEvents.BookingConfirmed,
            SystemEvents.BookingCancelledByHost);

        // An unanswered request expires.
        (_, _, Guid requestListingId) = await CreateHostWithListingAsync(listing => listing with { InstantBook = false });
        (HttpClient requester, _, Guid requestId) = await BookAsGuestAsync(requestListingId, NextFreeDate(), 2);
        await ExecuteSqlAsync($"UPDATE bookings SET created_on_utc = now() - interval '3 days' WHERE id = {requestId}");
        (await RunAsync<IExpireBookingRequestsJob, int>(job => job.ExpireAsync())).Should().BeGreaterThanOrEqualTo(1);
        await ProcessOutboxAsync();

        ConversationResponse requestConversation = await SingleConversationAsync(requester);
        (await AllMessagesAsync(requester, requestConversation.Id)).Select(m => m.SystemEvent).Should().Equal(
            SystemEvents.BookingRequested,
            SystemEvents.BookingExpired);
    }

    private static async Task<SentMessageResponse> InquireAsync(HttpClient guest, Guid listingId, string message)
    {
        HttpResponseMessage response = await guest.PostAsJsonAsync(
            "api/v1/conversations",
            new StartConversationRequest(listingId, message));
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<SentMessageResponse>())!;
    }

    private static async Task<ConversationResponse> SingleConversationAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<PagedList<ConversationResponse>>("api/v1/conversations"))!.Items.Should().ContainSingle().Subject;

    private static async Task<List<MessageResponse>> AllMessagesAsync(HttpClient client, Guid conversationId) =>
        [.. (await client.GetFromJsonAsync<MessagePageResponse>($"api/v1/conversations/{conversationId}/messages?limit=100"))!.Items];
}
