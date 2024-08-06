import { formatPrice, getImageUrl } from "@/lib/utils";

type Props = {
  apartment: Apartment;
};

export const ApartmentCard = ({ apartment }: Props) => {
  return (
    <div role="button" className="group max-h-1">
      <div className="flex flex-col gap-2 w-full overflow-hidden">
        <div className="relative aspect-square w-full overflow-hideen rounded-xl">
          <img
            src={getImageUrl(apartment.imageId)}
            alt={apartment.name}
            className="object-cover size-full rounded-xl group-hover:scale-110 transition aspect-square"
          />
        </div>
        <div className="font-bold">{apartment.name}</div>
        <div className="font-semibold text-lg line-clamp-1">
          {apartment.address.state}, {apartment.address.country}
        </div>
        <div className="flex flex-row items-center gap-1">
          <p className="font-semibold">
            {formatPrice(apartment.currency, apartment.price)}
          </p>
          <p>night</p>
        </div>
      </div>
    </div>
  );
};
