"use client";

import { Slider as SliderPrimitive } from "radix-ui";
import type * as React from "react";

import { cn } from "@/lib/utils";

/** Range or single-value slider; one thumb per value. Pass `thumbLabels` for accessible names. */
function Slider({
  className,
  value,
  defaultValue,
  thumbLabels,
  ...props
}: React.ComponentProps<typeof SliderPrimitive.Root> & { thumbLabels?: string[] }) {
  const count = (value ?? defaultValue ?? [0]).length;
  return (
    <SliderPrimitive.Root
      data-slot="slider"
      value={value}
      defaultValue={defaultValue}
      className={cn(
        "relative flex w-full touch-none items-center select-none data-[disabled]:opacity-50",
        className,
      )}
      {...props}
    >
      <SliderPrimitive.Track className="relative h-1.5 w-full grow overflow-hidden rounded-full bg-muted">
        <SliderPrimitive.Range className="absolute h-full bg-primary" />
      </SliderPrimitive.Track>
      {Array.from({ length: count }, (_, i) => (
        <SliderPrimitive.Thumb
          key={i}
          aria-label={thumbLabels?.[i]}
          className="block size-6 rounded-full border bg-background shadow-card ring-ring/40 transition-[box-shadow,transform] hover:scale-105 focus-visible:ring-[4px] focus-visible:outline-none"
        />
      ))}
    </SliderPrimitive.Root>
  );
}

export { Slider };
