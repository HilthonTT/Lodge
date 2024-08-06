import { CircleUserRound, Menu } from "lucide-react";

export const UserMenu = () => {
  return (
    <div className="ml-auto hidden md:flex">
      <div className="rounded-full py-2 px-4 border border-neutral-200  hover:shadow-lg transition cursor-pointer">
        <div className="flex items-center justify-center gap-4">
          <Menu className="size-5" />
          <CircleUserRound className="size-8 text-white bg-neutral-600 rounded-full" />
        </div>
      </div>
    </div>
  );
};
