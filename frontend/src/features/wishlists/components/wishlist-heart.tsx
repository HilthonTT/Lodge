"use client";

import { Heart } from "lucide-react";
import { usePathname, useRouter } from "next/navigation";
import { useTranslations } from "next-intl";
import { useState } from "react";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import { useSession } from "@/features/auth/session-provider";
import { useApiErrorMessage } from "@/i18n/api-errors";
import { loginUrl } from "@/lib/auth/routes";
import { cn } from "@/lib/utils";

import { useSavedApartmentIds, useUnsaveApartment } from "../hooks/use-wishlists";
import { SaveToWishlistDialog } from "./save-to-wishlist-dialog";

type WishlistHeartProps = {
  apartmentId: string;
  apartmentName: string;
  /** "overlay": round icon over a photo (cards). "button": labelled outline button (listing page). */
  variant?: "overlay" | "button";
  className?: string;
};

/**
 * Save/unsave toggle. Unsaved: opens "Save to wishlist". Saved: removes the stay
 * from every list, optimistically. Signed-out users are sent to log in.
 */
export function WishlistHeart({ apartmentId, apartmentName, variant = "overlay", className }: WishlistHeartProps) {
  const t = useTranslations("wishlists");
  const errorMessage = useApiErrorMessage();
  const router = useRouter();
  const pathname = usePathname();
  const { isAuthenticated } = useSession();
  const saved = useSavedApartmentIds();
  const unsave = useUnsaveApartment();
  const [dialogOpen, setDialogOpen] = useState(false);
  const isSaved = saved.data?.has(apartmentId) ?? false;

  const onClick = (event: React.MouseEvent) => {
    event.preventDefault();
    event.stopPropagation();
    if (!isAuthenticated) {
      router.push(loginUrl(pathname + window.location.search));
      return;
    }
    if (isSaved) {
      unsave.mutate(
        { apartmentId },
        {
          onSuccess: () => toast.success(t("removed", { name: apartmentName })),
          onError: (error) => toast.error(t("removeError"), { description: errorMessage(error) }),
        },
      );
    } else {
      setDialogOpen(true);
    }
  };

  const label = isSaved ? t("unsaveLabel", { name: apartmentName }) : t("saveLabel", { name: apartmentName });

  return (
    <>
      {variant === "overlay" ? (
        <button
          type="button"
          onClick={onClick}
          aria-pressed={isSaved}
          aria-label={label}
          className={cn(
            "grid size-9 place-items-center rounded-full transition-transform outline-none hover:scale-110 focus-visible:ring-[3px] focus-visible:ring-ring/60 active:scale-95",
            className,
          )}
        >
          <Heart
            aria-hidden="true"
            className={cn(
              "size-6 stroke-white stroke-2 drop-shadow-[0_1px_2px_rgb(0_0_0/0.45)] transition-colors",
              isSaved ? "fill-rose-500" : "fill-black/35",
            )}
          />
        </button>
      ) : (
        <Button
          variant="ghost"
          onClick={onClick}
          aria-pressed={isSaved}
          aria-label={label}
          className={cn("rounded-full underline-offset-4", className)}
        >
          <Heart aria-hidden="true" className={cn(isSaved && "fill-rose-500 stroke-rose-500")} />
          <span className="underline">{isSaved ? t("saved") : t("save")}</span>
        </Button>
      )}
      {isAuthenticated && (
        <SaveToWishlistDialog
          open={dialogOpen}
          onOpenChange={setDialogOpen}
          apartmentId={apartmentId}
          apartmentName={apartmentName}
        />
      )}
    </>
  );
}
