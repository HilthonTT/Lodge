import { reserveBooking } from "@/actions/bookings/reserve-booking";
import { useMutation } from "@tanstack/react-query";

export const useReserveBooking = () => {
  const mutation = useMutation({
    mutationFn: async (request: ReserveBookingRequest) =>
      await reserveBooking(request),
  });

  return mutation;
};
