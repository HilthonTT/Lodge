import { cn } from "@/lib/utils";

type Props = {
  children: React.ReactNode;
  className?: string;
};

export const Container = ({ children, className }: Props) => {
  return (
    <div
      className={cn(
        "max-w-[2520px] mx-auto xl:px-20 md:px-10 sm:px-2 px-4",
        className
      )}>
      {children}
    </div>
  );
};
