"use client";

import { AlarmClock, CalendarCheck, Check, Loader2, MessageCircle, Star, Wallet } from "lucide-react";
import { useTranslations } from "next-intl";
import { useState } from "react";
import { toast } from "sonner";

import { ErrorState } from "@/components/shared/state";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { Switch } from "@/components/ui/switch";
import { useApiErrorMessage } from "@/i18n/api-errors";
import { NOTIFICATION_CATEGORIES, type NotificationCategory, type NotificationPreferences } from "@/types/api";

import { useNotificationPreferences, useSaveNotificationPreferences } from "../hooks/use-notifications";

const ICONS = {
  messages: MessageCircle,
  reservations: CalendarCheck,
  reviews: Star,
  payouts: Wallet,
  reminders: AlarmClock,
} as const;

const CHANNELS = ["email", "inApp"] as const;

export function NotificationPreferencesForm() {
  const t = useTranslations("notifications.settings");
  const tCommon = useTranslations("common");
  const errorMessage = useApiErrorMessage();
  const query = useNotificationPreferences();

  if (query.isPending) {
    return (
      <div className="grid gap-3" role="status" aria-label={t("loading")}>
        {Array.from({ length: 5 }, (_, i) => (
          <Skeleton key={i} className="h-16 rounded-2xl" />
        ))}
      </div>
    );
  }
  if (query.isError) {
    return (
      <ErrorState
        title={t("loadError")}
        description={errorMessage(query.error)}
        action={
          <Button variant="outline" onClick={() => query.refetch()}>
            {tCommon("tryAgain")}
          </Button>
        }
      />
    );
  }
  return <PreferencesForm key={JSON.stringify(query.data)} initial={query.data} />;
}

function PreferencesForm({ initial }: { initial: NotificationPreferences }) {
  const t = useTranslations("notifications.settings");
  const errorMessage = useApiErrorMessage();
  const save = useSaveNotificationPreferences();
  const [draft, setDraft] = useState<NotificationPreferences>(initial);
  const dirty = JSON.stringify(draft) !== JSON.stringify(initial);

  const toggle = (channel: (typeof CHANNELS)[number], category: NotificationCategory, value: boolean) =>
    setDraft((d) => ({ ...d, [channel]: { ...d[channel], [category]: value } }));

  const onSave = () =>
    save.mutate(draft, {
      onSuccess: () => toast.success(t("saved")),
      onError: (error) => toast.error(t("saveError"), { description: errorMessage(error) }),
    });

  return (
    <div className="grid gap-6">
      <div className="overflow-hidden rounded-2xl border bg-card shadow-card">
        <div className="grid grid-cols-[minmax(0,1fr)_4.5rem_4.5rem] items-center gap-2 border-b bg-muted/40 px-4 py-3 text-xs font-semibold tracking-wide text-muted-foreground uppercase sm:grid-cols-[minmax(0,1fr)_6rem_6rem] sm:px-6">
          <span>{t("category")}</span>
          <span className="text-center">{t("email")}</span>
          <span className="text-center">{t("inApp")}</span>
        </div>
        <ul className="divide-y">
          {NOTIFICATION_CATEGORIES.map((category) => {
            const Icon = ICONS[category];
            return (
              <li
                key={category}
                className="grid grid-cols-[minmax(0,1fr)_4.5rem_4.5rem] items-center gap-2 px-4 py-4 sm:grid-cols-[minmax(0,1fr)_6rem_6rem] sm:px-6"
              >
                <div className="flex min-w-0 items-start gap-3">
                  <span className="hidden size-9 shrink-0 place-items-center rounded-xl bg-accent text-accent-foreground sm:grid">
                    <Icon className="size-4.5" aria-hidden="true" />
                  </span>
                  <span className="grid min-w-0 gap-0.5">
                    <span className="font-medium" id={`pref-${category}`}>
                      {t(`categories.${category}.title`)}
                    </span>
                    <span className="text-sm text-muted-foreground">{t(`categories.${category}.description`)}</span>
                  </span>
                </div>
                {CHANNELS.map((channel) => (
                  <div key={channel} className="flex justify-center">
                    <Switch
                      checked={draft[channel][category]}
                      onCheckedChange={(value) => toggle(channel, category, value)}
                      aria-label={t("toggleLabel", { channel, category: t(`categories.${category}.title`) })}
                      data-testid={`pref-${channel}-${category}`}
                    />
                  </div>
                ))}
              </li>
            );
          })}
        </ul>
      </div>
      <p className="text-sm text-muted-foreground">{t("securityNote")}</p>
      <div className="flex justify-end gap-2">
        <Button variant="ghost" onClick={() => setDraft(initial)} disabled={!dirty || save.isPending}>
          {t("reset")}
        </Button>
        <Button onClick={onSave} disabled={!dirty || save.isPending} data-testid="prefs-save">
          {save.isPending ? <Loader2 className="animate-spin" aria-hidden="true" /> : <Check aria-hidden="true" />}
          {t("save")}
        </Button>
      </div>
    </div>
  );
}
