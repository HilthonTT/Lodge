"use client";

import { isServer, QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ThemeProvider } from "next-themes";

import { Toaster } from "@/components/ui/sonner";
import { SessionProvider } from "@/features/auth/session-provider";
import type { ApiError } from "@/lib/api/client";
import type { Session } from "@/lib/auth/token";

function makeQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 60_000,
        refetchOnWindowFocus: false,
        retry: (failureCount, error) => {
          const status = (error as ApiError | undefined)?.status;
          // Don't hammer the API for client errors (401/403/404/...).
          if (status && status >= 400 && status < 500) return false;
          return failureCount < 2;
        },
      },
    },
  });
}

let browserQueryClient: QueryClient | undefined;

function getQueryClient() {
  if (isServer) return makeQueryClient();
  browserQueryClient ??= makeQueryClient();
  return browserQueryClient;
}

export function Providers({
  initialSession,
  children,
}: {
  initialSession: Session | null;
  children: React.ReactNode;
}) {
  const queryClient = getQueryClient();

  return (
    <ThemeProvider attribute="class" defaultTheme="system" enableSystem disableTransitionOnChange>
      <QueryClientProvider client={queryClient}>
        <SessionProvider initialSession={initialSession}>
          {children}
          <Toaster />
        </SessionProvider>
      </QueryClientProvider>
    </ThemeProvider>
  );
}
