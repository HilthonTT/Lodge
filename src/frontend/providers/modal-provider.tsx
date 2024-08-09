"use client";

import { useEffect, useState } from "react";

import { LocationApartmentModal } from "@/features/apartments/components/location-apartment-modal";
import { DateApartmentModal } from "@/features/apartments/components/date-apartment-modal";

export const ModalProvider = () => {
  const [isMounted, setIsMounted] = useState(false);

  useEffect(() => {
    setIsMounted(true);
  }, []);

  if (!isMounted) {
    return null;
  }

  return (
    <>
      <LocationApartmentModal />
      <DateApartmentModal />
    </>
  );
};
