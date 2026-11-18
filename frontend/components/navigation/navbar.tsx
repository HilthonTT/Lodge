"use client";

import { usePathname } from "next/navigation";

import { Container } from "@/components/container";
import { Logo } from "@/components/navigation/logo";
import { Search } from "@/components/navigation/search";
import { UserMenu } from "@/components/navigation/user-menu";
import { Amenities } from "@/components/navigation/amenities";

export const Navbar = () => {
  const pathname = usePathname();

  return (
    <div className="w-full bg-white z-10 shadow-sm">
      <div className="py-4 border-b">
        <Container>
          <div className="flex flex-row items-center justify-between gap-3 md:gap-8">
            <Logo />
            <Search />
            <UserMenu />
          </div>
        </Container>
      </div>
      {pathname === "/" && <Amenities />}
    </div>
  );
};
