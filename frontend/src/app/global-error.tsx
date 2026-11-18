"use client";

import { useSyncExternalStore } from "react";

// Last-resort boundary for errors thrown in the root layout. It replaces the
// whole document, so it can't rely on the app's providers (next-intl included)
// or stylesheet. Its three strings therefore live here instead of messages/*.json.
const COPY = {
  en: {
    title: "Something went wrong",
    description: "Lodge hit an unexpected error. Please try again.",
    retry: "Try again",
  },
  nl: {
    title: "Er ging iets mis",
    description: "Lodge liep tegen een onverwachte fout aan. Probeer het opnieuw.",
    retry: "Opnieuw proberen",
  },
  fr: {
    title: "Une erreur s’est produite",
    description: "Lodge a rencontré une erreur inattendue. Veuillez réessayer.",
    retry: "Réessayer",
  },
} as const;

type CopyLocale = keyof typeof COPY;

function readLocale(): CopyLocale {
  const fromCookie = /(?:^|;\s*)NEXT_LOCALE=([^;]+)/.exec(document.cookie)?.[1];
  const candidate = fromCookie ?? navigator.language.slice(0, 2);
  return candidate in COPY ? (candidate as CopyLocale) : "en";
}

const noopSubscribe = () => () => {};

export default function GlobalError({ retry }: { error: Error & { digest?: string }; retry: () => void }) {
  const locale = useSyncExternalStore(noopSubscribe, readLocale, () => "en" as const);
  const copy = COPY[locale];

  return (
    <html lang={locale}>
      <body
        style={{
          margin: 0,
          minHeight: "100dvh",
          display: "grid",
          placeItems: "center",
          fontFamily: "system-ui, sans-serif",
          background: "#fcfcfa",
          color: "#1f2430",
        }}
      >
        <main style={{ textAlign: "center", padding: 24, maxWidth: 420 }}>
          <h1 style={{ fontSize: 24, marginBottom: 8 }}>{copy.title}</h1>
          <p style={{ color: "#5f6673", marginBottom: 24 }}>{copy.description}</p>
          <button
            type="button"
            onClick={() => retry()}
            style={{
              background: "#2f7d63",
              color: "white",
              border: 0,
              borderRadius: 10,
              padding: "10px 18px",
              fontSize: 14,
              cursor: "pointer",
            }}
          >
            {copy.retry}
          </button>
        </main>
      </body>
    </html>
  );
}
