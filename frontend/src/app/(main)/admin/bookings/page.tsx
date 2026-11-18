import type { Metadata } from "next";

import { Container } from "@/components/shared/container";
import { PageHeader } from "@/components/shared/page-header";
import { Badge } from "@/components/ui/badge";
import { parseAdminBookingsSearch } from "@/features/bookings/admin-search-params";
import { AdminBookingsView } from "@/features/bookings/components/admin-bookings-view";
import { requireAdminSession } from "@/lib/auth/session.server";

export const metadata: Metadata = {
  title: "Manage bookings",
  description: "Review, reject and complete bookings across every guest.",
  robots: { index: false, follow: false },
};

// The proxy redirects optimistically; this re-checks on the server. The API
// authorizes every admin request on its own, so data is fetched client-side
// with the admin's token.
export default async function AdminBookingsPage({ searchParams }: PageProps<"/admin/bookings">) {
  const [, search] = await Promise.all([
    requireAdminSession("/admin/bookings"),
    searchParams.then(parseAdminBookingsSearch),
  ]);

  return (
    <Container className="grid gap-8">
      <PageHeader
        eyebrow={
          <Badge variant="secondary" className="uppercase tracking-wide">
            Admin
          </Badge>
        }
        title="Manage bookings"
        description="Every booking across all guests, newest first. Reject unpaid reservations and complete finished stays."
      />
      <AdminBookingsView search={search} />
    </Container>
  );
}
