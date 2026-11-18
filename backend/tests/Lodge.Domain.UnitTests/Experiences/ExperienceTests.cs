using Lodge.Domain.Bookings;
using Lodge.Domain.Experiences;
using Lodge.Domain.Experiences.Events;
using Lodge.Domain.Messaging;
using Lodge.Domain.Messaging.Events;

namespace Lodge.Domain.UnitTests.Experiences;

public class ExperienceTests
{
    private static readonly DateTime UtcNow = new(2026, 6, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly Guid HostId = Guid.NewGuid();
    private static readonly Guid GuestId = Guid.NewGuid();

    private static ExperienceDetails Details => new()
    {
        Title = "  Pottery for beginners ",
        Description = "Throw your first bowl.",
        Category = ExperienceCategory.Workshops,
        City = "Lisbon",
        Country = "Portugal",
        Latitude = 38.7,
        Longitude = -9.1,
        MeetingPoint = "Rua Augusta 1",
        DurationMinutes = 120,
        PricePerGuest = 40,
        Currency = "eur",
        MaxGuests = 6,
        Languages = ["EN", "pt", "en"],
        Included = ["Clay", " "],
    };

    [Fact]
    public void Create_Should_NormaliseTheDetails()
    {
        Experience experience = Experience.Create(HostId, Details, []).Value;

        experience.Title.Should().Be("Pottery for beginners");
        experience.Currency.Should().Be("EUR");
        experience.Languages.Should().Equal("en", "pt");
        experience.Included.Should().Equal("Clay");
        experience.IsListed.Should().BeTrue();
    }

    [Theory]
    [InlineData(29)]
    [InlineData(1441)]
    public void Create_Should_RejectDurationsOutOfRange(int minutes) =>
        Experience.Create(HostId, Details with { DurationMinutes = minutes }, []).Error.Should().Be(ExperienceErrors.InvalidDuration);

    [Fact]
    public void Create_Should_RejectInvalidValues()
    {
        Experience.Create(HostId, Details with { MaxGuests = 51 }, []).Error.Should().Be(ExperienceErrors.InvalidMaxGuests);
        Experience.Create(HostId, Details with { Currency = "GBP" }, []).Error.Should().Be(ExperienceErrors.InvalidCurrency);
        Experience.Create(HostId, Details with { Languages = [] }, []).Error.Should().Be(ExperienceErrors.InvalidLanguages);
        Experience.Create(HostId, Details with { Languages = ["eng"] }, []).Error.Should().Be(ExperienceErrors.InvalidLanguages);
        Experience.Create(HostId, Details with { Description = new string('d', 4001) }, []).Error.Should().Be(ExperienceErrors.InvalidDescription);
        Experience.Create(HostId, Details, [.. Enumerable.Range(0, 21).Select(_ => Guid.NewGuid())]).Error.Should().Be(ExperienceErrors.TooManyImages);
    }

    [Fact]
    public void Sessions_Should_HoldAndReleaseSpots_WithinTheirCapacity()
    {
        Experience experience = Experience.Create(HostId, Details, []).Value;

        ExperienceSession.Create(experience, UtcNow.AddDays(1), 7, UtcNow).Error.Should().Be(ExperienceSessionErrors.InvalidCapacity);
        ExperienceSession.Create(experience, UtcNow.AddHours(-1), 3, UtcNow).Error.Should().Be(ExperienceSessionErrors.StartsInPast);

        ExperienceSession session = ExperienceSession.Create(experience, UtcNow.AddDays(1).AddSeconds(42), 3, UtcNow).Value;
        session.StartsAtUtc.Second.Should().Be(0);

        session.Hold(2, UtcNow).IsSuccess.Should().BeTrue();
        session.Hold(2, UtcNow).Error.Should().Be(ExperienceSessionErrors.NotEnoughSpots);
        session.SpotsLeft.Should().Be(1);

        session.Release(2);
        session.SpotsLeft.Should().Be(3);

        session.Cancel(UtcNow);
        session.Hold(1, UtcNow).Error.Should().Be(ExperienceSessionErrors.Cancelled);
    }

    [Fact]
    public void Reserve_Should_PriceTheSpots_WithTheServiceFee_AndRejectTheHost()
    {
        Experience experience = Experience.Create(HostId, Details, []).Value;
        ExperienceSession session = ExperienceSession.Create(experience, UtcNow.AddDays(3), 6, UtcNow).Value;

        ExperienceBooking.Reserve(experience, session, HostId, 1, 12, UtcNow).Error.Should().Be(ExperienceBookingErrors.OwnExperience);
        ExperienceBooking.Reserve(experience, session, GuestId, 0, 12, UtcNow).Error.Should().Be(ExperienceBookingErrors.InvalidGuestCount);

        ExperienceBooking booking = ExperienceBooking.Reserve(experience, session, GuestId, 3, 12.5m, UtcNow).Value;

        booking.PriceTotal.Should().Be(120);
        booking.ServiceFee.Should().Be(15);
        booking.TotalPrice.Should().Be(135);
        booking.Currency.Should().Be("EUR");
        booking.Status.Should().Be(ExperienceBookingStatus.Reserved);
        session.BookedGuests.Should().Be(3);
    }

    [Theory]
    [InlineData(48, false, 135)]
    [InlineData(24, false, 135)]
    [InlineData(23, false, 0)]
    [InlineData(2, true, 135)]
    public void Cancel_Should_RefundInFull_AtLeast24HoursBefore_OrWhenTheHostCancels(int hoursBefore, bool byHost, decimal refund)
    {
        Experience experience = Experience.Create(HostId, Details, []).Value;
        DateTime startsAt = UtcNow.AddDays(10);
        ExperienceSession session = ExperienceSession.Create(experience, startsAt, 6, UtcNow).Value;
        ExperienceBooking booking = ExperienceBooking.Reserve(experience, session, GuestId, 3, 12.5m, UtcNow).Value;
        booking.ConfirmPayment("sim_pi_1", 135, UtcNow).IsSuccess.Should().BeTrue();
        booking.ClearDomainEvents();

        booking.Cancel(session, startsAt.AddHours(-hoursBefore), byHost).Value.Should().Be(refund);

        booking.Status.Should().Be(ExperienceBookingStatus.Cancelled);
        booking.RefundAmount.Should().Be(refund);
        booking.RefundStatus.Should().Be(refund > 0 ? RefundStatus.Pending : RefundStatus.None);
        booking.CancelledByHost.Should().Be(byHost);
        session.BookedGuests.Should().Be(0);
        booking.DomainEvents.Should().Contain(e => e is ExperienceBookingCancelledDomainEvent);
        booking.DomainEvents.Any(e => e is ExperienceBookingRefundRequestedDomainEvent).Should().Be(refund > 0);
    }

    [Fact]
    public void Guests_Should_NotCancelStartedSessions_AndCompleteAfterTheEnd()
    {
        Experience experience = Experience.Create(HostId, Details, []).Value;
        DateTime startsAt = UtcNow.AddDays(1);
        ExperienceSession session = ExperienceSession.Create(experience, startsAt, 6, UtcNow).Value;
        ExperienceBooking booking = ExperienceBooking.Reserve(experience, session, GuestId, 1, 0, UtcNow).Value;
        booking.ConfirmPayment("sim_pi_1", 40, UtcNow);

        booking.Cancel(session, startsAt.AddMinutes(5), byHost: false).Error.Should().Be(ExperienceBookingErrors.NotCancellable);
        booking.Complete(startsAt.AddMinutes(60), 120).Error.Should().Be(ExperienceBookingErrors.NotCompleted);
        booking.Complete(startsAt.AddMinutes(121), 120).IsSuccess.Should().BeTrue();

        ExperienceReview.Create(booking, 5, "Lovely", UtcNow).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Conversations_Should_TrimMessages_AndTrackReadMarkersAndEmailThrottling()
    {
        Conversation.Start(Guid.NewGuid(), HostId, HostId, UtcNow).Error.Should().Be(MessagingErrors.OwnListing);

        Conversation conversation = Conversation.Start(Guid.NewGuid(), GuestId, HostId, UtcNow).Value;

        conversation.Send(Guid.NewGuid(), "Hi", UtcNow).Error.Code.Should().Be("Conversation.NotFound");
        conversation.Send(GuestId, "   ", UtcNow).Error.Should().Be(MessagingErrors.EmptyMessage);
        conversation.Send(GuestId, new string('x', 2001), UtcNow).Error.Should().Be(MessagingErrors.MessageTooLong);

        Message message = conversation.Send(GuestId, "  Hello!  ", UtcNow.AddMinutes(1)).Value;

        message.Body.Should().Be("Hello!");
        message.Type.Should().Be(MessageType.User);
        message.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<MessageSentDomainEvent>();
        conversation.LastMessageOnUtc.Should().Be(UtcNow.AddMinutes(1));
        conversation.LastReadOnUtc(GuestId).Should().Be(UtcNow.AddMinutes(1));
        conversation.LastReadOnUtc(HostId).Should().BeNull();
        conversation.OtherParticipant(GuestId).Should().Be(HostId);

        Message system = conversation.Post(SystemEvents.BookingConfirmed, Guid.NewGuid(), "Confirmed.", UtcNow.AddMinutes(2));
        system.SenderId.Should().BeNull();
        system.Type.Should().Be(MessageType.System);
        system.DomainEvents.Should().BeEmpty();

        TimeSpan throttle = TimeSpan.FromMinutes(15);
        conversation.TryRecordEmail(HostId, UtcNow, throttle).Should().BeTrue();
        conversation.TryRecordEmail(HostId, UtcNow.AddMinutes(14), throttle).Should().BeFalse();
        conversation.TryRecordEmail(GuestId, UtcNow.AddMinutes(14), throttle).Should().BeTrue();
        conversation.TryRecordEmail(HostId, UtcNow.AddMinutes(15), throttle).Should().BeTrue();
    }
}
