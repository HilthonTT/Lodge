"use client";

import { useRouter } from "next/navigation";
import {
  createContext,
  Dispatch,
  SetStateAction,
  useContext,
  useEffect,
  useState,
} from "react";

import { extractJwtPayload, fetchToken } from "@/lib/auth";

export const INITIAL_USER: UserAuth = {
  id: "",
  email: "",
  name: "",
  imageId: "",
};

const INITIAL_STATE = {
  user: INITIAL_USER,
  isLoading: false,
  isAuthenticated: false,
  setUser: () => {},
  setIsAuthenticated: () => {},
  checkAuthUser: () => false,
};

type IContextType = {
  user: UserAuth;
  isLoading: boolean;
  setUser: Dispatch<SetStateAction<UserAuth>>;
  isAuthenticated: boolean;
  setIsAuthenticated: Dispatch<SetStateAction<boolean>>;
  checkAuthUser: () => boolean;
};

const AuthContext = createContext<IContextType>(INITIAL_STATE);

type Props = {
  children: React.ReactNode;
};

export const AuthProvider = ({ children }: Props) => {
  const router = useRouter();

  const [user, setUser] = useState<UserAuth>(INITIAL_USER);
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(false);

  const checkAuthUser = (): boolean => {
    try {
      setIsLoading(true);

      const token = fetchToken();
      if (!token) {
        return false;
      }

      const { userId, email, imageId, name } = extractJwtPayload(token);

      setUser({
        id: userId,
        email,
        imageId,
        name,
      });

      setIsAuthenticated(true);

      return true;
    } catch (error) {
      console.error(error);
      return false;
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    const token = fetchToken();

    if (!token) {
      router.push("/login");
    }

    const isLoggedIn = checkAuthUser();
    if (!isLoggedIn) {
      router.push("/login");
    }
  }, [router]);

  const value = {
    user,
    setUser,
    isLoading,
    isAuthenticated,
    setIsAuthenticated,
    checkAuthUser,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export const useUserContext = () => useContext(AuthContext);
