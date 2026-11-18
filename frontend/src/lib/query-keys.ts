export const queryKeys = {
  auth: {
    providers: ["auth", "providers"] as const,
  },
  apartments: {
    all: ["apartments"] as const,
    price: (id: string, start: string, end: string) =>
      ["apartments", id, "price", { start, end }] as const,
    bookedDates: (id: string) => ["apartments", id, "booked-dates"] as const,
    calendar: (id: string, from?: string, to?: string) => ["apartments", id, "calendar", { from, to }] as const,
    priceRange: ["apartments", "price-range"] as const,
    detail: (id: string) => ["apartments", id, "detail"] as const,
    search: (params: object) => ["apartments", "search", params] as const,
    suggest: (q: string) => ["apartments", "suggest", q] as const,
  },
  hosting: {
    all: ["hosting"] as const,
    summary: ["hosting", "summary"] as const,
    listings: ["hosting", "listings"] as const,
    listingsPage: (params: { page: number; pageSize: number }) => ["hosting", "listings", "page", params] as const,
    listing: (id: string) => ["hosting", "listings", "detail", id] as const,
    calendar: (id: string, from?: string, to?: string) =>
      ["hosting", "listings", "calendar", id, { from, to }] as const,
    calendarAll: (id: string) => ["hosting", "listings", "calendar", id] as const,
    bookings: ["hosting", "bookings"] as const,
    bookingsList: (params: { page: number; pageSize: number; status?: number; listingId?: string }) =>
      ["hosting", "bookings", params] as const,
    reviews: ["hosting", "reviews"] as const,
    reviewsList: (params: { page: number; pageSize: number; listingId?: string }) =>
      ["hosting", "reviews", params] as const,
    payoutAccount: ["hosting", "payouts", "account"] as const,
    payouts: ["hosting", "payouts", "list"] as const,
    payoutsList: (params: { page: number; pageSize: number; status?: string }) =>
      ["hosting", "payouts", "list", params] as const,
  },
  payments: {
    config: ["payments", "config"] as const,
    currencies: ["payments", "currencies"] as const,
  },
  reviews: {
    all: ["reviews"] as const,
    apartment: (apartmentId: string) => ["reviews", "apartment", apartmentId] as const,
  },
  wishlists: {
    all: ["wishlists"] as const,
    list: ["wishlists", "list"] as const,
    detail: (id: string) => ["wishlists", "detail", id] as const,
    savedIds: ["wishlists", "saved-apartment-ids"] as const,
  },
  bookings: {
    all: ["bookings"] as const,
    byUser: (userId: string) => ["bookings", "user", userId] as const,
    detail: (id: string) => ["bookings", "detail", id] as const,
    cancellationQuote: (id: string) => ["bookings", "detail", id, "cancellation-quote"] as const,
    admin: ["bookings", "admin"] as const,
    adminList: (params: { page: number; pageSize: number; status?: number }) =>
      ["bookings", "admin", params] as const,
  },
  users: {
    me: ["users", "me"] as const,
    profile: (userId: string) => ["users", userId, "profile"] as const,
  },
  messaging: {
    all: ["messaging"] as const,
    conversations: ["messaging", "conversations"] as const,
    conversationsList: (params: { page: number; pageSize: number }) =>
      ["messaging", "conversations", "list", params] as const,
    conversation: (id: string) => ["messaging", "conversations", "detail", id] as const,
    messages: (id: string) => ["messaging", "messages", id] as const,
    unreadCount: ["messaging", "unread-count"] as const,
  },
  notifications: {
    all: ["notifications"] as const,
    lists: ["notifications", "list"] as const,
    list: (params: { page: number; pageSize: number; unreadOnly: boolean }) =>
      ["notifications", "list", params] as const,
    unreadCount: ["notifications", "unread-count"] as const,
    preferences: (userId: string) => ["notifications", "preferences", userId] as const,
  },
  experiences: {
    all: ["experiences"] as const,
    search: (params: object) => ["experiences", "search", params] as const,
    detail: (id: string) => ["experiences", "detail", id] as const,
    sessions: (id: string) => ["experiences", "sessions", id] as const,
    reviews: (id: string, page: number) => ["experiences", "reviews", id, page] as const,
  },
  experienceBookings: {
    all: ["experience-bookings"] as const,
    byUser: (userId: string) => ["experience-bookings", "user", userId] as const,
    detail: (id: string) => ["experience-bookings", "detail", id] as const,
  },
  hostExperiences: {
    all: ["hosting", "experiences"] as const,
    list: (params: { page: number; pageSize: number }) => ["hosting", "experiences", "list", params] as const,
    detail: (id: string) => ["hosting", "experiences", "detail", id] as const,
    sessions: (id: string) => ["hosting", "experiences", "sessions", id] as const,
    bookings: (params: { page: number; pageSize: number; experienceId?: string }) =>
      ["hosting", "experiences", "bookings", params] as const,
  },
};
