import { Container } from "@/components/shared/container";
import { Skeleton } from "@/components/ui/skeleton";

export default function Loading() {
  return (
    <Container className="grid gap-8" aria-busy="true">
      <div className="grid gap-3">
        <Skeleton className="h-4 w-48" />
        <Skeleton className="h-9 w-2/3" />
        <Skeleton className="h-4 w-40" />
      </div>
      <Skeleton className="aspect-[16/10] max-h-[30rem] w-full rounded-3xl sm:aspect-[21/9]" />
      <div className="grid gap-12 lg:grid-cols-[minmax(0,1fr)_24rem] lg:gap-16">
        <div className="grid content-start gap-4">
          <Skeleton className="h-6 w-1/2" />
          <Skeleton className="h-4 w-full" />
          <Skeleton className="h-4 w-full" />
          <Skeleton className="h-4 w-3/4" />
        </div>
        <Skeleton className="h-96 rounded-2xl" />
      </div>
    </Container>
  );
}
