"use client";

import qs from "query-string";
import { useRouter, useSearchParams } from "next/navigation";
import {
  FaWifi,
  FaSnowflake,
  FaCar,
  FaPaw,
  FaSwimmingPool,
  FaDumbbell,
  FaSpa,
  FaMountain,
  FaTree,
  FaHome,
  FaRocket,
  FaWater,
  FaCampground,
  FaGlobeAmericas,
  FaHouseDamage,
  FaCity,
} from "react-icons/fa";
import { GiCastle, GiTreehouse, GiCaveEntrance, GiBarn } from "react-icons/gi";
import { MdOutlineAgriculture, MdCabin } from "react-icons/md";
import { LuPalmtree, LuFlower2 } from "react-icons/lu";
import { IoBoatOutline } from "react-icons/io5";

import { Amenity } from "@/enums";
import { ScrollArea, ScrollBar } from "@/components/ui/scroll-area";
import { cn } from "@/lib/utils";

const iconMap = {
  [Amenity.WiFi]: FaWifi,
  [Amenity.AirConditioning]: FaSnowflake,
  [Amenity.Parking]: FaCar,
  [Amenity.PetFriendly]: FaPaw,
  [Amenity.SwimmingPool]: FaSwimmingPool,
  [Amenity.Gym]: FaDumbbell,
  [Amenity.Spa]: FaSpa,
  [Amenity.Terrace]: LuFlower2,
  [Amenity.MountainView]: FaMountain,
  [Amenity.GardenView]: FaTree,
  [Amenity.CountrySide]: FaHome,
  [Amenity.TinyHomes]: FaHome,
  [Amenity.OMG]: FaRocket,
  [Amenity.Cabins]: MdCabin,
  [Amenity.Lakefront]: FaWater,
  [Amenity.Treehouses]: GiTreehouse,
  [Amenity.Camping]: FaCampground,
  [Amenity.Castles]: GiCastle,
  [Amenity.Farms]: MdOutlineAgriculture,
  [Amenity.Boats]: IoBoatOutline,
  [Amenity.Domes]: FaGlobeAmericas,
  [Amenity.Tropical]: LuPalmtree,
  [Amenity.Mansions]: FaHouseDamage,
  [Amenity.Caves]: GiCaveEntrance,
  [Amenity.Barns]: GiBarn,
  [Amenity.TopCities]: FaCity,
};

const labelMap = {
  [Amenity.WiFi]: "WiFi",
  [Amenity.AirConditioning]: "Air Conditioning",
  [Amenity.Parking]: "Parking",
  [Amenity.PetFriendly]: "Pet Friendly",
  [Amenity.SwimmingPool]: "Swimming Pool",
  [Amenity.Gym]: "Gym",
  [Amenity.Spa]: "Spa",
  [Amenity.Terrace]: "Terrace",
  [Amenity.MountainView]: "Mountain View",
  [Amenity.GardenView]: "Garden View",
  [Amenity.CountrySide]: "Countryside",
  [Amenity.TinyHomes]: "Tiny Homes",
  [Amenity.OMG]: "OMG!",
  [Amenity.Cabins]: "Cabins",
  [Amenity.Lakefront]: "Lakefront",
  [Amenity.Treehouses]: "Treehouses",
  [Amenity.Camping]: "Camping",
  [Amenity.Castles]: "Castles",
  [Amenity.Farms]: "Farms",
  [Amenity.Boats]: "Boats",
  [Amenity.Domes]: "Domes",
  [Amenity.Tropical]: "Tropical",
  [Amenity.Mansions]: "Mansions",
  [Amenity.Caves]: "Caves",
  [Amenity.Barns]: "Barns",
  [Amenity.TopCities]: "Top Cities",
};

export const Amenities = () => {
  const router = useRouter();
  const searchParams = useSearchParams();

  const amenityValues = Object.values(Amenity).filter(
    (value) => typeof value === "number"
  ) as number[];

  const onClick = (amenity: Amenity) => {
    const url = qs.stringifyUrl({
      url: "/",
      query: {
        amenity,
      },
    });

    router.push(url);
  };

  return (
    <div className="absolute top-20">
      <ScrollArea className="w-screen whitespace-nowrap px-12">
        <div className="flex items-center space-x-8 p-4">
          {amenityValues.map((amenity) => {
            const Icon = iconMap[amenity as Amenity];
            const label = labelMap[amenity as Amenity];

            const isSelected = Number(searchParams.get("amenity")) == amenity;

            return (
              <div
                key={amenity}
                className="flex flex-col items-center space-y-2 cursor-pointer"
                onClick={() => onClick(amenity)}>
                <Icon className="w-6 h-6" />
                <p className="text-sm">{label}</p>

                <div
                  className={cn(
                    "border h-1 w-full bg-neutral-200",
                    isSelected && "bg-neutral-500"
                  )}
                />
              </div>
            );
          })}
        </div>
        <ScrollBar orientation="horizontal" />
      </ScrollArea>
    </div>
  );
};
