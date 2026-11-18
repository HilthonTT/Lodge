import type { Http } from "@/lib/api/client";
import type { Wishlist, WishlistSummary } from "@/types/api";

const wishlistPath = (id: string) => `/wishlists/${encodeURIComponent(id)}`;
const itemPath = (id: string, apartmentId: string) =>
  `${wishlistPath(id)}/items/${encodeURIComponent(apartmentId)}`;

export const wishlistsApi = {
  list: (http: Http) => http.get<WishlistSummary[]>("/wishlists").then((r) => r.data),

  get: (http: Http, id: string) => http.get<Wishlist>(wishlistPath(id)).then((r) => r.data),

  /** Returns the new wishlist id. 409 `Wishlist.LimitReached`. */
  create: (http: Http, name: string) =>
    http.post<{ id: string }>("/wishlists", { name }).then((r) => r.data.id),

  rename: (http: Http, id: string, name: string) =>
    http.put<void>(wishlistPath(id), { name }).then(() => undefined),

  remove: (http: Http, id: string) => http.delete<void>(wishlistPath(id)).then(() => undefined),

  /** Idempotent. */
  addItem: (http: Http, id: string, apartmentId: string) =>
    http.put<void>(itemPath(id, apartmentId)).then(() => undefined),

  /** Idempotent. */
  removeItem: (http: Http, id: string, apartmentId: string) =>
    http.delete<void>(itemPath(id, apartmentId)).then(() => undefined),

  /** Ids of every apartment in any of the user's wishlists. */
  savedApartmentIds: (http: Http) =>
    http.get<string[]>("/wishlists/saved-apartment-ids").then((r) => r.data),
};
