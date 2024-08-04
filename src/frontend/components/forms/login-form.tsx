"use client";

import Link from "next/link";
import { z } from "zod";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { Loader2, TentTree } from "lucide-react";
import { zodResolver } from "@hookform/resolvers/zod";

import { useLogin } from "@/features/authentication/mutations/use-login";

import { LoginValidation } from "@/lib/validation";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { useToast } from "@/components/ui/use-toast";

export const LoginForm = () => {
  const router = useRouter();

  const { toast } = useToast();
  const { mutateAsync: login, isPending } = useLogin();

  const form = useForm<z.infer<typeof LoginValidation>>({
    resolver: zodResolver(LoginValidation),
    defaultValues: {
      email: "",
      password: "",
    },
  });

  const onSubmit = (values: z.infer<typeof LoginValidation>) => {
    const token = login(values);

    if (!token) {
      toast({ title: "Something went wrong" });
      return;
    }

    toast({ title: "Successfully logged in!" });

    router.push("/");
  };

  return (
    <div className="flex justify-center items-center min-h-screen">
      <div className="w-full max-w-md p-8 space-y-6 bg-white rounded-xl shadow-md">
        <div className="flex flex-col items-center justify-center">
          <TentTree className="size-16 text-center text-indigo-500" />
          <h1 className="text-2xl font-bold text-center text-gray-900 capitalize">
            Login to your account
          </h1>
        </div>

        <Form {...form}>
          <form
            onSubmit={form.handleSubmit(onSubmit)}
            className="space-y-5 flex flex-col mt-4">
            <FormField
              control={form.control}
              name="email"
              render={({ field }) => (
                <FormItem>
                  <FormLabel className="form-label" htmlFor="email">
                    Email
                  </FormLabel>
                  <FormControl>
                    <Input
                      className="form-input"
                      type="email"
                      autoComplete="username"
                      disabled={isPending}
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="password"
              render={({ field }) => (
                <FormItem>
                  <FormLabel className="form-label" htmlFor="password">
                    Password
                  </FormLabel>
                  <FormControl>
                    <Input
                      className="form-input"
                      type="password"
                      autoComplete="current-password"
                      disabled={isPending}
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <div>
              <Link
                href="/register"
                className="text-indigo-500 text-sm hover:underline">
                Don&apos;t have an account?
              </Link>
            </div>
            <Button type="submit" disabled={isPending}>
              {isPending ? <Loader2 className="animate-spin" /> : "Login"}
            </Button>
          </form>
        </Form>
      </div>
    </div>
  );
};
