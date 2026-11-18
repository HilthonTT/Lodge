"use client";

import {
  CheckCircle2,
  CircleCheckBig,
  Clock,
  Dumbbell,
  Flower2,
  Hammer,
  Landmark,
  type LucideIcon,
  Martini,
  Mountain,
  Palette,
  Utensils,
  XCircle,
} from "lucide-react";
import { useLocale, useTranslations } from "next-intl";

import { RemoteImage } from "@/components/shared/remote-image";
import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import type { ExperienceBookingStatus, ExperienceCategory } from "@/types/api";

/** Icon and gradient per category, for chips and image placeholders. */
export const CATEGORY_THEME: Record<ExperienceCategory, { icon: LucideIcon; gradient: string }> = {
  Food: { icon: Utensils, gradient: "from-amber-400 via-orange-500 to-rose-500" },
  Outdoors: { icon: Mountain, gradient: "from-emerald-400 via-teal-500 to-sky-600" },
  Art: { icon: Palette, gradient: "from-fuchsia-400 via-pink-500 to-orange-400" },
  Culture: { icon: Landmark, gradient: "from-indigo-400 via-violet-500 to-purple-600" },
  Sports: { icon: Dumbbell, gradient: "from-sky-400 via-blue-500 to-indigo-600" },
  Wellness: { icon: Flower2, gradient: "from-lime-300 via-emerald-400 to-teal-500" },
  Nightlife: { icon: Martini, gradient: "from-violet-600 via-purple-700 to-slate-900" },
  Workshops: { icon: Hammer, gradient: "from-yellow-400 via-amber-500 to-stone-600" },
};

/** The photo, or a category-themed gradient with a large icon when there's none. */
export function ExperienceImage({
  src,
  category,
  sizes,
  priority,
  className,
  iconClassName,
}: {
  src: string | null | undefined;
  category: ExperienceCategory;
  sizes: string;
  priority?: boolean;
  className?: string;
  iconClassName?: string;
}) {
  const theme = CATEGORY_THEME[category] ?? CATEGORY_THEME.Culture;
  const Icon = theme.icon;
  if (src) return <RemoteImage src={src} alt="" fill sizes={sizes} priority={priority} className={cn("object-cover", className)} />;
  return (
    <span
      className={cn("absolute inset-0 grid place-items-center bg-gradient-to-br text-white", theme.gradient, className)}
      data-testid="experience-placeholder"
    >
      <span className="absolute inset-0 bg-[radial-gradient(circle_at_30%_20%,rgb(255_255_255/0.35),transparent_55%)]" />
      <Icon className={cn("relative size-14 drop-shadow-md", iconClassName)} strokeWidth={1.5} aria-hidden="true" />
    </span>
  );
}

/** "2 h", "1 h 30 min", "45 min". */
export function useFormatDuration() {
  const t = useTranslations("experiences");
  return (minutes: number) => {
    const h = Math.floor(minutes / 60);
    const m = minutes % 60;
    if (h && m) return t("durationHm", { hours: h, minutes: m });
    if (h) return t("durationH", { hours: h });
    return t("durationM", { minutes: m });
  };
}

/** Language names in the UI language ("en" → "English" / "Engels"). */
export function useLanguageName() {
  const locale = useLocale();
  let names: Intl.DisplayNames | null = null;
  try {
    names = new Intl.DisplayNames([locale], { type: "language" });
  } catch {
    names = null;
  }
  return (code: string) => {
    const name = names?.of(code) ?? code;
    return name.charAt(0).toLocaleUpperCase(locale) + name.slice(1);
  };
}

/** Languages hosts can pick (ISO 639-1). */
export const LANGUAGE_CODES = [
  "en", "nl", "fr", "de", "es", "it", "pt", "sv", "da", "no", "fi", "pl", "cs", "el", "tr", "ru", "uk", "ar", "he", "hi",
  "ja", "ko", "zh", "th", "vi", "id",
] as const;

const STATUS = {
  Reserved: { variant: "warning", icon: Clock },
  Confirmed: { variant: "success", icon: CheckCircle2 },
  Completed: { variant: "info", icon: CircleCheckBig },
  Cancelled: { variant: "muted", icon: XCircle },
} as const;

export function ExperienceStatusBadge({ status }: { status: ExperienceBookingStatus }) {
  const t = useTranslations("experiences.status");
  const style = STATUS[status] ?? STATUS.Reserved;
  const Icon = style.icon;
  return (
    <Badge variant={style.variant}>
      <Icon aria-hidden="true" />
      {t(status)}
    </Badge>
  );
}

/** Service fee the API adds to experience bookings (Payments:GuestServiceFeePercent; not exposed by the API). */
export const EXPERIENCE_SERVICE_FEE_PERCENT = 12;

/** Mirrors the API's rounding (to cents). */
export function experienceTotal(pricePerGuest: number, guests: number) {
  const subtotal = Math.round(pricePerGuest * guests * 100) / 100;
  const fee = Math.round(subtotal * EXPERIENCE_SERVICE_FEE_PERCENT) / 100;
  return { subtotal, fee, total: Math.round((subtotal + fee) * 100) / 100 };
}
