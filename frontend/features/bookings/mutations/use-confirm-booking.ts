import { useQueryClient, useMutation } from "@tanstack/react-query";

import { confirmBooking } from "@/actions/bookings/confirm-booking";
import { QUERY_KEYS } from "@/features/query-keys";

export const useConfirmBooking = (userId: string) => {
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: async (request: ConfirmBookingRequest) =>
      await confirmBooking(request),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: [QUERY_KEYS.GET_BOOKINGS_BY_USER_ID, { userId }],
      });
    },
  });

  return mutation;
};
