"use client";

import { redirect } from "next/navigation";

import { Loader } from "@/components/loader";
import { useUserContext } from "@/context/auth-context";

import { Header } from "./_components/header";
import { Client } from "./_components/client";

const BookingsPage = () => {
  const { isAuthenticated, isLoading, user } = useUserContext();

  if (isLoading) {
    return <Loader />;
  }

  if (!isAuthenticated) {
    return redirect("/login");
  }

  return (
    <div className="mx-auto flex flex-col max-w-7xl space-y-14">
      <Header user={user} />
      <Client user={user} />
    </div>
  );
};

export default BookingsPage;
