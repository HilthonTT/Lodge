import type { Metadata } from "next";
import { Suspense } from "react";

import { Container } from "@/components/shared/container";
import { PageHeader } from "@/components/shared/page-header";
import { BookingsSkeleton, BookingsView } from "@/features/bookings/components/bookings-view";

export const metadata: Metadata = { title: "My bookings" };

// Auth is enforced by src/proxy.ts; data is fetched client-side with the user's token.
export default function BookingsPage() {
  return (
    <Container className="grid max-w-5xl gap-8">
      <PageHeader
        title="My bookings"
        description="Pay for reserved stays, manage upcoming trips and review places you've stayed."
      />
      <Suspense fallback={<BookingsSkeleton />}>
        <BookingsView />
      </Suspense>
    </Container>
  );
}
