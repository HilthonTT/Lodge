"use client";

import { Heart, Loader2, MoreHorizontal, Pencil, Trash2 } from "lucide-react";
import Link from "next/link";
import { useTranslations } from "next-intl";
import { useState } from "react";
import { toast } from "sonner";

import { RemoteImage } from "@/components/shared/remote-image";
import { EmptyState, ErrorState } from "@/components/shared/state";
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
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { useApiErrorMessage } from "@/i18n/api-errors";
import type { WishlistSummary } from "@/types/api";

import { useDeleteWishlist, useRenameWishlist, useWishlists } from "../hooks/use-wishlists";
import { WISHLIST_NAME_MAX } from "./save-to-wishlist-dialog";

const gridClass = "grid grid-cols-1 gap-x-6 gap-y-8 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4";

export function WishlistsView() {
  const t = useTranslations("wishlists");
  const tCommon = useTranslations("common");
  const errorMessage = useApiErrorMessage();
  const { data, isPending, isError, error, refetch, isRefetching } = useWishlists();

  if (isPending) return <WishlistsSkeleton />;
  if (isError) {
    return (
      <ErrorState
        title={t("loadError")}
        description={errorMessage(error)}
        action={
          <Button variant="outline" onClick={() => refetch()} disabled={isRefetching}>
            {tCommon("tryAgain")}
          </Button>
        }
      />
    );
  }
  if (data.length === 0) {
    return (
      <EmptyState
        icon={Heart}
        title={t("emptyTitle")}
        description={t("emptyDescription")}
        action={
          <Button asChild>
            <Link href="/search">{t("startExploring")}</Link>
          </Button>
        }
      />
    );
  }

  return (
    <ul className={gridClass}>
      {data.map((list, index) => (
        <li key={list.id}>
          <WishlistTile list={list} priority={index < 3} />
        </li>
      ))}
    </ul>
  );
}

function WishlistTile({ list, priority }: { list: WishlistSummary; priority: boolean }) {
  const t = useTranslations("wishlists");
  const tCommon = useTranslations("common");
  const errorMessage = useApiErrorMessage();
  const remove = useDeleteWishlist();
  const [renameOpen, setRenameOpen] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);

  const onDelete = () =>
    remove.mutate(list.id, {
      onSuccess: () => toast.success(t("deleted")),
      onError: (error) => toast.error(t("deleteError"), { description: errorMessage(error) }),
    });

  return (
    <article className="group relative grid gap-3">
      <div className="relative aspect-square overflow-hidden rounded-2xl bg-muted shadow-card">
        {list.coverImageUrl ? (
          <RemoteImage
            src={list.coverImageUrl}
            alt=""
            fill
            priority={priority}
            sizes="(min-width: 1024px) 33vw, (min-width: 640px) 50vw, 100vw"
            className="object-cover transition-transform duration-500 group-hover:scale-[1.03]"
          />
        ) : (
          <span className="grid size-full place-items-center text-muted-foreground">
            <Heart className="size-10" aria-hidden="true" />
          </span>
        )}
      </div>
      <div className="flex items-start justify-between gap-3 px-0.5">
        <div className="grid min-w-0 gap-0.5">
          <h2 className="line-clamp-1 font-semibold">
            <Link
              href={`/wishlists/${list.id}`}
              className="outline-none after:absolute after:inset-0 after:rounded-2xl focus-visible:after:ring-[3px] focus-visible:after:ring-ring/50"
            >
              {list.name}
            </Link>
          </h2>
          <p className="text-sm text-muted-foreground">{t("savedCount", { count: list.itemCount })}</p>
        </div>
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button
              variant="ghost"
              size="icon-sm"
              className="relative z-10 rounded-full"
              aria-label={t("options", { name: list.name })}
              disabled={remove.isPending}
            >
              {remove.isPending ? <Loader2 className="animate-spin" /> : <MoreHorizontal />}
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem onSelect={() => setRenameOpen(true)}>
              <Pencil /> {tCommon("rename")}
            </DropdownMenuItem>
            <DropdownMenuItem variant="destructive" onSelect={() => setDeleteOpen(true)}>
              <Trash2 /> {tCommon("delete")}
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>

      <RenameWishlistDialog list={list} open={renameOpen} onOpenChange={setRenameOpen} />

      <AlertDialog open={deleteOpen} onOpenChange={setDeleteOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{t("deleteTitle")}</AlertDialogTitle>
            <AlertDialogDescription>{t("deleteDescription", { name: list.name })}</AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{tCommon("cancel")}</AlertDialogCancel>
            <AlertDialogAction variant="destructive" onClick={onDelete}>
              {tCommon("delete")}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </article>
  );
}

export function RenameWishlistDialog({
  list,
  open,
  onOpenChange,
}: {
  list: Pick<WishlistSummary, "id" | "name">;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}) {
  const t = useTranslations("wishlists");
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{t("renameTitle")}</DialogTitle>
          <DialogDescription className="sr-only">{list.name}</DialogDescription>
        </DialogHeader>
        {open && <RenameForm list={list} onDone={() => onOpenChange(false)} />}
      </DialogContent>
    </Dialog>
  );
}

function RenameForm({ list, onDone }: { list: Pick<WishlistSummary, "id" | "name">; onDone: () => void }) {
  const t = useTranslations("wishlists");
  const tCommon = useTranslations("common");
  const errorMessage = useApiErrorMessage();
  const rename = useRenameWishlist();
  const [name, setName] = useState(list.name);
  const [error, setError] = useState<string>();

  const onSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    const trimmed = name.trim();
    if (!trimmed) return setError(t("nameRequired"));
    if (trimmed.length > WISHLIST_NAME_MAX) return setError(t("nameTooLong", { max: WISHLIST_NAME_MAX }));
    if (trimmed === list.name) return onDone();
    onDone();
    rename.mutate(
      { id: list.id, name: trimmed },
      {
        onSuccess: () => toast.success(t("renamed")),
        onError: (err) => toast.error(t("renameError"), { description: errorMessage(err) }),
      },
    );
  };

  return (
    <form onSubmit={onSubmit} className="grid gap-4" noValidate>
      <div className="grid gap-2">
        <label htmlFor="rename-wishlist" className="text-sm font-medium">
          {t("name")}
        </label>
        <Input
          id="rename-wishlist"
          value={name}
          onChange={(e) => {
            setName(e.target.value);
            setError(undefined);
          }}
          maxLength={WISHLIST_NAME_MAX}
          aria-invalid={Boolean(error)}
          aria-describedby="rename-wishlist-hint"
          autoFocus
        />
        <p id="rename-wishlist-hint" className={error ? "text-xs font-medium text-destructive" : "text-xs text-muted-foreground"}>
          {error ?? t("charactersLeft", { count: WISHLIST_NAME_MAX - name.length })}
        </p>
      </div>
      <DialogFooter>
        <Button type="button" variant="ghost" onClick={onDone}>
          {tCommon("cancel")}
        </Button>
        <Button type="submit">{tCommon("save")}</Button>
      </DialogFooter>
    </form>
  );
}

export function WishlistsSkeleton() {
  const t = useTranslations("wishlists");
  return (
    <div className={gridClass} role="status" aria-label={t("loading")}>
      {Array.from({ length: 3 }, (_, i) => (
        <div key={i} className="grid gap-3">
          <Skeleton className="aspect-square rounded-2xl" />
          <Skeleton className="h-4 w-1/2" />
          <Skeleton className="h-3.5 w-1/3" />
        </div>
      ))}
    </div>
  );
}
