import { Navbar } from "@/components/navigation/navbar";
import { Container } from "@/components/container";

type Props = {
  children: React.ReactNode;
};

const MainLayout = ({ children }: Props) => {
  return (
    <>
      <Navbar />
      <Container className="flex-1 pb-20 pt-5">{children}</Container>
    </>
  );
};

export default MainLayout;
