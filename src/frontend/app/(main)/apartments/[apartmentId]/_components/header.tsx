import { HeartIcon } from "lucide-react";

import { Button } from "@/components/ui/button";

type Props = {
  apartment: Apartment;
};

export const Header = ({ apartment }: Props) => {
  return (
    <div className="flex items-center justify-between">
      <h1 className="font-semibold text-xl lg:text-3xl">{apartment.name}</h1>

      <Button variant="ghost">
        <HeartIcon className="text-neutral-600 size-5" />
        <span className="ml-1 underline">Save</span>
      </Button>
    </div>
  );
};
