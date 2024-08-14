"use client";

import { cva, VariantProps } from "class-variance-authority";
import { IconType } from "react-icons/lib";

import { cn } from "@/lib/utils";

const boxVariant = cva("rounded-md p-2", {
  variants: {
    variant: {
      default: "bg-indigo-500/20",
      success: "bg-emerald-500/20",
      danger: "bg-rose-500/20",
      warning: "bg-yellow-500/20",
    },
  },
  defaultVariants: {
    variant: "default",
  },
});

const iconVariant = cva("size-5", {
  variants: {
    variant: {
      default: "fill-indigo-500",
      success: "fill-emerald-500",
      danger: "fill-rose-500",
      warning: "fill-yellow-500",
    },
  },
  defaultVariants: {
    variant: "default",
  },
});

type IconVariants = VariantProps<typeof iconVariant>;

type BoxVariants = VariantProps<typeof boxVariant>;

interface Props extends IconVariants, BoxVariants {
  icon: IconType;
}

export const BookingStatusIcon = ({ variant, icon: Icon }: Props) => {
  return (
    <div
      className={cn(
        "shrink-0",
        boxVariant({
          variant,
        })
      )}>
      <Icon
        className={cn(
          iconVariant({
            variant,
          })
        )}
      />
    </div>
  );
};
