import { Container } from "@/components/shared/container";
import { Skeleton } from "@/components/ui/skeleton";
import { ApartmentCardSkeleton } from "@/features/apartments/components/apartment-card";

export default function Loading() {
  return (
    <Container className="grid max-w-[110rem] gap-8">
      <div className="grid gap-2">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-4 w-32" />
      </div>
      <div className="grid gap-8 lg:grid-cols-[minmax(0,1fr)_minmax(0,0.9fr)] xl:grid-cols-[minmax(0,1.3fr)_minmax(0,1fr)]">
        <div className="grid grid-cols-1 gap-x-6 gap-y-10 sm:grid-cols-2">
          {Array.from({ length: 6 }, (_, i) => (
            <ApartmentCardSkeleton key={i} />
          ))}
        </div>
        <Skeleton className="hidden h-[calc(100dvh-8rem)] rounded-2xl lg:block" />
      </div>
    </Container>
  );
}
