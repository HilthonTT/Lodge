"use client";

import { Star } from "lucide-react";
import { useTranslations } from "next-intl";
import { useId, useState } from "react";

import { cn } from "@/lib/utils";

const VALUES = [1, 2, 3, 4, 5] as const;

type StarInputProps = {
  /** Visible label (the radio group's legend). */
  label: string;
  value: number;
  onChange: (value: number) => void;
  size?: "sm" | "lg";
  error?: string;
  /** Show the word for the chosen rating ("Excellent") next to the stars. */
  showLabel?: boolean;
  className?: string;
};

/**
 * Accessible 1–5 star rating: a native radio group, so arrow keys move between
 * stars, Tab enters and leaves the group and screen readers announce
 * "4 stars – Good, 4 of 5". Hovering previews a value.
 */
export function StarInput({ label, value, onChange, size = "sm", error, showLabel, className }: StarInputProps) {
  const t = useTranslations("reviews");
  const name = useId();
  const errorId = `${name}-error`;
  const [hover, setHover] = useState(0);
  const shown = hover || value;
  const word = (n: number) => t(`labels.${String(n) as "1" | "2" | "3" | "4" | "5"}`);

  return (
    <fieldset
      className={cn("flex flex-wrap items-center justify-between gap-x-4 gap-y-1", className)}
      aria-describedby={error ? errorId : undefined}
      aria-invalid={error ? true : undefined}
    >
      <legend className={cn("float-left font-medium", size === "lg" ? "text-base" : "text-sm")}>{label}</legend>
      <div className="flex items-center gap-2">
        {showLabel && (
          <span className="min-w-16 text-right text-sm text-muted-foreground" aria-hidden="true">
            {shown ? word(shown) : ""}
          </span>
        )}
        <div className="flex" onMouseLeave={() => setHover(0)}>
          {VALUES.map((n) => (
            <label
              key={n}
              onMouseEnter={() => setHover(n)}
              className="cursor-pointer rounded-md p-0.5 transition-transform hover:scale-110 has-[:focus-visible]:ring-[3px] has-[:focus-visible]:ring-ring/40"
            >
              <input
                type="radio"
                name={name}
                value={n}
                checked={value === n}
                onChange={() => onChange(n)}
                className="sr-only"
                aria-label={t("starLabel", { count: n, label: word(n) })}
              />
              <Star
                aria-hidden="true"
                className={cn(
                  size === "lg" ? "size-8" : "size-6",
                  n <= shown ? "fill-star text-star" : "text-muted-foreground/40",
                )}
              />
            </label>
          ))}
        </div>
      </div>
      {error && (
        <p id={errorId} className="w-full text-xs font-medium text-destructive">
          {error}
        </p>
      )}
    </fieldset>
  );
}
