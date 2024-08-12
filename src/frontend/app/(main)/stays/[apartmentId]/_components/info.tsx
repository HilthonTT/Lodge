"use client";

import { useRouter } from "next/navigation";
import { format, parseISO } from "date-fns";

import { useReserveBooking } from "@/features/bookings/mutations/use-reserve-booking";

import { DATE_FORMAT } from "@/constants";
import { useUserContext } from "@/context/auth-context";
import { Button } from "@/components/ui/button";
import { Loader } from "@/components/loader";
import { useToast } from "@/components/ui/use-toast";

type Props = {
  apartment: Apartment;
  startDate: string;
  endDate: string;
};

export const Info = ({ apartment, startDate, endDate }: Props) => {
  const router = useRouter();

  const { toast } = useToast();
  const { user } = useUserContext();

  const { mutateAsync: reserveBooking, isPending } = useReserveBooking();

  const start = parseISO(startDate);
  const end = parseISO(endDate);

  const formattedStart = format(start, DATE_FORMAT);
  const formattedEnd = format(end, DATE_FORMAT);

  const formattedDateRange = `${formattedStart} - ${formattedEnd}`;

  const onReserve = async () => {
    const request: ReserveBookingRequest = {
      jwtToken: user.jwtToken,
      apartmentId: apartment.id,
      userId: user.id,
      startDate,
      endDate,
    };

    const bookingId = await reserveBooking(request);

    if (bookingId) {
      router.push("/bookings");
      toast({
        title:
          "You've booked this apartment, please confirm it in the dashboard",
      });
    }
  };

  return (
    <div className="flex flex-col gap-y-4 mt-8">
      <h2 className="text-lg lg:text-xl font-semibold">Your trip</h2>
      <div className="flex flex-col gap-y-4">
        <div className="flex flex-col">
          <p className="font-bold">Dates</p>
          {formattedDateRange}
        </div>

        <div className="flex flex-col">
          <p className="font-bold">Guests</p>
          {apartment.maximumGuestCount} guests
        </div>

        <div className="flex flex-col">
          <p className="font-bold">Rooms</p>
          {apartment.maximumRoomCount} rooms
        </div>
      </div>
      <Button onClick={onReserve} disabled={isPending}>
        {isPending ? <Loader /> : "Reserve now"}
      </Button>
    </div>
  );
};
