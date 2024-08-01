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
        internal static readonly Error FirstNameIsRequired = Error.Problem(
            "CreateUser.FirstNameIsRequired", "The first name is required");

        internal static readonly Error LastNameIsRequired = Error.Problem(
            "CreateUser.LastNameIsRequired", "The last name is required");

        internal static readonly Error EmailIsRequired = Error.Problem(
            "CreateUser.EmailIsRequired", "The email is required");

        internal static readonly Error PasswordIsRequired = Error.Problem(
            "CreateUser.PasswordIsRequired", "The password is required");
    }

    /// <summary>
    /// Contains the change password errors.
    /// </summary>
    internal static class ChangePassword
    {
        internal static readonly Error UserIdIsRequired = Error.Problem(
            "ChangePassword.UserIdIsRequired", "The user identifier is required");

        internal static readonly Error PasswordIsRequired = Error.Problem(
            "ChangePassword.PasswordIsRequired", "The password is required");
    }

    /// <summary>
    /// Contains the update user errors.
    /// </summary>
    internal static class UpdateUser
    {
        internal static readonly Error UserIdIsRequired = Error.Problem(
            "UpdateUser.UserIdIsRequired", "The user identifier is required");

        internal static readonly Error FirstNameIsRequired = Error.Problem(
            "UpdateUser.FirstNameIsRequired", "The first name is required");

        internal static readonly Error LastNameIsRequired = Error.Problem(
            "UpdateUser.LastNameIsRequired", "The last name is required");
    }

    /// <summary>
    /// Contains the login errors.
    /// </summary>
    internal static class Login
    {
        internal static readonly Error EmailIsRequired = Error.Problem(
           "Login.EmailIsRequired", "The email is required");

        internal static readonly Error PasswordIsRequired = Error.Problem(
            "Login.PasswordIsRequired", "The password is required");
    }

    /// <summary>
    /// Contains the reserve booking errors.
    /// </summary>
    internal static class ReserveBooking
    {
        internal static readonly Error UserIdIsRequired = Error.Problem(
           "ReserveBooking.UserIdIsRequired", "The user identifier is required");

        internal static readonly Error ApartmentIdIsRequired = Error.Problem(
           "ReserveBooking.ApartmentIdIsRequired", "The apartment identifier is required");

        internal static readonly Error StartDateMustBeLessThanEndDate = Error.Problem(
            "ReserveBooking.StartDateMustBeLessThanEndDate", 
            "The start date must be less than the end date");
    }

    /// <summary>
    /// Contains the confirm booking errors.
    /// </summary>
    internal static class ConfirmBooking
    {
        internal static readonly Error UserIdIsRequired = Error.Problem(
           "ConfirmBooking.UserIdIsRequired", "The user identifier is required");

        internal static readonly Error BookingIdIsRequired = Error.Problem(
           "ConfirmBooking.BookingIdIsRequired", "The booking identifier is required");
    }

    /// <summary>
    /// Contains the reject booking errors.
    /// </summary>
    internal static class RejectBooking
    {
        internal static readonly Error UserIdIsRequired = Error.Problem(
           "RejectBooking.UserIdIsRequired", "The user identifier is required");

        internal static readonly Error BookingIdIsRequired = Error.Problem(
           "RejectBooking.BookingIdIsRequired", "The booking identifier is required");
    }

    /// <summary>
    /// Contains the confirm booking errors.
    /// </summary>
    internal static class CompleteBooking
    {
        internal static readonly Error UserIdIsRequired = Error.Problem(
           "CompleteBooking.UserIdIsRequired", "The user identifier is required");

        internal static readonly Error BookingIdIsRequired = Error.Problem(
           "CompleteBooking.BookingIdIsRequired", "The booking identifier is required");
    }

    /// <summary>
    /// Contains the cancel booking errors.
    /// </summary>
    internal static class CancelBooking
    {
        internal static readonly Error UserIdIsRequired = Error.Problem(
           "CancelBooking.UserIdIsRequired", "The user identifier is required");

        internal static readonly Error BookingIdIsRequired = Error.Problem(
           "CancelBooking.BookingIdIsRequired", "The booking identifier is required");
    }

    /// <summary>
    /// Contains the upload file errors.
    /// </summary>
    internal static class UploadFile
    {
        internal static readonly Error UserIdIsRequired = Error.Problem(
           "UploadFile.UserIdIsRequired", "The user identifier is required");

        internal static readonly Error ContentTypeIsRequired = Error.Problem(
           "UploadFile.ContentTypeIsRequired", "The content type is required");
    }
}
