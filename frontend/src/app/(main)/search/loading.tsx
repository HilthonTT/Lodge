import { Container } from "@/components/shared/container";
import { Skeleton } from "@/components/ui/skeleton";
import { ApartmentGridSkeleton } from "@/features/apartments/components/apartment-grid";

export default function Loading() {
  return (
    <Container className="grid gap-8">
      <div className="grid gap-2">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-4 w-32" />
      </div>
      <ApartmentGridSkeleton />
    </Container>
  );
}
