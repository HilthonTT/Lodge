"use client";

import type * as React from "react";

import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";

import { ONE_TIME_CODE_LENGTH } from "../schemas";

type OneTimeCodeInputProps = Omit<React.ComponentProps<"input">, "value" | "onChange" | "type"> & {
  value: string;
  onValueChange: (value: string) => void;
  /** Called once the user has typed or pasted all 6 digits. */
  onComplete?: (code: string) => void;
};

/** Single numeric field for authenticator codes; works with SMS/OS autofill and paste. */
export function OneTimeCodeInput({ value, onValueChange, onComplete, className, ...props }: OneTimeCodeInputProps) {
  return (
    <Input
      type="text"
      inputMode="numeric"
      autoComplete="one-time-code"
      pattern="[0-9]*"
      maxLength={ONE_TIME_CODE_LENGTH}
      placeholder={"0".repeat(ONE_TIME_CODE_LENGTH)}
      spellCheck={false}
      className={cn(
        "h-12 text-center font-mono text-xl tracking-[0.5em] placeholder:text-muted-foreground/40",
        className,
      )}
      value={value}
      onChange={(event) => {
        const digits = event.target.value.replace(/\D/g, "").slice(0, ONE_TIME_CODE_LENGTH);
        onValueChange(digits);
        if (digits.length === ONE_TIME_CODE_LENGTH && digits !== value) onComplete?.(digits);
      }}
      {...props}
    />
  );
}
