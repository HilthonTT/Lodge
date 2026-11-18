import { FaRegAddressCard } from "react-icons/fa";
import { GoShieldCheck } from "react-icons/go";
import { IoMdBook } from "react-icons/io";

import { SettingsCard } from "./_components/card";
import { Header } from "./_components/header";

const SettingsPage = () => {
  return (
    <div className="max-w-5xl mx-auto my-8">
      <Header />
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 mt-8 gap-8">
        <SettingsCard
          title="Personal info"
          description="Provide personal details and how we can reach you"
          icon={FaRegAddressCard}
          href="/settings/personal-info"
        />
        <SettingsCard
          title="Login & security"
          description="Update your password and secure your account"
          icon={GoShieldCheck}
          href="/settings/login-and-security"
        />
        <SettingsCard
          title="My bookings"
          description="See what you've booked in the past, confirm them or cancel them"
          icon={IoMdBook}
          href="/bookings"
        />
      </div>
    </div>
  );
};

export default SettingsPage;
