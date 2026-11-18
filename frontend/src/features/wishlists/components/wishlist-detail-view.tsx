"use client";

import { ArrowLeft, Heart, Pencil, X } from "lucide-react";
import Link from "next/link";
import { useTranslations } from "next-intl";
import { useState } from "react";
import { toast } from "sonner";

import { EmptyState, ErrorState } from "@/components/shared/state";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { ApartmentCard, ApartmentCardSkeleton } from "@/features/apartments/components/apartment-card";
import { useApiErrorMessage } from "@/i18n/api-errors";

import { useRemoveFromWishlist, useWishlist } from "../hooks/use-wishlists";
import { RenameWishlistDialog } from "./wishlists-view";

const gridClass = "grid grid-cols-1 gap-x-6 gap-y-10 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4";

export function WishlistDetailView({ wishlistId }: { wishlistId: string }) {
  const t = useTranslations("wishlists");
  const tCommon = useTranslations("common");
  const errorMessage = useApiErrorMessage();
  const { data, isPending, isError, error, refetch, isRefetching } = useWishlist(wishlistId);
  const remove = useRemoveFromWishlist(wishlistId);
  const [renameOpen, setRenameOpen] = useState(false);

  const back = (
    <Button asChild variant="ghost" size="icon" className="rounded-full">
      <Link href="/wishlists" aria-label={t("backToWishlists")}>
        <ArrowLeft />
      </Link>
    </Button>
  );

  if (isPending) {
    return (
      <div className="grid gap-8" role="status" aria-label={t("loading")}>
        <div className="flex items-center gap-3">
          {back}
          <Skeleton className="h-8 w-56" />
        </div>
        <div className={gridClass}>
          {Array.from({ length: 4 }, (_, i) => (
            <ApartmentCardSkeleton key={i} />
          ))}
        </div>
      </div>
    );
  }

  if (isError) {
    return (
      <div className="grid gap-8">
        <div className="flex items-center gap-3">{back}</div>
        <ErrorState
          title={t("listLoadError")}
          description={errorMessage(error)}
          action={
            error.isNotFound ? (
              <Button asChild>
                <Link href="/wishlists">{t("backToWishlists")}</Link>
              </Button>
            ) : (
              <Button variant="outline" onClick={() => refetch()} disabled={isRefetching}>
                {tCommon("tryAgain")}
              </Button>
            )
          }
        />
      </div>
    );
  }

  const onRemove = (apartmentId: string, name: string) =>
    remove.mutate(apartmentId, {
      onSuccess: () => toast.success(t("removed", { name })),
      onError: (err) => toast.error(t("removeError"), { description: errorMessage(err) }),
    });

  return (
    <div className="grid gap-8">
      <header className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex min-w-0 items-center gap-3">
          {back}
          <div className="grid min-w-0">
            <h1 className="truncate text-2xl font-semibold sm:text-3xl">{data.name}</h1>
            <p className="text-sm text-muted-foreground">{t("savedCount", { count: data.items.length })}</p>
          </div>
        </div>
        <Button variant="outline" className="rounded-full" onClick={() => setRenameOpen(true)}>
          <Pencil aria-hidden="true" /> {tCommon("rename")}
        </Button>
      </header>

      {data.items.length === 0 ? (
        <EmptyState
          icon={Heart}
          title={t("listEmptyTitle")}
          description={t("listEmptyDescription")}
          action={
            <Button asChild>
              <Link href="/search">{t("startExploring")}</Link>
            </Button>
          }
        />
      ) : (
        <ul className={gridClass}>
          {data.items.map((apartment, index) => (
            <li key={apartment.id} className="relative">
              <ApartmentCard apartment={apartment} priority={index < 4} hideWishlist />
              <Button
                variant="outline"
                size="icon-sm"
                onClick={() => onRemove(apartment.id, apartment.name)}
                className="absolute top-3 right-3 z-10 rounded-full bg-background/95 shadow-card"
                aria-label={t("removeFromList", { name: apartment.name })}
              >
                <X aria-hidden="true" />
              </Button>
            </li>
          ))}
        </ul>
      )}

      <RenameWishlistDialog list={data} open={renameOpen} onOpenChange={setRenameOpen} />
    </div>
  );
}
