"use client";

import { ArrowLeft } from "lucide-react";
import { useRouter } from "next/navigation";

import { Button } from "@/components/ui/button";

type Props = {
  apartmentId: string;
};

export const GoBackButton = ({ apartmentId }: Props) => {
  const router = useRouter();

  const onClick = () => {
    router.push(`/apartments/${apartmentId}`);
  };

  return (
    <Button
      onClick={onClick}
      aria-label="Go back"
      className="p-3 rounded-full hover:bg-indigo-800 transition size-auto">
      <ArrowLeft className="shrink-0 size-5" />
      <span className="sr-only">Go back</span>
    </Button>
  );
};
