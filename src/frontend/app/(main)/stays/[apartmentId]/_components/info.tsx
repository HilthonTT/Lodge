import { format, parseISO } from "date-fns";

import { Button } from "@/components/ui/button";

type Props = {
  apartment: Apartment;
  startDate: string;
  endDate: string;
};

export const Info = ({ apartment, startDate, endDate }: Props) => {
  // Parse the ISO date strings into Date objects
  const start = parseISO(startDate);
  const end = parseISO(endDate);

  // Format the dates
  const yearMonthDayFormat = "MMM d, yyyy";

  const formattedStart = format(start, yearMonthDayFormat);
  const formattedEnd = format(end, yearMonthDayFormat);

  // Concatenate start and end dates with a dash in between
  const formattedDateRange = `${formattedStart} - ${formattedEnd}`;

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
      <Button>Order now</Button>
    </div>
  );
};
