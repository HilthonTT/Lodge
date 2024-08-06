"use client";

import { useUserContext } from "@/context/auth-context";
import { Container } from "@/components/container";
import { Logo } from "@/components/navigation/logo";
import { Search } from "@/components/navigation/search";
import { UserMenu } from "@/components/navigation/user-menu";
import { Amenities } from "@/components/navigation/amenities";

export const Navbar = () => {
  const { user } = useUserContext();

  return (
    <div className="fixd w-full bg-white z-10 shadow-sm">
      <div className="py-4 border-b">
        <Container>
          <div className="flex flex-row items-center justify-between gap-3 md:gap-8">
            <Logo />
            <Search />
            <UserMenu />
          </div>
        </Container>
      </div>
      <Amenities />
    </div>
  );
};
