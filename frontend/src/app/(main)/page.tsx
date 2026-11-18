import { Home } from "lucide-react";
import Link from "next/link";

import { Container } from "@/components/shared/container";
import { Pagination } from "@/components/shared/pagination";
import { EmptyState, ErrorState } from "@/components/shared/state";
import { Button } from "@/components/ui/button";
import { apartmentsApi } from "@/features/apartments/api/apartments";
import { ApartmentGrid } from "@/features/apartments/components/apartment-grid";
import { PAGE_SIZE, parseApartmentSearch, totalPages } from "@/features/apartments/search-params";
import { attempt } from "@/lib/api/client";
import { getServerHttp } from "@/lib/api/server";

export default async function HomePage({ searchParams }: PageProps<"/">) {
  const { page } = parseApartmentSearch(await searchParams);
  const http = await getServerHttp();
  const result = await attempt(() => apartmentsApi.list(http, { page, pageSize: PAGE_SIZE }));

  return (
    <Container className="grid gap-10">
      <section className="relative overflow-hidden rounded-3xl border bg-gradient-to-br from-accent via-background to-background px-6 py-10 sm:px-10 sm:py-14">
        <div className="relative z-10 grid max-w-2xl gap-4">
          <p className="text-sm font-medium text-accent-foreground">Stays worth travelling for</p>
          <h1 className="text-3xl font-semibold sm:text-5xl">Find a place that feels like yours.</h1>
          <p className="text-base text-muted-foreground sm:text-lg">
            Cabins, lofts and family homes in destinations around the world. Pick your dates, see the full
            price up front and reserve in a few clicks.
          </p>
        </div>
        <div
          aria-hidden="true"
          className="absolute -top-24 -right-24 size-80 rounded-full bg-primary/15 blur-3xl"
        />
      </section>

      <section aria-labelledby="listings-heading" className="grid gap-6">
        <div className="flex items-end justify-between gap-4">
          <div>
            <h2 id="listings-heading" className="text-xl font-semibold sm:text-2xl">
              All stays
            </h2>
            {result.ok && result.data.totalCount > 0 && (
              <p className="text-sm text-muted-foreground">
                {result.data.totalCount.toLocaleString("en-US")} places to stay
              </p>
            )}
          </div>
          <Button asChild variant="outline" className="rounded-full">
            <Link href="/search">Filter &amp; sort</Link>
          </Button>
        </div>

        {!result.ok ? (
          <ErrorState
            title="We can't load stays right now"
            description={result.error.message}
            action={
              <Button asChild variant="outline">
                <Link href={page > 1 ? `/?page=${page}` : "/"} prefetch={false}>
                  Try again
                </Link>
              </Button>
            }
          />
        ) : result.data.items.length === 0 ? (
          <EmptyState
            icon={Home}
            title={page > 1 ? "No more stays on this page" : "No stays listed yet"}
            description={page > 1 ? "Go back to the first page to keep browsing." : "Check back soon."}
            action={
              page > 1 && (
                <Button asChild>
                  <Link href="/">Back to first page</Link>
                </Button>
              )
            }
          />
        ) : (
          <>
            <ApartmentGrid apartments={result.data.items} />
            <Pagination
              page={page}
              totalPages={totalPages(result.data.totalCount, PAGE_SIZE)}
              pathname="/"
            />
          </>
        )}
      </section>
    </Container>
  );
}
