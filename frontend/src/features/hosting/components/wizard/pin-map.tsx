"use client";

import "leaflet/dist/leaflet.css";

import L from "leaflet";
import { useTranslations } from "next-intl";
import { useEffect, useMemo } from "react";
import { MapContainer, Marker, TileLayer, useMap, useMapEvents } from "react-leaflet";

export type PinMapProps = {
  value: { lat: number; lng: number } | null;
  onChange: (value: { lat: number; lng: number }) => void;
  /** Where to look when there's no pin yet (e.g. the city the host typed). */
  focus: { lat: number; lng: number; zoom: number } | null;
};

// A CSS pin (Leaflet's default marker images don't survive bundling).
const pinIcon = L.divIcon({
  className: "",
  html: '<span style="display:block;width:28px;height:28px;border-radius:50% 50% 50% 0;background:var(--primary);border:3px solid white;transform:rotate(-45deg);box-shadow:0 4px 10px rgb(0 0 0 / .35)"></span>',
  iconSize: [28, 28],
  iconAnchor: [14, 28],
});

function ClickToDrop({ onChange }: { onChange: PinMapProps["onChange"] }) {
  useMapEvents({
    click: (event) => onChange({ lat: event.latlng.lat, lng: event.latlng.lng }),
  });
  return null;
}

function FlyTo({ focus }: { focus: PinMapProps["focus"] }) {
  const map = useMap();
  useEffect(() => {
    if (focus) map.flyTo([focus.lat, focus.lng], focus.zoom, { duration: 0.8 });
  }, [focus, map]);
  return null;
}

/** Click to drop a pin, drag to adjust. Loaded lazily (no SSR) by `LocationStep`. */
export default function PinMap({ value, onChange, focus }: PinMapProps) {
  const t = useTranslations("hosting.wizard.location");
  const eventHandlers = useMemo(
    () => ({
      dragend: (event: L.LeafletEvent) => {
        const { lat, lng } = (event.target as L.Marker).getLatLng();
        onChange({ lat, lng });
      },
    }),
    [onChange],
  );

  return (
    <MapContainer
      center={value ? [value.lat, value.lng] : [30, 10]}
      zoom={value ? 13 : 2}
      scrollWheelZoom
      className="size-full cursor-crosshair"
      aria-label={t("mapLabel")}
    >
      <TileLayer
        attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
        url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
      />
      <ClickToDrop onChange={onChange} />
      <FlyTo focus={focus} />
      {value && (
        <Marker
          position={[value.lat, value.lng]}
          draggable
          icon={pinIcon}
          eventHandlers={eventHandlers}
          title={t("pinTitle")}
          keyboard
        />
      )}
    </MapContainer>
  );
}
