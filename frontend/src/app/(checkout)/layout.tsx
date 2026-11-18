import { LanguageSwitcher } from "@/components/layout/language-switcher";
import { Logo } from "@/components/shared/logo";
import { ThemeToggle } from "@/components/shared/theme-toggle";

// A hosted-payment-page look: no site navigation, just the brand and settings.
export default function CheckoutLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex min-h-dvh flex-col bg-muted/40">
      <header className="flex items-center justify-between border-b bg-background px-4 py-3 sm:px-6">
        <Logo compact />
        <div className="flex items-center gap-1">
          <LanguageSwitcher />
          <ThemeToggle />
        </div>
      </header>
      <main id="main" className="flex-1 px-4 py-8 sm:px-6 sm:py-12">
        {children}
      </main>
    </div>
  );
}
