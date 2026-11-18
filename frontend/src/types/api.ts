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
  zipCode: string;
  city: string;
  street: string;
};

export const PROPERTY_TYPES = [
  "Apartment",
  "House",
  "Cabin",
  "Villa",
  "Cottage",
  "Treehouse",
  "Houseboat",
  "Igloo",
  "Other",
] as const;
export type PropertyType = (typeof PROPERTY_TYPES)[number];

/** Every value the API accepts in `amenities` (real amenities and category tags). */
export const AMENITY_VALUES = [
  "WiFi",
  "AirConditioning",
  "Parking",
  "PetFriendly",
  "SwimmingPool",
  "Gym",
  "Spa",
  "Terrace",
  "MountainView",
  "GardenView",
  "Cabins",
  "Lakefront",
  "Treehouses",
  "Camping",
  "Castles",
  "Farms",
  "Boats",
  "Domes",
  "Tropical",
  "Mansions",
  "Caves",
  "Barns",
  "TopCities",
  "Kitchen",
  "Washer",
  "Heating",
  "Workspace",
  "HotTub",
  "Fireplace",
  "BeachAccess",
  "EVCharger",
  "SelfCheckIn",
] as const;
export type Amenity = (typeof AMENITY_VALUES)[number];

export type ApartmentImage = { id: string; url: string };

export type ListingHost = {
  id: string;
  firstName: string;
  avatarUrl: string | null;
  isSuperhost: boolean;
  joinedOnUtc: string;
};

export type Apartment = {
  id: string;
  name: string;
  description: string;
  propertyType: PropertyType;
  /** Base nightly price. */
  price: number;
  currency: string;
  cleaningFee: number;
  weekendPrice: number | null;
  weeklyDiscountPercent: number;
  monthlyDiscountPercent: number;
  /** Cover photo (= images[0].url). */
  imageUrl: string;
  images: ApartmentImage[];
  bedrooms: number;
  beds: number;
  /** In steps of 0.5. */
  bathrooms: number;
  maximumGuestCount: number;
  latitude: number;
  longitude: number;
  amenities: Amenity[];
  /** "HH:mm" */
  checkInTime: string;
  /** "HH:mm" */
  checkOutTime: string;
  petsAllowed: boolean;
  smokingAllowed: boolean;
  partiesAllowed: boolean;
  houseRules: string | null;
  instantBook: boolean;
  minNights: number;
  maxNights: number;
  address: Address;
  /** Average review rating rounded to one decimal; null when there are no reviews. */
  averageRating: number | null;
  reviewCount: number;
  /** Average category ratings of published reviews; null when there are none. */
  ratingBreakdown: CategoryRatings | null;
  cancellationPolicy: CancellationPolicy;
  host: ListingHost;
};

/** GET /hosting/listings/{id}: the full listing as its host sees it. */
export type HostListing = Apartment & {
  checkInInstructions: string | null;
  isListed: boolean;
  createdOnUtc: string;
  modifiedOnUtc: string | null;
};

/** Row of GET /hosting/listings. */
export type HostListingSummary = {
  id: string;
  name: string;
  propertyType: PropertyType;
  city: string;
  country: string;
  imageUrl: string;
  price: number;
  currency: string;
  isListed: boolean;
  instantBook: boolean;
  averageRating: number | null;
  reviewCount: number;
  upcomingBookingCount: number;
  pendingRequestCount: number;
  createdOnUtc: string;
  modifiedOnUtc: string | null;
};

export type ListingCurrency = "USD" | "EUR";

export const CANCELLATION_POLICIES = ["Flexible", "Moderate", "Strict"] as const;
export type CancellationPolicy = (typeof CANCELLATION_POLICIES)[number];

export const REVIEW_CATEGORIES = ["cleanliness", "accuracy", "checkIn", "communication", "location", "value"] as const;
export type ReviewCategory = (typeof REVIEW_CATEGORIES)[number];
/** 1–5 per category (averages may have decimals). */
export type CategoryRatings = Record<ReviewCategory, number>;

/** Body of POST /hosting/listings and PUT /hosting/listings/{id}. */
export type ListingRequest = {
  name: string;
  description: string;
  propertyType: PropertyType;
  address: Address;
  latitude: number;
  longitude: number;
  price: number;
  currency: ListingCurrency;
  cleaningFee: number;
  weekendPrice: number | null;
  weeklyDiscountPercent: number;
  monthlyDiscountPercent: number;
  bedrooms: number;
  beds: number;
  bathrooms: number;
  maximumGuestCount: number;
  amenities: Amenity[];
  checkInTime: string;
  checkOutTime: string;
  petsAllowed: boolean;
  smokingAllowed: boolean;
  partiesAllowed: boolean;
  houseRules: string | null;
  checkInInstructions: string | null;
  instantBook: boolean;
  minNights: number;
  maxNights: number;
  /** Uploaded photo ids in display order (the first is the cover). */
  imageIds: string[];
  isListed?: boolean;
  /** Defaults to "Moderate". */
  cancellationPolicy: CancellationPolicy;
};

export type UploadedPhoto = { fileId: string; url: string };

/** Night of GET /hosting/listings/{id}/calendar. */
export type HostCalendarDay = {
  /** yyyy-MM-dd */
  date: string;
  available: boolean;
  blocked: boolean;
  booked: boolean;
  price: number;
  customPrice: boolean;
};

/** Body of PUT /hosting/listings/{id}/calendar (dates inclusive). */
export type UpdateCalendarRequest = {
  from: string;
  to: string;
  blocked?: boolean | null;
  price?: number | null;
  clearPrice?: boolean;
};

/** Night of GET /apartments/{id}/calendar. */
export type CalendarDay = {
  /** yyyy-MM-dd */
  date: string;
  available: boolean;
  price: number;
};

export type HostingSummary = {
  listingCount: number;
  upcomingStays: number;
  pendingRequests: number;
  earningsThisMonth: number;
  /** Payouts already paid out. */
  paidTotal: number;
  /** Scheduled and on-hold payouts. */
  upcomingPayouts: number;
  currency: string;
  averageRating: number | null;
  reviewCount: number;
  isSuperhost: boolean;
  superhost: {
    completedStays: number;
    averageRating: number | null;
    /** 0..1 */
    cancellationRate: number;
    meetsRequirements: boolean;
    requirements: {
      minCompletedStays: number;
      minAverageRating: number;
      maxCancellationRate: number;
    };
  };
};

export type PriceRange = { min: number; max: number; currency: string };

export type LocationSuggestion = {
  label: string;
  city: string;
  country: string;
  latitude: number;
  longitude: number;
  listingCount: number;
};

export type FlexibleStay = "weekend" | "week" | "month";

export type WishlistSummary = {
  id: string;
  name: string;
  itemCount: number;
  coverImageUrl: string | null;
  updatedOnUtc: string;
};

export type Wishlist = { id: string; name: string; items: Apartment[] };

export type PriceDetails = {
  currency: string;
  nights: number;
  /** Sum of the nightly prices, before any discount. */
  pricePerPeriod: number;
  averageNightlyPrice: number;
  discountAmount: number;
  discountType: "weekly" | "monthly" | null;
  cleaningFee: number;
  amenitiesUpCharge: number;
  /** 12% of the stay subtotal. */
  serviceFee: number;
  taxes: number;
  /** e.g. 12.5 for 12.5%. */
  taxRatePercent: number;
  /** Includes the service fee and taxes. */
  totalPrice: number;
};

export type HostReply = { text: string; createdOnUtc: string; modifiedOnUtc: string | null };

export type Review = {
  id: string;
  apartmentId: string;
  listingName: string;
  bookingId: string;
  userId: string;
  rating: number;
  categories: CategoryRatings;
  comment: string;
  createdOnUtc: string;
  modifiedOnUtc: string | null;
  /** Display name such as "Jamie L.". */
  authorName: string;
  authorAvatarUrl: string | null;
  hostReply: HostReply | null;
  /** False only for the caller's own review while the double-blind window is open. */
  isPublished: boolean;
};

/** A host's review of a guest, shown on the guest's public profile. */
export type GuestReview = {
  id: string;
  rating: number;
  comment: string;
  hostFirstName: string;
  hostAvatarUrl: string | null;
  listingName: string;
  createdOnUtc: string;
};

export type RefundStatus = "None" | "Pending" | "Succeeded" | "Failed" | "Manual";

/** GET /bookings/{id}/cancellation-quote */
export type CancellationQuote = {
  policy: CancellationPolicy;
  refundPercent: number;
  refundAmount: number;
  nonRefundableAmount: number;
  currency: string;
  /** Ordered tiers; the last one has `until: null`. */
  timeline: { until: string | null; refundPercent: number }[];
  isPaid: boolean;
};

export type PaymentMode = "Stripe" | "Simulated";

export type PaymentsConfig = { mode: PaymentMode };

export type ExchangeRates = { base: string; rates: Record<string, number>; updatedOnUtc: string };

export type PayoutStatus = "Scheduled" | "OnHold" | "Paid" | "Failed" | "Cancelled";
export const PAYOUT_STATUSES: PayoutStatus[] = ["Scheduled", "OnHold", "Paid", "Failed", "Cancelled"];

export type Payout = {
  id: string;
  /** Null for experience payouts. */
  bookingId: string | null;
  /** Set for experience payouts (then `listingName` holds the experience title). */
  experienceBookingId: string | null;
  listingName: string;
  guestName: string;
  amount: number;
  currency: string;
  status: PayoutStatus;
  /** yyyy-MM-dd */
  scheduledFor: string;
  paidOnUtc: string | null;
};

export type PayoutAccountStatus = "NotConnected" | "Pending" | "Active";

export type PayoutAccount = { status: PayoutAccountStatus; payoutsEnabled: boolean; mode: PaymentMode };

export enum BookingStatus {
  Reserved = 1,
  Confirmed = 2,
  Rejected = 3,
  Cancelled = 4,
  Completed = 5,
  /** Request-to-book listing: waiting for the host (24 hours). */
  PendingApproval = 6,
  /** The host didn't answer a request in time. */
  Expired = 7,
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
  discountAmount: number;
  guestCount: number;
  messageToHost: string | null;
  listingHostId: string;
  /** Only for the listing's host, or for the guest once the booking is Confirmed. */
  checkInInstructions: string | null;
  serviceFeeAmount: number;
  taxAmount: number;
  taxRatePercent: number;
  cancellationPolicy: CancellationPolicy;
  amountPaid: number | null;
  paidOnUtc: string | null;
  refundAmount: number | null;
  refundStatus: RefundStatus;
  cancelledByHost: boolean;
  /** As guest: may review the listing. As host: may review the guest. */
  canReview: boolean;
  /** yyyy-MM-dd: last day to leave a review. */
  reviewDeadline: string | null;
  hasGuestReview: boolean;
  /** PendingApproval only: when the request expires. */
  requestExpiresOnUtc: string | null;
};

export type UserRole = "Guest" | "Admin";

export type ExternalProviderName = "google" | "github";

export type User = {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  imageId: string | null;
  role: UserRole;
  createdOnUtc: string;
  emailVerified: boolean;
  twoFactorEnabled: boolean;
  /** False for accounts that only sign in with Google/GitHub. */
  hasPassword: boolean;
  externalLogins: ExternalProviderName[];
  /** Absolute URL of the profile picture, or null. */
  avatarUrl: string | null;
  bio: string | null;
};

/** GET /users/{userId}/profile (anonymous). */
export type PublicProfile = {
  id: string;
  firstName: string;
  lastInitial: string;
  avatarUrl: string | null;
  bio: string | null;
  joinedOnUtc: string;
  emailVerified: boolean;
  reviewsWrittenCount: number;
  isSuperhost: boolean;
  hostingSince: string | null;
  /** At most 12. */
  listings: Apartment[];
  hostReviewCount: number;
  hostAverageRating: number | null;
  guestReviewCount: number;
  guestAverageRating: number | null;
  /** Latest 10 reviews hosts wrote about this guest. */
  guestReviews: GuestReview[];
};

export type TokenResponse = { token: string };

export type LoginRequest = { email: string; password: string };

/** Without 2FA `token` is set; with 2FA `token` is null and `twoFactorToken` is set (valid 5 minutes). */
export type LoginResponse = {
  token: string | null;
  requiresTwoFactor: boolean;
  twoFactorToken: string | null;
};

/** Send either `code` (authenticator app) or `recoveryCode`. */
export type TwoFactorLoginRequest = {
  twoFactorToken: string;
  code?: string | null;
  recoveryCode?: string | null;
};

export type ResetPasswordRequest = { token: string; password: string };

export type ExternalProvider = { name: ExternalProviderName; displayName: string };

export type TwoFactorSetupResponse = { secret: string; otpauthUri: string };

export type RecoveryCodesResponse = { recoveryCodes: string[] };

export type DisableTwoFactorRequest = { password: string | null; code: string };

export type AvatarResponse = { avatarUrl: string };

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
  guestCount: number;
  /** At most 1000 characters. */
  messageToHost?: string | null;
};

export type UpdateUserRequest = {
  firstName: string;
  lastName: string;
  imageId: string | null;
  /** null leaves the bio unchanged, "" clears it (max 500 characters). */
  bio?: string | null;
};

export type CreateReviewRequest = {
  userId: string;
  bookingId: string;
  rating: number;
  comment: string;
  categories: CategoryRatings;
};

export type UpdateReviewRequest = Omit<CreateReviewRequest, "bookingId">;

/** Body of POST /hosting/bookings/{id}/guest-review. */
export type GuestReviewRequest = { rating: number; comment: string };

/** RFC 7807 problem details as produced by CustomResults.Problem. */
export type ProblemDetails = {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  errors?: { code: string; description: string }[];
};

export type ApartmentSortColumn = "name" | "country" | "price_amount" | "rating" | "newest";
export type SortOrder = "asc" | "desc";

/* ------------------------------------------------------------------ */
/* Messaging                                                           */
/* ------------------------------------------------------------------ */

export const SYSTEM_EVENTS = [
  "BookingRequested",
  "BookingReserved",
  "BookingAccepted",
  "BookingDeclined",
  "BookingExpired",
  "BookingConfirmed",
  "BookingDatesChanged",
  "BookingCancelledByGuest",
  "BookingCancelledByHost",
] as const;
export type SystemEvent = (typeof SYSTEM_EVENTS)[number];

export type MessageType = "User" | "System";

export type Message = {
  id: string;
  conversationId: string;
  /** Null for system messages. */
  senderId: string | null;
  type: MessageType;
  /** English fallback for system messages; localize by `systemEvent`. */
  body: string;
  systemEvent: SystemEvent | null;
  bookingId: string | null;
  sentOnUtc: string;
};

/** GET /conversations/{id}/messages: newest page first, oldest first within a page. */
export type MessagesPage = { items: Message[]; hasMore: boolean; nextBefore: string | null };

/** Booking status as a string (the conversation endpoints don't use the numeric enum). */
export type BookingStatusName =
  | "PendingApproval"
  | "Reserved"
  | "Confirmed"
  | "Rejected"
  | "Cancelled"
  | "Completed"
  | "Expired";

export type ParticipantRole = "Host" | "Guest";

export type Conversation = {
  id: string;
  apartment: { id: string; name: string; imageUrl: string | null };
  otherParticipant: { id: string; firstName: string; avatarUrl: string | null; role: ParticipantRole };
  lastMessage: {
    body: string;
    type: MessageType;
    sentOnUtc: string;
    senderId: string | null;
    systemEvent: SystemEvent | null;
  } | null;
  unreadCount: number;
  latestBooking: { id: string; status: BookingStatusName; durationStart: string; durationEnd: string } | null;
  lastMessageOnUtc: string;
};

export type StartConversationResponse = { conversationId: string; message: Message };

export type CountResponse = { count: number };

/* ------------------------------------------------------------------ */
/* Notifications                                                       */
/* ------------------------------------------------------------------ */

export type AppNotification = {
  id: string;
  /** e.g. "BookingConfirmed"; unknown types get a generic text. */
  type: string;
  data: Record<string, unknown>;
  /** Relative frontend path. */
  link: string;
  createdOnUtc: string;
  readOnUtc: string | null;
};

export const NOTIFICATION_CATEGORIES = ["messages", "reservations", "reviews", "payouts", "reminders"] as const;
export type NotificationCategory = (typeof NOTIFICATION_CATEGORIES)[number];

export type NotificationPreferences = {
  email: Record<NotificationCategory, boolean>;
  inApp: Record<NotificationCategory, boolean>;
};

/** SignalR `unreadCounts` payload. */
export type UnreadCounts = { messages: number; notifications: number };

/* ------------------------------------------------------------------ */
/* Experiences                                                         */
/* ------------------------------------------------------------------ */

export const EXPERIENCE_CATEGORIES = [
  "Food",
  "Outdoors",
  "Art",
  "Culture",
  "Sports",
  "Wellness",
  "Nightlife",
  "Workshops",
] as const;
export type ExperienceCategory = (typeof EXPERIENCE_CATEGORIES)[number];

export type ExperienceBookingStatus = "Reserved" | "Confirmed" | "Cancelled" | "Completed";

export type ExperienceSummary = {
  id: string;
  title: string;
  category: ExperienceCategory;
  city: string;
  country: string;
  latitude: number;
  longitude: number;
  durationMinutes: number;
  pricePerGuest: number;
  currency: string;
  imageUrl: string | null;
  /** ISO 639-1 codes, e.g. "en". */
  languages: string[];
  averageRating: number | null;
  reviewCount: number;
  nextSessionStartsAtUtc: string | null;
  hostId: string;
  hostFirstName: string;
};

export type Experience = {
  id: string;
  title: string;
  description: string;
  category: ExperienceCategory;
  city: string;
  country: string;
  latitude: number;
  longitude: number;
  meetingPoint: string;
  durationMinutes: number;
  pricePerGuest: number;
  currency: string;
  maxGuests: number;
  languages: string[];
  included: string[];
  images: { id: string; url: string }[];
  host: { id: string; firstName: string; avatarUrl: string | null; isSuperhost: boolean };
  averageRating: number | null;
  reviewCount: number;
  isListed: boolean;
  freeCancellationHours: number;
  createdOnUtc: string;
};

export type ExperienceSession = { id: string; startsAtUtc: string; capacity: number; spotsLeft: number };

export type HostExperienceSession = ExperienceSession & {
  bookedGuests: number;
  bookingCount: number;
  isCancelled: boolean;
};

export type ExperienceReview = {
  id: string;
  rating: number;
  comment: string;
  createdOnUtc: string;
  guest: { id: string; firstName: string; avatarUrl: string | null };
};

export type ExperienceSearchParams = {
  q?: string;
  city?: string;
  category?: ExperienceCategory;
  /** yyyy-MM-dd */
  date?: string;
  guests?: number;
  /** USD. */
  minPrice?: number;
  /** USD. */
  maxPrice?: number;
  page?: number;
  pageSize?: number;
};

export type ExperienceBooking = {
  id: string;
  experienceId: string;
  experienceTitle: string;
  category: ExperienceCategory;
  imageUrl: string | null;
  city: string;
  country: string;
  meetingPoint: string;
  durationMinutes: number;
  sessionId: string;
  startsAtUtc: string;
  hostId: string;
  hostFirstName: string;
  guestId: string;
  guestName: string;
  guestCount: number;
  priceTotal: number;
  serviceFee: number;
  totalPrice: number;
  currency: string;
  status: ExperienceBookingStatus;
  amountPaid: number | null;
  paidOnUtc: string | null;
  refundAmount: number | null;
  refundStatus: RefundStatus;
  cancelledByHost: boolean;
  cancelledOnUtc: string | null;
  freeCancellationUntilUtc: string;
  canCancel: boolean;
  canReview: boolean;
  hasReview: boolean;
  createdOnUtc: string;
};

export type HostExperienceSummary = {
  id: string;
  title: string;
  category: ExperienceCategory;
  city: string;
  country: string;
  pricePerGuest: number;
  currency: string;
  isListed: boolean;
  imageUrl: string | null;
  upcomingSessionCount: number;
  averageRating: number | null;
  reviewCount: number;
  createdOnUtc: string;
};

/** Body of POST/PUT /hosting/experiences. */
export type ExperienceRequest = {
  title: string;
  description: string;
  category: ExperienceCategory;
  city: string;
  country: string;
  latitude: number;
  longitude: number;
  meetingPoint: string;
  durationMinutes: number;
  pricePerGuest: number;
  currency: ListingCurrency;
  maxGuests: number;
  languages: string[];
  included: string[];
  imageIds: string[];
  isListed?: boolean;
};

export const WEEKDAYS = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"] as const;
export type Weekday = (typeof WEEKDAYS)[number];

/** Body of POST /hosting/experiences/{id}/sessions: exactly one of `startsAtUtc` or `recurring`. */
export type CreateSessionsRequest = {
  startsAtUtc?: string;
  capacity?: number;
  recurring?: { daysOfWeek: Weekday[]; startTime: string; timeZone: string; from: string; until: string };
};
