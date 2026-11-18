"use client";

import { Minus, Plus } from "lucide-react";

import { cn } from "@/lib/utils";

type Props = {
  value: number;
  title: string;
  description?: string;
  onIncrease: () => void;
  onDecrease: () => void;
  canDecrease: boolean;
};

export const Counter = ({
  value,
  title,
  description,
  onIncrease,
  onDecrease,
  canDecrease,
}: Props) => {
  return (
    <div className="flex items-center justify-between pr-8">
      <div className="flex flex-col items-start justify-center">
        <h1 className="text-2xl font-bold">{title}</h1>
        {!!description && (
          <p className="text-neutral-500 text-sm">{description}</p>
        )}
      </div>
      <div className="flex items-center justify-center gap-4">
        <button
          onClick={onDecrease}
          disabled={!canDecrease}
          className={cn(
            "rounded-full p-2 border",
            !canDecrease && "border-neutral-100"
          )}>
          <Minus className="size-4" />
        </button>
        <span>{value}</span>
        <button onClick={onIncrease} className="rounded-full p-2 border">
          <Plus className="size-4" />
        </button>
      </div>
    </div>
  );
};
