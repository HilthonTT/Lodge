import type { Metadata } from "next";
import { getTranslations } from "next-intl/server";

import { Container } from "@/components/shared/container";
import { PageHeader } from "@/components/shared/page-header";
import { Badge } from "@/components/ui/badge";
import { parseAdminBookingsSearch } from "@/features/bookings/admin-search-params";
import { AdminBookingsView } from "@/features/bookings/components/admin-bookings-view";
import { requireAdminSession } from "@/lib/auth/session.server";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("admin");
  return {
    title: t("title"),
    description: t("metaDescription"),
    robots: { index: false, follow: false },
  };
}

// The proxy redirects optimistically; this re-checks on the server. The API
// authorizes every admin request on its own, so data is fetched client-side
// with the admin's token.
export default async function AdminBookingsPage({ searchParams }: PageProps<"/admin/bookings">) {
  const [, search, t] = await Promise.all([
    requireAdminSession("/admin/bookings"),
    searchParams.then(parseAdminBookingsSearch),
    getTranslations("admin"),
  ]);

  return (
    <Container className="grid gap-8">
      <PageHeader
        eyebrow={
          <Badge variant="secondary" className="uppercase tracking-wide">
            {t("badge")}
          </Badge>
        }
        title={t("title")}
        description={t("description")}
      />
      <AdminBookingsView search={search} />
    </Container>
  );
}
