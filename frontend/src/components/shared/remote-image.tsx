"use client";

import { ImageOff } from "lucide-react";
import Image, { type ImageProps } from "next/image";
import { useTranslations } from "next-intl";
import { useCallback, useState } from "react";

import { env } from "@/lib/env";
import { isOptimizableImage } from "@/lib/image-hosts";
import { cn } from "@/lib/utils";

type RemoteImageProps = Omit<ImageProps, "src"> & { src: string | null | undefined };

/**
 * next/image for API-provided URLs. Hosts outside `remotePatterns` are rendered
 * unoptimized instead of throwing, and broken images show a neutral placeholder.
 */
export function RemoteImage({ src, alt, className, ...props }: RemoteImageProps) {
  const t = useTranslations("common");
  const [failedSrc, setFailedSrc] = useState<string | null>(null);

  // A server-rendered <img> can fail before React hydrates and attaches
  // `onError`, so also check the element's state once it mounts.
  const ref = useCallback(
    (img: HTMLImageElement | null) => {
      if (img && src && img.complete && img.naturalWidth === 0) setFailedSrc(src);
    },
    [src],
  );

  if (!src || failedSrc === src) {
    return (
      <div
        role="img"
        aria-label={alt || t("imageUnavailable")}
        className={cn(
          "grid size-full place-items-center bg-muted text-muted-foreground",
          props.fill && "absolute inset-0",
          className,
        )}
      >
        <ImageOff className="size-6" aria-hidden="true" />
      </div>
    );
  }

  return (
    <Image
      ref={ref}
      src={src}
      alt={alt}
      className={className}
      unoptimized={!isOptimizableImage(src, env.NEXT_PUBLIC_API_BASE_URL)}
      onError={() => setFailedSrc(src)}
      {...props}
    />
  );
}
