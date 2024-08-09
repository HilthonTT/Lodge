import { SearchApartmentList } from "../_components/search-apartment-list";

type Props = {
  searchParams: {
    page?: number;
    country?: string;
    amenity?: string;
    startDate?: string;
    endDate?: string;
    guestCount?: number;
    roomCount?: number;
  };
};

const SearchPage = ({ searchParams }: Props) => {
  return <SearchApartmentList {...searchParams} />;
};

export default SearchPage;
