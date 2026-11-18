import { SearchX } from "lucide-react";
import Link from "next/link";
import { getTranslations } from "next-intl/server";

import { Container } from "@/components/shared/container";
import { EmptyState } from "@/components/shared/state";
import { Button } from "@/components/ui/button";

export default async function ApartmentNotFound() {
  const t = await getTranslations("apartment.notFound");

  return (
    <Container>
      <EmptyState
        icon={SearchX}
        title={t("title")}
        description={t("description")}
        action={
          <Button asChild>
            <Link href="/">{t("browse")}</Link>
          </Button>
        }
      />
    </Container>
  );
}
