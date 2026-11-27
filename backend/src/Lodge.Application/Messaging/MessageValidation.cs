using FluentValidation;
using Lodge.Application.Core.Extensions;
using Lodge.Domain.Messaging;

namespace Lodge.Application.Messaging;

internal static class MessageValidation
{
    /// <summary>
    /// Applies the same rules as <see cref="Message.NormalizeBody"/>: not blank, at most
    /// <see cref="Message.BodyMaxLength"/> characters once trimmed.
    /// </summary>
    public static IRuleBuilderOptions<T, string?> ValidMessageBody<T>(this IRuleBuilder<T, string?> rule) =>
        rule
            .Must(body => !string.IsNullOrWhiteSpace(body)).WithError(MessagingErrors.EmptyMessage)
            .Must(body => body is null || body.Trim().Length <= Message.BodyMaxLength).WithError(MessagingErrors.MessageTooLong);
}
