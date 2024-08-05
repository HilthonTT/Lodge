import { Navbar } from "@/components/navigation/navbar";
import { Amenities } from "@/components/navigation/amenities";

type Props = {
  children: React.ReactNode;
};

const MainLayout = ({ children }: Props) => {
  return (
    <div className="relative">
      <Navbar />
      <Amenities />
      <div className="size-full pt-64">{children}</div>
    </div>
  );
};

export default MainLayout;
