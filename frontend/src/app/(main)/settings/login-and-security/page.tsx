import type { Metadata } from "next";

import { Container } from "@/components/shared/container";
import { PageHeader } from "@/components/shared/page-header";
import { Breadcrumbs } from "@/components/ui/breadcrumb";
import { LoginSecurity } from "@/features/users/components/login-security";

export const metadata: Metadata = { title: "Login & security" };

export default function LoginAndSecurityPage() {
  return (
    <Container className="grid max-w-3xl gap-8">
      <div className="grid gap-3">
        <Breadcrumbs items={[{ label: "Account", href: "/settings" }, { label: "Login & security" }]} />
        <PageHeader title="Login & security" />
      </div>
      <LoginSecurity />
    </Container>
  );
}
