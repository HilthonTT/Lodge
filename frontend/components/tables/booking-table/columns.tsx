import Image from "next/image";
import Link from "next/link";
import { ColumnDef } from "@tanstack/react-table";
import { FaCog } from "react-icons/fa";
import { IconType } from "react-icons/lib";
import {
  FiBookmark,
  FiXCircle,
  FiSlash,
  FiCheckCircle,
  FiCheckSquare,
  FiList,
} from "react-icons/fi";
import { IoCheckmarkCircle } from "react-icons/io5";
import { X } from "lucide-react";

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
import { BookingStatus } from "@/enums";
import { useCancelBooking } from "@/features/bookings/mutations/use-cancel-booking";
import { BookingStatusIcon } from "@/components/booking-status-icon";

const BookingStatusMap: Record<number, string> = {
  1: "Reserved",
  2: "Confirmed",
  3: "Rejected",
  4: "Cancelled",
  5: "Completed",
};

const BookingIconMap: Record<number, IconType> = {
  1: FiBookmark,
  2: FiCheckCircle,
  3: FiSlash,
  4: FiXCircle,
  5: FiCheckSquare,
};

const BookingVariantMap: Record<
  number,
  "default" | "warning" | "danger" | "success"
> = {
  1: "default",
  2: "success",
  3: "danger",
  4: "warning",
  5: "default",
};

export const columns: ColumnDef<Booking>[] = [
  {
    header: "ID",
    cell: ({ row }) => <p>{row.index + 1}</p>,
  },
  {
    accessorKey: "apartmentName",
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
    accessorKey: "status",
    header: "Status",
    cell: ({ row }) => {
      const booking = row.original;

      const Icon = BookingIconMap[booking.status];

      return (
        <div className="flex items-center gap-2">
          <BookingStatusIcon
            icon={Icon}
            variant={BookingVariantMap[booking.status]}
          />
          <p className="line-clamp-1 font-semibold">
            {BookingStatusMap[booking.status]}
          </p>
        </div>
      );
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

      return <p className="line-clamp-1 font-semibold">{formattedDateRange}</p>;
    },
  },
  {
    id: "actions",

    cell: ({ row }) => {
      const { user, isLoading, isAuthenticated } = useUserContext();

      const { mutateAsync: confirmBooking, isPending: isConfirming } =
        useConfirmBooking(user.id);

      const { mutateAsync: cancelBooking, isPending: isCancelling } =
        useCancelBooking(user.id);

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

      const onCancel = async () => {
        await cancelBooking({
          jwtToken: user.jwtToken,
          userId: user.id,
          bookingId: booking.id,
        });
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
            {booking.status === BookingStatus.Reserved && (
              <>
                {!isConfirming && (
                  <DropdownMenuItem
                    onClick={onConfirm}
                    disabled={isConfirming}
                    className="gap-2">
                    <IoCheckmarkCircle className="text-emerald-500" />
                    <span>Confirm</span>
                  </DropdownMenuItem>
                )}
                {isConfirming && <Loader />}
              </>
            )}
            {booking.status === BookingStatus.Confirmed && (
              <DropdownMenuItem
                onClick={onCancel}
                disabled={isCancelling}
                className="gap-2">
                <X className="text-rose-500" />
                <span>Cancel</span>
              </DropdownMenuItem>
            )}
          </DropdownMenuContent>
        </DropdownMenu>
      );
    },
  },
];
