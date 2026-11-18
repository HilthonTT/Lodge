import { CalendarCheck, ChevronRight, type LucideIcon, ShieldCheck, UserRound } from "lucide-react";
import type { Metadata } from "next";
import Link from "next/link";
import { getTranslations } from "next-intl/server";

import { Container } from "@/components/shared/container";
import { PageHeader } from "@/components/shared/page-header";
import { getServerSession } from "@/lib/auth/session.server";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("settings");
  return { title: t("title") };
}

const SECTIONS = [
  { key: "personalInfo", href: "/settings/personal-info", icon: UserRound },
  { key: "loginSecurity", href: "/settings/login-and-security", icon: ShieldCheck },
  { key: "bookings", href: "/bookings", icon: CalendarCheck },
] as const satisfies readonly { key: string; href: string; icon: LucideIcon }[];

export default async function SettingsPage() {
  const [session, t] = await Promise.all([getServerSession(), getTranslations("settings.sections")]);
  const tSettings = await getTranslations("settings");

  return (
    <Container className="grid max-w-5xl gap-8">
      <PageHeader
        title={tSettings("title")}
        description={
          session ? (
            <>
              <span className="font-medium text-foreground">{session.name}</span> · {session.email}
            </>
          ) : undefined
        }
      />
      <ul className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {SECTIONS.map(({ key, href, icon: Icon }) => (
          <li key={href}>
            <Link
              href={href}
              className="group flex h-full flex-col gap-6 rounded-2xl border bg-card p-6 shadow-card transition hover:-translate-y-0.5 hover:shadow-float focus-visible:ring-[3px] focus-visible:ring-ring/40 focus-visible:outline-none"
            >
              <span className="grid size-11 place-items-center rounded-xl bg-accent text-accent-foreground">
                <Icon className="size-5" aria-hidden="true" />
              </span>
              <span className="grid gap-1">
                <span className="flex items-center justify-between font-semibold">
                  {t(key)}
                  <ChevronRight
                    className="size-4 text-muted-foreground transition-transform group-hover:translate-x-0.5"
                    aria-hidden="true"
                  />
                </span>
                <span className="text-sm text-muted-foreground">{t(`${key}Description`)}</span>
              </span>
            </Link>
          </li>
        ))}
      </ul>
    </Container>
  );
}
