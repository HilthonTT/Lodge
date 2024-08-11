using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Core.Errors;

/// <summary>
/// Contains the validation errors.
/// </summary>
internal static class ValidationErrors
{
    /// <summary>
    /// Contains the create user errors.
    /// </summary>
    internal static class CreateUser
    {
        internal static readonly Error FirstNameIsRequired = Error.Validation(
            "CreateUser.FirstNameIsRequired", "The first name is required");

        internal static readonly Error LastNameIsRequired = Error.Validation(
            "CreateUser.LastNameIsRequired", "The last name is required");

        internal static readonly Error EmailIsRequired = Error.Validation(
            "CreateUser.EmailIsRequired", "The email is required");

        internal static readonly Error PasswordIsRequired = Error.Validation(
            "CreateUser.PasswordIsRequired", "The password is required");

        internal static readonly Error EmailMustBeARealEmail = Error.Validation(
            "CreateUser.EmailMustBeAnEmail", "The email must be a real email");
    }

    /// <summary>
    /// Contains the change password errors.
    /// </summary>
    internal static class ChangePassword
    {
        internal static readonly Error UserIdIsRequired = Error.Validation(
            "ChangePassword.UserIdIsRequired", "The user identifier is required");

        internal static readonly Error PasswordIsRequired = Error.Validation(
            "ChangePassword.PasswordIsRequired", "The password is required");
    }

    /// <summary>
    /// Contains the update user errors.
    /// </summary>
    internal static class UpdateUser
    {
        internal static readonly Error UserIdIsRequired = Error.Validation(
            "UpdateUser.UserIdIsRequired", "The user identifier is required");

        internal static readonly Error FirstNameIsRequired = Error.Validation(
            "UpdateUser.FirstNameIsRequired", "The first name is required");

        internal static readonly Error LastNameIsRequired = Error.Validation(
            "UpdateUser.LastNameIsRequired", "The last name is required");
    }

    /// <summary>
    /// Contains the login errors.
    /// </summary>
    internal static class Login
    {
        internal static readonly Error EmailIsRequired = Error.Validation(
           "Login.EmailIsRequired", "The email is required");

        internal static readonly Error PasswordIsRequired = Error.Validation(
            "Login.PasswordIsRequired", "The password is required");

        internal static readonly Error EmailMustBeARealEmail = Error.Validation(
            "Login.EmailMustBeAnEmail", "The email must be a real email");
    }

    /// <summary>
    /// Contains the reserve booking errors.
    /// </summary>
    internal static class ReserveBooking
    {
        internal static readonly Error UserIdIsRequired = Error.Validation(
           "ReserveBooking.UserIdIsRequired", "The user identifier is required");

        internal static readonly Error ApartmentIdIsRequired = Error.Validation(
           "ReserveBooking.ApartmentIdIsRequired", "The apartment identifier is required");

        internal static readonly Error StartDateMustBeLessThanEndDate = Error.Validation(
            "ReserveBooking.StartDateMustBeLessThanEndDate",
            "The start date must be less than the end date");
    }

    /// <summary>
    /// Contains the confirm booking errors.
    /// </summary>
    internal static class ConfirmBooking
    {
        internal static readonly Error UserIdIsRequired = Error.Validation(
           "ConfirmBooking.UserIdIsRequired", "The user identifier is required");

        internal static readonly Error BookingIdIsRequired = Error.Validation(
           "ConfirmBooking.BookingIdIsRequired", "The booking identifier is required");
    }

    /// <summary>
    /// Contains the reject booking errors.
    /// </summary>
    internal static class RejectBooking
    {
        internal static readonly Error UserIdIsRequired = Error.Validation(
           "RejectBooking.UserIdIsRequired", "The user identifier is required");

        internal static readonly Error BookingIdIsRequired = Error.Validation(
           "RejectBooking.BookingIdIsRequired", "The booking identifier is required");
    }

    /// <summary>
    /// Contains the confirm booking errors.
    /// </summary>
    internal static class CompleteBooking
    {
        internal static readonly Error UserIdIsRequired = Error.Validation(
           "CompleteBooking.UserIdIsRequired", "The user identifier is required");

        internal static readonly Error BookingIdIsRequired = Error.Validation(
           "CompleteBooking.BookingIdIsRequired", "The booking identifier is required");
    }

    /// <summary>
    /// Contains the cancel booking errors.
    /// </summary>
    internal static class CancelBooking
    {
        internal static readonly Error UserIdIsRequired = Error.Validation(
           "CancelBooking.UserIdIsRequired", "The user identifier is required");

        internal static readonly Error BookingIdIsRequired = Error.Validation(
           "CancelBooking.BookingIdIsRequired", "The booking identifier is required");
    }

    /// <summary>
    /// Contains the upload file errors.
    /// </summary>
    internal static class UploadFile
    {
        internal static readonly Error UserIdIsRequired = Error.Validation(
           "UploadFile.UserIdIsRequired", "The user identifier is required");

        internal static readonly Error ContentTypeIsRequired = Error.Validation(
           "UploadFile.ContentTypeIsRequired", "The content type is required");

        internal static readonly Error InvalidContentType = Error.Validation(
            "UploadFile.InvalidContentType", "The content type must be an image");
    }

    /// <summary>
    /// Contains the create review errors.
    /// </summary>
    internal static class CreateReview
    {
        internal static readonly Error UserIdIsRequired = Error.Validation(
           "CreateReview.UserIdIsRequired", "The user identifier is required");

        internal static readonly Error BookingIdIsRequired = Error.Validation(
            "CreateReview.UserIdIsRequired", "The booking identifier is required");

        internal static readonly Error RatingMustBeBelowFive = Error.Validation(
            "CreateReview.RatingMustBeBelowFive",
            "Your rating must not be above 5");

        internal static readonly Error RatingMustBeGreaterOrEqualToZero = Error.Validation(
            "CreateReview.RatingMustBeGreaterOrEqualToZero",
            "Your rating must be greater than or equal to 0");

        internal static readonly Error CommentIsRequired = Error.Validation(
            "CreateReview.CommentIsRequired", "The comment is required");

        internal static Error CommentMustNotBeAbove(int limit) => Error.Validation(
            "CreateReview.CommentMustNotBeAbove", $"The comment must not be above {limit} characters");
    }

    /// <summary>
    /// Contains the update review errors.
    /// </summary>
    internal static class UpdateReview
    {
        internal static readonly Error ReviewIdIsRequired = Error.Validation(
           "UpdateReview.ReviewIdIsRequired", "The review identifier is required");

        internal static readonly Error UserIdIsRequired = Error.Validation(
           "UpdateReview.UserIdIsRequired", "The user identifier is required");

        internal static readonly Error RatingMustBeBelowFive = Error.Validation(
            "UpdateReview.RatingMustBeBelowFive",
            "Your rating must not be above 5");

        internal static readonly Error RatingMustBeGreaterOrEqualToZero = Error.Validation(
            "UpdateReview.RatingMustBeGreaterOrEqualToZero",
            "Your rating must be greater than or equal to 0");

        internal static readonly Error CommentIsRequired = Error.Validation(
            "UpdateReview.CommentIsRequired", "The comment is required");

        internal static Error CommentMustNotBeAbove(int limit) => Error.Validation(
            "UpdateReview.CommentMustNotBeAbove", $"The comment must not be above {limit} characters");
    }

    /// <summary>
    /// Contains the remove review errors.
    /// </summary>
    internal static class RemoveReview
    {
        internal static readonly Error ReviewIdIsRequired = Error.Validation(
           "RemoveReview.ReviewIdIsRequired", "The review identifier is required");
    }

    /// <summary>
    /// Contains the calcute price errors
    /// </summary>
    internal static class CalculatePrice
    {
        internal static readonly Error StartDateMustBeLessThanEndDate = Error.Validation(
            "CalculatePrice.StartDateMustBeLessThanEndDate",
            "The start date must be less than the end date");
    }
}