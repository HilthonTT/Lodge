import { cookies, headers } from "next/headers";
import { getRequestConfig } from "next-intl/server";

import { isLocale, LOCALE_COOKIE, negotiateLocale } from "./config";

/**
 * next-intl request config ("without i18n routing" setup): the locale comes from
 * the `NEXT_LOCALE` cookie set by the language switcher, falling back to the
 * browser's `Accept-Language`. URLs never carry a locale prefix.
 */
export default getRequestConfig(async () => {
  const cookieLocale = (await cookies()).get(LOCALE_COOKIE)?.value;
  const locale = isLocale(cookieLocale)
    ? cookieLocale
    : negotiateLocale((await headers()).get("accept-language"));

  return {
    locale,
    messages: (await import(`../../messages/${locale}.json`)).default,
  };
});
