"use client";

import { useCallback, useRef } from "react";

import { uuid } from "@/lib/uuid";

/**
 * One `X-Idempotency-Key` per distinct payload. The API replays the original
 * success for a repeated key and ignores the new body, so a retry of the same
 * request must reuse its key while a changed request (other dates, guests,
 * message…) needs a fresh one.
 */
export function useIdempotencyKey() {
  const keys = useRef(new Map<string, string>());
  return useCallback((payload: unknown) => {
    const fingerprint = JSON.stringify(payload);
    let key = keys.current.get(fingerprint);
    if (!key) {
      key = uuid();
      keys.current.set(fingerprint, key);
    }
    return key;
  }, []);
}
