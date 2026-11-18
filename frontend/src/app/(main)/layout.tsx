import { Footer } from "@/components/layout/footer";
import { Navbar } from "@/components/layout/navbar";
import { EmailVerificationBanner } from "@/features/auth/components/email-verification";

export default function MainLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex min-h-dvh flex-col">
      <Navbar />
      <EmailVerificationBanner />
      <main id="main" className="flex-1 pt-8 pb-8 sm:pt-10">
        {children}
      </main>
      <Footer />
    </div>
  );
}
