import type { NextConfig } from "next";

import { getImageRemotePatterns } from "./src/lib/image-hosts";

const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:8000";

const nextConfig: NextConfig = {
  output: "standalone",
  reactStrictMode: true,
  poweredByHeader: false,
  // Don't let `next dev` generate/maintain an AGENTS.md file in the repo.
  agentRules: false,
  images: {
    remotePatterns: getImageRemotePatterns(apiBaseUrl).map((p) => ({
      protocol: p.protocol,
      hostname: p.hostname,
      port: p.port ?? "",
    })),
  },
  async headers() {
    return [
      {
        source: "/:path*",
        headers: [
          { key: "X-Content-Type-Options", value: "nosniff" },
          { key: "Referrer-Policy", value: "strict-origin-when-cross-origin" },
          { key: "X-Frame-Options", value: "DENY" },
        ],
      },
    ];
  },
};

export default nextConfig;
