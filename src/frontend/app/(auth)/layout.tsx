"use client";

import Image from "next/image";
import { redirect } from "next/navigation";

import { useUserContext } from "@/context/auth-context";

type Props = {
  children: React.ReactNode;
};

const AuthLayout = ({ children }: Props) => {
  const { isAuthenticated } = useUserContext();

  if (isAuthenticated) {
    return redirect("/");
  }

  return (
    <div className="flex bg-gray-100 overflow-y-hidden">
      <section className="flex flex-col flex-1 w-full">{children}</section>

      <div className="relative w-1/2 h-auto hidden xl:block">
        <Image
          src="/assets/images/side-img.jpg"
          alt="logo"
          className="object-cover bg-no-repeat"
          fill
        />
      </div>
    </div>
  );
};

export default AuthLayout;
