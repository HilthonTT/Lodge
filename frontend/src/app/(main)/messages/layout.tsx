import { Container } from "@/components/shared/container";
import { InboxShell } from "@/features/messaging/components/inbox-shell";

// Auth is enforced by src/proxy.ts; conversations are fetched client-side with the user's token.
export default function MessagesLayout({ children }: { children: React.ReactNode }) {
  return (
    <Container className="-mt-4 sm:-mt-6">
      <InboxShell>{children}</InboxShell>
    </Container>
  );
}
