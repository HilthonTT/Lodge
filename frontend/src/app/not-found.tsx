import { Compass } from "lucide-react";
import type { Metadata } from "next";
import Link from "next/link";
import { getTranslations } from "next-intl/server";

import { Logo } from "@/components/shared/logo";
import { Button } from "@/components/ui/button";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("errors.notFound");
  return { title: t("metaTitle") };
}

export default async function NotFound() {
  const t = await getTranslations("errors.notFound");

  return (
    <div className="flex min-h-dvh flex-col">
      <header className="p-6">
        <Logo />
      </header>
      <main id="main" className="flex flex-1 items-center justify-center px-6 pb-24">
        <div className="grid max-w-md justify-items-center gap-6 text-center">
          <div className="relative">
            <span className="block text-[7rem] leading-none font-semibold tracking-tighter text-primary/15 sm:text-[9rem]">
              404
            </span>
            <span className="absolute inset-0 grid place-items-center">
              <span className="grid size-16 place-items-center rounded-2xl bg-card shadow-float">
                <Compass className="size-8 text-primary" aria-hidden="true" />
              </span>
            </span>
          </div>
          <div className="grid gap-2">
            <h1 className="text-2xl font-semibold sm:text-3xl">{t("title")}</h1>
            <p className="text-muted-foreground">{t("description")}</p>
          </div>
          <div className="flex flex-wrap justify-center gap-2">
            <Button asChild size="lg">
              <Link href="/">{t("explore")}</Link>
            </Button>
            <Button asChild size="lg" variant="outline">
              <Link href="/search">{t("search")}</Link>
            </Button>
          </div>
        </div>
      </main>
    </div>
  );
}
