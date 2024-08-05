"use client";

import Link from "next/link";
import { CircleUserRound, Menu, Search, TentTree } from "lucide-react";
import { Open_Sans } from "next/font/google";

import { cn } from "@/lib/utils";
import { useUserContext } from "@/context/auth-context";

const font = Open_Sans({
  subsets: ["latin"],
  weight: ["300", "400", "500", "600", "700", "800"],
});

export const Navbar = () => {
  const { user } = useUserContext();

  return (
    <div className="w-full h-20 border-b border-b-neutral-200 top-0 absolute">
      <div className="mx-24 flex items-center h-full">
        <Link
          href="/"
          className="items-center justify-center group hidden md:flex">
          <TentTree className="text-indigo-500 size-10 group-hover:text-black transition" />
          <p
            className={cn(
              "font-bold text-indigo-500 group-hover:text-black transition text-xl",
              font.className
            )}>
            Lodge
          </p>
        </Link>

        <div className="flex items-center justify-center w-full min-w-[360px]">
          <div className="rounded-full px-4 py-3 border border-neutral-200 hover:shadow-lg transition cursor-pointer flex items-center">
            <p className="text-neutral-700">Anywhere</p>
            <span className="mx-2 opacity-40">|</span>
            <p className="text-neutral-700">Any week</p>
            <span className="mx-2 opacity-40">|</span>
            <span className="text-neutral-700">Add guests</span>
            <div className="bg-indigo-500 rounded-full ml-4 p-2">
              <Search className="size-4 text-white" />
            </div>
          </div>
        </div>

        <div className="ml-auto hidden md:flex">
          <div className="rounded-full py-2 px-4 border border-neutral-200  hover:shadow-lg transition cursor-pointer">
            <div className="flex items-center justify-center gap-4">
              <Menu className="size-5" />
              <CircleUserRound className="size-8 text-white bg-neutral-600 rounded-full" />
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
