"use client";

import Cookies from "js-cookie";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";

import { JwtPayload, extractJwtPayload } from "@/lib/auth";

const ProtectedPage = () => {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [claims, setClaims] = useState<JwtPayload | null>(null);
  const router = useRouter();

  useEffect(() => {
    const token = Cookies.get("token");
    if (!token) {
      router.replace("/"); // Redirect to login if not authenticated
    } else {
      // You might want to validate the token or make an API call to fetch user data here
      const decodedClaims = extractJwtPayload(token);
      setClaims(decodedClaims);
      setIsAuthenticated(true);
    }
  }, [router]);

  if (!isAuthenticated) {
    return (
      <div className="flex items-center justify-center h-screen">
        <p className="text-lg text-gray-600">Loading...</p>
      </div>
    );
  }

  return (
    <div className="flex flex-col items-center justify-center min-h-screen bg-gray-100">
      <h1 className="text-3xl font-bold mb-4 text-gray-800">Protected Page</h1>
      <p className="text-lg text-gray-700 mb-6">
        This page is only accessible to authenticated users.
      </p>
      {claims && (
        <div className="mb-6 text-center">
          <p>
            <strong>User ID:</strong> {claims.userId}
          </p>
          <p>
            <strong>Email:</strong> {claims.email}
          </p>
          <p>
            <strong>Name:</strong> {claims.name}
          </p>
          <p>
            <strong>Image:</strong> {claims.imageId ? claims.imageId : "None"}
          </p>
        </div>
      )}
      <button
        className="px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600 transition"
        onClick={() => {
          Cookies.remove("token");
          router.replace("/login");
        }}>
        Logout
      </button>
    </div>
  );
};

export default ProtectedPage;
