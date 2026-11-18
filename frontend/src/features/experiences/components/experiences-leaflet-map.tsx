"use client";

import "leaflet/dist/leaflet.css";

import L from "leaflet";
import Link from "next/link";
import { useLocale, useTranslations } from "next-intl";
import { useEffect } from "react";
import { MapContainer, Marker, Popup, TileLayer, useMap } from "react-leaflet";

import { useDisplayMoney } from "@/features/payments/currency-provider";
import { formatPriceCompact } from "@/lib/format";
import type { ExperienceSummary } from "@/types/api";

export type ExperiencesLeafletMapProps = {
  experiences: ExperienceSummary[];
  hoveredId: string | null;
  onHover: (id: string | null) => void;
};

function priceIcon(label: string, active: boolean) {
  const tone = active
    ? "scale-110 border-foreground bg-foreground text-background"
    : "border-border bg-background text-foreground hover:scale-105";
  return L.divIcon({
    className: "",
    iconSize: [0, 0],
    html: `<span class="absolute top-0 left-0 inline-flex -translate-x-1/2 -translate-y-1/2 items-center whitespace-nowrap rounded-full border px-2.5 py-1 text-sm font-semibold shadow-card transition-transform ${tone}">${label}</span>`,
  });
}

function FitBounds({ experiences }: { experiences: ExperienceSummary[] }) {
  const map = useMap();
  const ids = experiences.map((e) => e.id).join(",");
  useEffect(() => {
    const points = experiences.map((e) => [e.latitude, e.longitude] as [number, number]);
    if (points.length === 1) map.setView(points[0]!, 11);
    else if (points.length > 1) map.fitBounds(L.latLngBounds(points), { padding: [40, 40], maxZoom: 12 });
    // Only refit when the result set changes.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [ids, map]);
  return null;
}

/** Price pins for the experiences on the current page. Loaded lazily (no SSR). */
export default function ExperiencesLeafletMap({ experiences, hoveredId, onHover }: ExperiencesLeafletMapProps) {
  const t = useTranslations("experiences");
  const locale = useLocale();
  const { convert } = useDisplayMoney();

  return (
    <MapContainer center={[30, 10]} zoom={2} scrollWheelZoom className="size-full" aria-label={t("mapLabel")}>
      <TileLayer
        attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
        url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
      />
      <FitBounds experiences={experiences} />
      {experiences.map((e) => {
        const money = convert(e.pricePerGuest, e.currency);
        const label = `${money.converted ? "≈ " : ""}${formatPriceCompact(money.amount, money.currency, locale)}`;
        return (
          <Marker
            key={e.id}
            position={[e.latitude, e.longitude]}
            icon={priceIcon(label, hoveredId === e.id)}
            zIndexOffset={hoveredId === e.id ? 1000 : 0}
            eventHandlers={{ mouseover: () => onHover(e.id), mouseout: () => onHover(null) }}
            title={e.title}
          >
            <Popup>
              <Link href={`/experiences/${e.id}`} className="grid gap-0.5 font-sans text-foreground no-underline">
                <span className="font-semibold">{e.title}</span>
                <span className="text-xs text-muted-foreground">{[e.city, e.country].filter(Boolean).join(", ")}</span>
              </Link>
            </Popup>
          </Marker>
        );
      })}
    </MapContainer>
  );
}
