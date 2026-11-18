import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { initials } from "@/lib/utils";

type UserAvatarProps = {
  name: string;
  src?: string | null;
  className?: string;
  fallbackClassName?: string;
};

/** Profile picture with an initials fallback (also while loading or when the image fails). */
export function UserAvatar({ name, src, className, fallbackClassName }: UserAvatarProps) {
  return (
    <Avatar className={className}>
      {src && <AvatarImage src={src} alt="" />}
      <AvatarFallback className={fallbackClassName} delayMs={src ? 300 : undefined}>
        {initials(name)}
      </AvatarFallback>
    </Avatar>
  );
}
