type UserAuth = {
  id: string;
  email: string;
  name: string;
  imageId?: string;
  jwtToken: string;
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

type ReserveBookingRequest = {
  jwtToken: string;
  apartmentId: string;
  userId: string;
  startDate: string;
  endDate: string;
};

type ConfirmBookingRequest = {
  jwtToken: string;
  userId: string;
  bookingId: string;
};

type CancelBookingRequest = {
  jwtToken: string;
  userId: string;
  bookingId: string;
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
  maximumRoomCount: number;
  maximumGuestCount: number;
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

type DateRange = {
  startDate: Date;
  endDate: Date;
};

type PriceDetails = {
  currency: string;
  pricePerPeriod: number;
  cleaningFee: number;
  amenitiesUpCharge: number;
  totalPrice: number;
};

type Booking = {
  id: string;
  apartmentId: string;
  userId: string;
  status: number;
  priceAmount: number;
  priceCurrency: string;
  amenitiesUpChargeAmount: string;
  AmenitiesUpChargeCurrency: string;
  totalPriceAmount: string;
  totalPriceCurrency: string;
  apartmentName: string;
  apartmentImageUrl: string;
  durationStart: string;
  durationEnd: string;
  createdOnUtc: Date;
};
