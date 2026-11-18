import { ApartmentList } from "./_components/apartment-list";

type Props = {
  searchParams: {
    page?: number;
  };
};

const HomePage = async ({ searchParams }: Props) => {
  return <ApartmentList page={searchParams.page} />;
};

export default HomePage;
