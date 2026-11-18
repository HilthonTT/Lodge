"use client";

import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

import { useSession, useUserId } from "@/features/auth/session-provider";
import { getBrowserHttp } from "@/lib/api/browser";
import type { ApiError } from "@/lib/api/client";
import { queryKeys } from "@/lib/query-keys";
import type {
  CreateSessionsRequest,
  Experience,
  ExperienceBooking,
  ExperienceRequest,
  ExperienceReview,
  ExperienceSearchParams,
  ExperienceSession,
  ExperienceSummary,
  HostExperienceSession,
  HostExperienceSummary,
  PagedList,
} from "@/types/api";

import { experienceBookingsApi, experiencesApi, hostExperiencesApi } from "../api/experiences";

const noRetryOnClientError = (count: number, error: ApiError) =>
  !(error.status && error.status >= 400 && error.status < 500) && count < 2;

/* ------------------------------------------------------------------ */
/* Public                                                              */
/* ------------------------------------------------------------------ */

export function useExperienceSearch(params: ExperienceSearchParams) {
  return useQuery<PagedList<ExperienceSummary>, ApiError>({
    queryKey: queryKeys.experiences.search(params),
    queryFn: () => experiencesApi.search(getBrowserHttp(), params),
    placeholderData: keepPreviousData,
    staleTime: 30_000,
  });
}

export function useExperienceSessions(id: string) {
  return useQuery<ExperienceSession[], ApiError>({
    queryKey: queryKeys.experiences.sessions(id),
    queryFn: () => experiencesApi.sessions(getBrowserHttp(), id),
    staleTime: 15_000,
    refetchOnWindowFocus: true,
  });
}

export function useExperienceReviews(id: string, page: number, initial?: PagedList<ExperienceReview> | null) {
  return useQuery<PagedList<ExperienceReview>, ApiError>({
    queryKey: queryKeys.experiences.reviews(id, page),
    queryFn: () => experiencesApi.reviews(getBrowserHttp(), id, { page, pageSize: 6 }),
    initialData: page === 1 && initial ? initial : undefined,
    placeholderData: keepPreviousData,
    staleTime: 60_000,
  });
}

/* ------------------------------------------------------------------ */
/* Guest                                                               */
/* ------------------------------------------------------------------ */

export function useBookExperience() {
  const queryClient = useQueryClient();
  return useMutation<string, ApiError, { sessionId: string; guestCount: number; idempotencyKey: string }>({
    mutationFn: async ({ sessionId, guestCount, idempotencyKey }) => {
      const http = getBrowserHttp();
      const id = await experienceBookingsApi.create(http, { sessionId, guestCount }, idempotencyKey);
      return experienceBookingsApi.checkout(http, id);
    },
    onSettled: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.experienceBookings.all }),
        queryClient.invalidateQueries({ queryKey: queryKeys.experiences.all }),
      ]),
  });
}

/** Payment for an unpaid (Reserved) booking: resolves to the checkout URL. */
export function useExperienceCheckout() {
  return useMutation<string, ApiError, string>({
    mutationFn: (id) => experienceBookingsApi.checkout(getBrowserHttp(), id),
    onSuccess: (url) => window.location.assign(url),
  });
}

export function useUserExperienceBookings() {
  const userId = useUserId();
  return useQuery<ExperienceBooking[], ApiError>({
    queryKey: queryKeys.experienceBookings.byUser(userId ?? ""),
    queryFn: () => experienceBookingsApi.byUser(getBrowserHttp(), userId!),
    enabled: userId !== null,
    staleTime: 15_000,
    refetchOnWindowFocus: true,
    // Refunds settle asynchronously: poll until they do.
    refetchInterval: (query) => (query.state.data?.some((b) => b.refundStatus === "Pending") ? 3_000 : false),
  });
}

export function useExperienceBooking(id: string | undefined) {
  return useQuery<ExperienceBooking, ApiError>({
    queryKey: queryKeys.experienceBookings.detail(id ?? ""),
    queryFn: () => experienceBookingsApi.get(getBrowserHttp(), id!),
    enabled: Boolean(id),
    staleTime: 5_000,
    retry: noRetryOnClientError,
    refetchInterval: (query) => (query.state.data?.refundStatus === "Pending" ? 3_000 : false),
  });
}

export function useCancelExperienceBooking() {
  const queryClient = useQueryClient();
  return useMutation<void, ApiError, string>({
    mutationFn: (id) => experienceBookingsApi.cancel(getBrowserHttp(), id),
    onSettled: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.experienceBookings.all }),
        queryClient.invalidateQueries({ queryKey: queryKeys.experiences.all }),
      ]),
  });
}

export function useReviewExperience() {
  const queryClient = useQueryClient();
  return useMutation<string, ApiError, { bookingId: string; rating: number; comment: string }>({
    mutationFn: ({ bookingId, ...body }) => experienceBookingsApi.review(getBrowserHttp(), bookingId, body),
    onSuccess: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.experienceBookings.all }),
        queryClient.invalidateQueries({ queryKey: queryKeys.experiences.all }),
      ]),
  });
}

export function useCompleteSimulatedExperiencePayment() {
  const queryClient = useQueryClient();
  return useMutation<void, ApiError, string>({
    mutationFn: (id) => experienceBookingsApi.completeSimulated(getBrowserHttp(), id),
    onSettled: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.experienceBookings.all }),
        queryClient.invalidateQueries({ queryKey: queryKeys.hostExperiences.all }),
      ]),
  });
}

/* ------------------------------------------------------------------ */
/* Host                                                                */
/* ------------------------------------------------------------------ */

function useVerified() {
  return Boolean(useSession().session?.emailVerified);
}

export function useHostExperiences(params: { page: number; pageSize: number }) {
  const verified = useVerified();
  return useQuery<PagedList<HostExperienceSummary>, ApiError>({
    queryKey: queryKeys.hostExperiences.list(params),
    queryFn: () => hostExperiencesApi.list(getBrowserHttp(), params),
    enabled: verified,
    placeholderData: keepPreviousData,
    staleTime: 15_000,
    retry: noRetryOnClientError,
  });
}

export function useHostExperience(id: string | undefined) {
  const verified = useVerified();
  return useQuery<Experience, ApiError>({
    queryKey: queryKeys.hostExperiences.detail(id ?? ""),
    queryFn: () => hostExperiencesApi.get(getBrowserHttp(), id!),
    enabled: verified && Boolean(id),
    staleTime: 15_000,
    retry: noRetryOnClientError,
  });
}

function useInvalidateHostExperiences() {
  const queryClient = useQueryClient();
  return () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: queryKeys.hostExperiences.all }),
      queryClient.invalidateQueries({ queryKey: queryKeys.experiences.all }),
    ]);
}

export function useSaveExperience(id?: string) {
  const invalidate = useInvalidateHostExperiences();
  return useMutation<string, ApiError, ExperienceRequest>({
    mutationFn: async (body) => {
      const http = getBrowserHttp();
      if (id) {
        await hostExperiencesApi.update(http, id, body);
        return id;
      }
      return hostExperiencesApi.create(http, body);
    },
    onSuccess: () => invalidate(),
  });
}

export function useSetExperienceListed() {
  const invalidate = useInvalidateHostExperiences();
  return useMutation<void, ApiError, { id: string; isListed: boolean }>({
    mutationFn: ({ id, isListed }) => hostExperiencesApi.setListed(getBrowserHttp(), id, isListed),
    onSettled: () => invalidate(),
  });
}

export function useDeleteExperience() {
  const invalidate = useInvalidateHostExperiences();
  return useMutation<void, ApiError, string>({
    mutationFn: (id) => hostExperiencesApi.remove(getBrowserHttp(), id),
    onSuccess: () => invalidate(),
  });
}

export function useHostSessions(id: string, range: { from: string; to: string }) {
  const verified = useVerified();
  return useQuery<HostExperienceSession[], ApiError>({
    queryKey: [...queryKeys.hostExperiences.sessions(id), range],
    queryFn: () => hostExperiencesApi.sessions(getBrowserHttp(), id, range),
    enabled: verified,
    placeholderData: keepPreviousData,
    staleTime: 10_000,
    retry: noRetryOnClientError,
  });
}

export function useAddSessions(id: string) {
  const invalidate = useInvalidateHostExperiences();
  return useMutation<string[], ApiError, CreateSessionsRequest>({
    mutationFn: (body) => hostExperiencesApi.addSessions(getBrowserHttp(), id, body),
    onSuccess: () => invalidate(),
  });
}

export function useCancelSession(id: string) {
  const invalidate = useInvalidateHostExperiences();
  return useMutation<void, ApiError, string>({
    mutationFn: (sessionId) => hostExperiencesApi.cancelSession(getBrowserHttp(), id, sessionId),
    onSettled: () => invalidate(),
  });
}

export function useHostExperienceBookings(params: { page: number; pageSize: number; experienceId?: string }) {
  const verified = useVerified();
  return useQuery<PagedList<ExperienceBooking>, ApiError>({
    queryKey: queryKeys.hostExperiences.bookings(params),
    queryFn: () => hostExperiencesApi.bookings(getBrowserHttp(), params),
    enabled: verified,
    placeholderData: keepPreviousData,
    staleTime: 10_000,
    retry: noRetryOnClientError,
  });
}
