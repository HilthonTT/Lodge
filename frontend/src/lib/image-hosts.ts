/**
 * Hosts whose images are served through the Next.js image optimizer.
 * Shared by `next.config.ts` (remotePatterns) and the `RemoteImage` component,
 * which falls back to an unoptimized <img> for any other host instead of crashing.
 */

type RemotePattern = {
  protocol: "http" | "https";
  hostname: string;
  port?: string;
};

/** Hosts used by the API's seed data for apartment pictures. */
const STATIC_IMAGE_HOSTS: RemotePattern[] = [
  { protocol: "https", hostname: "images.unsplash.com" },
  { protocol: "https", hostname: "www.thesprucecrafts.com" },
];

function patternFromUrl(value: string | undefined): RemotePattern | null {
  if (!value) return null;
  try {
    const url = new URL(value);
    const protocol = url.protocol.replace(":", "");
    if (protocol !== "http" && protocol !== "https") return null;
    return { protocol, hostname: url.hostname, port: url.port || undefined };
  } catch {
    return null;
  }
}

export function getImageRemotePatterns(apiBaseUrl: string | undefined): RemotePattern[] {
  const api = patternFromUrl(apiBaseUrl);
  return api ? [...STATIC_IMAGE_HOSTS, api] : STATIC_IMAGE_HOSTS;
}

export function isOptimizableImage(src: string, apiBaseUrl: string | undefined): boolean {
  if (src.startsWith("/")) return true;
  const target = patternFromUrl(src);
  if (!target) return false;
  // Local/private hosts are blocked by the Next 16 optimizer by default.
  if (isPrivateHost(target.hostname)) return false;
  return getImageRemotePatterns(apiBaseUrl).some(
    (p) =>
      p.protocol === target.protocol &&
      p.hostname === target.hostname &&
      (p.port ?? "") === (target.port ?? ""),
  );
}

export function isPrivateHost(hostname: string): boolean {
  return (
    hostname === "localhost" ||
    hostname.endsWith(".localhost") ||
    hostname === "::1" ||
    hostname === "[::1]" ||
    /^127\./.test(hostname) ||
    /^10\./.test(hostname) ||
    /^192\.168\./.test(hostname) ||
    /^172\.(1[6-9]|2\d|3[01])\./.test(hostname) ||
    !hostname.includes(".")
  );
}
