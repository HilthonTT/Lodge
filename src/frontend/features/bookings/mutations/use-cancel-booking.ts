import { useQueryClient, useMutation } from "@tanstack/react-query";

import { cancelBooking } from "@/actions/bookings/cancel-booking";
import { QUERY_KEYS } from "@/features/query-keys";

export const useCancelBooking = (userId: string) => {
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: async (request: CancelBookingRequest) =>
      await cancelBooking(request),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: [QUERY_KEYS.GET_BOOKINGS_BY_USER_ID, { userId }],
      });
    },
  });

  return mutation;
};
