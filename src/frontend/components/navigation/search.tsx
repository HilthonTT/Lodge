"use client";

import { SearchIcon } from "lucide-react";

export const Search = () => {
  return (
    <div className="flex items-center justify-center size-full">
      <div className="flex items-center justify-center w-full min-w-[360px]">
        <div className="rounded-full px-4 py-3 border border-neutral-200 hover:shadow-lg transition cursor-pointer flex items-center">
          <p className="text-neutral-700">Anywhere</p>
          <span className="mx-2 opacity-40">|</span>
          <p className="text-neutral-700">Any week</p>
          <span className="mx-2 opacity-40">|</span>
          <span className="text-neutral-700">Add guests</span>
          <div className="bg-indigo-500 rounded-full ml-4 p-2">
            <SearchIcon className="size-4 text-white" />
          </div>
        </div>
      </div>
    </div>
  );
};
