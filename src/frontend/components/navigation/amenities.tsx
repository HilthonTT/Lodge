"use client";

import { useSearchParams } from "next/navigation";

import { AMENITIES } from "@/constants";
import { AmenityBox } from "@/components/navigation/amenity-box";
import { ScrollArea, ScrollBar } from "@/components/ui/scroll-area";
import { Container } from "@/components/container";

export const Amenities = () => {
  const searchParams = useSearchParams();

  const amenity = searchParams.get("amenity");

  return (
    <Container>
      <ScrollArea>
        <div className="flex w-max space-x-4 p-4">
          {AMENITIES.map((item) => (
            <AmenityBox
              key={item.index}
              label={item.label}
              icon={item.icon}
              selected={amenity === item.label}
            />
          ))}
        </div>
        <ScrollBar orientation="horizontal" />
      </ScrollArea>
    </Container>
  );
};
