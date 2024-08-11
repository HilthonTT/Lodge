import { notFound } from "next/navigation";

import { getApartmentById } from "@/actions/apartments/get-apartment-by-id";

type Props = {
  params: {
    apartmentId: string;
  };
};

const ApartmentPage = async ({ params: { apartmentId } }: Props) => {
  const apartment = await getApartmentById(apartmentId);

  if (!apartment) {
    return notFound();
  }

  return <div>{apartment.name}</div>;
};

export default ApartmentPage;
