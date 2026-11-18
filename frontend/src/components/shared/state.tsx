import { CloudOff, type LucideIcon, SearchX } from "lucide-react";
import { useTranslations } from "next-intl";
import type * as React from "react";

import { cn } from "@/lib/utils";

type StateProps = {
  icon?: LucideIcon;
  title: string;
  description?: React.ReactNode;
  action?: React.ReactNode;
  className?: string;
  tone?: "default" | "error";
};

/** Centered illustration + message used for empty and error states. */
export function StatePanel({
  icon: Icon = SearchX,
  title,
  description,
  action,
  className,
  tone = "default",
}: StateProps) {
  return (
    <div
      role={tone === "error" ? "alert" : undefined}
      className={cn(
        "flex flex-col items-center justify-center gap-4 rounded-2xl border border-dashed px-6 py-16 text-center",
        className,
      )}
    >
      <span
        className={cn(
          "grid size-14 place-items-center rounded-2xl",
          tone === "error" ? "bg-destructive/10 text-destructive" : "bg-accent text-accent-foreground",
        )}
      >
        <Icon className="size-6" aria-hidden="true" />
      </span>
      <div className="grid max-w-md gap-1.5">
        <h2 className="text-lg font-semibold">{title}</h2>
        {description && <p className="text-sm text-muted-foreground">{description}</p>}
      </div>
      {action && <div className="mt-2 flex flex-wrap items-center justify-center gap-2">{action}</div>}
    </div>
  );
}

export function EmptyState(props: Omit<StateProps, "tone">) {
  return <StatePanel {...props} />;
}

export function ErrorState({ title, ...props }: Omit<StateProps, "tone" | "title"> & { title?: string }) {
  const t = useTranslations("common");
  return <StatePanel icon={CloudOff} tone="error" title={title ?? t("couldNotLoad")} {...props} />;
}
