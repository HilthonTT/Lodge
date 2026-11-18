import { Home } from "lucide-react";
import Link from "next/link";
import { getTranslations } from "next-intl/server";

import { Container } from "@/components/shared/container";
import { Pagination } from "@/components/shared/pagination";
import { EmptyState, ErrorState } from "@/components/shared/state";
import { Button } from "@/components/ui/button";
import { apartmentsApi } from "@/features/apartments/api/apartments";
import { ApartmentGrid } from "@/features/apartments/components/apartment-grid";
import { PAGE_SIZE, parseApartmentSearch, totalPages } from "@/features/apartments/search-params";
import { apiErrorMessage } from "@/i18n/api-errors";
import { attempt } from "@/lib/api/client";
import { getServerHttp } from "@/lib/api/server";

export default async function HomePage({ searchParams }: PageProps<"/">) {
  const { page } = parseApartmentSearch(await searchParams);
  const http = await getServerHttp();
  const [result, t, tCommon, tErrors] = await Promise.all([
    attempt(() => apartmentsApi.list(http, { page, pageSize: PAGE_SIZE })),
    getTranslations("home"),
    getTranslations("common"),
    getTranslations("errors.api"),
  ]);

  return (
    <Container className="grid gap-10">
      <section className="relative overflow-hidden rounded-3xl border bg-gradient-to-br from-accent via-background to-background px-6 py-10 sm:px-10 sm:py-14">
        <div className="relative z-10 grid max-w-2xl gap-4">
          <p className="text-sm font-medium text-accent-foreground">{t("eyebrow")}</p>
          <h1 className="text-3xl font-semibold sm:text-5xl">{t("title")}</h1>
          <p className="text-base text-muted-foreground sm:text-lg">{t("intro")}</p>
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
              {t("allStays")}
            </h2>
            {result.ok && result.data.totalCount > 0 && (
              <p className="text-sm text-muted-foreground">
                {t("placesCount", { count: result.data.totalCount })}
              </p>
            )}
          </div>
          <Button asChild variant="outline" className="rounded-full">
            <Link href="/search">{t("filterSort")}</Link>
          </Button>
        </div>

        {!result.ok ? (
          <ErrorState
            title={t("loadError")}
            description={apiErrorMessage(result.error, tErrors)}
            action={
              <Button asChild variant="outline">
                <Link href={page > 1 ? `/?page=${page}` : "/"} prefetch={false}>
                  {tCommon("tryAgain")}
                </Link>
              </Button>
            }
          />
        ) : result.data.items.length === 0 ? (
          <EmptyState
            icon={Home}
            title={page > 1 ? t("emptyPageTitle") : t("emptyTitle")}
            description={page > 1 ? t("emptyPageDescription") : t("emptyDescription")}
            action={
              page > 1 && (
                <Button asChild>
                  <Link href="/">{t("backToFirst")}</Link>
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
