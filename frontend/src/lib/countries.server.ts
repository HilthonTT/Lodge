import "server-only";

import countries from "world-countries";

export type CountryInfo = { name: string; flag: string; latlng: [number, number] };

const normalize = (value: string) =>
  value
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .toLowerCase()
    .replace(/[^a-z]/g, "");

let index: Map<string, CountryInfo> | undefined;

function getIndex() {
  if (index) return index;
  index = new Map();
  for (const country of countries) {
    const [lat, lng] = country.latlng;
    if (lat === undefined || lng === undefined) continue;
    const info: CountryInfo = { name: country.name.common, flag: country.flag, latlng: [lat, lng] };
    const names = [country.name.common, country.name.official, country.cca2, country.cca3, ...country.altSpellings];
    for (const name of names) {
      const key = normalize(name);
      if (key && !index.has(key)) index.set(key, info);
    }
  }
  return index;
}

/**
 * Resolves a free-text country name (as stored by the API) to coordinates so the
 * listing can show an approximate location. Server-only: keeps the ~1 MB country
 * dataset out of the client bundle.
 */
export function findCountry(name: string | undefined | null): CountryInfo | null {
  if (!name) return null;
  const key = normalize(name);
  if (!key) return null;
  const map = getIndex();
  const exact = map.get(key);
  if (exact) return exact;
  // e.g. "Korea" -> "South Korea", "Congo" -> first Congo match
  for (const [candidate, info] of map) {
    if (candidate.length > 3 && (candidate.startsWith(key) || key.startsWith(candidate))) return info;
  }
  return null;
}
