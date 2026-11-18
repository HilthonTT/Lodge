"use client";

import "leaflet/dist/leaflet.css";

import { useTranslations } from "next-intl";
import { Circle, MapContainer, TileLayer } from "react-leaflet";

export type LeafletMapProps = {
  center: [number, number];
  label: string;
};

/** Approximate-area map. Loaded lazily (no SSR) by `ApartmentMap`. */
export default function LeafletMap({ center, label }: LeafletMapProps) {
  const t = useTranslations("apartment");
  return (
    <MapContainer
      center={center}
      zoom={4}
      scrollWheelZoom={false}
      className="size-full"
      attributionControl
      aria-label={t("mapLabel", { label })}
    >
      <TileLayer
        attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
        url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
      />
      <Circle
        center={center}
        radius={180_000}
        pathOptions={{ color: "#2f8f6f", fillColor: "#2f8f6f", fillOpacity: 0.18, weight: 2 }}
      />
    </MapContainer>
  );
}
