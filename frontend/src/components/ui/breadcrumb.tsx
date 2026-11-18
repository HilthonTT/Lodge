import { ChevronRight } from "lucide-react";
import Link from "next/link";
import { useTranslations } from "next-intl";
import type * as React from "react";

import { cn } from "@/lib/utils";

type Crumb = { label: string; href?: string };

/** Simple accessible breadcrumb trail; the last item is the current page. */
function Breadcrumbs({ items, className }: { items: Crumb[]; className?: string }) {
  const t = useTranslations("common");
  return (
    <nav aria-label={t("breadcrumb")} className={className}>
      <ol className="flex flex-wrap items-center gap-1.5 text-sm text-muted-foreground">
        {items.map((item, index) => {
          const isLast = index === items.length - 1;
          return (
            <li key={item.label} className="inline-flex items-center gap-1.5">
              {item.href && !isLast ? (
                <Link href={item.href} className="rounded-sm transition-colors hover:text-foreground">
                  {item.label}
                </Link>
              ) : (
                <span aria-current={isLast ? "page" : undefined} className={cn(isLast && "text-foreground")}>
                  {item.label}
                </span>
              )}
              {!isLast && <ChevronRight className="size-3.5" aria-hidden="true" />}
            </li>
          );
        })}
      </ol>
    </nav>
  );
}

export type { Crumb };
export { Breadcrumbs };

export type BreadcrumbProps = React.ComponentProps<typeof Breadcrumbs>;
