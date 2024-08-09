"use client";

import qs from "query-string";
import dynamic from "next/dynamic";
import { useMemo, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";

import { useLocationApartment } from "@/features/apartments/hooks/use-location-apartment";

import { useCountries } from "@/hooks/use-countries";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
} from "@/components/ui/dialog";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Button } from "@/components/ui/button";

export const LocationApartmentModal = () => {
  const router = useRouter();
  const searchParams = useSearchParams();

  const { isOpen, onClose } = useLocationApartment();
  const { countries, getByValue } = useCountries();

  const selectedCountry = getByValue(searchParams.get("country") || "");

  const [location, setLocation] = useState<Country | undefined>(
    selectedCountry
  );

  const Map = useMemo(
    () =>
      dynamic(() => import("@/components/map"), {
        ssr: false,
      }),
    [location]
  );

  const onChange = (label: string) => {
    const country = getByValue(label);
    if (!country) {
      return;
    }

    setLocation(country);
  };

  const onSaveChanges = () => {
    const allParams = Object.fromEntries(searchParams.entries());

    const url = qs.stringifyUrl(
      {
        url: "/",
        query: {
          ...allParams,
          country: location?.label,
        },
      },
      {
        skipEmptyString: true,
        skipNull: true,
      }
    );

    router.push(url);

    onClose();
  };

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent>
        <DialogHeader className="font-bold">Search by region</DialogHeader>

        <Select value={location?.label} onValueChange={onChange}>
          <SelectTrigger>
            <SelectValue placeholder="Select a country" />
          </SelectTrigger>
          <SelectContent>
            {countries.map((country) => (
              <SelectItem key={country.label} value={country.label}>
                {country.flag} {country.label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        <Map center={location?.latlng as L.LatLngExpression} />

        <DialogFooter>
          <Button onClick={onSaveChanges}>Save changes</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
};
