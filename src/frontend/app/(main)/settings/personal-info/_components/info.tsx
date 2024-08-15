"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import { useRouter } from "next/navigation";
import { z } from "zod";

import { useUpdateUser } from "@/features/users/mutations/use-update-user";
import { LegalNameValidation } from "@/features/users/validation";

import {
  Form,
  FormControl,
  FormDescription,
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

  const { mutateAsync: updateUser, isPending: isUpdatingUser } =
    useUpdateUser();

  const [isEditingName, setIsEditingName] = useState(false);

  const fullName = user.name.split(" ");
  const firstName = fullName[0];
  const lastName = fullName[1];

  const form = useForm<z.infer<typeof LegalNameValidation>>({
    resolver: zodResolver(LegalNameValidation),
    defaultValues: {
      firstName: firstName,
      lastName: lastName,
    },
  });

  const onSubmit = async (values: z.infer<typeof LegalNameValidation>) => {
    await updateUser({
      ...values,
      jwtToken: user.jwtToken,
      userId: user.id,
    });

    router.push("/login");
  };

  return (
    <div className="flex flex-col gap-y-4 mt-12 max-w-4xl mr-auto">
      {!isEditingName && (
        <div className="flex items-center justify-between">
          <div className="flex flex-col space-y-2">
            <Label className="text-neural-500 text-lg">Legal name</Label>
            <p className="text-muted-foreground line-clamp-1 text-sm">
              {user.name}
            </p>
          </div>
          <Button
            onClick={() => setIsEditingName(true)}
            className="w-auto"
            variant="link">
            Edit
          </Button>
        </div>
      )}

      {isEditingName && (
        <Form {...form}>
          <form
            onSubmit={form.handleSubmit(onSubmit)}
            className="flex flex-col">
            <div className="flex items-center justify-between">
              <FormLabel className="text-lg">Legal name</FormLabel>
              <Button
                onClick={() => setIsEditingName(false)}
                className="w-auto"
                type="button"
                variant="link">
                Cancel
              </Button>
            </div>
            <FormDescription className="text-base">
              Make sure this matches the name on your government ID.
            </FormDescription>
            <div className="flex items-center justify-center w-full mt-4 gap-4">
              <FormField
                control={form.control}
                name="firstName"
                render={({ field }) => (
                  <FormItem className="w-full">
                    <FormControl>
                      <Input
                        autoComplete="username"
                        placeholder="John"
                        disabled={isUpdatingUser}
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
                  <FormItem className="w-full">
                    <FormControl>
                      <Input
                        autoComplete="username"
                        placeholder="Doe"
                        disabled={isUpdatingUser}
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
              disabled={isUpdatingUser}>
              {isUpdatingUser ? <Loader /> : "Save"}
            </Button>
          </form>
        </Form>
      )}

      <Separator className="my-4" />
    </div>
  );
};
