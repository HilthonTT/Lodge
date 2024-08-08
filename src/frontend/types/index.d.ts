type UserAuth = {
  id: string;
  email: string;
  name: string;
  imageId?: string;
};

type LoginRequest = {
  email: string;
  password: string;
};

type RegisterRequest = {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
};

type PagedList<T> = {
  page: number;
  pageSize: number;
  totalCount: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
  items: T[];
};

type Address = {
  country: string;
  state: string;
  city: string;
  street: string;
};

type Apartment = {
  id: string;
  name: string;
  description: string;
  price: number;
  currency: string;
  imageUrl: string;
  address: Address;
};

type ApartmentPagedList = PagedList<Apartment>;

type TokenResponse = {
  token: string;
};

type Country = {
  flag: string;
  label: string;
  latlng: number[];
  region: string;
  value: string;
};
