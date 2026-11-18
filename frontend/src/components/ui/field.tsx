import type * as React from "react";

import { cn } from "@/lib/utils";

import { Label } from "./label";

type FieldControlProps = {
  id: string;
  "aria-invalid": boolean;
  "aria-describedby": string | undefined;
};

type FieldProps = {
  id: string;
  label: React.ReactNode;
  error?: string;
  description?: React.ReactNode;
  className?: string;
  /** Rendered next to the label, e.g. a "show password" toggle. */
  aside?: React.ReactNode;
  children: (control: FieldControlProps) => React.ReactNode;
};

/** Accessible form field: wires label, description and error message to the control. */
function Field({ id, label, error, description, className, aside, children }: FieldProps) {
  const descriptionId = description ? `${id}-description` : undefined;
  const errorId = error ? `${id}-error` : undefined;
  const describedBy = [descriptionId, errorId].filter(Boolean).join(" ") || undefined;

  return (
    <div className={cn("grid gap-2", className)}>
      <div className="flex min-h-4 items-center justify-between gap-2">
        <Label htmlFor={id} className={cn(error && "text-destructive")}>
          {label}
        </Label>
        {aside}
      </div>
      {children({ id, "aria-invalid": Boolean(error), "aria-describedby": describedBy })}
      {description && !error && (
        <p id={descriptionId} className="text-xs text-muted-foreground">
          {description}
        </p>
      )}
      {error && (
        <p id={errorId} className="text-xs font-medium text-destructive">
          {error}
        </p>
      )}
    </div>
  );
}

export { Field };
