"use client";

import dynamic from "next/dynamic";

/** The URL fragment only exists in the browser, so the callback view skips server rendering. */
export const AuthCallback = dynamic(
  () => import("./auth-callback-view").then((m) => m.AuthCallbackView),
  { ssr: false },
);
