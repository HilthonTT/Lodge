#!/usr/bin/env node
/**
 * Fails when the locale files in messages/ drift apart: every locale must have
 * exactly the same keys as en.json, no empty strings, and the same ICU
 * arguments ({count}, {name}, <link>…) per message. Runs as part of `npm run lint`.
 */
import { readdirSync, readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const dir = join(dirname(fileURLToPath(import.meta.url)), "..", "messages");
const SOURCE = "en";

function flatten(value, prefix = "", out = new Map()) {
  for (const [key, child] of Object.entries(value)) {
    const path = prefix ? `${prefix}.${key}` : key;
    if (child && typeof child === "object") flatten(child, path, out);
    else out.set(path, child);
  }
  return out;
}

/** Argument and tag names used by an ICU message, e.g. "count,name,<link>". */
function icuArguments(message) {
  if (typeof message !== "string") return "";
  const args = new Set();
  for (const match of message.matchAll(/\{\s*([A-Za-z0-9_]+)\s*[,}]/g)) args.add(match[1]);
  for (const match of message.matchAll(/<([A-Za-z0-9_]+)>/g)) args.add(`<${match[1]}>`);
  return [...args].sort().join(",");
}

const files = readdirSync(dir).filter((f) => f.endsWith(".json"));
const catalogs = new Map(
  files.map((f) => [f.replace(/\.json$/, ""), flatten(JSON.parse(readFileSync(join(dir, f), "utf8")))]),
);
const source = catalogs.get(SOURCE);
if (!source) {
  console.error(`messages/${SOURCE}.json is missing`);
  process.exit(1);
}

const problems = [];
for (const [locale, catalog] of catalogs) {
  for (const [key, value] of catalog) {
    if (typeof value !== "string" || value.trim() === "") problems.push(`${locale}: "${key}" is empty or not a string`);
  }
  if (locale === SOURCE) continue;
  for (const key of source.keys()) {
    if (!catalog.has(key)) problems.push(`${locale}: missing "${key}"`);
    else if (icuArguments(source.get(key)) !== icuArguments(catalog.get(key))) {
      problems.push(
        `${locale}: "${key}" uses {${icuArguments(catalog.get(key))}} but ${SOURCE} uses {${icuArguments(source.get(key))}}`,
      );
    }
  }
  for (const key of catalog.keys()) {
    if (!source.has(key)) problems.push(`${locale}: extra key "${key}" (not in ${SOURCE}.json)`);
  }
}

if (problems.length > 0) {
  console.error(`Locale files are out of sync (${problems.length} problem${problems.length === 1 ? "" : "s"}):`);
  for (const problem of problems) console.error(`  - ${problem}`);
  process.exit(1);
}
console.log(`messages: ${catalogs.size} locales, ${source.size} keys each, in sync.`);
