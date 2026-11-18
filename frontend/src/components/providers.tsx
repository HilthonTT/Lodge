"use client";

import { isServer, QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ThemeProvider } from "next-themes";

import { Toaster } from "@/components/ui/sonner";
import { SessionProvider } from "@/features/auth/session-provider";
import { CurrencyProvider } from "@/features/payments/currency-provider";
import { RealtimeProvider } from "@/features/realtime/realtime-provider";
import type { ApiError } from "@/lib/api/client";
import type { Session } from "@/lib/auth/token";
import type { DisplayCurrency } from "@/lib/currency";
import type { ExchangeRates } from "@/types/api";

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
  initialCurrency,
  initialRates,
  children,
}: {
  initialSession: Session | null;
  initialCurrency: DisplayCurrency;
  initialRates: ExchangeRates | null;
  children: React.ReactNode;
}) {
  const queryClient = getQueryClient();

  return (
    <ThemeProvider attribute="class" defaultTheme="system" enableSystem disableTransitionOnChange>
      <QueryClientProvider client={queryClient}>
        <SessionProvider initialSession={initialSession}>
          <RealtimeProvider>
            <CurrencyProvider initialCurrency={initialCurrency} initialRates={initialRates}>
              {children}
              <Toaster />
            </CurrencyProvider>
          </RealtimeProvider>
        </SessionProvider>
      </QueryClientProvider>
    </ThemeProvider>
  );
}
