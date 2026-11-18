import Image from "next/image";
import { notFound } from "next/navigation";

import { DATE_10_DAYS_LATER, INITIAL_DATE } from "@/constants";
import { getApartmentById } from "@/actions/apartments/get-apartment-by-id";
import { getApartmentBookedDates } from "@/actions/apartments/get-apartment-booked-dates";
import { getApartmentPrice } from "@/actions/apartments/get-apartment-price";

import { Header } from "./_components/header";
import { Info } from "./_components/info";

type Props = {
  params: {
    apartmentId: string;
  };
  searchParams: {
    startDate?: string;
    endDate?: string;
  };
};

const ApartmentPage = async ({
  params: { apartmentId },
  searchParams: { startDate, endDate },
}: Props) => {
  const apartment = await getApartmentById(apartmentId);

  if (!apartment) {
    return notFound();
  }

  const [bookedDates, priceDetails] = await Promise.all([
    getApartmentBookedDates(apartmentId),
    getApartmentPrice(
      apartmentId,
      startDate || INITIAL_DATE,
      endDate || DATE_10_DAYS_LATER
    ),
  ]);

  if (!priceDetails) {
    return notFound();
  }

  return (
    <div className="max-w-6xl mx-auto mb-24">
      <Header apartment={apartment} />
      <div className="w-full h-[525px] relative my-2">
        <Image
          src={apartment.imageUrl}
          alt={apartment.name}
          className="object-fit rounded-lg bg-no-repeat shadow-xl"
          fill
        />
      </div>
      <Info
        apartment={apartment}
        bookedDates={bookedDates}
        priceDetails={priceDetails}
      />
    </div>
  );
};

export default ApartmentPage;
