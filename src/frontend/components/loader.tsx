import { LoaderIcon } from "lucide-react";

export const Loader = () => {
  return (
    <div className="flex items-center justify-center h-full">
      <LoaderIcon className="animate-spin" />
    </div>
  );
};
