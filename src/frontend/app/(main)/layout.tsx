import { Navbar } from "@/components/navigation/navbar";
import { Container } from "@/components/container";

type Props = {
  children: React.ReactNode;
};

const MainLayout = ({ children }: Props) => {
  return (
    <div className="relative">
      <Navbar />
      <Container className="size-full pb-20 pt-5">{children}</Container>
    </div>
  );
};

export default MainLayout;
