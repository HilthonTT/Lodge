import type { Http } from "@/lib/api/client";
import type {
  Amenity,
  Apartment,
  ApartmentSortColumn,
  CalendarDay,
  FlexibleStay,
  LocationSuggestion,
  PagedList,
  PriceDetails,
  PriceRange,
  PropertyType,
  Review,
  SortOrder,
} from "@/types/api";

export type SearchApartmentsParams = {
  page: number;
  pageSize: number;
  searchTerm?: string;
  sortColumn?: ApartmentSortColumn;
  sortOrder?: SortOrder;
  startDate?: string;
  endDate?: string;
  guestCount?: number;
  bedrooms?: number;
  beds?: number;
  bathrooms?: number;
  minPrice?: number;
  maxPrice?: number;
  amenities?: Amenity[];
  propertyTypes?: PropertyType[];
  instantBook?: boolean;
  superhost?: boolean;
  minLat?: number;
  maxLat?: number;
  minLng?: number;
  maxLng?: number;
  flexible?: FlexibleStay;
  /** "yyyy-MM" values. */
  months?: string[];
};

/**
 * Drops undefined/empty values so axios doesn't send `?searchTerm=`, and joins
 * arrays with commas (the API accepts comma-separated lists).
 */
function compact<T extends Record<string, unknown>>(params: T) {
  return Object.fromEntries(
    Object.entries(params)
      .filter(([, v]) => v !== undefined && v !== null && v !== "" && v !== false && !(Array.isArray(v) && v.length === 0))
      .map(([k, v]) => [k, Array.isArray(v) ? v.join(",") : v]),
  );
}

const apartmentPath = (apartmentId: string) => `/apartments/${encodeURIComponent(apartmentId)}`;

export const apartmentsApi = {
  list: (http: Http, params: { page: number; pageSize: number }) =>
    http.get<PagedList<Apartment>>("/apartments", { params }).then((r) => r.data),

  search: (http: Http, params: SearchApartmentsParams) =>
    http
      .get<PagedList<Apartment>>("/apartments/search", { params: compact(params) })
      .then((r) => r.data),

  get: (http: Http, apartmentId: string) =>
    http.get<Apartment>(apartmentPath(apartmentId)).then((r) => r.data),

  /** Every unavailable night: bookings, pending requests and host-blocked nights. */
  bookedDates: (http: Http, apartmentId: string) =>
    http.get<string[]>(`${apartmentPath(apartmentId)}/booked-dates`).then((r) => r.data),

  /** Nightly availability and prices (default today + 90 days, max 366). */
  calendar: (http: Http, apartmentId: string, params: { from?: string; to?: string } = {}) =>
    http
      .get<CalendarDay[]>(`${apartmentPath(apartmentId)}/calendar`, { params: compact(params) })
      .then((r) => r.data),

  price: (http: Http, apartmentId: string, startDate: string, endDate: string) =>
    http
      .get<PriceDetails>(`${apartmentPath(apartmentId)}/price`, { params: { startDate, endDate } })
      .then((r) => r.data),

  priceRange: (http: Http) => http.get<PriceRange>("/apartments/price-range").then((r) => r.data),

  reviews: (http: Http, apartmentId: string, params: { page: number; pageSize: number }) =>
    http.get<PagedList<Review>>(`${apartmentPath(apartmentId)}/reviews`, { params }).then((r) => r.data),

  /** Destination autocomplete (2–100 characters), up to 8 results. */
  suggestLocations: (http: Http, q: string, signal?: AbortSignal) =>
    http.get<LocationSuggestion[]>("/locations/suggest", { params: { q }, signal }).then((r) => r.data),
};
