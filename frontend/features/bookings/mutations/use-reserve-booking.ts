import { useMutation, useQueryClient } from "@tanstack/react-query";

import { reserveBooking } from "@/actions/bookings/reserve-booking";
import { QUERY_KEYS } from "@/features/query-keys";

export const useReserveBooking = (userId: string) => {
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: async (request: ReserveBookingRequest) =>
      await reserveBooking(request),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: [QUERY_KEYS.GET_BOOKINGS_BY_USER_ID, { userId }],
      });
    },
  });

  return mutation;
};
