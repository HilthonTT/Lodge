import Link from "next/link";
import { TentTree } from "lucide-react";
import { Nunito } from "next/font/google";

import { cn } from "@/lib/utils";

const font = Nunito({
  subsets: ["latin"],
  weight: ["300", "400", "500", "600", "700", "800"],
});

export const Logo = () => {
  return (
    <Link href="/" className="items-center justify-center group hidden md:flex">
      <TentTree className="text-indigo-500 size-10 group-hover:text-black transition" />
      <p
        className={cn(
          "font-bold text-indigo-500 group-hover:text-black transition text-xl",
          font.className
        )}>
        Lodge
      </p>
    </Link>
  );
};
