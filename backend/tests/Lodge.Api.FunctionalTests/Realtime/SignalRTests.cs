using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Lodge.Api.FunctionalTests.Abstractions;
using Lodge.Api.FunctionalTests.Extensions;
using Lodge.Contracts.Authentication;
using Lodge.Contracts.Messaging;
using Lodge.Contracts.Notifications;
using Lodge.Domain.Notifications;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Lodge.Api.FunctionalTests.Realtime;

public sealed class SignalRTests(FunctionalTestWebAppFactory factory) : HostingFunctionalTest(factory)
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Messages_Should_BeDelivered_ToBothParticipants_AndNobodyElse()
    {
        (HttpClient host, string hostToken, Guid listingId) = await CreateHostAsync();
        (HttpClient guest, string guestToken) = await CreateUserWithTokenAsync();
        (_, string strangerToken) = await CreateUserWithTokenAsync();

        await using HubClient hostHub = await ConnectAsync(hostToken, HttpTransportType.LongPolling);
        await using HubClient guestHub = await ConnectAsync(guestToken, HttpTransportType.WebSockets);
        await using HubClient strangerHub = await ConnectAsync(strangerToken, HttpTransportType.LongPolling);

        // Every connection gets its unread counters right away.
        (await hostHub.UnreadCounts.WaitAsync(_ => true)).Should().Be(new UnreadCountsResponse(0, 0));

        HttpResponseMessage response = await guest.PostAsJsonAsync(
            "api/v1/conversations",
            new StartConversationRequest(listingId, "Is parking included?"));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        SentMessageResponse sent = (await response.Content.ReadFromJsonAsync<SentMessageResponse>())!;

        MessageReceivedPayload received = await hostHub.Messages.WaitAsync(m => m.Message.Id == sent.Message.Id);
        received.ConversationId.Should().Be(sent.ConversationId);
        received.Message.Body.Should().Be("Is parking included?");
        received.Message.Type.Should().Be("User");

        // The author's other tabs get it too, and the conversation lists are refreshed.
        (await guestHub.Messages.WaitAsync(m => m.Message.Id == sent.Message.Id)).ConversationId.Should().Be(sent.ConversationId);
        (await hostHub.ConversationUpdates.WaitAsync(c => c.ConversationId == sent.ConversationId)).Should().NotBeNull();
        (await hostHub.UnreadCounts.WaitAsync(c => c.Messages == 1)).Notifications.Should().Be(0);

        // Reading the conversation pushes new counters.
        (await host.PutAsync($"api/v1/conversations/{sent.ConversationId}/read", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await hostHub.UnreadCounts.WaitAsync(c => c.Messages == 0)).Should().NotBeNull();

        // The stranger heard nothing but their own counters.
        await Task.Delay(500);
        strangerHub.Messages.All.Should().BeEmpty();
        strangerHub.ConversationUpdates.All.Should().BeEmpty();
        strangerHub.Notifications.All.Should().BeEmpty();
    }

    [Fact]
    public async Task Notifications_Should_BePushed_ToTheirRecipientOnly()
    {
        (_, string hostToken, Guid listingId) = await CreateHostAsync(instantBook: false);
        (HttpClient guest, string guestToken) = await CreateUserWithTokenAsync();
        Guid guestId = UserIdOf(guestToken);

        await using HubClient hostHub = await ConnectAsync(hostToken, HttpTransportType.WebSockets);
        await using HubClient guestHub = await ConnectAsync(guestToken, HttpTransportType.LongPolling);

        HttpResponseMessage booked = await PostBookingAsync(guest, guestId, listingId, NextFreeDate(), 2, messageToHost: "Hello!");
        booked.StatusCode.Should().Be(HttpStatusCode.OK, await booked.Content.ReadAsStringAsync());
        Guid bookingId = await booked.Content.ReadFromJsonAsync<Guid>();

        await ProcessOutboxAsync();

        NotificationReceivedPayload pushed = await hostHub.Notifications.WaitAsync(n => n.Notification.Type == NotificationTypes.BookingRequested);
        pushed.Notification.Data.GetProperty("bookingId").GetGuid().Should().Be(bookingId);
        pushed.Notification.Link.Should().Be($"/hosting/reservations?bookingId={bookingId}");
        (await hostHub.UnreadCounts.WaitAsync(c => c.Notifications == 1)).Should().NotBeNull();

        // The request's system message and the guest's message reached the host as messages.
        (await hostHub.Messages.WaitAsync(m => m.Message.Type == "System")).Message.SystemEvent.Should().Be("BookingRequested");
        (await hostHub.Messages.WaitAsync(m => m.Message.Body == "Hello!")).Message.BookingId.Should().Be(bookingId);

        await Task.Delay(300);
        guestHub.Notifications.All.Should().BeEmpty();
    }

    [Fact]
    public async Task Hub_Should_RejectAnonymousConnections()
    {
        HubConnection connection = BuildConnection(accessToken: null, HttpTransportType.LongPolling);

        Func<Task> connect = () => connection.StartAsync();

        await connect.Should().ThrowAsync<HttpRequestException>();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task QueryStringTokens_Should_OnlyBeAccepted_OnTheHubs()
    {
        (_, string token) = await CreateUserWithTokenAsync();

        (await HttpClient.GetAsync($"api/v1/users/auth?access_token={Uri.EscapeDataString(token)}"))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        HttpResponseMessage negotiate = await HttpClient.PostAsync(
            $"hubs/notifications/negotiate?negotiateVersion=1&access_token={Uri.EscapeDataString(token)}",
            null);
        negotiate.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<(HttpClient Client, string Token, Guid ListingId)> CreateHostAsync(bool instantBook = true)
    {
        (HttpClient host, string token) = await CreateUserWithTokenAsync();
        Guid listingId = await CreateListingAsync(host, listing => listing with { InstantBook = instantBook });

        return (host, token, listingId);
    }

    private async Task<(HttpClient Client, string Token)> CreateUserWithTokenAsync()
    {
        (_, _, string email) = await RegisterAsync();

        HttpResponseMessage response = await HttpClient.PostAsJsonAsync("api/v1/authentication/login", new LoginRequest(email, Password));
        response.EnsureSuccessStatusCode();
        string token = (await response.GetTokenAsync()).Token;

        return (Authenticate(token).Client, token);
    }

    private static Guid UserIdOf(string token) =>
        Guid.Parse(new Microsoft.IdentityModel.JsonWebTokens.JsonWebToken(token).GetClaim("userId").Value);

    private async Task<HubClient> ConnectAsync(string accessToken, HttpTransportType transport)
    {
        HubConnection connection = BuildConnection(accessToken, transport);
        var client = new HubClient(connection);

        await connection.StartAsync();

        return client;
    }

    private HubConnection BuildConnection(string? accessToken, HttpTransportType transport) =>
        new HubConnectionBuilder()
            .WithUrl(new Uri(Factory.Server.BaseAddress, "hubs/notifications"), options =>
            {
                options.Transports = transport;
                options.HttpMessageHandlerFactory = _ => Factory.Server.CreateHandler();

                if (accessToken is not null)
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
                }

                // WebSockets in the test server: the token travels in the "access_token" query string, like in browsers.
                options.WebSocketFactory = async (context, cancellationToken) =>
                {
                    Uri uri = context.Uri;

                    if (accessToken is not null && !uri.Query.Contains("access_token=", StringComparison.Ordinal))
                    {
                        string separator = string.IsNullOrEmpty(uri.Query) ? "?" : "&";
                        uri = new Uri($"{uri}{separator}access_token={Uri.EscapeDataString(accessToken)}");
                    }

                    var webSocketClient = Factory.Server.CreateWebSocketClient();
                    return await webSocketClient.ConnectAsync(uri, cancellationToken);
                };
            })
            .Build();

    private sealed record MessageReceivedPayload(Guid ConversationId, MessageResponse Message);

    private sealed record ConversationUpdatedPayload(Guid ConversationId);

    private sealed record NotificationReceivedPayload(NotificationResponse Notification);

    /// <summary>
    /// Records the events a connection receives.
    /// </summary>
    private sealed class HubClient : IAsyncDisposable
    {
        private readonly HubConnection _connection;

        public HubClient(HubConnection connection)
        {
            _connection = connection;
            connection.On<MessageReceivedPayload>("messageReceived", Messages.Add);
            connection.On<ConversationUpdatedPayload>("conversationUpdated", ConversationUpdates.Add);
            connection.On<NotificationReceivedPayload>("notificationReceived", Notifications.Add);
            connection.On<UnreadCountsResponse>("unreadCounts", UnreadCounts.Add);
        }

        public Recorder<MessageReceivedPayload> Messages { get; } = new();

        public Recorder<ConversationUpdatedPayload> ConversationUpdates { get; } = new();

        public Recorder<NotificationReceivedPayload> Notifications { get; } = new();

        public Recorder<UnreadCountsResponse> UnreadCounts { get; } = new();

        public ValueTask DisposeAsync() => _connection.DisposeAsync();
    }

    private sealed class Recorder<T>
    {
        private readonly ConcurrentQueue<T> _items = new();

        public IReadOnlyList<T> All => [.. _items];

        public void Add(T item) => _items.Enqueue(item);

        public async Task<T> WaitAsync(Func<T, bool> predicate)
        {
            DateTime deadline = DateTime.UtcNow + Timeout;

            while (DateTime.UtcNow < deadline)
            {
                T? match = _items.FirstOrDefault(predicate);

                if (match is not null)
                {
                    return match;
                }

                await Task.Delay(50);
            }

            throw new TimeoutException($"No matching {typeof(T).Name} received.");
        }
    }
}
