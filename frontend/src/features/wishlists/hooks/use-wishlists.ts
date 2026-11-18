"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

import { useSession } from "@/features/auth/session-provider";
import { getBrowserHttp } from "@/lib/api/browser";
import type { ApiError } from "@/lib/api/client";
import { queryKeys } from "@/lib/query-keys";
import type { Wishlist, WishlistSummary } from "@/types/api";

import { wishlistsApi } from "../api/wishlists";

/** Ids of every saved apartment; empty (and not fetched) when signed out. */
export function useSavedApartmentIds() {
  const { isAuthenticated } = useSession();
  return useQuery({
    queryKey: queryKeys.wishlists.savedIds,
    queryFn: () => wishlistsApi.savedApartmentIds(getBrowserHttp()),
    enabled: isAuthenticated,
    staleTime: 60_000,
    select: (ids) => new Set(ids),
  });
}

export function useWishlists(enabled = true) {
  const { isAuthenticated } = useSession();
  return useQuery({
    queryKey: queryKeys.wishlists.list,
    queryFn: () => wishlistsApi.list(getBrowserHttp()),
    enabled: isAuthenticated && enabled,
    staleTime: 30_000,
  });
}

export function useWishlist(id: string) {
  return useQuery<Wishlist, ApiError>({
    queryKey: queryKeys.wishlists.detail(id),
    queryFn: () => wishlistsApi.get(getBrowserHttp(), id),
    staleTime: 30_000,
  });
}

function setSaved(ids: string[] | undefined, apartmentId: string, saved: boolean) {
  const set = new Set(ids ?? []);
  if (saved) set.add(apartmentId);
  else set.delete(apartmentId);
  return [...set];
}

/** Adds an apartment to a list (optimistically marks it saved). */
export function useAddToWishlist() {
  const queryClient = useQueryClient();
  return useMutation<void, ApiError, { wishlistId: string; apartmentId: string }, { previous?: string[] }>({
    mutationFn: ({ wishlistId, apartmentId }) => wishlistsApi.addItem(getBrowserHttp(), wishlistId, apartmentId),
    onMutate: async ({ apartmentId }) => {
      await queryClient.cancelQueries({ queryKey: queryKeys.wishlists.savedIds });
      const previous = queryClient.getQueryData<string[]>(queryKeys.wishlists.savedIds);
      queryClient.setQueryData<string[]>(queryKeys.wishlists.savedIds, (ids) => setSaved(ids, apartmentId, true));
      return { previous };
    },
    onError: (_error, _vars, context) => {
      queryClient.setQueryData(queryKeys.wishlists.savedIds, context?.previous);
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: queryKeys.wishlists.all }),
  });
}

/**
 * Removes an apartment from every list that holds it (optimistic). Lists that are
 * cached are checked locally; otherwise each list is loaded once to find it.
 */
export function useUnsaveApartment() {
  const queryClient = useQueryClient();
  return useMutation<void, ApiError, { apartmentId: string }, { previous?: string[] }>({
    mutationFn: async ({ apartmentId }) => {
      const http = getBrowserHttp();
      const lists =
        queryClient.getQueryData<WishlistSummary[]>(queryKeys.wishlists.list) ??
        (await queryClient.fetchQuery<WishlistSummary[]>({
          queryKey: queryKeys.wishlists.list,
          queryFn: () => wishlistsApi.list(http),
        }));
      const candidates = lists.filter((list) => list.itemCount > 0);
      await Promise.all(
        candidates.map(async (list) => {
          const cached = queryClient.getQueryData<Wishlist>(queryKeys.wishlists.detail(list.id));
          if (cached && !cached.items.some((item) => item.id === apartmentId)) return;
          // DELETE is idempotent, so a list without the item is harmless.
          await wishlistsApi.removeItem(http, list.id, apartmentId);
        }),
      );
    },
    onMutate: async ({ apartmentId }) => {
      await queryClient.cancelQueries({ queryKey: queryKeys.wishlists.savedIds });
      const previous = queryClient.getQueryData<string[]>(queryKeys.wishlists.savedIds);
      queryClient.setQueryData<string[]>(queryKeys.wishlists.savedIds, (ids) => setSaved(ids, apartmentId, false));
      // Drop it from any cached list detail right away too.
      queryClient.setQueriesData<Wishlist>({ queryKey: ["wishlists", "detail"] }, (list) =>
        list ? { ...list, items: list.items.filter((item) => item.id !== apartmentId) } : list,
      );
      return { previous };
    },
    onError: (_error, _vars, context) => {
      queryClient.setQueryData(queryKeys.wishlists.savedIds, context?.previous);
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: queryKeys.wishlists.all }),
  });
}

/** Removes one apartment from one list (used on the list page). */
export function useRemoveFromWishlist(wishlistId: string) {
  const queryClient = useQueryClient();
  return useMutation<void, ApiError, string, { previous?: Wishlist }>({
    mutationFn: (apartmentId) => wishlistsApi.removeItem(getBrowserHttp(), wishlistId, apartmentId),
    onMutate: async (apartmentId) => {
      const key = queryKeys.wishlists.detail(wishlistId);
      await queryClient.cancelQueries({ queryKey: key });
      const previous = queryClient.getQueryData<Wishlist>(key);
      queryClient.setQueryData<Wishlist>(key, (list) =>
        list ? { ...list, items: list.items.filter((item) => item.id !== apartmentId) } : list,
      );
      return { previous };
    },
    onError: (_error, _id, context) => {
      queryClient.setQueryData(queryKeys.wishlists.detail(wishlistId), context?.previous);
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: queryKeys.wishlists.all }),
  });
}

export function useCreateWishlist() {
  const queryClient = useQueryClient();
  return useMutation<string, ApiError, string>({
    mutationFn: (name) => wishlistsApi.create(getBrowserHttp(), name),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.wishlists.list }),
  });
}

export function useRenameWishlist() {
  const queryClient = useQueryClient();
  return useMutation<void, ApiError, { id: string; name: string }, { previous?: WishlistSummary[] }>({
    mutationFn: ({ id, name }) => wishlistsApi.rename(getBrowserHttp(), id, name),
    onMutate: async ({ id, name }) => {
      await queryClient.cancelQueries({ queryKey: queryKeys.wishlists.list });
      const previous = queryClient.getQueryData<WishlistSummary[]>(queryKeys.wishlists.list);
      queryClient.setQueryData<WishlistSummary[]>(queryKeys.wishlists.list, (lists) =>
        lists?.map((list) => (list.id === id ? { ...list, name } : list)),
      );
      return { previous };
    },
    onError: (_error, _vars, context) => queryClient.setQueryData(queryKeys.wishlists.list, context?.previous),
    onSettled: () => queryClient.invalidateQueries({ queryKey: queryKeys.wishlists.all }),
  });
}

export function useDeleteWishlist() {
  const queryClient = useQueryClient();
  return useMutation<void, ApiError, string, { previous?: WishlistSummary[] }>({
    mutationFn: (id) => wishlistsApi.remove(getBrowserHttp(), id),
    onMutate: async (id) => {
      await queryClient.cancelQueries({ queryKey: queryKeys.wishlists.list });
      const previous = queryClient.getQueryData<WishlistSummary[]>(queryKeys.wishlists.list);
      queryClient.setQueryData<WishlistSummary[]>(queryKeys.wishlists.list, (lists) =>
        lists?.filter((list) => list.id !== id),
      );
      return { previous };
    },
    onError: (_error, _id, context) => queryClient.setQueryData(queryKeys.wishlists.list, context?.previous),
    onSettled: () => queryClient.invalidateQueries({ queryKey: queryKeys.wishlists.all }),
  });
}
