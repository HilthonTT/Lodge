"use client";

import { cva, VariantProps } from "class-variance-authority";
import { IconType } from "react-icons/lib";

import { CountUp } from "@/components/count-up";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { cn } from "@/lib/utils";
import { Skeleton } from "@/components/ui/skeleton";

const boxVariant = cva("rounded-md p-3", {
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

const iconVariant = cva("size-6", {
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

type BoxVariants = VariantProps<typeof boxVariant>;
type IconVariants = VariantProps<typeof iconVariant>;

interface Props extends BoxVariants, IconVariants {
  icon: IconType;
  title: string;
  description: string;
  value: number;
  className?: string;
}

export const StatCard = ({
  icon: Icon,
  value,
  title,
  description,
  variant,
  className,
}: Props) => {
  return (
    <Card className={cn("border-none drop-shadow-sm", className)}>
      <CardHeader className="flex flex-row items-center justify-between gap-x-4">
        <div className="space-y-2">
          <CardTitle className="text-2xl lime-clamp-1">{title}</CardTitle>
          <CardDescription className="line-clamp-1">
            {description}
          </CardDescription>
        </div>
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
      </CardHeader>
      <CardContent>
        <h1 className="font-bold text-2xl mb-2 line-clamp-1 break-all">
          <CountUp
            preserveValue
            start={0}
            end={value}
            decimal="2"
            decimalPlaces={2}
          />
        </h1>
      </CardContent>
    </Card>
  );
};

export const StatCardSkeleton = () => {
  return <Skeleton className="h-24 w-full" />;
};
