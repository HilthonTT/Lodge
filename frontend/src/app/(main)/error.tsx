"use client";

import { useEffect } from "react";

import { Container } from "@/components/shared/container";
import { ErrorState } from "@/components/shared/state";
import { Button } from "@/components/ui/button";

export default function MainError({
  error,
  retry,
}: {
  error: Error & { digest?: string };
  /** Re-fetches and re-renders the segment (stable since Next 16.3). */
  retry: () => void;
}) {
  useEffect(() => {
    console.error(error);
  }, [error]);

  return (
    <Container className="py-10">
      <ErrorState
        title="Something went wrong"
        description="An unexpected error occurred while loading this page. Please try again."
        action={<Button onClick={() => retry()}>Try again</Button>}
      />
    </Container>
  );
}
