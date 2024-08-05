import { Navbar } from "@/components/navigation/navbar";

type Props = {
  children: React.ReactNode;
};

const MainLayout = ({ children }: Props) => {
  return (
    <div className="relative">
      <Navbar />
      <div className="size-full pt-20">{children}</div>
    </div>
  );
};

export default MainLayout;
