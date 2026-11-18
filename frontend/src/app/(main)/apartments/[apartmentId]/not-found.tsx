import { SearchX } from "lucide-react";
import Link from "next/link";

import { Container } from "@/components/shared/container";
import { EmptyState } from "@/components/shared/state";
import { Button } from "@/components/ui/button";

export default function ApartmentNotFound() {
  return (
    <Container>
      <EmptyState
        icon={SearchX}
        title="This stay isn't available"
        description="It may have been removed, or the link is incorrect."
        action={
          <Button asChild>
            <Link href="/">Browse all stays</Link>
          </Button>
        }
      />
    </Container>
  );
}
