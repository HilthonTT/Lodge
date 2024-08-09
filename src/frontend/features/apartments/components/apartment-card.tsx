"use client";

import Image from "next/image";
import { useRouter } from "next/navigation";

import { formatPrice } from "@/lib/utils";

type Props = {
  apartment: Apartment;
};

export const ApartmentCard = ({ apartment }: Props) => {
  const router = useRouter();

  const onClick = () => {
    router.push(`/apartments/${apartment.id}`);
  };

  return (
    <div
      onClick={onClick}
      className="flex flex-col gap-2 cursor-pointer group shadow-sm rounded-xl p-2 hover:shadow-lg transition">
      <div className="aspect-square relative  rounded-xl h-full">
        <Image
          src={apartment.imageUrl}
          alt={apartment.name}
          className="object-cover rounded-xl h-full group-hover:scale-110 transform"
          height={500}
          width={500}
        />
      </div>
      <div className="font-bold text-lg truncate">{apartment.name}</div>
      <div className="font-semibold text-md line-clamp-1">
        {apartment.address.state}, {apartment.address.country}
      </div>
      <div className="flex flex-row items-center gap-1">
        <p className="font-semibold">
          {formatPrice(apartment.currency, apartment.price)}
        </p>
        <p className="text-gray-500">/ night</p>
      </div>
    </div>
  );
};
