import { Container } from "@/components/shared/container";
import { HostingGate } from "@/features/hosting/components/hosting-gate";
import { HostingNav } from "@/features/hosting/components/hosting-nav";

// Auth is enforced by src/proxy.ts; data is fetched client-side with the host's token.
export default function HostingLayout({ children }: { children: React.ReactNode }) {
  return (
    <Container className="grid gap-8">
      <HostingNav />
      <HostingGate>{children}</HostingGate>
    </Container>
  );
}
