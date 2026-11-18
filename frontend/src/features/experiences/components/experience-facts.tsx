"use client";

import { Clock, Languages, UsersRound } from "lucide-react";
import { useTranslations } from "next-intl";

import type { Experience, ExperienceCategory } from "@/types/api";

import { CATEGORY_THEME, ExperienceImage, useFormatDuration, useLanguageName } from "../experience-options";

/** Duration, group size and languages. */
export function ExperienceFacts({ experience: e }: { experience: Experience }) {
  const t = useTranslations("experiences.detail");
  const duration = useFormatDuration();
  const languageName = useLanguageName();
  const facts = [
    { icon: Clock, title: t("duration"), value: duration(e.durationMinutes) },
    { icon: UsersRound, title: t("groupSize"), value: t("upToGuests", { count: e.maxGuests }) },
    { icon: Languages, title: t("languages"), value: e.languages.map(languageName).join(", ") || "—" },
  ];
  return (
    <ul className="grid gap-4 sm:grid-cols-3">
      {facts.map((fact) => (
        <li key={fact.title} className="flex items-start gap-3 rounded-2xl border p-4">
          <fact.icon className="mt-0.5 size-5 shrink-0 text-primary" aria-hidden="true" />
          <span className="grid gap-0.5">
            <span className="text-sm text-muted-foreground">{fact.title}</span>
            <span className="font-medium">{fact.value}</span>
          </span>
        </li>
      ))}
    </ul>
  );
}

/** Big category-themed banner when an experience has no photos. */
export function ExperienceHero({ category }: { category: ExperienceCategory }) {
  const t = useTranslations("experiences");
  const Icon = CATEGORY_THEME[category]?.icon;
  return (
    <div className="relative h-64 overflow-hidden rounded-3xl sm:h-96">
      <ExperienceImage src={null} category={category} sizes="100vw" iconClassName="size-24" />
      <span className="absolute bottom-4 left-4 inline-flex items-center gap-1.5 rounded-full bg-background/95 px-3 py-1.5 text-sm font-semibold shadow-card">
        {Icon && <Icon className="size-4 text-primary" aria-hidden="true" />}
        {t(`categories.${category}`)}
      </span>
    </div>
  );
}
