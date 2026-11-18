import Image from "next/image";
import { parseISO, differenceInDays } from "date-fns";

import { Separator } from "@/components/ui/separator";
import { formatPrice } from "@/lib/utils";

type Props = {
  apartment: Apartment;
  startDate: string;
  endDate: string;
  priceDetails: PriceDetails;
};

export const PriceDetails = ({
  apartment,
  startDate,
  endDate,
  priceDetails,
}: Props) => {
  const start = parseISO(startDate);
  const end = parseISO(endDate);

  // Calculate the number of days between the start and end dates
  const numberOfDays = differenceInDays(end, start);

  return (
    <div className="rounded-xl border border-neutral-300 p-4 mt-[60px] w-full">
      <div className="flex items-start gap-2">
        <div className="relative size-24">
          <Image
            src={apartment.imageUrl}
            alt={apartment.name}
            className="object-cover rounded-lg"
            fill
          />
        </div>
        <p className="line-clamp-1 font-bold">{apartment.name}</p>
      </div>
      <Separator className="my-4" />
      <div className="flex flex-col gap-y-4">
        <h2 className="font-bold text-xl xl:text-2xl">Price details</h2>

        <div className="flex items-center justify-between">
          <p className="font-semibold">
            {formatPrice(apartment.currency, apartment.price)} x {numberOfDays}{" "}
            nights
          </p>
          <p>{formatPrice(apartment.currency, priceDetails.pricePerPeriod)}</p>
        </div>

        <div className="flex items-center justify-between">
          <p className="font-semibold">Amenities up charge</p>
          <p>
            {formatPrice(apartment.currency, priceDetails.amenitiesUpCharge)}
          </p>
        </div>

        <div className="flex items-center justify-between">
          <p className="font-semibold">Cleaning fee</p>
          <p>{formatPrice(apartment.currency, priceDetails.cleaningFee)}</p>
        </div>

        <Separator className="my-4" />

        <div className="flex items-center justify-between">
          <p className="font-semibold">Total price</p>
          <p>{formatPrice(apartment.currency, priceDetails.totalPrice)}</p>
        </div>
      </div>
    </div>
  );
};
