"use client";

import qs from "query-string";
import { useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";

import { useCountApartment } from "@/features/apartments/hooks/use-count-apartment";

import { Dialog, DialogContent } from "@/components/ui/dialog";
import { Separator } from "@/components/ui/separator";
import { Counter } from "@/components/counter";
import { Button } from "@/components/ui/button";

export const CountApartmentModal = () => {
  const router = useRouter();
  const searchParams = useSearchParams();

  const { isOpen, onClose } = useCountApartment();

  const guestCountAsString = searchParams.get("guestCount");
  const roomCountAsString = searchParams.get("roomCount");

  const [guestCount, setGuestCount] = useState(Number(guestCountAsString) || 1);
  const [roomCount, setRoomCount] = useState(Number(roomCountAsString) || 1);

  const canDecreaseGuestCount = guestCount > 1;
  const canDecreaseRoomCount = roomCount > 1;

  const decreaseGuestCount = () => {
    if (!canDecreaseGuestCount) {
      return;
    }

    setGuestCount((prev) => prev - 1);
  };

  const decreaseRoomCount = () => {
    if (!canDecreaseRoomCount) {
      return;
    }

    setRoomCount((prev) => prev - 1);
  };

  const increaseRoomCount = () => {
    setRoomCount((prev) => prev + 1);
  };

  const increaseGuestCount = () => {
    setGuestCount((prev) => prev + 1);
  };

  const onSaveChanges = () => {
    const allParams = Object.fromEntries(searchParams.entries());

    const url = qs.stringifyUrl(
      {
        url: "/search",
        query: {
          ...allParams,
          guestCount,
          roomCount,
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
        <Counter
          value={guestCount}
          title="Guest Count"
          description="Amount of guests that will be attending"
          onIncrease={increaseGuestCount}
          onDecrease={decreaseGuestCount}
          canDecrease={canDecreaseGuestCount}
        />
        <Separator className="my-4" />
        <Counter
          value={roomCount}
          title="Room Count"
          description="Rooms you want in your lodge"
          onIncrease={increaseRoomCount}
          onDecrease={decreaseRoomCount}
          canDecrease={canDecreaseRoomCount}
        />

        <Separator className="my-4" />

        <div className="w-full">
          <Button onClick={onSaveChanges}>Save changes</Button>
        </div>
      </DialogContent>
    </Dialog>
  );
};
