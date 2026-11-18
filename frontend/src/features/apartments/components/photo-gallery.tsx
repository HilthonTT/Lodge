"use client";

import { ChevronLeft, ChevronRight, Grid2x2, X } from "lucide-react";
import { useTranslations } from "next-intl";
import { Dialog as DialogPrimitive } from "radix-ui";
import { useCallback, useEffect, useState } from "react";

import { RemoteImage } from "@/components/shared/remote-image";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import type { ApartmentImage } from "@/types/api";

type PhotoGalleryProps = {
  images: ApartmentImage[];
  /** Cover fallback for listings without an `images` array. */
  coverUrl: string;
  name: string;
};

/**
 * Airbnb-style gallery: 1 large + 4 small photos with five or more, a balanced
 * layout for fewer, and a full-screen lightbox (arrow keys, Escape, thumbnails).
 */
export function PhotoGallery({ images, coverUrl, name }: PhotoGalleryProps) {
  const t = useTranslations("apartment.gallery");
  const photos = images.length > 0 ? images : [{ id: "cover", url: coverUrl }];
  const [index, setIndex] = useState<number | null>(null);
  const count = photos.length;
  const shown = photos.slice(0, 5);

  const tile = (photo: ApartmentImage, i: number, className: string, sizes: string) => (
    <button
      key={photo.id}
      type="button"
      onClick={() => setIndex(i)}
      className={cn(
        "group relative overflow-hidden bg-muted outline-none focus-visible:ring-[3px] focus-visible:ring-ring/60 focus-visible:ring-inset",
        className,
      )}
      aria-label={t("open", { index: String(i + 1), count: String(count) })}
    >
      <RemoteImage
        src={photo.url}
        alt={t("alt", { name, index: String(i + 1) })}
        fill
        priority={i === 0}
        sizes={sizes}
        className="object-cover transition duration-300 group-hover:brightness-90"
      />
    </button>
  );

  return (
    <section aria-label={t("label")} className="relative">
      {/* Mobile: swipeable strip */}
      <div className="-mx-4 flex snap-x snap-mandatory overflow-x-auto sm:hidden">
        {photos.map((photo, i) => (
          <div key={photo.id} className="relative aspect-[4/3] w-full shrink-0 snap-center">
            {tile(photo, i, "absolute inset-0", "100vw")}
            <span className="pointer-events-none absolute right-3 bottom-3 rounded-full bg-black/60 px-2.5 py-0.5 text-xs font-medium text-white tabular-nums">
              {i + 1} / {count}
            </span>
          </div>
        ))}
      </div>

      {/* Tablet and up */}
      <div
        className={cn(
          "hidden h-[22rem] gap-2 overflow-hidden rounded-3xl sm:grid lg:h-[30rem]",
          count === 1 && "grid-cols-1",
          count === 2 && "grid-cols-2",
          count >= 3 && "grid-cols-4 grid-rows-2",
        )}
      >
        {count === 1 && shown[0] && tile(shown[0], 0, "size-full", "(min-width: 1280px) 1216px, 100vw")}
        {count === 2 && shown.map((p, i) => tile(p, i, "size-full", "(min-width: 1280px) 608px, 50vw"))}
        {count >= 3 &&
          shown.map((p, i) => {
            if (i === 0) return tile(p, i, "col-span-2 row-span-2", "(min-width: 1280px) 608px, 50vw");
            // 3 photos: two stacked on the right. 4: one wide on top, two below. 5+: a 2×2 grid.
            const wide = count === 3 || (count === 4 && i === 1);
            return tile(p, i, wide ? "col-span-2" : "col-span-1", wide ? "(min-width: 1280px) 608px, 50vw" : "(min-width: 1280px) 304px, 25vw");
          })}
      </div>

      {count > 1 && (
        <Button
          variant="outline"
          size="sm"
          onClick={() => setIndex(0)}
          className="absolute right-4 bottom-4 hidden bg-background/95 shadow-card sm:inline-flex"
        >
          <Grid2x2 aria-hidden="true" /> {t("showAll", { count })}
        </Button>
      )}

      <Lightbox photos={photos} name={name} index={index} onIndexChange={setIndex} />
    </section>
  );
}

function Lightbox({
  photos,
  name,
  index,
  onIndexChange,
}: {
  photos: ApartmentImage[];
  name: string;
  index: number | null;
  onIndexChange: (index: number | null) => void;
}) {
  const t = useTranslations("apartment.gallery");
  const tCommon = useTranslations("common");
  const count = photos.length;
  const open = index !== null;

  const go = useCallback(
    (delta: number) => {
      if (index === null) return;
      onIndexChange((index + delta + count) % count);
    },
    [count, index, onIndexChange],
  );

  useEffect(() => {
    if (!open) return;
    const onKey = (event: KeyboardEvent) => {
      if (event.key === "ArrowRight") go(1);
      else if (event.key === "ArrowLeft") go(-1);
      else if (event.key === "Home") onIndexChange(0);
      else if (event.key === "End") onIndexChange(count - 1);
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [open, go, count, onIndexChange]);

  const photo = index !== null ? photos[index] : undefined;

  return (
    <DialogPrimitive.Root open={open} onOpenChange={(next) => !next && onIndexChange(null)}>
      <DialogPrimitive.Portal>
        <DialogPrimitive.Overlay className="fixed inset-0 z-50 bg-neutral-950 data-[state=open]:animate-in data-[state=open]:fade-in-0" />
        <DialogPrimitive.Content
          className="fixed inset-0 z-50 flex flex-col text-white outline-none data-[state=open]:animate-in data-[state=open]:fade-in-0"
          aria-describedby={undefined}
        >
          <div className="flex items-center justify-between gap-4 px-4 py-3 sm:px-6">
            <DialogPrimitive.Close asChild>
              <Button variant="ghost" size="icon" className="rounded-full text-white hover:bg-white/15 hover:text-white">
                <X aria-hidden="true" />
                <span className="sr-only">{tCommon("close")}</span>
              </Button>
            </DialogPrimitive.Close>
            <DialogPrimitive.Title className="min-w-0 truncate text-sm font-medium">{name}</DialogPrimitive.Title>
            <p className="text-sm tabular-nums" aria-live="polite">
              {index !== null && t("position", { index: String(index + 1), count: String(count) })}
            </p>
          </div>

          <div className="relative flex min-h-0 flex-1 items-center justify-center px-2 sm:px-20">
            {photo && (
              <div className="relative size-full">
                <RemoteImage
                  key={photo.id}
                  src={photo.url}
                  alt={t("alt", { name, index: String((index ?? 0) + 1) })}
                  fill
                  sizes="100vw"
                  className="object-contain"
                />
              </div>
            )}
            {count > 1 && (
              <>
                <Button
                  variant="outline"
                  size="icon"
                  onClick={() => go(-1)}
                  className="absolute top-1/2 left-3 -translate-y-1/2 rounded-full border-white/30 bg-black/40 text-white hover:bg-white/20 hover:text-white sm:left-6"
                  aria-label={t("previous")}
                >
                  <ChevronLeft aria-hidden="true" />
                </Button>
                <Button
                  variant="outline"
                  size="icon"
                  onClick={() => go(1)}
                  className="absolute top-1/2 right-3 -translate-y-1/2 rounded-full border-white/30 bg-black/40 text-white hover:bg-white/20 hover:text-white sm:right-6"
                  aria-label={t("next")}
                >
                  <ChevronRight aria-hidden="true" />
                </Button>
              </>
            )}
          </div>

          {count > 1 && (
            <ul className="flex justify-center gap-2 overflow-x-auto px-4 py-4" aria-label={t("thumbnails")}>
              {photos.map((p, i) => (
                <li key={p.id} className="shrink-0">
                  <button
                    type="button"
                    onClick={() => onIndexChange(i)}
                    aria-label={t("open", { index: String(i + 1), count: String(count) })}
                    aria-current={i === index ? "true" : undefined}
                    className={cn(
                      "relative block size-14 overflow-hidden rounded-lg opacity-60 transition outline-none hover:opacity-100 focus-visible:ring-2 focus-visible:ring-white",
                      i === index && "opacity-100 ring-2 ring-white",
                    )}
                  >
                    <RemoteImage src={p.url} alt="" fill sizes="56px" className="object-cover" />
                  </button>
                </li>
              ))}
            </ul>
          )}
        </DialogPrimitive.Content>
      </DialogPrimitive.Portal>
    </DialogPrimitive.Root>
  );
}
