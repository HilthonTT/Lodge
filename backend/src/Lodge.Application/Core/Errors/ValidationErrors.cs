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
    /// Contains the two-factor login errors.
    /// </summary>
    internal static class LoginTwoFactor
    {
        internal static readonly Error TwoFactorTokenIsRequired = Error.Validation(
            "LoginTwoFactor.TwoFactorTokenIsRequired", "The two-factor token is required");

        internal static readonly Error CodeIsRequired = Error.Validation(
            "LoginTwoFactor.CodeIsRequired", "Either a verification code or a recovery code is required");
    }

    /// <summary>
    /// Contains the forgot password errors.
    /// </summary>
    internal static class ForgotPassword
    {
        internal static readonly Error EmailIsRequired = Error.Validation(
            "ForgotPassword.EmailIsRequired", "The email is required");
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

        internal static readonly Error GuestCountIsRequired = Error.Validation(
            "ReserveBooking.GuestCountIsRequired",
            "The number of guests is required and must be at least 1");
    }

    /// <summary>
    /// Contains the change booking dates errors.
    /// </summary>
    internal static class ChangeBookingDates
    {
        internal static readonly Error StartDateMustBeLessThanEndDate = Error.Validation(
            "ChangeBookingDates.StartDateMustBeLessThanEndDate",
            "The start date must be less than the end date");
    }

    /// <summary>
    /// Contains the reject booking errors.
    /// </summary>
    internal static class RejectBooking
    {
        internal static readonly Error BookingIdIsRequired = Error.Validation(
           "RejectBooking.BookingIdIsRequired", "The booking identifier is required");
    }

    /// <summary>
    /// Contains the confirm booking errors.
    /// </summary>
    internal static class CompleteBooking
    {
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
            "UploadFile.InvalidContentType", "The file must be a JPEG, PNG or WebP image");

        internal static readonly Error FileIsRequired = Error.Validation(
            "UploadFile.FileIsRequired", "The file is required");

        internal static Error FileTooLarge(long maxBytes) => Error.Validation(
            "UploadFile.FileTooLarge", $"The file must not be larger than {maxBytes / 1024 / 1024} MB");
    }

    /// <summary>
    /// Contains the create review errors.
    /// </summary>
    internal static class CreateReview
    {
        internal static readonly Error UserIdIsRequired = Error.Validation(
           "CreateReview.UserIdIsRequired", "The user identifier is required");

        internal static readonly Error BookingIdIsRequired = Error.Validation(
            "CreateReview.BookingIdIsRequired", "The booking identifier is required");

        internal static readonly Error RatingMustBeBetweenOneAndFive = Error.Validation(
            "CreateReview.RatingMustBeBetweenOneAndFive",
            "The rating must be between 1 and 5");

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

        internal static readonly Error RatingMustBeBetweenOneAndFive = Error.Validation(
            "UpdateReview.RatingMustBeBetweenOneAndFive",
            "The rating must be between 1 and 5");

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

        internal static readonly Error TooManyNights = Error.Validation(
            "CalculatePrice.TooManyNights",
            "The price can be calculated for at most 365 nights");
    }

    internal static class StripeCheckout
    {
        internal static readonly Error UserIdIsRequired = Error.Validation(
           "StripeCheckout.UserIdIsRequired", "The user identifier is required");

        internal static readonly Error BookingIdIsRequired = Error.Validation(
           "StripeCheckout.BookingIdIsRequired", "The booking identifier is required");
    }

    internal static class GetBookings
    {
        internal static readonly Error InvalidStatus = Error.Validation(
            "GetBookings.InvalidStatus", "The status must be between 1 and 7");
    }

    internal static class Paging
    {
        internal const int MaxPageSize = 100;

        internal static readonly Error PageMustBePositive = Error.Validation(
            "Paging.PageMustBePositive", "The page must be greater than or equal to 1");

        internal static readonly Error PageSizeOutOfRange = Error.Validation(
            "Paging.PageSizeOutOfRange", $"The page size must be between 1 and {MaxPageSize}");
    }

    internal static class SearchApartments
    {
        internal static readonly Error StartDateMustBeLessThanEndDate = Error.Validation(
            "SearchApartments.StartDateMustBeLessThanEndDate",
            "The start date must be less than the end date");

        internal static readonly Error BothDatesRequired = Error.Validation(
            "SearchApartments.BothDatesRequired",
            "Both the start date and the end date must be provided");

        internal static readonly Error InvalidAmenity = Error.Validation(
            "SearchApartments.InvalidAmenity",
            "Unknown amenity");

        internal static readonly Error InvalidPropertyType = Error.Validation(
            "SearchApartments.InvalidPropertyType",
            "Unknown property type");

        internal static readonly Error NegativeFilter = Error.Validation(
            "SearchApartments.NegativeFilter",
            "Guest count must be at least 1; bedrooms, beds, bathrooms and prices must not be negative");

        internal static readonly Error InvalidPriceRange = Error.Validation(
            "SearchApartments.InvalidPriceRange",
            "The minimum price must not be greater than the maximum price");

        internal static readonly Error InvalidBounds = Error.Validation(
            "SearchApartments.InvalidBounds",
            "Latitudes must be between -90 and 90 (minimum first) and longitudes between -180 and 180");

        internal static readonly Error InvalidFlexible = Error.Validation(
            "SearchApartments.InvalidFlexible",
            "Flexible must be 'weekend', 'week' or 'month'");

        internal static readonly Error FlexibleWithExactDates = Error.Validation(
            "SearchApartments.FlexibleWithExactDates",
            "Flexible dates cannot be combined with a start or end date");

        internal static readonly Error InvalidMonths = Error.Validation(
            "SearchApartments.InvalidMonths",
            "Flexible dates need 1 to 12 months written as yyyy-MM");

        internal static readonly Error MonthsRequireFlexible = Error.Validation(
            "SearchApartments.MonthsRequireFlexible",
            "Months can only be used with flexible dates");
    }

    internal static class Listing
    {
        internal static readonly Error InvalidPropertyType = Error.Validation(
            "Listing.InvalidPropertyType",
            "Unknown property type");

        internal static readonly Error InvalidAmenity = Error.Validation(
            "Listing.InvalidAmenity",
            "Unknown amenity");

        internal static readonly Error InvalidCurrency = Error.Validation(
            "Listing.InvalidCurrency",
            "The currency must be USD or EUR");

        internal static readonly Error InvalidTime = Error.Validation(
            "Listing.InvalidTime",
            "Check-in and check-out times must be written as HH:mm");

        internal static readonly Error InvalidAddress = Error.Validation(
            "Listing.InvalidAddress",
            "The country, city and street are required (at most 200 characters each)");
    }

    internal static class Payouts
    {
        internal static readonly Error InvalidStatus = Error.Validation(
            "Payouts.InvalidStatus",
            "The status must be Scheduled, OnHold, Paid, Failed or Cancelled");
    }

    internal static class Calendar
    {
        internal static readonly Error NothingToChange = Error.Validation(
            "Calendar.NothingToChange",
            "Set 'blocked', 'price' or 'clearPrice'");

        internal static readonly Error PriceAndClearPrice = Error.Validation(
            "Calendar.PriceAndClearPrice",
            "'price' and 'clearPrice' cannot be combined");
    }

    internal static class Locations
    {
        internal static readonly Error InvalidQuery = Error.Validation(
            "Locations.InvalidQuery",
            "The search text must be between 2 and 100 characters");
    }

    /// <summary>
    /// Contains the experience errors.
    /// </summary>
    internal static class Experiences
    {
        internal static readonly Error ExperienceIdIsRequired = Error.Validation(
            "Experiences.ExperienceIdIsRequired", "The experience identifier is required");

        internal static readonly Error ExperienceBookingIdIsRequired = Error.Validation(
            "Experiences.ExperienceBookingIdIsRequired", "The experience booking identifier is required");

        internal static readonly Error SessionIdIsRequired = Error.Validation(
            "Experiences.SessionIdIsRequired", "The session identifier is required");

        internal static readonly Error RequestIdIsRequired = Error.Validation(
            "Experiences.RequestIdIsRequired", "The request identifier is required");

        internal static readonly Error UserIdIsRequired = Error.Validation(
            "Experiences.UserIdIsRequired", "The user identifier is required");

        internal static readonly Error ExperienceIsRequired = Error.Validation(
            "Experiences.ExperienceIsRequired", "The experience details are required");

        internal static readonly Error SessionsAreRequired = Error.Validation(
            "Experiences.SessionsAreRequired", "The sessions are required");
    }

    /// <summary>
    /// Contains the apartment errors.
    /// </summary>
    internal static class Apartments
    {
        internal static readonly Error ApartmentIdIsRequired = Error.Validation(
            "Apartments.ApartmentIdIsRequired", "The apartment identifier is required");
    }

    /// <summary>
    /// Contains the booking errors.
    /// </summary>
    internal static class Bookings
    {
        internal static readonly Error BookingIdIsRequired = Error.Validation(
            "Bookings.BookingIdIsRequired", "The booking identifier is required");

        internal static readonly Error UserIdIsRequired = Error.Validation(
            "Bookings.UserIdIsRequired", "The user identifier is required");
    }

    /// <summary>
    /// Contains the file errors.
    /// </summary>
    internal static class Files
    {
        internal static readonly Error FileIdIsRequired = Error.Validation(
            "Files.FileIdIsRequired", "The file identifier is required");

        internal static readonly Error UserIdIsRequired = Error.Validation(
            "Files.UserIdIsRequired", "The user identifier is required");
    }

    /// <summary>
    /// Contains the hosting errors.
    /// </summary>
    internal static class Hosting
    {
        internal static readonly Error BookingIdIsRequired = Error.Validation(
            "Hosting.BookingIdIsRequired", "The booking identifier is required");

        internal static readonly Error ApartmentIdIsRequired = Error.Validation(
            "Hosting.ApartmentIdIsRequired", "The listing identifier is required");

        internal static readonly Error ReviewIdIsRequired = Error.Validation(
            "Hosting.ReviewIdIsRequired", "The review identifier is required");
    }

    /// <summary>
    /// Contains the messaging errors.
    /// </summary>
    internal static class Messaging
    {
        internal static readonly Error ConversationIdIsRequired = Error.Validation(
            "Messaging.ConversationIdIsRequired", "The conversation identifier is required");

        internal static readonly Error ApartmentIdIsRequired = Error.Validation(
            "Messaging.ApartmentIdIsRequired", "The apartment identifier is required");
    }

    /// <summary>
    /// Contains the notification errors.
    /// </summary>
    internal static class Notifications
    {
        internal static readonly Error NotificationIdIsRequired = Error.Validation(
            "Notifications.NotificationIdIsRequired", "The notification identifier is required");

        internal static readonly Error UserIdIsRequired = Error.Validation(
            "Notifications.UserIdIsRequired", "The user identifier is required");
    }

    /// <summary>
    /// Contains the payment errors.
    /// </summary>
    internal static class Payments
    {
        internal static readonly Error BookingIdIsRequired = Error.Validation(
            "Payments.BookingIdIsRequired", "The booking identifier is required");
    }

    /// <summary>
    /// Contains the wishlist errors.
    /// </summary>
    internal static class Wishlists
    {
        internal static readonly Error WishlistIdIsRequired = Error.Validation(
            "Wishlists.WishlistIdIsRequired", "The wishlist identifier is required");

        internal static readonly Error ApartmentIdIsRequired = Error.Validation(
            "Wishlists.ApartmentIdIsRequired", "The apartment identifier is required");
    }

    /// <summary>
    /// Contains the user errors.
    /// </summary>
    internal static class Users
    {
        internal static readonly Error UserIdIsRequired = Error.Validation(
            "Users.UserIdIsRequired", "The user identifier is required");
    }

    /// <summary>
    /// Contains the two-factor errors.
    /// </summary>
    internal static class TwoFactor
    {
        internal static readonly Error UserIdIsRequired = Error.Validation(
            "TwoFactor.UserIdIsRequired", "The user identifier is required");

        internal static readonly Error CodeIsRequired = Error.Validation(
            "TwoFactor.CodeIsRequired", "The verification code is required");
    }

    /// <summary>
    /// Contains the reset password errors.
    /// </summary>
    internal static class ResetPassword
    {
        internal static readonly Error TokenIsRequired = Error.Validation(
            "ResetPassword.TokenIsRequired", "The reset token is required");

        internal static readonly Error PasswordIsRequired = Error.Validation(
            "ResetPassword.PasswordIsRequired", "The password is required");
    }

    /// <summary>
    /// Contains the verify email errors.
    /// </summary>
    internal static class VerifyEmail
    {
        internal static readonly Error TokenIsRequired = Error.Validation(
            "VerifyEmail.TokenIsRequired", "The verification token is required");
    }

    /// <summary>
    /// Contains the external login errors.
    /// </summary>
    internal static class ExternalLogin
    {
        internal static readonly Error ProviderIsRequired = Error.Validation(
            "ExternalLogin.ProviderIsRequired", "The provider is required");
    }

    /// <summary>
    /// Contains the Stripe webhook errors.
    /// </summary>
    internal static class StripeWebhook
    {
        internal static readonly Error PayloadIsRequired = Error.Validation(
            "StripeWebhook.PayloadIsRequired", "The payload is required");
    }
}
