import { notFound } from "next/navigation";

import { getApartmentById } from "@/actions/apartments/get-apartment-by-id";
import { getApartmentPrice } from "@/actions/apartments/get-apartment-price";

import { Header } from "./_components/header";
import { Info } from "./_components/info";
import { GoBackButton } from "./_components/go-back-button";
import { PriceDetails } from "./_components/price-details";

type Props = {
  params: {
    apartmentId: string;
  };
  searchParams: {
    startDate: string;
    endDate: string;
  };
};

const StaysPage = async ({
  params: { apartmentId },
  searchParams: { startDate, endDate },
}: Props) => {
  const apartment = await getApartmentById(apartmentId);

  if (!apartment) {
    return notFound();
  }

  const priceDetails = await getApartmentPrice(apartmentId, startDate, endDate);
  if (!priceDetails) {
    return notFound();
  }

  return (
    <div className="max-w-6xl mx-auto mb-24 flex flex-col lg:flex-row">
      <div className="w-auto mr-4 hidden lg:block">
        <GoBackButton apartmentId={apartmentId} />
      </div>
      <div className="flex-1 size-full">
        <Header apartmentId={apartmentId} />
        <Info apartment={apartment} startDate={startDate} endDate={endDate} />
      </div>
      <div className="ml-4 size-full p-2 flex">
        <PriceDetails
          apartment={apartment}
          startDate={startDate}
          endDate={endDate}
          priceDetails={priceDetails}
        />
      </div>
    </div>
  );
};

export default StaysPage;
