import Link from "next/link";
import { TentTree } from "lucide-react";

export const Logo = () => {
  return (
    <Link href="/" className="items-center justify-center group hidden md:flex">
      <TentTree className="text-indigo-500 size-10 group-hover:text-black transition" />
      <p className="font-bold text-indigo-500 group-hover:text-black transition text-xl">
        Lodge
      </p>
    </Link>
  );
};
