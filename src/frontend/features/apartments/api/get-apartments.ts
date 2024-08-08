import { requestGetAxios } from "@/lib/axios";

export const getApartments = async (page: number, pageSize: number) => {
  const data = await requestGetAxios("apartments", { page, pageSize });

  const apartments = data as ApartmentPagedList;

  return apartments;
};
