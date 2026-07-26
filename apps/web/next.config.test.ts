import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const originalCoreApiBaseUrl = process.env.CORE_API_BASE_URL;

beforeEach(() => {
  delete process.env.CORE_API_BASE_URL;
  vi.resetModules();
});

afterEach(() => {
  if (originalCoreApiBaseUrl === undefined) delete process.env.CORE_API_BASE_URL;
  else process.env.CORE_API_BASE_URL = originalCoreApiBaseUrl;
  vi.resetModules();
});

async function loadRewrites() {
  const { default: config } = await import("./next.config");
  if (!config.rewrites) throw new Error("Next config must define rewrites");
  const rewrites = await config.rewrites();
  if (Array.isArray(rewrites)) return rewrites;
  return [...(rewrites.beforeFiles ?? []), ...(rewrites.afterFiles ?? []), ...(rewrites.fallback ?? [])];
}

describe("Next API rewrite", () => {
  it("rewrites same-origin API requests to the local Core API by default", async () => {
    const rewrites = await loadRewrites();

    expect(rewrites).toContainEqual({
      source: "/api/:path*",
      destination: "http://localhost:5080/api/:path*",
    });
  });

  it("uses a trimmed server-only Core API base URL when configured", async () => {
    process.env.CORE_API_BASE_URL = "https://core.example.test///";

    const rewrites = await loadRewrites();

    expect(rewrites).toContainEqual({
      source: "/api/:path*",
      destination: "https://core.example.test/api/:path*",
    });
  });
});
