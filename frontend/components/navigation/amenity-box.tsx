"use client";

import qs, { ParsedQuery } from "query-string";
import { useRouter, useSearchParams } from "next/navigation";
import { IconType } from "react-icons/lib";

import { cn } from "@/lib/utils";

type Props = {
  label: string;
  icon: IconType;
  selected?: boolean;
};

export const AmenityBox = ({ label, icon: Icon, selected }: Props) => {
  const router = useRouter();
  const searchParams = useSearchParams();

  const handleClick = () => {
    let currentQuery: ParsedQuery<string> = qs.parse(searchParams.toString());

    const updatedQuery: any = {
      ...currentQuery,
      amenity: label,
    };

    if (searchParams.get("amenity") === label) {
      delete updatedQuery.amenity;
    }

    const url = qs.stringifyUrl(
      {
        url: "/",
        query: updatedQuery,
      },
      {
        skipEmptyString: true,
        skipNull: true,
      }
    );

    router.push(url);
  };

  return (
    <div
      onClick={handleClick}
      className={cn(
        "flex flex-col items-center justify-center gap-2 p-3 border-b-2 hover:text-neutral-800 transition",
        selected
          ? "border-b-neutral-800 text-neutral-800"
          : "border-transparent text-neutral-500"
      )}
      role="button">
      <Icon size={26} />
      <p className="font-medium text-sm">{label}</p>
    </div>
  );
};
