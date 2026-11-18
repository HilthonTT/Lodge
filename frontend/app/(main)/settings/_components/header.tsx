"use client";

import Link from "next/link";

import { useUserContext } from "@/context/auth-context";

export const Header = () => {
  const { user } = useUserContext();

  return (
    <div className="flex flex-col">
      <h1 className="text-2xl lg:text-4xl font-semibold">Account</h1>
      <h2 className="text-lg xl:text-xl line-clamp-1">
        <span className="font-semibold">{user.name}</span>,{" "}
        <span>{user.email}</span> ·{" "}
        <Link href="/profile" className="font-semibold underline">
          Go to profile
        </Link>
      </h2>
    </div>
  );
};
