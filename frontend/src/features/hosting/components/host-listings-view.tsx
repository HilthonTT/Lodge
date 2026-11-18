"use client";

import {
  CalendarRange,
  ExternalLink,
  Home,
  Loader2,
  MoreHorizontal,
  Pencil,
  Plus,
  Trash2,
  Zap,
} from "lucide-react";
import Link from "next/link";
import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";
import { toast } from "sonner";

import { Pagination } from "@/components/shared/pagination";
import { RemoteImage } from "@/components/shared/remote-image";
import { EmptyState } from "@/components/shared/state";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Skeleton } from "@/components/ui/skeleton";
import { Switch } from "@/components/ui/switch";
import { RatingSummary } from "@/features/apartments/components/rating-summary";
import { hasErrorCode, useApiErrorMessage } from "@/i18n/api-errors";
import { formatPriceCompact } from "@/lib/format";
import { cn } from "@/lib/utils";
import type { HostListingSummary } from "@/types/api";

import { useDeleteListing, useHostListings, useSetListed } from "../hooks/use-hosting";
import { HostingError } from "./hosting-gate";

export const HOST_LISTINGS_PAGE_SIZE = 20;

const COLUMNS = "lg:grid-cols-[minmax(0,2.2fr)_8rem_7rem_8rem_9rem_8.5rem_3rem]";

export function HostListingsView({ page }: { page: number }) {
  const t = useTranslations("hosting.listings");
  const listings = useHostListings({ page, pageSize: HOST_LISTINGS_PAGE_SIZE });
  const [toDelete, setToDelete] = useState<HostListingSummary | null>(null);

  if (listings.isPending) return <ListingsSkeleton />;
  if (listings.isError)
    return <HostingError error={listings.error} title={t("loadError")} onRetry={() => listings.refetch()} />;

  const { items, totalCount } = listings.data;

  if (items.length === 0) {
    return (
      <EmptyState
        icon={Home}
        title={t("emptyTitle")}
        description={t("emptyDescription")}
        action={
          <Button asChild>
            <Link href="/hosting/listings/new">
              <Plus aria-hidden="true" /> {t("create")}
            </Link>
          </Button>
        }
      />
    );
  }

  return (
    <div className="grid gap-6">
      <div className="overflow-hidden rounded-2xl border bg-card shadow-card">
        <div
          className={cn(
            "hidden gap-4 border-b bg-muted/50 px-5 py-3 text-xs font-medium tracking-wide text-muted-foreground uppercase lg:grid",
            COLUMNS,
          )}
          aria-hidden="true"
        >
          <span>{t("columns.listing")}</span>
          <span>{t("columns.status")}</span>
          <span>{t("columns.price")}</span>
          <span>{t("columns.rating")}</span>
          <span>{t("columns.bookings")}</span>
          <span>{t("columns.listed")}</span>
          <span className="sr-only">{t("columns.actions")}</span>
        </div>
        <ul className="divide-y">
          {items.map((listing) => (
            <li key={listing.id}>
              <ListingRow listing={listing} onDelete={() => setToDelete(listing)} />
            </li>
          ))}
        </ul>
      </div>
      <Pagination
        page={page}
        totalPages={Math.ceil(totalCount / HOST_LISTINGS_PAGE_SIZE)}
        pathname="/hosting/listings"
      />
      <DeleteListingDialog listing={toDelete} onClose={() => setToDelete(null)} />
    </div>
  );
}

function ListingRow({ listing, onDelete }: { listing: HostListingSummary; onDelete: () => void }) {
  const t = useTranslations("hosting.listings");
  const tListing = useTranslations("listing");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const errorMessage = useApiErrorMessage();
  const setListed = useSetListed();
  const location = [listing.city, listing.country].filter(Boolean).join(", ");

  const onToggle = (isListed: boolean) =>
    setListed.mutate(
      { id: listing.id, isListed },
      {
        onSuccess: () => toast.success(isListed ? t("listedToast", { name: listing.name }) : t("unlistedToast", { name: listing.name })),
        onError: (error) => toast.error(t("listedError"), { description: errorMessage(error) }),
      },
    );

  return (
    <div className={cn("grid gap-4 p-4 sm:p-5 lg:items-center", COLUMNS)}>
      <div className="flex min-w-0 items-center gap-4">
        <Link
          href={`/hosting/listings/${listing.id}/edit`}
          className="relative size-16 shrink-0 overflow-hidden rounded-xl bg-muted sm:size-20 lg:size-14"
          tabIndex={-1}
          aria-hidden="true"
        >
          <RemoteImage src={listing.imageUrl} alt="" fill sizes="80px" className="object-cover" />
        </Link>
        <div className="grid min-w-0 gap-0.5">
          <Link
            href={`/hosting/listings/${listing.id}/edit`}
            className="line-clamp-1 font-semibold hover:underline"
          >
            {listing.name}
          </Link>
          <p className="line-clamp-1 text-sm text-muted-foreground">
            {[tListing(`propertyTypes.${listing.propertyType}`), location].filter(Boolean).join(" · ")}
          </p>
        </div>
      </div>

      <div className="flex flex-wrap items-center gap-2 lg:contents">
        <div className="flex flex-wrap gap-1.5">
          <Badge variant={listing.isListed ? "success" : "muted"}>
            {listing.isListed ? t("status.listed") : t("status.unlisted")}
          </Badge>
          {listing.instantBook && (
            <Badge variant="outline" className="lg:hidden">
              <Zap aria-hidden="true" /> {t("instantBook")}
            </Badge>
          )}
        </div>
        <p className="text-sm lg:text-base">
          <span className="font-semibold tabular-nums">
            {formatPriceCompact(listing.price, listing.currency, locale)}
          </span>{" "}
          <span className="text-muted-foreground">{tCommon("perNight")}</span>
        </p>
        <RatingSummary averageRating={listing.averageRating} reviewCount={listing.reviewCount} />
        <div className="grid text-sm text-muted-foreground">
          <span>{t("upcoming", { count: listing.upcomingBookingCount })}</span>
          {listing.pendingRequestCount > 0 && (
            <Link href={`/hosting/reservations?status=pending&listing=${listing.id}`} className="font-medium text-info hover:underline">
              {t("pending", { count: listing.pendingRequestCount })}
            </Link>
          )}
        </div>
      </div>

      <div className="flex items-center justify-between gap-3 border-t pt-3 lg:contents">
        <label className="flex items-center gap-2.5 text-sm">
          <Switch
            checked={listing.isListed}
            onCheckedChange={onToggle}
            disabled={setListed.isPending}
            aria-label={t("listedLabel", { name: listing.name })}
          />
          <span className="lg:sr-only">{listing.isListed ? t("status.listed") : t("status.unlisted")}</span>
        </label>
        <div className="flex items-center gap-1 lg:justify-end">
          <Button asChild variant="outline" size="sm" className="lg:hidden">
            <Link href={`/hosting/listings/${listing.id}/calendar`}>
              <CalendarRange aria-hidden="true" /> {t("calendar")}
            </Link>
          </Button>
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" className="rounded-full" aria-label={t("actions", { name: listing.name })}>
                <MoreHorizontal aria-hidden="true" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-52">
              <DropdownMenuItem asChild>
                <Link href={`/hosting/listings/${listing.id}/edit`}>
                  <Pencil /> {t("edit")}
                </Link>
              </DropdownMenuItem>
              <DropdownMenuItem asChild>
                <Link href={`/hosting/listings/${listing.id}/calendar`}>
                  <CalendarRange /> {t("calendar")}
                </Link>
              </DropdownMenuItem>
              <DropdownMenuItem asChild>
                <Link href={`/apartments/${listing.id}`}>
                  <ExternalLink /> {t("view")}
                </Link>
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem variant="destructive" onSelect={onDelete}>
                <Trash2 /> {t("delete")}
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </div>
    </div>
  );
}

function DeleteListingDialog({ listing, onClose }: { listing: HostListingSummary | null; onClose: () => void }) {
  const t = useTranslations("hosting.listings");
  const tCommon = useTranslations("common");
  const errorMessage = useApiErrorMessage();
  const remove = useDeleteListing();
  const setListed = useSetListed();
  const [conflict, setConflict] = useState(false);

  const close = () => {
    setConflict(false);
    remove.reset();
    onClose();
  };

  const onDelete = (event: React.MouseEvent) => {
    event.preventDefault();
    if (!listing) return;
    remove.mutate(listing.id, {
      onSuccess: () => {
        toast.success(t("deleted", { name: listing.name }));
        close();
      },
      onError: (error) => {
        if (hasErrorCode(error, "Apartment.HasUpcomingBookings") || error.status === 409) setConflict(true);
        else toast.error(t("deleteError"), { description: errorMessage(error) });
      },
    });
  };

  const onUnlist = (event: React.MouseEvent) => {
    event.preventDefault();
    if (!listing) return;
    setListed.mutate(
      { id: listing.id, isListed: false },
      {
        onSuccess: () => {
          toast.success(t("unlistedToast", { name: listing.name }));
          close();
        },
        onError: (error) => toast.error(t("listedError"), { description: errorMessage(error) }),
      },
    );
  };

  return (
    <AlertDialog open={listing !== null} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>{conflict ? t("conflictTitle") : t("deleteTitle")}</AlertDialogTitle>
          <AlertDialogDescription>
            {conflict
              ? t("conflictDescription", { name: listing?.name ?? "" })
              : t("deleteDescription", { name: listing?.name ?? "" })}
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel>{tCommon("cancel")}</AlertDialogCancel>
          {conflict ? (
            listing?.isListed && (
              <AlertDialogAction onClick={onUnlist} disabled={setListed.isPending}>
                {setListed.isPending && <Loader2 className="animate-spin" aria-hidden="true" />}
                {t("unlistInstead")}
              </AlertDialogAction>
            )
          ) : (
            <AlertDialogAction variant="destructive" onClick={onDelete} disabled={remove.isPending}>
              {remove.isPending && <Loader2 className="animate-spin" aria-hidden="true" />}
              {t("deleteConfirm")}
            </AlertDialogAction>
          )}
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}

function ListingsSkeleton() {
  return (
    <div className="grid gap-3 rounded-2xl border p-4" role="status">
      {Array.from({ length: 4 }, (_, i) => (
        <div key={i} className="flex items-center gap-4">
          <Skeleton className="size-14 rounded-xl" />
          <div className="grid flex-1 gap-2">
            <Skeleton className="h-4 w-1/3" />
            <Skeleton className="h-3.5 w-1/4" />
          </div>
          <Skeleton className="h-6 w-11 rounded-full" />
        </div>
      ))}
    </div>
  );
}
