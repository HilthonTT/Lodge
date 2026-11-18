import { ArrowLeft, CalendarDays, CreditCard, ShieldCheck, Users } from "lucide-react";
import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";

import { Container } from "@/components/shared/container";
import { RemoteImage } from "@/components/shared/remote-image";
import { ErrorState } from "@/components/shared/state";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { apartmentsApi } from "@/features/apartments/api/apartments";
import { loadApartment } from "@/features/apartments/api/apartments.server";
import { formatLocation } from "@/features/apartments/components/apartment-card";
import { PriceBreakdown } from "@/features/apartments/components/price-breakdown";
import { ReserveButton } from "@/features/bookings/components/reserve-button";
import { attempt } from "@/lib/api/client";
import { getServerHttp } from "@/lib/api/server";
import { formatDateRange, nightsBetween, parseStayRange, toDateOnly } from "@/lib/dates";
import { pluralize } from "@/lib/format";

export const metadata: Metadata = { title: "Confirm and reserve" };

const first = (v: string | string[] | undefined) => (Array.isArray(v) ? v[0] : v);

// Auth is enforced by src/proxy.ts.
export default async function StayCheckoutPage({
  params,
  searchParams,
}: PageProps<"/stays/[apartmentId]">) {
  const [{ apartmentId }, query] = await Promise.all([params, searchParams]);
  const range = parseStayRange(first(query.startDate), first(query.endDate));
  const guests = Math.max(1, Number(first(query.guests)) || 1);

  // Missing/invalid/past dates: send the user back to pick dates on the listing.
  if (!range) redirect(`/apartments/${encodeURIComponent(apartmentId)}#booking`);

  const result = await loadApartment(apartmentId);
  const startDate = toDateOnly(range.from);
  const endDate = toDateOnly(range.to);
  const editHref = `/apartments/${apartmentId}?startDate=${startDate}&endDate=${endDate}&guests=${guests}#booking`;

  if (!result.ok) {
    return (
      <Container>
        <ErrorState title="We can't load this stay right now" description={result.error.message} />
      </Container>
    );
  }

  const apartment = result.data;
  const http = await getServerHttp();
  const price = await attempt(() => apartmentsApi.price(http, apartment.id, startDate, endDate));
  const nights = nightsBetween(range.from, range.to);

  return (
    <Container className="grid max-w-6xl gap-8">
      <div className="flex items-center gap-3">
        <Button asChild variant="ghost" size="icon" className="rounded-full">
          <Link href={editHref} aria-label="Back to listing">
            <ArrowLeft />
          </Link>
        </Button>
        <h1 className="text-2xl font-semibold sm:text-3xl">Confirm and reserve</h1>
      </div>

      <div className="grid gap-10 lg:grid-cols-[minmax(0,1fr)_26rem] lg:gap-16">
        <div className="grid content-start gap-8">
          <section aria-labelledby="trip-heading" className="grid gap-5">
            <h2 id="trip-heading" className="text-xl font-semibold">
              Your trip
            </h2>
            <dl className="grid gap-5">
              <div className="flex items-start justify-between gap-4">
                <div className="flex items-start gap-3">
                  <CalendarDays className="mt-0.5 size-5 text-muted-foreground" aria-hidden="true" />
                  <div>
                    <dt className="font-medium">Dates</dt>
                    <dd className="text-muted-foreground">
                      {formatDateRange(range.from, range.to)} · {pluralize(nights, "night")}
                    </dd>
                  </div>
                </div>
                <Link href={editHref} className="text-sm font-medium underline underline-offset-4">
                  Edit
                </Link>
              </div>
              <div className="flex items-start justify-between gap-4">
                <div className="flex items-start gap-3">
                  <Users className="mt-0.5 size-5 text-muted-foreground" aria-hidden="true" />
                  <div>
                    <dt className="font-medium">Guests</dt>
                    <dd className="text-muted-foreground">
                      {pluralize(Math.min(guests, apartment.maximumGuestCount || guests), "guest")}
                    </dd>
                  </div>
                </div>
                <Link href={editHref} className="text-sm font-medium underline underline-offset-4">
                  Edit
                </Link>
              </div>
            </dl>
          </section>

          <Separator />

          <section aria-labelledby="payment-heading" className="grid gap-4">
            <h2 id="payment-heading" className="text-xl font-semibold">
              How payment works
            </h2>
            <ol className="grid gap-4 text-sm">
              <li className="flex items-start gap-3">
                <ShieldCheck className="mt-0.5 size-5 shrink-0 text-primary" aria-hidden="true" />
                <span>
                  <span className="font-medium">Reserve now.</span>{" "}
                  <span className="text-muted-foreground">We hold these dates for you — nothing is charged yet.</span>
                </span>
              </li>
              <li className="flex items-start gap-3">
                <CreditCard className="mt-0.5 size-5 shrink-0 text-primary" aria-hidden="true" />
                <span>
                  <span className="font-medium">Pay to confirm.</span>{" "}
                  <span className="text-muted-foreground">
                    Complete secure checkout from <Link href="/bookings" className="underline underline-offset-4">My bookings</Link> to confirm your stay.
                  </span>
                </span>
              </li>
            </ol>
          </section>

          <Separator />

          <ReserveButton apartmentId={apartment.id} startDate={startDate} endDate={endDate} />
        </div>

        <aside aria-label="Price summary" className="lg:sticky lg:top-28 lg:self-start">
          <div className="grid gap-5 rounded-2xl border bg-card p-6 shadow-card">
            <div className="flex gap-4">
              <div className="relative size-24 shrink-0 overflow-hidden rounded-xl bg-muted">
                <RemoteImage src={apartment.imageUrl} alt="" fill sizes="96px" className="object-cover" />
              </div>
              <div className="grid content-center gap-1">
                <p className="line-clamp-2 font-semibold">{apartment.name}</p>
                <p className="text-sm text-muted-foreground">{formatLocation(apartment.address)}</p>
              </div>
            </div>
            <Separator />
            <h2 className="font-semibold">Price details</h2>
            {price.ok ? (
              <PriceBreakdown price={price.data} nights={nights} />
            ) : (
              <p role="alert" className="text-sm text-destructive">
                {price.error.message}
              </p>
            )}
          </div>
        </aside>
      </div>
    </Container>
  );
}
