"use client";

import { ChevronLeftIcon, ChevronRightIcon } from "lucide-react";
import type * as React from "react";
import { DayPicker } from "react-day-picker";

import { cn } from "@/lib/utils";

import { buttonVariants } from "./button";

/** shadcn-style Calendar built on react-day-picker v10. */
function Calendar({
  className,
  classNames,
  showOutsideDays = false,
  ...props
}: React.ComponentProps<typeof DayPicker>) {
  return (
    <DayPicker
      showOutsideDays={showOutsideDays}
      className={cn("p-1 [--cell-size:2.5rem]", className)}
      classNames={{
        root: "relative w-fit",
        months: "relative flex flex-col gap-6 sm:flex-row",
        month: "flex w-full flex-col gap-3",
        nav: "absolute inset-x-0 top-0 z-10 flex items-center justify-between",
        button_previous: cn(
          buttonVariants({ variant: "ghost", size: "icon-sm" }),
          "rounded-full aria-disabled:opacity-40",
        ),
        button_next: cn(
          buttonVariants({ variant: "ghost", size: "icon-sm" }),
          "rounded-full aria-disabled:opacity-40",
        ),
        month_caption: "flex h-8 items-center justify-center",
        caption_label: "text-sm font-semibold",
        month_grid: "w-full border-collapse",
        weekdays: "flex",
        weekday: "w-(--cell-size) pb-1 text-center text-[0.7rem] font-medium text-muted-foreground uppercase",
        week: "mt-1 flex w-full",
        day: "group/day relative size-(--cell-size) p-0 text-center text-sm select-none",
        day_button: cn(
          "inline-flex size-(--cell-size) items-center justify-center rounded-full font-medium transition-colors outline-none",
          "hover:bg-muted focus-visible:ring-[3px] focus-visible:ring-ring/40",
          "group-data-[selected=true]/day:bg-primary group-data-[selected=true]/day:text-primary-foreground group-data-[selected=true]/day:hover:bg-primary/90",
          "group-data-[disabled=true]/day:pointer-events-none group-data-[disabled=true]/day:text-muted-foreground/45",
        ),
        range_start: "rounded-l-full bg-accent",
        range_middle:
          "bg-accent first:rounded-l-full last:rounded-r-full [&>button]:bg-transparent! [&>button]:text-accent-foreground! [&>button:hover]:bg-primary/15!",
        range_end: "rounded-r-full bg-accent",
        selected: "",
        today: "[&>button]:underline [&>button]:decoration-2 [&>button]:underline-offset-4",
        outside: "text-muted-foreground/50",
        disabled: "",
        hidden: "invisible",
        ...classNames,
      }}
      components={{
        Chevron: ({ orientation, className: chevronClass, ...rest }) =>
          orientation === "left" ? (
            <ChevronLeftIcon className={cn("size-4", chevronClass)} {...rest} />
          ) : (
            <ChevronRightIcon className={cn("size-4", chevronClass)} {...rest} />
          ),
      }}
      {...props}
    />
  );
}

export { Calendar };
