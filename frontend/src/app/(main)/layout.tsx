import { Suspense } from "react";

import { Footer } from "@/components/layout/footer";
import { MobileDiscoverToggle } from "@/components/layout/discover-toggle";
import { Navbar } from "@/components/layout/navbar";
import { CategoryBar } from "@/features/apartments/components/category-bar";
import { EmailVerificationBanner } from "@/features/auth/components/email-verification";

export default function MainLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex min-h-dvh flex-col">
      <Navbar />
      <MobileDiscoverToggle />
      <EmailVerificationBanner />
      <Suspense>
        <CategoryBar />
      </Suspense>
      <main id="main" className="flex-1 pt-8 pb-8 sm:pt-10">
        {children}
      </main>
      <Footer />
    </div>
  );
}
