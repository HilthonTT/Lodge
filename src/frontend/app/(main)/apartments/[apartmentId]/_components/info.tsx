"use client";

import qs from "query-string";
import dynamic from "next/dynamic";
import { useRouter, useSearchParams } from "next/navigation";
import { useState } from "react";
import { TentTree } from "lucide-react";
import { MdOutlineBedroomChild } from "react-icons/md";
import { TbFriends } from "react-icons/tb";
import { CiCloudMoon } from "react-icons/ci";
import { BsDoorOpen } from "react-icons/bs";
import { Range, RangeKeyDict } from "react-date-range";
import { add, format } from "date-fns";

import { formatPrice } from "@/lib/utils";
import { INITIAL_DATE_RANGE } from "@/constants";
import { Separator } from "@/components/ui/separator";
import { Calendar } from "@/components/calendar";
import { useCountries } from "@/hooks/use-countries";
import { Button } from "@/components/ui/button";

import { InfoCard } from "./info-card";

type Props = {
  apartment: Apartment;
  bookedDates: Date[];
  priceDetails: PriceDetails;
};

const Map = dynamic(() => import("@/components/map"), {
  ssr: false,
});

export const Info = ({ apartment, bookedDates, priceDetails }: Props) => {
  const router = useRouter();
  const searchParams = useSearchParams();

  const [dateRange, setDateRange] = useState<Range>(INITIAL_DATE_RANGE);

  const { getByValue } = useCountries();

  const country = getByValue(apartment.address.country);

  const onChange = (item: RangeKeyDict) => {
    const newDateRange = item.selection as Range;
    setDateRange(newDateRange);

    const allParams = Object.fromEntries(searchParams.entries());
    const url = qs.stringifyUrl({
      url: `/apartments/${apartment.id}`,
      query: {
        ...allParams,
        startDate: format(newDateRange.startDate || new Date(), "yyyy-MM-dd"),
        endDate: format(
          newDateRange.endDate || add(new Date(), { days: 1 }),
          "yyyy-MM-dd"
        ),
      },
    });

    router.push(url);
  };

  return (
    <div className="relative flex items-start justify-center w-full gap-4 flex-col md:flex-row">
      <div className="md:w-[60%] w-full">
        <h3 className="text-lg font-semibold">{apartment.description}</h3>

        <Separator className="my-4" />

        <div className="flex items-center">
          <TentTree className="text-indigo-500 size-10" />
          <div className="flex flex-col items-start ml-4">
            <p className="font-semibold">Hosted by Lodge</p>
            <p className="text-muted-foreground">The one running the site!</p>
          </div>
        </div>

        <Separator className="my-4" />

        <div className="flex flex-col gap-y-4">
          <InfoCard
            title={`Many rooms (${apartment.maximumRoomCount})`}
            description="Your own room and rooms for your peers."
            icon={<MdOutlineBedroomChild className="size-7 text-neutral-700" />}
          />
          <InfoCard
            title={`Max guests (${apartment.maximumGuestCount})`}
            description="You can invite lots of friends!"
            icon={<TbFriends className="size-7 text-neutral-700" />}
          />
          <InfoCard
            title={`${formatPrice(
              apartment.currency,
              apartment.price
            )} / night`}
            description="Sleeping here isn't free as you might expect!"
            icon={<CiCloudMoon className="size-7 text-neutral-700" />}
          />
          <InfoCard
            title="Self check-in"
            description="Check yourself in with the keypad."
            icon={<BsDoorOpen className="size-7 text-neutral-700" />}
          />
        </div>

        <Separator className="my-4" />

        <div className="flex flex-col gap-4">
          <h3 className="font-semibold text-lg lg:text-xl">
            Located in {country?.label}
          </h3>
          <Map center={country?.latlng as L.LatLngExpression} />
        </div>
      </div>

      <div className="md:w-[40%] size-full">
        <div className="bg-white rounded-xl border border-neutral-200 overflow-hidden">
          <div className="p-4">
            <p>
              <span className="font-bold text-xl lg:text-2xl mr-2">
                {formatPrice(apartment.currency, apartment.price)}
              </span>
              <span className="text-lg">night</span>
            </p>
          </div>

          <Separator className="my-4" />

          <div className="flex items-center justify-center">
            <Calendar
              disabledDates={bookedDates}
              value={dateRange}
              onChange={onChange}
            />
          </div>

          <div className="flex flex-col px-5 mb-10 gap-2">
            <div className="flex items-center justify-between">
              <div className="font-semibold underline text-lg">
                Amenities up charge
              </div>
              <div>
                {formatPrice(
                  priceDetails.currency,
                  priceDetails.amenitiesUpCharge
                )}
              </div>
            </div>

            <div className="flex items-center justify-between">
              <div className="font-semibold underline text-lg">
                Cleaning fee
              </div>
              <div>
                {formatPrice(priceDetails.currency, priceDetails.cleaningFee)}
              </div>
            </div>

            <div className="flex items-center justify-between">
              <div className="font-semibold underline text-lg">Total price</div>
              <div>
                {formatPrice(priceDetails.currency, priceDetails.totalPrice)}
              </div>
            </div>
          </div>

          <Button className="p-4">Reserve now</Button>
        </div>
      </div>
    </div>
  );
};
