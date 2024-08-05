"use client";

import { redirect } from "next/navigation";

import { useUserContext } from "@/context/auth-context";
import { DirectionAwareHover } from "@/components/ui/direction-aware-hover";

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
        <DirectionAwareHover
          className="size-full rounded-none"
          imageUrl="/assets/images/side-img.jpg">
          <p className="font-bold text-xl">Welcome back!</p>
        </DirectionAwareHover>
      </div>
    </div>
  );
};

export default AuthLayout;
