import { useQuery } from "@tanstack/react-query";

import { QUERY_KEYS } from "@/features/query-keys";
import { getApartments } from "@/features/apartments/api/get-apartments";

export const useGetApartments = (page: number = 1, pageSize: number = 36) => {
  const query = useQuery({
    queryKey: [QUERY_KEYS.GET_APARTMENTS],
    queryFn: async () => await getApartments(page, pageSize),
  });

  return query;
};
