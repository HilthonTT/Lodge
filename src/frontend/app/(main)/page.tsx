"use client";

import { ApartmentCard } from "@/features/apartments/components/apartment-card";
import { useGetApartments } from "@/features/apartments/queries/use-get-apartments";

import { Loader } from "@/components/loader";
import { CustomPagination } from "@/components/custom-pagination";

type Props = {
  searchParams: {
    amenity?: string;
    page?: number;
  };
};

const HomePage = ({ searchParams }: Props) => {
  const { data: apartments, isPending } = useGetApartments(searchParams.page);

  if (isPending) {
    return <Loader />;
  }

  return (
    <>
      <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
        {apartments?.items.map((apartment) => (
          <ApartmentCard key={apartment.id} apartment={apartment} />
        ))}
      </div>
      <div className="w-full flex items-center justify-center pt-5">
        <CustomPagination
          totalCount={apartments?.totalCount || 0}
          pageSize={apartments?.pageSize || 0}
          hasPreviousPage={apartments?.hasPreviousPage || false}
          hasNextPage={apartments?.hasNextPage || false}
        />
      </div>
    </>
  );
};

export default HomePage;
