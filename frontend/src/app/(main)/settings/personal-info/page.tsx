import type { Metadata } from "next";

import { Container } from "@/components/shared/container";
import { PageHeader } from "@/components/shared/page-header";
import { Breadcrumbs } from "@/components/ui/breadcrumb";
import { PersonalInfo } from "@/features/users/components/personal-info";

export const metadata: Metadata = { title: "Personal info" };

export default function PersonalInfoPage() {
  return (
    <Container className="grid max-w-3xl gap-8">
      <div className="grid gap-3">
        <Breadcrumbs items={[{ label: "Account", href: "/settings" }, { label: "Personal info" }]} />
        <PageHeader title="Personal info" />
      </div>
      <PersonalInfo />
    </Container>
  );
}
