import { Container } from "@/components/shared/container";
import { Skeleton } from "@/components/ui/skeleton";
import { AdminBookingsSkeleton } from "@/features/bookings/components/admin-bookings-view";

export default function Loading() {
  return (
    <Container className="grid gap-8">
      <div className="grid gap-2">
        <Skeleton className="h-5 w-16 rounded-full" />
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-4 w-96 max-w-full" />
      </div>
      <AdminBookingsSkeleton />
    </Container>
  );
}
