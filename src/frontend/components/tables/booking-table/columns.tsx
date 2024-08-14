import Image from "next/image";
import Link from "next/link";
import { ColumnDef } from "@tanstack/react-table";
import { FaCog } from "react-icons/fa";
import { IoCheckmarkCircle } from "react-icons/io5";

import { useConfirmBooking } from "@/features/bookings/mutations/use-confirm-booking";

import { formatDate } from "@/lib/utils";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Button } from "@/components/ui/button";
import { useUserContext } from "@/context/auth-context";
import { Loader } from "@/components/loader";

const BookingStatusMap: Record<number, string> = {
  1: "Reserved",
  2: "Confirmed",
  3: "Rejected",
  4: "Cancelled",
  5: "Completed",
};

export const columns: ColumnDef<Booking>[] = [
  {
    header: "ID",
    cell: ({ row }) => <p>{row.index + 1}</p>,
  },
  {
    header: "Apartment",
    cell: ({ row }) => {
      const booking = row.original;

      return (
        <div className="flex items-center gap-4">
          <div className="relative aspect-square size-8">
            <Image
              src={booking.apartmentImageUrl}
              alt={booking.apartmentName}
              className="object-cover rounded-lg"
              fill
            />
          </div>
          <Link
            href={`/apartments/${booking.apartmentId}`}
            className="line-clamp-1 hover:underline">
            {booking.apartmentName}
          </Link>
        </div>
      );
    },
  },
  {
    header: "Status",
    cell: ({ row }) => {
      const booking = row.original;

      return <p className="line-clamp-1">{BookingStatusMap[booking.status]}</p>;
    },
  },
  {
    header: "Duration",
    cell: ({ row }) => {
      const booking = row.original;

      const formattedDateRange = formatDate(
        booking.durationStart,
        booking.durationEnd
      );

      return <p className="line-clamp-1">{formattedDateRange}</p>;
    },
  },
  {
    id: "actions",

    cell: ({ row }) => {
      const { user, isLoading, isAuthenticated } = useUserContext();

      const { mutateAsync: confirmBooking, isPending: isConfirming } =
        useConfirmBooking(user.id);

      const booking = row.original;

      const onConfirm = async () => {
        const url = await confirmBooking({
          jwtToken: user.jwtToken,
          userId: user.id,
          bookingId: booking.id,
        });

        if (url) {
          window.location.href = url;
        }
      };

      return (
        <DropdownMenu>
          <DropdownMenuTrigger
            disabled={isLoading || !isAuthenticated}
            className="ml-auto w-full">
            <Button variant="ghost">
              <FaCog />
              <span className="sr-only">Modify booking</span>
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent className="space-y-2">
            {!isConfirming && (
              <DropdownMenuItem onClick={onConfirm} className="gap-2">
                <IoCheckmarkCircle className="text-emerald-500" />
                <span>Confirm</span>
              </DropdownMenuItem>
            )}
            {isConfirming && <Loader />}
          </DropdownMenuContent>
        </DropdownMenu>
      );
    },
  },
];
