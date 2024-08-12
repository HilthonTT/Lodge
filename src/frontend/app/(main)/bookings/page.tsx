"use client";

import { useUserContext } from "@/context/auth-context";

const BookingsPage = () => {
  const { user, isAuthenticated } = useUserContext();

  return <div></div>;
};

export default BookingsPage;
