"use client";

import { SearchIcon } from "lucide-react";

import { useLocationApartment } from "@/features/apartments/hooks/use-location-apartment";

export const Search = () => {
  const { onOpen: openLocation } = useLocationApartment();

  return (
    <div className="flex items-center justify-center size-full">
      <div className="flex items-center justify-center w-full min-w-[360px]">
        <div className="flex rounded-full border border-neutral-200 hover:shadow-lg transition cursor-pointer">
          <div onClick={openLocation} className="search-section rounded-l-full">
            <p>Anywhere</p>
          </div>
          <div className="search-section border-l border-neutral-200 hover:bg-neutral-100 transition">
            <p>Any week</p>
          </div>
          <div className="relative search-section !pr-16 border-l border-neutral-200 rounded-r-full">
            <p>Add guests</p>
            <div className="absolute right-1 bg-indigo-500 hover:bg-indigo-600 transition rounded-full p-2 ml-4 flex items-center size-9 my-auto mr-2 justify-center">
              <SearchIcon className="size-4 text-white" />
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
