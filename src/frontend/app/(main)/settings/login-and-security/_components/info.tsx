"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import { useRouter } from "next/navigation";
import { z } from "zod";

import { useChangePassword } from "@/features/users/mutations/use-change-password";
import { PasswordValidation } from "@/features/users/validation";

import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
} from "@/components/ui/form";
import { useUserContext } from "@/context/auth-context";
import { Separator } from "@/components/ui/separator";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Loader } from "@/components/loader";

export const Info = () => {
  const router = useRouter();

  const { user } = useUserContext();

  const { mutateAsync: changePassword, isPending: isChangingPassword } =
    useChangePassword();

  const [isEditingPassword, setIsEditingPassword] = useState(false);

  const form = useForm<z.infer<typeof PasswordValidation>>({
    resolver: zodResolver(PasswordValidation),
    defaultValues: {},
  });

  const onSubmit = async (values: z.infer<typeof PasswordValidation>) => {
    await changePassword({
      ...values,
      jwtToken: user.jwtToken,
      userId: user.id,
    });

    router.push("/login");
  };

  return (
    <div className="flex flex-col gap-y-4 mt-12 max-w-4xl mr-auto">
      {!isEditingPassword && (
        <div className="flex items-center justify-between">
          <div className="flex flex-col space-y-2">
            <Label className="text-neural-500 text-lg">Password</Label>
            <p className="text-muted-foreground line-clamp-1 text-sm">
              Make sure your password is strong
            </p>
          </div>
          <Button
            onClick={() => setIsEditingPassword(true)}
            className="w-auto"
            variant="link">
            Edit
          </Button>
        </div>
      )}

      {isEditingPassword && (
        <Form {...form}>
          <form
            onSubmit={form.handleSubmit(onSubmit)}
            className="flex flex-col">
            <div className="flex items-center justify-between">
              <FormLabel className="text-lg">Password</FormLabel>
              <Button
                onClick={() => setIsEditingPassword(false)}
                className="w-auto"
                type="button"
                variant="link">
                Cancel
              </Button>
            </div>
            <div className="flex items-center justify-center w-full mt-4 gap-4">
              <FormField
                control={form.control}
                name="password"
                render={({ field }) => (
                  <FormItem className="w-full">
                    <FormControl>
                      <Input
                        autoComplete="username"
                        placeholder="AbC-123"
                        disabled={isChangingPassword}
                        {...field}
                      />
                    </FormControl>
                  </FormItem>
                )}
              />
            </div>
            <Button
              className="mt-4 w-24"
              type="submit"
              disabled={isChangingPassword}>
              {isChangingPassword ? <Loader /> : "Save"}
            </Button>
          </form>
        </Form>
      )}

      <Separator className="my-4" />
    </div>
  );
};
