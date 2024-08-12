import { IoDiamondOutline } from "react-icons/io5";

import { GoBackButton } from "./go-back-button";

type Props = {
  apartmentId: string;
};

export const Header = ({ apartmentId }: Props) => {
  return (
    <div className="flex flex-col gap-8">
      <div className="flex items-center gap-4">
        <div className="lg:hidden">
          <GoBackButton apartmentId={apartmentId} />
        </div>
        <h1 className="text-2xl lg:text-3xl font-semibold">
          Confirm and pay later
        </h1>
      </div>

      <div className="rounded-xl border border-neutral-300 w-full lg:w-[658px] p-4">
        <div className="flex items-center justify-between">
          <div className="flex flex-col">
            <p className="font-semibold">This is a rare friends</p>
            <p>Lodge&apos;s place is usually booked</p>
          </div>
          <IoDiamondOutline className="text-rose-400 size-10" />
        </div>
      </div>
    </div>
  );
};
