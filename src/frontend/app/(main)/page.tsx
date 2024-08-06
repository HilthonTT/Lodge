"use client";

import { ApartmentCard } from "@/features/apartments/components/apartment-card";
import { useGetApartments } from "@/features/apartments/queries/use-get-apartments";

const HomePage = () => {
  const { data: apartments, isPending } = useGetApartments(1);

  if (isPending) {
    return <div>Loading...</div>;
  }

  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 2xl:grid-cols-6 gap-5">
      {apartments?.items.map((apartment) => (
        <ApartmentCard key={apartment.id} apartment={apartment} />
      ))}
    </div>
  );
};

export default HomePage;
