import { ApartmentCard } from "@/features/apartments/components/apartment-card";

import { MAX_PAGE_SIZE } from "@/constants";
import { Earth } from "@/components/earth";
import { getApartments } from "@/actions/apartments/get-apartments";
import { CustomPagination } from "@/components/custom-pagination";

type Props = {
  page?: number;
};

export const ApartmentList = async ({ page }: Props) => {
  const { data: apartments, isSuccess } = await getApartments(
    page || 1,
    MAX_PAGE_SIZE
  );

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
          link="/"
          totalCount={apartments?.totalCount || 0}
          pageSize={apartments?.pageSize || 0}
          hasPreviousPage={apartments?.hasPreviousPage || false}
          hasNextPage={apartments?.hasNextPage || false}
        />
      </div>
    </>
  );
};
