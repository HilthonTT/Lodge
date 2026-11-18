export const queryKeys = {
  auth: {
    providers: ["auth", "providers"] as const,
  },
  apartments: {
    all: ["apartments"] as const,
    price: (id: string, start: string, end: string) =>
      ["apartments", id, "price", { start, end }] as const,
    bookedDates: (id: string) => ["apartments", id, "booked-dates"] as const,
  },
  bookings: {
    all: ["bookings"] as const,
    byUser: (userId: string) => ["bookings", "user", userId] as const,
    admin: ["bookings", "admin"] as const,
    adminList: (params: { page: number; pageSize: number; status?: number }) =>
      ["bookings", "admin", params] as const,
  },
  users: {
    me: ["users", "me"] as const,
    profile: (userId: string) => ["users", userId, "profile"] as const,
  },
};
