import { MdOutlineDashboard } from "react-icons/md";

type Props = {
  user: UserAuth;
};

export const Header = ({ user }: Props) => {
  return (
    <header className="flex items-start gap-2 lg:gap-4">
      <MdOutlineDashboard className="size-6 lg:size-8" />
      <div className="flex flex-col">
        <h1 className="font-semibold text-xl lg:text-3xl">
          Bookings Dashboard
        </h1>
        <p className="text-muted-foreground lg:text-lg">Hello {user.name} 👋</p>
      </div>
    </header>
  );
};
