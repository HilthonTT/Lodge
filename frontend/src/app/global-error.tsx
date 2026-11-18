"use client";

// Last-resort boundary for errors thrown in the root layout. It replaces the
// whole document, so it can't rely on the app's providers or stylesheet.
export default function GlobalError({ retry }: { error: Error & { digest?: string }; retry: () => void }) {
  return (
    <html lang="en">
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
          <h1 style={{ fontSize: 24, marginBottom: 8 }}>Something went wrong</h1>
          <p style={{ color: "#5f6673", marginBottom: 24 }}>
            Lodge hit an unexpected error. Please try again.
          </p>
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
            Try again
          </button>
        </main>
      </body>
    </html>
  );
}
