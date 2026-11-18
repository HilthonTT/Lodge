"use client";

import { MapPin } from "lucide-react";
import dynamic from "next/dynamic";
import { useEffect, useRef, useState } from "react";

import { Skeleton } from "@/components/ui/skeleton";

import type { LeafletMapProps } from "./leaflet-map";

const LeafletMap = dynamic(() => import("./leaflet-map"), {
  ssr: false,
  loading: () => <Skeleton className="size-full rounded-none" />,
});

/** Defers loading Leaflet (~150 KB) until the map scrolls into view. */
export function ApartmentMap(props: LeafletMapProps) {
  const ref = useRef<HTMLDivElement>(null);
  const [visible, setVisible] = useState(false);

  useEffect(() => {
    const node = ref.current;
    if (!node) return;
    const observer = new IntersectionObserver(
      (entries) => {
        if (entries.some((e) => e.isIntersecting)) {
          setVisible(true);
          observer.disconnect();
        }
      },
      { rootMargin: "200px" },
    );
    observer.observe(node);
    return () => observer.disconnect();
  }, []);

  return (
    <div ref={ref} className="relative isolate h-72 overflow-hidden rounded-2xl border bg-muted sm:h-80">
      {visible ? (
        <LeafletMap {...props} />
      ) : (
        <div className="grid size-full place-items-center text-muted-foreground">
          <MapPin className="size-6" aria-hidden="true" />
        </div>
      )}
    </div>
  );
}
