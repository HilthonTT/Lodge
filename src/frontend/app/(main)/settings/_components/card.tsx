import Link from "next/link";
import { IconType } from "react-icons/lib";

import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { cn } from "@/lib/utils";

type Props = {
  title: string;
  description: string;
  icon: IconType;
  href: string;
  className?: string;
};

export const SettingsCard = ({
  title,
  description,
  className,
  icon: Icon,
  href,
}: Props) => {
  return (
    <Link href={href}>
      <Card
        className={cn(
          "border-none drop-shadow-xl rounded-3xl hover:drop-shadow-2xl transition",
          className
        )}>
        <CardHeader className="flex flex-row items-center justify-between gap-x-4">
          <Icon size={32} className="shrink-0" />
        </CardHeader>
        <CardContent>
          <div className="space-y-2">
            <h1 className="text-2xl lime-clamp-1 font-semibold tracking-wider">
              {title}
            </h1>
            <p className="text-muted-foreground">{description}</p>
          </div>
        </CardContent>
      </Card>
    </Link>
  );
};
