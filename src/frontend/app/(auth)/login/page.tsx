"use client";

import { useState } from "react";
import { TentTree } from "lucide-react";
import { useRouter } from "next/navigation";

import { login, storeToken } from "@/lib/auth";
import { LoginForm } from "@/components/forms/login-form";

const LoginPage = () => {
  const router = useRouter();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);

  const handleLogin = async (event: React.FormEvent) => {
    event.preventDefault();

    try {
      const token = await login(email, password);

      if (token) {
        storeToken(token);
        router.push("/protected");
      }
    } catch (error) {
      setError("Invalid credentials");
    }
  };

  return <LoginForm />;

  // return (
  //   <div className="flex items-center justify-center min-h-screen bg-gray-100">
  //     <div className="w-full max-w-md p-8 space-y-6 bg-white rounded-lg shadow-md">
  //       <div className="flex flex-col items-center justify-center">
  //         <TentTree className="size-16 text-center text-indigo-500" />
  //         <h1 className="text-2xl font-bold text-center text-gray-900 capitalize">
  //           Login to your account
  //         </h1>
  //       </div>

  //       <form onSubmit={handleLogin} className="space-y-6">
  //         <div>
  //           <label
  //             htmlFor="email"
  //             className="block text-sm font-medium text-gray-700">
  //             Username
  //           </label>
  //           <input
  //             id="email"
  //             name="email"
  //             type="text"
  //             autoComplete="username"
  //             value={email}
  //             onChange={(e) => setEmail(e.target.value)}
  //             required
  //             className="w-full px-3 py-2 mt-1 text-gray-900 border rounded-md shadow-sm focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
  //           />
  //         </div>
  //         <div>
  //           <label
  //             htmlFor="password"
  //             className="block text-sm font-medium text-gray-700">
  //             Password
  //           </label>
  //           <input
  //             id="password"
  //             name="password"
  //             type="password"
  //             autoComplete="current-password"
  //             value={password}
  //             onChange={(e) => setPassword(e.target.value)}
  //             required
  //             className="w-full px-3 py-2 mt-1 text-gray-900 border rounded-md shadow-sm focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
  //           />
  //         </div>
  //         {error && <p className="text-sm text-red-600">{error}</p>}
  //         <button
  //           type="submit"
  //           className="w-full px-4 py-2 font-medium transition text-white bg-indigo-600 rounded-md hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500">
  //           Login
  //         </button>
  //       </form>
  //     </div>
  //   </div>
  // );
};

export default LoginPage;
