import Image from "next/image";
import { getTranslations } from "next-intl/server";

import { LanguageSwitcher } from "@/components/layout/language-switcher";
import { Logo } from "@/components/shared/logo";
import { ThemeToggle } from "@/components/shared/theme-toggle";

// Signed-in users are redirected away from these pages by `src/proxy.ts`.
export default async function AuthLayout({ children }: { children: React.ReactNode }) {
  const t = await getTranslations("auth");

  return (
    <div className="grid min-h-dvh lg:grid-cols-2">
      <div className="flex flex-col">
        <header className="flex items-center justify-between p-6">
          <Logo />
          <div className="flex items-center gap-1">
            <LanguageSwitcher />
            <ThemeToggle />
          </div>
        </header>
        <main id="main" className="flex flex-1 items-center justify-center px-6 pb-16">
          <div className="w-full max-w-md">{children}</div>
        </main>
      </div>
      <div className="relative hidden overflow-hidden lg:block">
        <Image
          src="/images/auth-side.jpg"
          alt=""
          fill
          priority
          sizes="50vw"
          className="object-cover"
        />
        <div className="absolute inset-0 bg-gradient-to-t from-black/60 via-black/10 to-transparent" />
        <blockquote className="absolute inset-x-10 bottom-10 grid gap-2 text-white">
          <p className="text-2xl font-semibold text-balance">{t("quote")}</p>
          <footer className="text-sm text-white/80">{t("quoteAuthor")}</footer>
        </blockquote>
      </div>
    </div>
  );
}
