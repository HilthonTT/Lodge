"use client";

import { ApartmentCard } from "@/features/apartments/components/apartment-card";
import { useGetApartments } from "@/features/apartments/queries/use-get-apartments";

import { Loader } from "@/components/loader";

const HomePage = () => {
  const { data: apartments, isPending } = useGetApartments(1);

  if (isPending) {
    return <Loader />;
  }

  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
      {apartments?.items.map((apartment) => (
        <ApartmentCard key={apartment.id} apartment={apartment} />
      ))}
    </div>
  );
};

export default HomePage;
