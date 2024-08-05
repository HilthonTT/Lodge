"use client";

import Link from "next/link";
import { z } from "zod";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { Loader2, TentTree } from "lucide-react";
import { zodResolver } from "@hookform/resolvers/zod";

import { useRegister } from "@/features/authentication/mutations/use-register";
import { RegisterValidation } from "@/features/authentication/validation";

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

export const RegisterForm = () => {
  const router = useRouter();

  const { toast } = useToast();
  const { mutateAsync: register, isPending } = useRegister();

  const form = useForm<z.infer<typeof RegisterValidation>>({
    resolver: zodResolver(RegisterValidation),
    defaultValues: {
      email: "",
      password: "",
      firstName: "",
      lastName: "",
    },
  });

  const onSubmit = async (values: z.infer<typeof RegisterValidation>) => {
    const token = await register(values);

    if (!token) {
      toast({ title: "Something went wrong, please try again." });
      return;
    }

    toast({ title: "Successfully registered!" });

    router.push("/");
  };

  return (
    <div className="flex justify-center items-center min-h-screen">
      <div className="w-full max-w-md p-8 space-y-6 bg-white rounded-xl shadow-md">
        <div className="flex flex-col items-center justify-center">
          <TentTree className="size-16 text-center text-indigo-500" />
          <h1 className="text-2xl font-bold text-center text-gray-900 capitalize">
            Registration
          </h1>
        </div>

        <Form {...form}>
          <form
            onSubmit={form.handleSubmit(onSubmit)}
            className="space-y-5 flex flex-col mt-4">
            <div className="flex items-center justify-between">
              <FormField
                control={form.control}
                name="firstName"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel className="form-label" htmlFor="first-name">
                      First name
                    </FormLabel>
                    <FormControl>
                      <Input
                        className="form-input"
                        type="text"
                        disabled={isPending}
                        placeholder="John"
                        {...field}
                      />
                    </FormControl>
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name="lastName"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel className="form-label" htmlFor="first-name">
                      Last name
                    </FormLabel>
                    <FormControl>
                      <Input
                        className="form-input"
                        type="text"
                        disabled={isPending}
                        placeholder="Doe"
                        {...field}
                      />
                    </FormControl>
                  </FormItem>
                )}
              />
            </div>

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
                      placeholder="johndoe@email.com"
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
                      placeholder="Choose a strong password"
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <div>
              <Link
                href="/login"
                className="text-indigo-500 text-sm hover:underline">
                Already have an account?
              </Link>
            </div>
            <Button type="submit" disabled={isPending}>
              {isPending ? <Loader2 className="animate-spin" /> : "Register"}
            </Button>
          </form>
        </Form>
      </div>
    </div>
  );
};
