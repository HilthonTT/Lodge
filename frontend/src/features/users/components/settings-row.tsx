import type * as React from "react";

import { Button } from "@/components/ui/button";

type SettingsRowProps = {
  title: string;
  value: React.ReactNode;
  editing: boolean;
  onEdit: () => void;
  onCancel: () => void;
  editLabel?: string;
  disabled?: boolean;
  children: React.ReactNode;
};

/** Airbnb-style "label · value · Edit" row that expands into a form. */
export function SettingsRow({
  title,
  value,
  editing,
  onEdit,
  onCancel,
  editLabel = "Edit",
  disabled,
  children,
}: SettingsRowProps) {
  return (
    <section className="grid gap-4 border-b py-6 first:pt-0">
      <div className="flex items-start justify-between gap-4">
        <div className="grid gap-1">
          <h2 className="font-medium">{title}</h2>
          {!editing && <div className="text-sm text-muted-foreground">{value}</div>}
        </div>
        <Button
          variant="link"
          className="h-auto p-0 text-foreground underline"
          onClick={editing ? onCancel : onEdit}
          disabled={disabled}
          aria-expanded={editing}
        >
          {editing ? "Cancel" : editLabel}
        </Button>
      </div>
      {editing && children}
    </section>
  );
}
