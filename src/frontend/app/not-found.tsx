import { Earth } from "@/components/earth";

const NotFound = () => {
  return (
    <div className="h-screen flex items-center justify-center">
      <Earth
        title="Not Found"
        description="We haven't found the page you're requesting."
      />
    </div>
  );
};

export default NotFound;
