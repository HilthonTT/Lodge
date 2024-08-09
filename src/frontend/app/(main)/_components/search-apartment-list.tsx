import { ApartmentCard } from "@/features/apartments/components/apartment-card";

import { Earth } from "@/components/earth";
import { CustomPagination } from "@/components/custom-pagination";
import { searchApartments } from "@/actions/apartments/search-apartments";

type Props = {
  page?: number;
  country?: string;
  amenity?: string;
  startDate?: string;
  endDate?: string;
  guestCount?: number;
  roomCount?: number;
};

export const SearchApartmentList = async ({
  page,
  country,
  guestCount,
  roomCount,
}: Props) => {
  const { data: apartments, isSuccess } = await searchApartments({
    page: page || 1,
    pageSize: 36,
    searchTerm: country,
    guestCount,
    roomCount,
  });

  if (!isSuccess) {
    return (
      <Earth
        title="Something went wrong ;("
        description="This globe is interactive, have fun until we fix the problem!"
      />
    );
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
          link="/search"
          totalCount={apartments?.totalCount || 0}
          pageSize={apartments?.pageSize || 0}
          hasPreviousPage={apartments?.hasPreviousPage || false}
          hasNextPage={apartments?.hasNextPage || false}
        />
      </div>
    </>
  );
};
