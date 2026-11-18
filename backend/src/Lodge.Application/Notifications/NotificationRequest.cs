using Lodge.Domain.Notifications;

namespace Lodge.Application.Notifications;

/// <summary>
/// An in-app notification to create.
/// </summary>
/// <param name="UserId">The recipient.</param>
/// <param name="Type">One of <see cref="NotificationTypes"/>.</param>
/// <param name="Data">An object serialized as the JSON <c>data</c> (camelCase property names).</param>
/// <param name="Link">The relative frontend path to open.</param>
/// <param name="DedupeKey">Identifies the source event: a second request with the same key is ignored.</param>
public sealed record NotificationRequest(Guid UserId, string Type, object Data, string Link, string DedupeKey);
