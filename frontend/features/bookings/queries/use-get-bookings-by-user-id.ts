import { useQuery } from "@tanstack/react-query";

import { QUERY_KEYS } from "@/features/query-keys";
import { getBookingsByUserId } from "@/actions/bookings/get-bookings-by-user-id";

export const useGetBookingsByUserId = (userId: string, jwtToken: string) => {
  const query = useQuery({
    queryKey: [QUERY_KEYS.GET_BOOKINGS_BY_USER_ID, { userId }],
    queryFn: () => getBookingsByUserId(userId, jwtToken),
  });

  return query;
};
