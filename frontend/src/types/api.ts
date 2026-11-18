/**
 * Types mirroring the Lodge API contracts (backend/src/Lodge.Contracts).
 * C# `decimal` -> number, `Guid` -> string, `DateTime`/`DateOnly` -> ISO string.
 */

export type PagedList<T> = {
  page: number;
  pageSize: number;
  totalCount: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
  items: T[];
};

export type Address = {
  country: string;
  state: string;
  city: string;
  street: string;
};

export type Apartment = {
  id: string;
  name: string;
  description: string;
  price: number;
  currency: string;
  imageUrl: string;
  maximumRoomCount: number;
  maximumGuestCount: number;
  address: Address;
  /** Average review rating rounded to one decimal; null when there are no reviews. */
  averageRating: number | null;
  reviewCount: number;
};

export type PriceDetails = {
  currency: string;
  pricePerPeriod: number;
  cleaningFee: number;
  amenitiesUpCharge: number;
  totalPrice: number;
};

export type Review = {
  id: string;
  apartmentId: string;
  userId: string;
  /** Display name such as "Jamie L.". */
  authorName: string;
  rating: number;
  comment: string;
  createdOnUtc: string;
  modifiedOnUtc: string | null;
};

export enum BookingStatus {
  Reserved = 1,
  Confirmed = 2,
  Rejected = 3,
  Cancelled = 4,
  Completed = 5,
}

export type Booking = {
  id: string;
  apartmentId: string;
  userId: string;
  status: BookingStatus;
  priceAmount: number;
  priceCurrency: string;
  cleaningFeeAmount: number;
  cleaningFeeCurrency: string;
  amenitiesUpChargeAmount: number;
  amenitiesUpChargeCurrency: string;
  totalPriceAmount: number;
  totalPriceCurrency: string;
  apartmentName: string;
  apartmentImageUrl: string;
  durationStart: string;
  durationEnd: string;
  createdOnUtc: string;
  guestName: string;
  guestEmail: string;
  /** True once the guest has reviewed this (completed) stay. */
  hasReview: boolean;
};

export type UserRole = "Guest" | "Admin";

export type User = {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  imageId: string | null;
  role: UserRole;
  createdOnUtc: string;
};

export type TokenResponse = { token: string };

export type LoginRequest = { email: string; password: string };

export type RegisterRequest = {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
};

export type ReserveBookingRequest = {
  apartmentId: string;
  userId: string;
  /** yyyy-MM-dd */
  startDate: string;
  /** yyyy-MM-dd */
  endDate: string;
};

export type UpdateUserRequest = {
  firstName: string;
  lastName: string;
  imageId: string | null;
};

export type CreateReviewRequest = {
  userId: string;
  bookingId: string;
  rating: number;
  comment: string;
};

/** RFC 7807 problem details as produced by CustomResults.Problem. */
export type ProblemDetails = {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  errors?: { code: string; description: string }[];
};

export type ApartmentSortColumn = "name" | "country" | "price_amount" | "rating";
export type SortOrder = "asc" | "desc";
