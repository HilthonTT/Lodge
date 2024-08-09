import { ApartmentCard } from "@/features/apartments/components/apartment-card";

import { CustomPagination } from "@/components/custom-pagination";
import { getApartments } from "@/actions/apartments/get-apartments";

type Props = {
  searchParams: {
    amenity?: string;
    page?: number;
  };
};

const HomePage = async ({ searchParams }: Props) => {
  const apartments = await getApartments(searchParams.page || 1, 36);

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
