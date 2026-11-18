"use client";

import {
  FiBookmark,
  FiXCircle,
  FiSlash,
  FiCheckCircle,
  FiCheckSquare,
  FiList,
} from "react-icons/fi";

import { useGetBookingsByUserId } from "@/features/bookings/queries/use-get-bookings-by-user-id";

import {
  DataTable,
  DataTableSkeleton,
} from "@/components/tables/booking-table";
import { columns } from "@/components/tables/booking-table/columns";
import { BookingStatus } from "@/enums";

import { StatCard, StatCardSkeleton } from "./stat-card";

type Props = {
  user: UserAuth;
};

export const Client = ({ user }: Props) => {
  const { data, isPending } = useGetBookingsByUserId(user.id, user.jwtToken);

  if (isPending) {
    return (
      <div className="flex flex-col">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8 pb-2 mb-8">
          {Array.from({ length: 6 }, (_, index) => (
            <StatCardSkeleton key={index} />
          ))}
        </div>
        <DataTableSkeleton />
      </div>
    );
  }

  const reservedBookingsCount =
    data?.filter((booking) => booking.status === BookingStatus.Rejected)
      .length || 0;

  const cancelledBookingsCount =
    data?.filter((booking) => booking.status === BookingStatus.Cancelled)
      .length || 0;

  const rejectedBookingsCount =
    data?.filter((booking) => booking.status === BookingStatus.Rejected)
      .length || 0;

  const confirmedBookingsCount =
    data?.filter((booking) => booking.status === BookingStatus.Confirmed)
      .length || 0;

  const completedBookingsCount =
    data?.filter((booking) => booking.status === BookingStatus.Completed)
      .length || 0;

  return (
    <div className="flex flex-col">
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8 pb-2 mb-8">
        <StatCard
          title="Reserved"
          description="The total amount of apartment you've booked"
          value={reservedBookingsCount}
          icon={FiBookmark}
        />
        <StatCard
          title="Cancelled"
          description="The total amount of apartment you've booked"
          variant="warning"
          value={cancelledBookingsCount}
          icon={FiXCircle}
        />
        <StatCard
          title="Rejected"
          description="The total amount of apartment you've booked"
          variant="danger"
          value={rejectedBookingsCount}
          icon={FiSlash}
        />
        <StatCard
          title="Confirmed"
          description="The total amount of apartment you've booked"
          variant="success"
          value={confirmedBookingsCount}
          icon={FiCheckCircle}
        />
        <StatCard
          title="Completed"
          description="The total amount of apartment you've booked"
          value={completedBookingsCount}
          icon={FiCheckSquare}
        />

        <StatCard
          title="Total"
          description="The total amount of apartment you've booked"
          value={data?.length || 0}
          icon={FiList}
        />
      </div>
      <DataTable columns={columns} data={data || []} columnId="apartmentName" />
    </div>
  );
};
