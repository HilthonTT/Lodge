"use client";

import "leaflet/dist/leaflet.css";

import L from "leaflet";
import Link from "next/link";
import { useTranslations } from "next-intl";
import { useEffect, useMemo, useRef } from "react";
import { MapContainer, Marker, Popup, TileLayer, useMap, useMapEvents } from "react-leaflet";

import { RemoteImage } from "@/components/shared/remote-image";
import { useDisplayMoney } from "@/features/payments/currency-provider";
import type { Apartment } from "@/types/api";

import type { MapBounds } from "../search-params";
import { RatingSummary } from "./rating-summary";

export type SearchLeafletMapProps = {
  apartments: Apartment[];
  /** Query string forwarded to the listing links. */
  query?: string;
  hoveredId: string | null;
  onHover: (id: string | null) => void;
  /** "Search as I move the map" is on. */
  moveSearch: boolean;
  /** Bounds from the URL (only while `moveSearch`). */
  bounds?: MapBounds;
  onBoundsChange: (bounds: MapBounds) => void;
};

const toBounds = (b: L.LatLngBounds): MapBounds => ({
  minLat: b.getSouth(),
  maxLat: b.getNorth(),
  minLng: Math.max(-180, b.getWest()),
  maxLng: Math.min(180, b.getEast()),
});

/** Whether a move happened inside the window reserved for code-driven moves. */
const isProgrammaticMove = (until: number) => Date.now() < until;

const iconCache = new Map<string, L.DivIcon>();

function priceIcon(label: string, active: boolean) {
  const cacheKey = `${label}|${active}`;
  const cached = iconCache.get(cacheKey);
  if (cached) return cached;
  const tone = active
    ? "scale-110 border-foreground bg-foreground text-background"
    : "border-border bg-background text-foreground hover:scale-105";
  const icon = L.divIcon({
    className: "",
    iconSize: [0, 0],
    html: `<span class="absolute top-0 left-0 inline-flex -translate-x-1/2 -translate-y-1/2 items-center whitespace-nowrap rounded-full border px-2.5 py-1 text-sm font-semibold shadow-card transition-transform ${tone}">${label}</span>`,
  });
  iconCache.set(cacheKey, icon);
  return icon;
}

/** Fits the results (or the URL bounds) once, then on every new result set unless searching by map. */
function Viewport({
  apartments,
  moveSearch,
  bounds,
  programmaticRef,
}: {
  apartments: Apartment[];
  moveSearch: boolean;
  bounds?: MapBounds;
  /** Moves before this timestamp were made by code, not the user. */
  programmaticRef: React.RefObject<number>;
}) {
  const map = useMap();
  const ids = apartments.map((a) => a.id).join(",");
  const initialised = useRef(false);

  useEffect(() => {
    if (moveSearch && initialised.current) return;
    const first = !initialised.current;
    initialised.current = true;
    if (moveSearch && bounds && first) {
      programmaticRef.current = Date.now() + 900;
      map.fitBounds(
        [
          [bounds.minLat, bounds.minLng],
          [bounds.maxLat, bounds.maxLng],
        ],
        { animate: false },
      );
      return;
    }
    const points = apartments
      .filter((a) => Number.isFinite(a.latitude) && Number.isFinite(a.longitude))
      .map((a) => [a.latitude, a.longitude] as [number, number]);
    if (points.length === 0) return;
    programmaticRef.current = Date.now() + 900;
    if (points.length === 1) map.setView(points[0]!, 11, { animate: !first });
    else map.fitBounds(points, { padding: [48, 48], maxZoom: 12, animate: !first });
    // Only re-fit when the result set changes.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [ids, moveSearch, map]);

  return null;
}

/** Reports the visible area after the user pans or zooms (debounced). */
function MoveListener({
  moveSearch,
  onBoundsChange,
  programmaticRef,
}: {
  moveSearch: boolean;
  onBoundsChange: (bounds: MapBounds) => void;
  /** Moves before this timestamp were made by code, not the user. */
  programmaticRef: React.RefObject<number>;
}) {
  const timer = useRef<number | undefined>(undefined);
  const latest = useRef(onBoundsChange);
  useEffect(() => {
    latest.current = onBoundsChange;
  }, [onBoundsChange]);

  const map = useMapEvents({
    moveend: () => {
      if (isProgrammaticMove(programmaticRef.current)) return;
      if (!moveSearch) return;
      window.clearTimeout(timer.current);
      timer.current = window.setTimeout(() => latest.current(toBounds(map.getBounds())), 450);
    },
  });

  // Turning the option on searches the current view right away.
  const wasOn = useRef(moveSearch);
  useEffect(() => {
    if (moveSearch && !wasOn.current) latest.current(toBounds(map.getBounds()));
    wasOn.current = moveSearch;
  }, [moveSearch, map]);

  useEffect(() => () => window.clearTimeout(timer.current), []);
  return null;
}

export default function SearchLeafletMap({
  apartments,
  query,
  hoveredId,
  onHover,
  moveSearch,
  bounds,
  onBoundsChange,
}: SearchLeafletMapProps) {
  const t = useTranslations("search.map");
  const tCommon = useTranslations("common");
  const money = useDisplayMoney();
  const programmaticRef = useRef(0);

  const markers = useMemo(
    () =>
      apartments
        .filter((a) => Number.isFinite(a.latitude) && Number.isFinite(a.longitude))
        .map((a) => ({ apartment: a, label: money.format(a.price, a.currency, true) })),
    [apartments, money],
  );

  return (
    <MapContainer
      center={[30, 10]}
      zoom={2}
      minZoom={2}
      worldCopyJump
      scrollWheelZoom
      className="size-full"
      aria-label={t("label")}
    >
      <TileLayer
        attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
        url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
      />
      <Viewport apartments={apartments} moveSearch={moveSearch} bounds={bounds} programmaticRef={programmaticRef} />
      <MoveListener moveSearch={moveSearch} onBoundsChange={onBoundsChange} programmaticRef={programmaticRef} />
      {markers.map(({ apartment, label }) => {
        const active = hoveredId === apartment.id;
        const href = `/apartments/${apartment.id}${query ? `?${query}` : ""}`;
        return (
          <Marker
            key={apartment.id}
            position={[apartment.latitude, apartment.longitude]}
            icon={priceIcon(label, active)}
            zIndexOffset={active ? 1000 : 0}
            title={apartment.name}
            alt={t("marker", { name: apartment.name, price: label })}
            eventHandlers={{
              mouseover: () => onHover(apartment.id),
              mouseout: () => onHover(null),
            }}
          >
            <Popup closeButton={false} minWidth={240} maxWidth={260} className="[&_.leaflet-popup-content]:m-0! [&_.leaflet-popup-content-wrapper]:overflow-hidden [&_.leaflet-popup-content-wrapper]:rounded-xl! [&_.leaflet-popup-content-wrapper]:p-0!">
              <Link href={href} className="block w-60 overflow-hidden rounded-xl text-foreground! no-underline">
                <span className="relative block aspect-[4/3] bg-muted">
                  <RemoteImage src={apartment.imageUrl} alt="" fill sizes="240px" className="object-cover" />
                </span>
                <span className="grid gap-0.5 p-3">
                  <span className="flex items-start justify-between gap-2">
                    <span className="line-clamp-1 font-semibold">{apartment.name}</span>
                    <RatingSummary
                      averageRating={apartment.averageRating}
                      reviewCount={apartment.reviewCount}
                      className="shrink-0"
                    />
                  </span>
                  <span className="text-sm">
                    <span className="font-semibold">{label}</span>{" "}
                    <span className="text-muted-foreground">{tCommon("perNight")}</span>
                  </span>
                </span>
              </Link>
            </Popup>
          </Marker>
        );
      })}
    </MapContainer>
  );
}
