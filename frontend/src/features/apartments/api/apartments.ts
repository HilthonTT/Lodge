import type { Http } from "@/lib/api/client";
import type {
  Apartment,
  ApartmentSortColumn,
  PagedList,
  PriceDetails,
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
  roomCount?: number;
};

/** Drops undefined/empty values so axios doesn't send `?searchTerm=`. */
function compact<T extends Record<string, unknown>>(params: T) {
  return Object.fromEntries(
    Object.entries(params).filter(([, v]) => v !== undefined && v !== null && v !== ""),
  );
}

export const apartmentsApi = {
  list: (http: Http, params: { page: number; pageSize: number }) =>
    http.get<PagedList<Apartment>>("/apartments", { params }).then((r) => r.data),

  search: (http: Http, params: SearchApartmentsParams) =>
    http
      .get<PagedList<Apartment>>("/apartments/search", { params: compact(params) })
      .then((r) => r.data),

  get: (http: Http, apartmentId: string) =>
    http.get<Apartment>(`/apartments/${encodeURIComponent(apartmentId)}`).then((r) => r.data),

  /** Every booked night (check-in up to, but excluding, check-out). */
  bookedDates: (http: Http, apartmentId: string) =>
    http
      .get<string[]>(`/apartments/${encodeURIComponent(apartmentId)}/booked-dates`)
      .then((r) => r.data),

  price: (http: Http, apartmentId: string, startDate: string, endDate: string) =>
    http
      .get<PriceDetails>(`/apartments/${encodeURIComponent(apartmentId)}/price`, {
        params: { startDate, endDate },
      })
      .then((r) => r.data),

  reviews: (http: Http, apartmentId: string, params: { page: number; pageSize: number }) =>
    http
      .get<PagedList<Review>>(`/apartments/${encodeURIComponent(apartmentId)}/reviews`, { params })
      .then((r) => r.data),
};
