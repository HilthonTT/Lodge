// Captures the README screenshots (PNG) and walkthrough GIFs from a running Lodge stack.
//
//   docker compose up -d --build        # from the repo root; seeds demo data
//   cd scripts/screenshots && npm install && npm run capture
//
// Environment:
//   LODGE_WEB_URL   web app URL           (default http://localhost:3000)
//   BROWSER_CHANNEL Playwright channel     (default "msedge"; use "chromium" after `npx playwright install chromium`)
//   OUT_DIR         output directory       (default ../../docs/images)

import { mkdir, writeFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "playwright";
import { PNG } from "pngjs";
import gifenc from "gifenc";

const { GIFEncoder, quantize, applyPalette } = gifenc;

const here = path.dirname(fileURLToPath(import.meta.url));
const BASE = process.env.LODGE_WEB_URL ?? "http://localhost:3000";
const OUT = path.resolve(here, process.env.OUT_DIR ?? "../../docs/images");
const CHANNEL = process.env.BROWSER_CHANNEL ?? "msedge";
const DEMO = { email: "demo@lodge.dev", password: "Demo123!" };
const ADMIN = { email: "admin@lodge.dev", password: "Admin123!" };

const DESKTOP = { width: 1440, height: 900 };
const GIF_VIEWPORT = { width: 1200, height: 750 };
const MOBILE = { width: 390, height: 844 };

// ---------------------------------------------------------------- helpers

/** Wait until the page has settled: network idle, fonts and visible images loaded. */
async function settle(page, extraMs = 300) {
  await page.waitForLoadState("networkidle", { timeout: 5_000 }).catch(() => {});
  // Wait for loading skeletons (streamed server components) to be replaced by content.
  await page
    .waitForFunction(() => !document.querySelector('[data-slot="skeleton"]'), null, { timeout: 10_000 })
    .catch(() => {});
  // Only wait for images inside the viewport: lazy offscreen images never fire load events.
  await page
    .evaluate(async () => {
      await document.fonts.ready;
      const inView = (img) => {
        const r = img.getBoundingClientRect();
        return r.bottom > 0 && r.top < innerHeight && r.width > 0;
      };
      await Promise.race([
        Promise.all(
          [...document.images]
            .filter((img) => !img.complete && inView(img))
            .map((img) => new Promise((r) => img.addEventListener("load", r) || img.addEventListener("error", r))),
        ),
        new Promise((r) => setTimeout(r, 5_000)),
      ]);
    })
    .catch(() => {});
  await page.waitForTimeout(extraMs);
}

async function shot(page, name, options = {}) {
  await settle(page);
  const file = path.join(OUT, `${name}.png`);
  await page.screenshot({ path: file, ...options });
  console.log("  ✓", path.relative(process.cwd(), file));
}

async function signIn(page, account = DEMO) {
  await page.goto(`${BASE}/login`);
  await page.locator("#email").fill(account.email);
  await page.locator("#password").fill(account.password);
  await page.getByRole("button", { name: "Sign in" }).click();
  await page.waitForURL((url) => !url.pathname.startsWith("/login"));
  await settle(page);
}

async function firstApartmentHref(page) {
  await page.goto(BASE);
  return page.locator("main a[href^='/apartments/']").first().getAttribute("href");
}

/** Finds a check-in date ≥ `minDaysAhead` with `nights` consecutive free nights in the open calendar. */
async function findFreeRange(page, minDaysAhead, nights) {
  const days = await page
    .locator("[data-day]")
    .evaluateAll((cells) => cells.map((c) => ({ day: c.dataset.day, disabled: c.dataset.disabled === "true" })));
  const free = new Set(days.filter((d) => !d.disabled).map((d) => d.day));
  const iso = (d) => d.toISOString().slice(0, 10);
  const start = new Date();
  start.setUTCDate(start.getUTCDate() + minDaysAhead);
  for (let i = 0; i < 40; i++) {
    const from = new Date(start);
    from.setUTCDate(from.getUTCDate() + i);
    const range = Array.from({ length: nights + 1 }, (_, n) => {
      const d = new Date(from);
      d.setUTCDate(d.getUTCDate() + n);
      return iso(d);
    });
    if (range.every((d) => free.has(d))) return { from: range[0], to: range.at(-1) };
  }
  throw new Error("No free date range found in the visible months");
}

const pickDay = (page, day) => page.locator(`[data-day="${day}"] button`).first().click();

/** Records a GIF by taking a screenshot after each step (frames hold for `ms`). */
class GifRecorder {
  frames = [];
  constructor(page) { this.page = page; }

  async snap(ms = 120) {
    const buf = await this.page.screenshot();
    this.frames.push({ buf, ms });
  }

  async hold(ms) {
    await settle(this.page, 150);
    await this.snap(ms);
  }

  /** Types text character by character, capturing a frame every few keystrokes. */
  async type(locator, text, every = 2) {
    await locator.click();
    for (let i = 0; i < text.length; i++) {
      await locator.press(text[i] === " " ? "Space" : text[i]);
      if (i % every === every - 1 || i === text.length - 1) await this.snap(90);
    }
  }

  /** Smoothly scrolls the page by `dy` pixels over `steps` frames. */
  async scroll(dy, steps = 6) {
    for (let i = 0; i < steps; i++) {
      await this.page.mouse.wheel(0, dy / steps);
      await this.page.waitForTimeout(80);
      await this.snap(70);
    }
  }

  async save(name) {
    const gif = GIFEncoder();
    for (const { buf, ms } of this.frames) {
      const { data, width, height } = PNG.sync.read(buf);
      const palette = quantize(data, 256, { format: "rgb565" });
      const index = applyPalette(data, palette, "rgb565");
      gif.writeFrame(index, width, height, { palette, delay: ms });
    }
    gif.finish();
    const file = path.join(OUT, `${name}.gif`);
    await writeFile(file, gif.bytes());
    console.log("  ✓", path.relative(process.cwd(), file), `(${this.frames.length} frames)`);
  }
}

// ---------------------------------------------------------------- captures

async function screenshots(browser) {
  console.log("Screenshots");

  // Public pages, light theme.
  {
    const ctx = await browser.newContext({ viewport: DESKTOP, colorScheme: "light" });
    const page = await ctx.newPage();

    await page.goto(BASE);
    await shot(page, "home");

    await page.goto(`${BASE}/search?guests=4&sort=price-desc`);
    await shot(page, "search");

    const href = await firstApartmentHref(page);
    await page.goto(`${BASE}${href}`);
    await page.getByRole("button", { name: /check-in and check-out/i }).click();
    const { from, to } = await findFreeRange(page, 21, 4);
    await page.keyboard.press("Escape");
    await page.goto(`${BASE}${href}?startDate=${from}&endDate=${to}&guests=2`);
    await shot(page, "apartment");
    // Second shot scrolled to the booking panel with the price breakdown.
    await page.evaluate(() => {
      document.querySelector("#booking")?.scrollIntoView({ block: "start" });
      window.scrollBy(0, -110);
    });
    await shot(page, "apartment-booking");

    await page.goto(`${BASE}/login`);
    await shot(page, "login");

    await page.goto(`${BASE}/this-page-does-not-exist`);
    await shot(page, "not-found");

    await ctx.close();
  }

  // Signed-in pages.
  {
    const ctx = await browser.newContext({ viewport: DESKTOP, colorScheme: "light" });
    const page = await ctx.newPage();
    await signIn(page);

    await page.goto(`${BASE}/bookings`);
    await shot(page, "bookings");

    await page.goto(`${BASE}/settings`);
    await shot(page, "settings");

    await ctx.close();
  }

  // Admin pages.
  {
    const ctx = await browser.newContext({ viewport: DESKTOP, colorScheme: "light" });
    const page = await ctx.newPage();
    await signIn(page, ADMIN);

    await page.goto(`${BASE}/admin/bookings`);
    await shot(page, "admin-bookings");

    await ctx.close();
  }

  // Dark theme.
  {
    const ctx = await browser.newContext({ viewport: DESKTOP, colorScheme: "dark" });
    const page = await ctx.newPage();
    await page.goto(BASE);
    await shot(page, "home-dark");
    const href = await firstApartmentHref(page);
    await page.goto(`${BASE}${href}`);
    await shot(page, "apartment-dark");
    await ctx.close();
  }

  // Mobile.
  {
    const ctx = await browser.newContext({
      viewport: MOBILE,
      deviceScaleFactor: 2,
      isMobile: true,
      hasTouch: true,
      colorScheme: "light",
    });
    const page = await ctx.newPage();
    await page.goto(BASE);
    await shot(page, "mobile-home");
    const href = await firstApartmentHref(page);
    await page.goto(`${BASE}${href}`);
    await shot(page, "mobile-apartment");
    await ctx.close();
  }
}

async function gifs(browser) {
  console.log("GIFs");

  // 1. Search: open the search dialog, type a destination, browse results.
  {
    const ctx = await browser.newContext({ viewport: GIF_VIEWPORT, colorScheme: "light" });
    const page = await ctx.newPage();
    const rec = new GifRecorder(page);
    await page.goto(BASE);
    await rec.hold(1200);
    await page.getByRole("button", { name: "Open search" }).click();
    await rec.hold(700);
    const destination = page.getByRole("dialog").getByRole("textbox").first();
    await rec.type(destination, "United");
    await rec.hold(600);
    await page.getByRole("dialog").getByRole("button", { name: /^search$/i }).click();
    await page.waitForURL(/\/search/);
    await rec.hold(1500);
    await rec.scroll(300);
    await rec.hold(1500);
    await rec.save("search-flow");
    await ctx.close();
  }

  // 2. Booking: sign in, pick dates on an apartment, review the price, reserve.
  {
    const ctx = await browser.newContext({ viewport: GIF_VIEWPORT, colorScheme: "light" });
    const page = await ctx.newPage();
    const rec = new GifRecorder(page);
    await signIn(page);
    await page.goto(BASE);
    // Use a different apartment than the screenshots for variety.
    const hrefs = await page.locator("main a[href^='/apartments/']").evaluateAll((as) => [...new Set(as.map((a) => a.getAttribute("href")))]);
    const target = hrefs[Math.min(3, hrefs.length - 1)];
    await page.goto(`${BASE}${target}`);
    await rec.hold(1300);
    await page.getByRole("button", { name: /check-in and check-out/i }).click();
    await rec.hold(800);
    const { from, to } = await findFreeRange(page, 35, 3);
    await pickDay(page, from);
    await rec.hold(500);
    await pickDay(page, to);
    await rec.hold(1500);
    const reserve = page.getByRole("button", { name: /^reserve$/i }).first();
    await reserve.hover();
    await rec.hold(700);
    await reserve.click();
    await page.waitForURL(/\/stays\//);
    await rec.hold(1800);
    await rec.scroll(250, 4);
    await rec.hold(1800);
    await rec.save("booking-flow");
    await ctx.close();
  }

  // 3. Dark mode toggle.
  {
    const ctx = await browser.newContext({ viewport: GIF_VIEWPORT, colorScheme: "light" });
    const page = await ctx.newPage();
    const rec = new GifRecorder(page);
    await page.goto(BASE);
    await rec.hold(1000);
    await page.getByRole("button", { name: "Change theme" }).click();
    await rec.hold(500);
    await page.getByRole("menuitemradio", { name: /dark/i }).or(page.getByRole("menuitem", { name: /dark/i })).first().click();
    await rec.hold(1500);
    await rec.scroll(450);
    await rec.hold(1200);
    await rec.save("dark-mode");
    await ctx.close();
  }
}

// ---------------------------------------------------------------- main

await mkdir(OUT, { recursive: true });
const browser = await chromium.launch({ channel: CHANNEL === "chromium" ? undefined : CHANNEL });
try {
  const only = process.argv[2];
  if (!only || only === "screenshots") await screenshots(browser);
  if (!only || only === "gifs") await gifs(browser);
} finally {
  await browser.close();
}
