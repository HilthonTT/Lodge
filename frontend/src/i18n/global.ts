import type en from "../../messages/en.json";
import type fr from "../../messages/fr.json";
import type nl from "../../messages/nl.json";

import type { Locale } from "./config";

/**
 * Typed messages: `t("missing.key")` fails `npm run typecheck`.
 * English is the source of truth for keys.
 */
declare module "next-intl" {
  interface AppConfig {
    Locale: Locale;
    Messages: typeof en;
  }
}

/*
 * Every locale file must have exactly the same keys as `en.json`. If one of
 * these lines errors, a key is missing (or extra) in that locale. Run
 * `node scripts/check-messages.mjs` for a readable list.
 */
type Exact<A, B> = [A] extends [B] ? ([B] extends [A] ? true : false) : false;
const nlMatchesEn: Exact<typeof nl, typeof en> = true;
const frMatchesEn: Exact<typeof fr, typeof en> = true;
export { frMatchesEn as _frMatchesEn, nlMatchesEn as _nlMatchesEn };
