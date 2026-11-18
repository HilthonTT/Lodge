"use client";

import { Heart, Loader2, Plus } from "lucide-react";
import { useTranslations } from "next-intl";
import { useState } from "react";
import { toast } from "sonner";

import { RemoteImage } from "@/components/shared/remote-image";
import { ErrorState } from "@/components/shared/state";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { useApiErrorMessage } from "@/i18n/api-errors";

import { useAddToWishlist, useCreateWishlist, useWishlists } from "../hooks/use-wishlists";

export const WISHLIST_NAME_MAX = 50;

type SaveToWishlistDialogProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  apartmentId: string;
  apartmentName: string;
};

export function SaveToWishlistDialog({ open, onOpenChange, apartmentId, apartmentName }: SaveToWishlistDialogProps) {
  const t = useTranslations("wishlists");
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md" onClick={(e) => e.stopPropagation()}>
        <DialogHeader>
          <DialogTitle>{t("saveTitle")}</DialogTitle>
          <DialogDescription>{t("saveDescription", { name: apartmentName })}</DialogDescription>
        </DialogHeader>
        {open && <SavePicker apartmentId={apartmentId} apartmentName={apartmentName} onDone={() => onOpenChange(false)} />}
      </DialogContent>
    </Dialog>
  );
}

function SavePicker({
  apartmentId,
  apartmentName,
  onDone,
}: {
  apartmentId: string;
  apartmentName: string;
  onDone: () => void;
}) {
  const t = useTranslations("wishlists");
  const tCommon = useTranslations("common");
  const errorMessage = useApiErrorMessage();
  const lists = useWishlists();
  const add = useAddToWishlist();
  const create = useCreateWishlist();
  const [creating, setCreating] = useState(false);
  const [name, setName] = useState("");
  const [nameError, setNameError] = useState<string>();

  const saveTo = (wishlistId: string, listName: string) => {
    onDone();
    add.mutate(
      { wishlistId, apartmentId },
      {
        onSuccess: () => toast.success(t("savedTo", { name: listName })),
        onError: (error) => toast.error(t("saveError"), { description: errorMessage(error) }),
      },
    );
  };

  const onCreate = (event: React.FormEvent) => {
    event.preventDefault();
    const trimmed = name.trim();
    if (!trimmed) return setNameError(t("nameRequired"));
    if (trimmed.length > WISHLIST_NAME_MAX) return setNameError(t("nameTooLong", { max: WISHLIST_NAME_MAX }));
    create.mutate(trimmed, {
      onSuccess: (id) => saveTo(id, trimmed),
      onError: (error) => setNameError(errorMessage(error)),
    });
  };

  if (creating || (lists.data && lists.data.length === 0)) {
    return (
      <form onSubmit={onCreate} className="grid gap-4" noValidate>
        <div className="grid gap-2">
          <label htmlFor="wishlist-name" className="text-sm font-medium">
            {t("name")}
          </label>
          <Input
            id="wishlist-name"
            value={name}
            onChange={(e) => {
              setName(e.target.value);
              setNameError(undefined);
            }}
            placeholder={t("namePlaceholder")}
            maxLength={WISHLIST_NAME_MAX}
            aria-invalid={Boolean(nameError)}
            aria-describedby="wishlist-name-hint"
            autoFocus
          />
          <p
            id="wishlist-name-hint"
            className={nameError ? "text-xs font-medium text-destructive" : "text-xs text-muted-foreground"}
          >
            {nameError ?? t("charactersLeft", { count: WISHLIST_NAME_MAX - name.length })}
          </p>
        </div>
        <div className="flex justify-between gap-2">
          {lists.data && lists.data.length > 0 ? (
            <Button type="button" variant="ghost" onClick={() => setCreating(false)}>
              {tCommon("back")}
            </Button>
          ) : (
            <span />
          )}
          <Button type="submit" disabled={create.isPending}>
            {create.isPending && <Loader2 className="animate-spin" aria-hidden="true" />}
            {t("createAndSave")}
          </Button>
        </div>
      </form>
    );
  }

  return (
    <div className="grid gap-4">
      {lists.isPending ? (
        <div className="grid grid-cols-2 gap-4" role="status" aria-label={t("loading")}>
          {Array.from({ length: 2 }, (_, i) => (
            <Skeleton key={i} className="aspect-square rounded-2xl" />
          ))}
        </div>
      ) : lists.isError ? (
        <ErrorState
          className="py-8"
          description={errorMessage(lists.error)}
          action={
            <Button variant="outline" onClick={() => lists.refetch()}>
              {tCommon("tryAgain")}
            </Button>
          }
        />
      ) : (
        <ul className="grid max-h-[50dvh] grid-cols-2 gap-4 overflow-y-auto p-1">
          {lists.data.map((list) => (
            <li key={list.id}>
              <button
                type="button"
                onClick={() => saveTo(list.id, list.name)}
                className="group grid w-full gap-2 rounded-2xl text-left outline-none focus-visible:ring-[3px] focus-visible:ring-ring/40"
                aria-label={t("saveToList", { list: list.name, name: apartmentName })}
              >
                <span className="relative aspect-square overflow-hidden rounded-2xl bg-muted">
                  {list.coverImageUrl ? (
                    <RemoteImage
                      src={list.coverImageUrl}
                      alt=""
                      fill
                      sizes="200px"
                      className="object-cover transition-transform group-hover:scale-105"
                    />
                  ) : (
                    <span className="grid size-full place-items-center text-muted-foreground">
                      <Heart className="size-8" aria-hidden="true" />
                    </span>
                  )}
                </span>
                <span className="grid gap-0.5 px-0.5">
                  <span className="line-clamp-1 text-sm font-semibold">{list.name}</span>
                  <span className="text-xs text-muted-foreground">{t("savedCount", { count: list.itemCount })}</span>
                </span>
              </button>
            </li>
          ))}
        </ul>
      )}
      <Button variant="outline" onClick={() => setCreating(true)} className="w-full">
        <Plus aria-hidden="true" /> {t("createNew")}
      </Button>
    </div>
  );
}
