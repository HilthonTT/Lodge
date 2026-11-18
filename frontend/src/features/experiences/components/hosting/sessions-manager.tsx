"use client";

import { AlertTriangle, CalendarPlus, CalendarX2, Loader2, Repeat, Trash2, Users } from "lucide-react";
import Link from "next/link";
import { useLocale, useTranslations } from "next-intl";
import { useMemo, useState } from "react";
import { toast } from "sonner";

import { EmptyState } from "@/components/shared/state";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "@/components/ui/alert-dialog";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { HostingError } from "@/features/hosting/components/hosting-gate";
import { useApiErrorMessage } from "@/i18n/api-errors";
import { addDays, formatLongDay, formatTime, toDateOnly } from "@/lib/dates";
import { cn } from "@/lib/utils";
import { type CreateSessionsRequest, type Experience, type HostExperienceSession, WEEKDAYS, type Weekday } from "@/types/api";

import { useAddSessions, useCancelSession, useHostExperience, useHostSessions } from "../../hooks/use-experiences";

const RANGE_DAYS = 90;

function browserTimeZone() {
  try {
    return Intl.DateTimeFormat().resolvedOptions().timeZone || "UTC";
  } catch {
    return "UTC";
  }
}

function timeZones(current: string): string[] {
  try {
    const all = Intl.supportedValuesOf("timeZone");
    return all.includes(current) ? all : [current, ...all];
  } catch {
    return [current, "UTC"];
  }
}

export function SessionsManager({ experienceId }: { experienceId: string }) {
  const t = useTranslations("experiences.hosting.sessions");
  const experience = useHostExperience(experienceId);

  if (experience.isPending) {
    return <Skeleton className="h-96 rounded-3xl" />;
  }
  if (experience.isError) {
    return <HostingError error={experience.error} title={t("loadError")} onRetry={() => experience.refetch()} />;
  }
  return (
    <div className="grid gap-8">
      <header className="grid gap-1">
        <p className="text-sm text-muted-foreground">
          <Link href="/hosting/experiences" className="underline-offset-4 hover:underline">
            {t("breadcrumb")}
          </Link>
        </p>
        <h1 className="text-2xl font-semibold sm:text-3xl">{t("title", { title: experience.data.title })}</h1>
        <p className="text-muted-foreground">{t("description")}</p>
      </header>
      <div className="grid gap-8 lg:grid-cols-[minmax(0,1fr)_24rem] lg:items-start">
        <SessionList experience={experience.data} />
        <AddSessionsForm experience={experience.data} />
      </div>
    </div>
  );
}

function SessionList({ experience }: { experience: Experience }) {
  const t = useTranslations("experiences.hosting.sessions");
  const locale = useLocale();
  const [range] = useState(() => {
    const from = new Date();
    return { from: toDateOnly(from), to: toDateOnly(addDays(from, RANGE_DAYS)) };
  });
  const sessions = useHostSessions(experience.id, range);

  const groups = useMemo(() => {
    const result: { day: string; date: Date; sessions: HostExperienceSession[] }[] = [];
    for (const s of [...(sessions.data ?? [])].sort((a, b) => a.startsAtUtc.localeCompare(b.startsAtUtc))) {
      const date = new Date(s.startsAtUtc);
      const day = toDateOnly(date);
      const last = result.at(-1);
      if (last?.day === day) last.sessions.push(s);
      else result.push({ day, date, sessions: [s] });
    }
    return result;
  }, [sessions.data]);

  if (sessions.isPending) {
    return (
      <div className="grid gap-3" role="status" aria-label={t("loading")}>
        {Array.from({ length: 4 }, (_, i) => (
          <Skeleton key={i} className="h-16 rounded-2xl" />
        ))}
      </div>
    );
  }
  if (sessions.isError) {
    return <HostingError error={sessions.error} title={t("loadError")} onRetry={() => sessions.refetch()} />;
  }
  if (groups.length === 0) {
    return <EmptyState icon={CalendarX2} title={t("emptyTitle")} description={t("emptyDescription")} />;
  }

  return (
    <section aria-label={t("listLabel")} className="grid gap-5" data-testid="host-sessions">
      <p className="text-sm text-muted-foreground">{t("upcomingCount", { count: sessions.data.filter((s) => !s.isCancelled).length, days: RANGE_DAYS })}</p>
      {groups.map((group) => (
        <div key={group.day} className="grid gap-2">
          <h2 className="text-sm font-semibold">{formatLongDay(group.date, locale)}</h2>
          <ul className="grid gap-2">
            {group.sessions.map((s) => (
              <li key={s.id}>
                <SessionRow experience={experience} session={s} />
              </li>
            ))}
          </ul>
        </div>
      ))}
    </section>
  );
}

function SessionRow({ experience, session: s }: { experience: Experience; session: HostExperienceSession }) {
  const t = useTranslations("experiences.hosting.sessions");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const errorMessage = useApiErrorMessage();
  const cancel = useCancelSession(experience.id);
  const start = new Date(s.startsAtUtc);
  const end = new Date(start.getTime() + experience.durationMinutes * 60_000);
  const filled = s.capacity > 0 ? Math.min(1, s.bookedGuests / s.capacity) : 0;

  return (
    <div
      className={cn("flex flex-wrap items-center gap-4 rounded-2xl border bg-card px-4 py-3 shadow-card", s.isCancelled && "opacity-60")}
      data-testid="host-session"
    >
      <div className="grid min-w-32 gap-0.5">
        <span className="font-medium tabular-nums">
          {formatTime(start, locale)} – {formatTime(end, locale)}
        </span>
        {s.isCancelled && (
          <Badge variant="muted" className="w-fit">
            {t("cancelled")}
          </Badge>
        )}
      </div>
      <div className="grid min-w-40 flex-1 gap-1.5">
        <span className="flex items-center gap-1.5 text-sm">
          <Users className="size-4 text-muted-foreground" aria-hidden="true" />
          {t("booked", { booked: s.bookedGuests, capacity: s.capacity })}
          {s.bookingCount > 0 && <span className="text-muted-foreground">· {t("bookings", { count: s.bookingCount })}</span>}
        </span>
        <span className="h-1.5 overflow-hidden rounded-full bg-muted" aria-hidden="true">
          <span className="block h-full rounded-full bg-primary" style={{ width: `${filled * 100}%` }} />
        </span>
      </div>
      {!s.isCancelled && (
        <AlertDialog>
          <AlertDialogTrigger asChild>
            <Button variant="ghost" size="sm" className="text-destructive hover:text-destructive" disabled={cancel.isPending}>
              {cancel.isPending ? <Loader2 className="animate-spin" aria-hidden="true" /> : <Trash2 aria-hidden="true" />}
              {t("cancel")}
            </Button>
          </AlertDialogTrigger>
          <AlertDialogContent>
            <AlertDialogHeader>
              <AlertDialogTitle>{t("cancelTitle")}</AlertDialogTitle>
              <AlertDialogDescription asChild>
                <div className="grid gap-3">
                  <p>{t("cancelDescription", { date: `${formatLongDay(start, locale)} · ${formatTime(start, locale)}` })}</p>
                  {s.bookingCount > 0 && (
                    <p className="flex items-start gap-2 rounded-xl border border-warning/40 bg-warning/10 p-3 font-medium text-foreground" role="alert">
                      <AlertTriangle className="mt-0.5 size-4 shrink-0 text-warning" aria-hidden="true" />
                      {t("cancelWarning", { count: s.bookingCount, guests: s.bookedGuests })}
                    </p>
                  )}
                </div>
              </AlertDialogDescription>
            </AlertDialogHeader>
            <AlertDialogFooter>
              <AlertDialogCancel>{tCommon("cancel")}</AlertDialogCancel>
              <AlertDialogAction
                variant="destructive"
                onClick={() =>
                  cancel.mutate(s.id, {
                    onSuccess: () => toast.success(t("cancelledToast")),
                    onError: (error) => toast.error(t("cancelError"), { description: errorMessage(error) }),
                  })
                }
              >
                {t("cancelConfirm")}
              </AlertDialogAction>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialog>
      )}
    </div>
  );
}

function AddSessionsForm({ experience }: { experience: Experience }) {
  const t = useTranslations("experiences.hosting.sessions.add");
  const tDays = useTranslations("experiences.weekdays");
  const errorMessage = useApiErrorMessage();
  const add = useAddSessions(experience.id);
  const [mode, setMode] = useState<"single" | "recurring">("recurring");
  const todayStr = toDateOnly(new Date());
  const [date, setDate] = useState(toDateOnly(addDays(new Date(), 1)));
  const [time, setTime] = useState("10:00");
  const [capacity, setCapacity] = useState(String(experience.maxGuests));
  const [days, setDays] = useState<Weekday[]>(["Saturday", "Sunday"]);
  const [timeZone, setTimeZone] = useState(browserTimeZone);
  const [from, setFrom] = useState(toDateOnly(addDays(new Date(), 1)));
  const [until, setUntil] = useState(toDateOnly(addDays(new Date(), 30)));
  const zones = useMemo(() => timeZones(timeZone), [timeZone]);
  const capacityNumber = Number(capacity);
  const capacityValid = Number.isInteger(capacityNumber) && capacityNumber >= 1 && capacityNumber <= experience.maxGuests;

  const submit = (event: React.FormEvent) => {
    event.preventDefault();
    let body: CreateSessionsRequest;
    if (mode === "single") {
      const start = new Date(`${date}T${time}`);
      if (Number.isNaN(start.getTime())) return;
      body = { startsAtUtc: start.toISOString(), capacity: capacityNumber };
    } else {
      body = { capacity: capacityNumber, recurring: { daysOfWeek: days, startTime: time, timeZone, from, until } };
    }
    add.mutate(body, {
      onSuccess: (ids) => toast.success(t("added", { count: ids.length })),
      onError: (error) => toast.error(t("error"), { description: errorMessage(error) }),
    });
  };

  const invalid = !capacityValid || !time || (mode === "single" ? !date : days.length === 0 || !from || !until || until < from);

  return (
    <form onSubmit={submit} className="grid gap-5 rounded-3xl border bg-card p-6 shadow-card lg:sticky lg:top-28" data-testid="add-sessions">
      <div className="grid gap-1">
        <h2 className="text-lg font-semibold">{t("title")}</h2>
        <p className="text-sm text-muted-foreground">{t("description")}</p>
      </div>
      <Tabs value={mode} onValueChange={(v) => setMode(v as "single" | "recurring")}>
        <TabsList className="w-full">
          <TabsTrigger value="recurring" className="flex-1">
            <Repeat className="size-4" aria-hidden="true" /> {t("recurring")}
          </TabsTrigger>
          <TabsTrigger value="single" className="flex-1">
            <CalendarPlus className="size-4" aria-hidden="true" /> {t("single")}
          </TabsTrigger>
        </TabsList>
        <TabsContent value="single" className="mt-4 grid gap-4">
          <label className="grid gap-1.5 text-sm">
            <span className="font-medium">{t("date")}</span>
            <Input type="date" min={todayStr} value={date} onChange={(e) => setDate(e.target.value)} required />
          </label>
          <p className="text-xs text-muted-foreground">{t("localTime", { zone: browserTimeZone() })}</p>
        </TabsContent>
        <TabsContent value="recurring" className="mt-4 grid gap-4">
          <fieldset className="grid gap-2">
            <legend className="mb-2 text-sm font-medium">{t("days")}</legend>
            <div className="flex flex-wrap gap-1.5">
              {WEEKDAYS.map((day) => {
                const selected = days.includes(day);
                return (
                  <button
                    key={day}
                    type="button"
                    aria-pressed={selected}
                    aria-label={tDays(`long.${day}`)}
                    onClick={() => setDays(selected ? days.filter((d) => d !== day) : [...days, day])}
                    className={cn(
                      "grid h-9 min-w-11 place-items-center rounded-full border px-2 text-xs font-semibold transition-colors focus-visible:ring-[3px] focus-visible:ring-ring/40 focus-visible:outline-none",
                      selected ? "border-foreground bg-foreground text-background" : "bg-card hover:bg-muted",
                    )}
                    data-testid={`day-${day}`}
                  >
                    {tDays(`short.${day}`)}
                  </button>
                );
              })}
            </div>
          </fieldset>
          <div className="grid grid-cols-2 gap-3">
            <label className="grid gap-1.5 text-sm">
              <span className="font-medium">{t("from")}</span>
              <Input type="date" min={todayStr} value={from} onChange={(e) => setFrom(e.target.value)} required />
            </label>
            <label className="grid gap-1.5 text-sm">
              <span className="font-medium">{t("until")}</span>
              <Input type="date" min={from || todayStr} value={until} onChange={(e) => setUntil(e.target.value)} required />
            </label>
          </div>
          <label className="grid gap-1.5 text-sm">
            <span className="font-medium">{t("timeZone")}</span>
            <select
              value={timeZone}
              onChange={(e) => setTimeZone(e.target.value)}
              className="h-11 w-full rounded-lg border border-input bg-background px-3 text-sm shadow-xs outline-none focus-visible:ring-[3px] focus-visible:ring-ring/25 dark:bg-input/20"
            >
              {zones.map((zone) => (
                <option key={zone} value={zone}>
                  {zone.replaceAll("_", " ")}
                </option>
              ))}
            </select>
          </label>
        </TabsContent>
      </Tabs>
      <div className="grid grid-cols-2 gap-3">
        <label className="grid gap-1.5 text-sm">
          <span className="font-medium">{t("time")}</span>
          <Input type="time" value={time} onChange={(e) => setTime(e.target.value)} required />
        </label>
        <label className="grid gap-1.5 text-sm">
          <span className="font-medium">{t("capacity")}</span>
          <Input
            type="number"
            inputMode="numeric"
            min={1}
            max={experience.maxGuests}
            value={capacity}
            onChange={(e) => setCapacity(e.target.value)}
            aria-invalid={!capacityValid || undefined}
          />
        </label>
      </div>
      <p className="text-xs text-muted-foreground">{t("capacityHint", { max: experience.maxGuests })}</p>
      <Button type="submit" disabled={invalid || add.isPending} data-testid="add-sessions-submit">
        {add.isPending ? <Loader2 className="animate-spin" aria-hidden="true" /> : <CalendarPlus aria-hidden="true" />}
        {mode === "single" ? t("submitSingle") : t("submitRecurring")}
      </Button>
    </form>
  );
}
