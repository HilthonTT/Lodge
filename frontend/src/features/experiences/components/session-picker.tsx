"use client";

import { AlertCircle, CalendarX2, Loader2, ShieldCheck } from "lucide-react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { useMemo, useState } from "react";

import { Stepper } from "@/components/shared/stepper";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { Skeleton } from "@/components/ui/skeleton";
import { useSession } from "@/features/auth/session-provider";
import { ChargedAmount, ChargedInNote, Money } from "@/features/payments/components/money";
import { useIdempotencyKey } from "@/hooks/use-idempotency-key";
import { useApiErrorMessage } from "@/i18n/api-errors";
import { loginUrl } from "@/lib/auth/routes";
import { formatLongDay, formatTime, toDateOnly } from "@/lib/dates";
import { formatPrice } from "@/lib/format";
import { cn } from "@/lib/utils";
import type { Experience, ExperienceSession } from "@/types/api";

import { EXPERIENCE_SERVICE_FEE_PERCENT, experienceTotal } from "../experience-options";
import { useBookExperience, useExperienceSessions } from "../hooks/use-experiences";

type Props = {
  experience: Pick<Experience, "id" | "pricePerGuest" | "currency" | "maxGuests" | "durationMinutes" | "host">;
};

/** Sessions grouped by day with spots left, a guest stepper, the total and "Book". */
export function SessionPicker({ experience }: Props) {
  const t = useTranslations("experiences.book");
  const locale = useLocale();
  const pathname = usePathname();
  const errorMessage = useApiErrorMessage();
  const { session } = useSession();
  const sessions = useExperienceSessions(experience.id);
  const book = useBookExperience();
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [guests, setGuests] = useState(1);
  const [showAll, setShowAll] = useState(false);
  // One idempotency key per (session, guests) choice: a double click can't book twice.
  const idempotencyKeyFor = useIdempotencyKey();

  const available = useMemo(() => (sessions.data ?? []).filter((s) => s.spotsLeft > 0), [sessions.data]);
  const selected = available.find((s) => s.id === selectedId) ?? null;
  const maxGuests = Math.max(1, Math.min(experience.maxGuests, selected?.spotsLeft ?? experience.maxGuests));
  const guestCount = Math.min(guests, maxGuests);
  const price = experienceTotal(experience.pricePerGuest, guestCount);
  const isOwn = session?.userId === experience.host.id;

  const days = useMemo(() => {
    const visible = showAll ? available : available.slice(0, 8);
    const groups: { day: string; date: Date; sessions: ExperienceSession[] }[] = [];
    for (const s of visible) {
      const date = new Date(s.startsAtUtc);
      const day = toDateOnly(date);
      const last = groups.at(-1);
      if (last?.day === day) last.sessions.push(s);
      else groups.push({ day, date, sessions: [s] });
    }
    return groups;
  }, [available, showAll]);

  const onBook = () => {
    if (!selected) return;
    book.mutate(
      { sessionId: selected.id, guestCount, idempotencyKey: idempotencyKeyFor({ sessionId: selected.id, guestCount }) },
      { onSuccess: (url) => window.location.assign(url) },
    );
  };

  const end = (start: Date) => new Date(start.getTime() + experience.durationMinutes * 60_000);

  return (
    <div className="grid gap-5 rounded-3xl border bg-card p-6 shadow-float" id="book">
      <p className="flex items-baseline gap-1.5">
        <span className="text-sm text-muted-foreground">{t("from")}</span>
        <Money amount={experience.pricePerGuest} currency={experience.currency} tooltip className="text-2xl font-semibold" />
        <span className="text-muted-foreground">{t("perGuest")}</span>
      </p>

      <div className="grid gap-3">
        <h3 className="font-semibold">{t("chooseSession")}</h3>
        {sessions.isPending ? (
          <div className="grid gap-2" role="status" aria-label={t("loadingSessions")}>
            {Array.from({ length: 3 }, (_, i) => (
              <Skeleton key={i} className="h-14 rounded-xl" />
            ))}
          </div>
        ) : sessions.isError ? (
          <p className="text-sm text-destructive">{errorMessage(sessions.error)}</p>
        ) : available.length === 0 ? (
          <p className="flex items-center gap-2 rounded-xl bg-muted/60 p-3 text-sm text-muted-foreground">
            <CalendarX2 className="size-4 shrink-0" aria-hidden="true" /> {t("noSessions")}
          </p>
        ) : (
          <div role="radiogroup" aria-label={t("chooseSession")} className="grid max-h-[26rem] gap-4 overflow-y-auto pr-1">
            {days.map((group) => (
              <div key={group.day} className="grid gap-2">
                <p className="text-sm font-medium">{formatLongDay(group.date, locale)}</p>
                {group.sessions.map((s) => {
                  const start = new Date(s.startsAtUtc);
                  const active = s.id === selectedId;
                  const few = s.spotsLeft <= 3;
                  return (
                    <button
                      key={s.id}
                      type="button"
                      role="radio"
                      aria-checked={active}
                      onClick={() => setSelectedId(s.id)}
                      className={cn(
                        "flex items-center justify-between gap-3 rounded-xl border px-4 py-3 text-left text-sm transition-colors focus-visible:ring-[3px] focus-visible:ring-ring/40 focus-visible:outline-none",
                        active ? "border-foreground bg-muted/70 ring-1 ring-foreground" : "hover:bg-muted/50",
                      )}
                      data-testid="session-option"
                    >
                      <span className="font-medium tabular-nums">
                        {formatTime(start, locale)} – {formatTime(end(start), locale)}
                      </span>
                      <span className={cn("text-xs", few ? "font-medium text-destructive" : "text-muted-foreground")}>
                        {t("spotsLeft", { count: s.spotsLeft })}
                      </span>
                    </button>
                  );
                })}
              </div>
            ))}
            {available.length > 8 && !showAll && (
              <Button variant="ghost" size="sm" onClick={() => setShowAll(true)}>
                {t("showAllDates", { count: available.length })}
              </Button>
            )}
          </div>
        )}
      </div>

      <div className="rounded-xl border px-4 py-3">
        <Stepper
          id="experience-guests"
          label={t("guests")}
          description={t("maxGuests", { count: maxGuests })}
          value={guestCount}
          min={1}
          max={maxGuests}
          onChange={setGuests}
        />
      </div>

      {selected && (
        <dl className="grid gap-2 text-sm">
          <div className="flex justify-between gap-3">
            <dt className="text-muted-foreground">
              {t("priceTimesGuests", {
                price: formatPrice(experience.pricePerGuest, experience.currency, locale),
                count: guestCount,
              })}
            </dt>
            <dd className="tabular-nums">{formatPrice(price.subtotal, experience.currency, locale)}</dd>
          </div>
          <div className="flex justify-between gap-3">
            <dt className="text-muted-foreground">{t("serviceFee", { percent: String(EXPERIENCE_SERVICE_FEE_PERCENT) })}</dt>
            <dd className="tabular-nums">{formatPrice(price.fee, experience.currency, locale)}</dd>
          </div>
          <Separator className="my-1" />
          <div className="flex items-start justify-between gap-3 font-semibold">
            <dt>{t("total")}</dt>
            <dd className="tabular-nums" data-testid="experience-total">
              <ChargedAmount amount={price.total} currency={experience.currency} />
            </dd>
          </div>
          <ChargedInNote currency={experience.currency} />
        </dl>
      )}

      {book.error && (
        <p role="alert" className="flex items-start gap-2 rounded-xl bg-destructive/10 p-3 text-sm text-destructive">
          <AlertCircle className="mt-0.5 size-4 shrink-0" aria-hidden="true" />
          {errorMessage(book.error)}
        </p>
      )}

      {isOwn ? (
        <p className="rounded-xl bg-muted/60 p-3 text-center text-sm text-muted-foreground">{t("ownExperience")}</p>
      ) : !session ? (
        <Button asChild size="lg" className="w-full">
          <Link href={loginUrl(pathname)}>{t("signInToBook")}</Link>
        </Button>
      ) : (
        <Button
          size="lg"
          className="w-full"
          onClick={onBook}
          disabled={!selected || book.isPending || book.isSuccess}
          data-testid="experience-book"
        >
          {(book.isPending || book.isSuccess) && <Loader2 className="animate-spin" aria-hidden="true" />}
          {selected ? t("bookFor", { count: guestCount }) : t("pickFirst")}
        </Button>
      )}
      <p className="flex items-start gap-2 text-xs text-muted-foreground">
        <ShieldCheck className="size-4 shrink-0 text-primary" aria-hidden="true" />
        {t("holdNote")}
      </p>
    </div>
  );
}
