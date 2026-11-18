"use client";

import { usePathname } from "next/navigation";

import { SearchBar } from "@/features/apartments/components/search-bar";
import { ExperienceSearchBar } from "@/features/experiences/components/experience-search-bar";

import { isExperiencesPath } from "./discover-toggle";

/** Stay search everywhere, experience search on /experiences. */
export function NavbarSearch() {
  const pathname = usePathname();
  return isExperiencesPath(pathname) ? <ExperienceSearchBar /> : <SearchBar />;
}
