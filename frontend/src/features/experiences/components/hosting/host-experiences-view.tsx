"use client";

import { CalendarClock, Compass, ExternalLink, ListChecks, MoreHorizontal, Pencil, Trash2 } from "lucide-react";
import Link from "next/link";
import { useTranslations } from "next-intl";
import { useState } from "react";
import { toast } from "sonner";

import { Pagination } from "@/components/shared/pagination";
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
import { HostingError } from "@/features/hosting/components/hosting-gate";
import { Money } from "@/features/payments/components/money";
import { hasErrorCode, useApiErrorMessage } from "@/i18n/api-errors";
import { cn } from "@/lib/utils";
import type { HostExperienceSummary } from "@/types/api";

import { ExperienceImage } from "../../experience-options";
import { useDeleteExperience, useHostExperiences, useSetExperienceListed } from "../../hooks/use-experiences";

export const HOST_EXPERIENCES_PAGE_SIZE = 20;

export function HostExperiencesView({ page }: { page: number }) {
  const t = useTranslations("experiences.hosting.list");
  const experiences = useHostExperiences({ page, pageSize: HOST_EXPERIENCES_PAGE_SIZE });
  const [toDelete, setToDelete] = useState<HostExperienceSummary | null>(null);

  if (experiences.isPending) {
    return (
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3" role="status" aria-label={t("loading")}>
        {Array.from({ length: 3 }, (_, i) => (
          <Skeleton key={i} className="h-80 rounded-2xl" />
        ))}
      </div>
    );
  }
  if (experiences.isError) {
    return <HostingError error={experiences.error} title={t("loadError")} onRetry={() => experiences.refetch()} />;
  }
  if (experiences.data.items.length === 0) {
    return (
      <EmptyState
        icon={Compass}
        title={t("emptyTitle")}
        description={t("emptyDescription")}
        action={
          <Button asChild>
            <Link href="/hosting/experiences/new">{t("create")}</Link>
          </Button>
        }
      />
    );
  }

  const totalPages = Math.ceil(experiences.data.totalCount / HOST_EXPERIENCES_PAGE_SIZE);
  return (
    <div className="grid gap-6">
      <ul className="grid gap-6 sm:grid-cols-2 xl:grid-cols-3" data-testid="host-experiences">
        {experiences.data.items.map((experience) => (
          <li key={experience.id}>
            <HostExperienceCard experience={experience} onDelete={() => setToDelete(experience)} />
          </li>
        ))}
      </ul>
      <Pagination page={page} totalPages={totalPages} pathname="/hosting/experiences" />
      <DeleteExperienceDialog experience={toDelete} onClose={() => setToDelete(null)} />
    </div>
  );
}

function HostExperienceCard({ experience: e, onDelete }: { experience: HostExperienceSummary; onDelete: () => void }) {
  const t = useTranslations("experiences.hosting.list");
  const tExp = useTranslations("experiences");
  const errorMessage = useApiErrorMessage();
  const setListed = useSetExperienceListed();
  const base = `/hosting/experiences/${e.id}`;

  return (
    <article className="flex h-full flex-col overflow-hidden rounded-2xl border bg-card shadow-card" data-testid="host-experience">
      <div className="relative aspect-[16/10] bg-muted">
        <ExperienceImage src={e.imageUrl} category={e.category} sizes="(min-width: 1280px) 33vw, (min-width: 640px) 50vw, 100vw" iconClassName="size-10" />
        <Badge variant={e.isListed ? "success" : "muted"} className="absolute top-3 left-3 bg-background/95">
          {e.isListed ? t("listed") : t("unlisted")}
        </Badge>
      </div>
      <div className="flex flex-1 flex-col gap-3 p-4">
        <div className="flex items-start justify-between gap-2">
          <div className="grid min-w-0 gap-0.5">
            <h3 className="line-clamp-2 font-semibold">{e.title}</h3>
            <p className="text-sm text-muted-foreground">
              {tExp(`categories.${e.category}`)} · {[e.city, e.country].filter(Boolean).join(", ")}
            </p>
          </div>
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon-sm" className="shrink-0 rounded-full" aria-label={t("actions", { title: e.title })}>
                <MoreHorizontal />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem asChild>
                <Link href={`${base}/edit`}>
                  <Pencil /> {t("edit")}
                </Link>
              </DropdownMenuItem>
              <DropdownMenuItem asChild>
                <Link href={`/experiences/${e.id}`}>
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
        <div className="flex flex-wrap items-center justify-between gap-2 text-sm">
          <span>
            <Money amount={e.pricePerGuest} currency={e.currency} compact className="font-semibold" />{" "}
            <span className="text-muted-foreground">{tExp("perGuest")}</span>
          </span>
          <RatingSummary averageRating={e.averageRating} reviewCount={e.reviewCount} />
        </div>
        <p className="flex items-center gap-1.5 text-sm text-muted-foreground">
          <CalendarClock className="size-4" aria-hidden="true" />
          {t("upcomingSessions", { count: e.upcomingSessionCount })}
        </p>
        <div className="flex items-center justify-between gap-3 border-t pt-3">
          <label htmlFor={`listed-${e.id}`} className="text-sm font-medium">
            {t("listedToggle")}
          </label>
          <Switch
            id={`listed-${e.id}`}
            checked={e.isListed}
            disabled={setListed.isPending}
            onCheckedChange={(isListed) =>
              setListed.mutate(
                { id: e.id, isListed },
                { onError: (error) => toast.error(t("statusError"), { description: errorMessage(error) }) },
              )
            }
          />
        </div>
        <div className="mt-auto grid grid-cols-2 gap-2">
          <Button asChild variant="outline" size="sm">
            <Link href={`${base}/sessions`}>
              <CalendarClock aria-hidden="true" /> {t("sessions")}
            </Link>
          </Button>
          <Button asChild variant="outline" size="sm">
            <Link href={`${base}/bookings`}>
              <ListChecks aria-hidden="true" /> {t("bookings")}
            </Link>
          </Button>
        </div>
      </div>
    </article>
  );
}

function DeleteExperienceDialog({ experience, onClose }: { experience: HostExperienceSummary | null; onClose: () => void }) {
  const t = useTranslations("experiences.hosting.list");
  const tCommon = useTranslations("common");
  const errorMessage = useApiErrorMessage();
  const remove = useDeleteExperience();

  return (
    <AlertDialog open={experience !== null} onOpenChange={(open) => !open && onClose()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>{t("deleteTitle")}</AlertDialogTitle>
          <AlertDialogDescription>{t("deleteDescription", { title: experience?.title ?? "" })}</AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel>{tCommon("cancel")}</AlertDialogCancel>
          <AlertDialogAction
            variant="destructive"
            className={cn(remove.isPending && "opacity-70")}
            onClick={() =>
              experience &&
              remove.mutate(experience.id, {
                onSuccess: () => toast.success(t("deleted")),
                onError: (error) =>
                  toast.error(t("deleteError"), {
                    description: hasErrorCode(error, "Experience.HasUpcomingBookings") ? t("hasBookings") : errorMessage(error),
                  }),
              })
            }
          >
            {tCommon("delete")}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
