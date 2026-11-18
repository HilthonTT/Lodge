import { redirect } from "next/navigation";

import { requireAdminSession } from "@/lib/auth/session.server";

export default async function AdminPage() {
  await requireAdminSession("/admin/bookings");
  redirect("/admin/bookings");
}
