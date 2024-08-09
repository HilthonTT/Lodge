"use client";

import qs from "query-string";
import { useState } from "react";
import { Range, RangeKeyDict } from "react-date-range";
import { useRouter, useSearchParams } from "next/navigation";
import { formatISO } from "date-fns";

import { useDateApartment } from "@/features/apartments/hooks/use-date-apartment";

import { Calendar } from "@/components/calendar";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";

export const DateApartmentModal = () => {
  const router = useRouter();
  const searchParams = useSearchParams();

  const { isOpen, onClose } = useDateApartment();

  const [dateRange, setDateRange] = useState<Range>({
    startDate: new Date(),
    endDate: new Date(),
    key: "selection",
  });

  const onChange = (value: RangeKeyDict) => {
    setDateRange(value.selection);
  };

  const onSaveChanges = () => {
    const allParams = Object.fromEntries(searchParams.entries());

    const url = qs.stringifyUrl(
      {
        url: "/",
        query: {
          ...allParams,
          startDate: formatISO(dateRange.startDate || new Date()),
          endDate: formatISO(dateRange.endDate || new Date()),
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
        <DialogHeader className="font-bold">Schedule your dates</DialogHeader>

        <Calendar value={dateRange} onChange={onChange} />

        <DialogFooter>
          <Button onClick={onSaveChanges}>Save changes</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
};
