import type { Metadata } from "next";
import { Suspense } from "react";

import { RegisterForm } from "@/features/auth/components/register-form";

export const metadata: Metadata = { title: "Create account" };

export default function RegisterPage() {
  return (
    <div className="grid gap-8">
      <div className="grid gap-2">
        <h1 className="text-3xl font-semibold">Create your account</h1>
        <p className="text-muted-foreground">Book unique stays and keep all your trips in one place.</p>
      </div>
      <Suspense>
        <RegisterForm />
      </Suspense>
    </div>
  );
}
