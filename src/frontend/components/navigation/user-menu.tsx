"use client";

import { CircleUserRound, Menu } from "lucide-react";
import { FaUserCircle, FaUserCog, FaBook, FaShieldAlt } from "react-icons/fa";
import { useRouter } from "next/navigation";

import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { useUserContext } from "@/context/auth-context";
import { Loader } from "@/components/loader";

export const UserMenu = () => {
  const router = useRouter();
  const { isAuthenticated, isLoading } = useUserContext();

  const onProfileClick = () => {
    router.push("/profile");
  };

  const onBookingsClick = () => {
    router.push("/bookings");
  };

  const onSettingsClick = () => {
    router.push("/settings");
  };

  const onLoginClick = () => {
    router.push("/login");
  };

  if (isLoading) {
    return <Loader />;
  }

  if (isAuthenticated) {
    return (
      <DropdownMenu>
        <DropdownMenuTrigger>
          <div className="ml-auto hidden md:flex">
            <div className="rounded-full py-2 px-4 border border-neutral-200  hover:shadow-lg transition cursor-pointer">
              <div className="flex items-center justify-center gap-4">
                <Menu className="size-5" />
                <CircleUserRound className="size-8 text-white bg-neutral-600 rounded-full" />
              </div>
            </div>
          </div>
        </DropdownMenuTrigger>
        <DropdownMenuContent className="space-y-2">
          <DropdownMenuItem onClick={onProfileClick} className="gap-2">
            <FaUserCircle className="size-5" />
            <p className="font-semibold tracking-wider">Profile</p>
          </DropdownMenuItem>

          <DropdownMenuItem onClick={onBookingsClick} className="gap-2">
            <FaBook className="size-5" />
            <p className="font-semibold tracking-wider">My bookings</p>
          </DropdownMenuItem>
          <DropdownMenuItem onClick={onSettingsClick} className="gap-2">
            <FaUserCog className="size-5" />
            <p className="font-semibold tracking-wider">Settings</p>
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
    );
  }

  return (
    <div className="ml-auto hidden md:flex">
      <div
        onClick={onLoginClick}
        role="button"
        className="rounded-full py-2 px-4 border border-neutral-200  hover:shadow-lg transition cursor-pointer">
        <div className="flex items-center justify-center gap-4">
          <FaShieldAlt />
          <p className="font-semibold tracking-wider">Login</p>
        </div>
      </div>
    </div>
  );
};
