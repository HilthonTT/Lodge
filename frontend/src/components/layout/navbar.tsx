import { Suspense } from "react";

import { Container } from "@/components/shared/container";
import { Logo } from "@/components/shared/logo";
import { ThemeToggle } from "@/components/shared/theme-toggle";
import { Skeleton } from "@/components/ui/skeleton";
import { SearchBar } from "@/features/apartments/components/search-bar";

import { UserMenu } from "./user-menu";

export function Navbar() {
  return (
    <header className="sticky top-0 z-40 border-b bg-background/85 backdrop-blur-md supports-backdrop-filter:bg-background/70">
      <Container className="flex h-20 items-center gap-3 sm:gap-6">
        <Logo compact className="shrink-0" />
        <div className="flex min-w-0 flex-1 justify-center">
          <Suspense
            fallback={
              <Skeleton className="h-12 w-full max-w-md rounded-full" />
            }
          >
            <SearchBar />
          </Suspense>
        </div>
        <nav aria-label="Account" className="flex shrink-0 items-center gap-1">
          <ThemeToggle />
          <UserMenu />
        </nav>
      </Container>
    </header>
  );
}
