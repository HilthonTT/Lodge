import { Container } from "@/components/shared/container";
import { Skeleton } from "@/components/ui/skeleton";
import { ApartmentGridSkeleton } from "@/features/apartments/components/apartment-grid";

export default function Loading() {
  return (
    <Container className="grid gap-10">
      <Skeleton className="h-56 rounded-3xl sm:h-64" />
      <div className="grid gap-6">
        <Skeleton className="h-7 w-40" />
        <ApartmentGridSkeleton />
      </div>
    </Container>
  );
}
