import type { NextConfig } from "next";

const LOCAL_CORE_API_BASE_URL = "http://localhost:5080";

function getCoreApiBaseUrl() {
  const configuredBaseUrl = process.env.CORE_API_BASE_URL?.trim();
  return (configuredBaseUrl || LOCAL_CORE_API_BASE_URL).replace(/\/+$/, "");
}

const nextConfig: NextConfig = {
  async rewrites() {
    return [
      {
        source: "/api/:path*",
        destination: `${getCoreApiBaseUrl()}/api/:path*`,
      },
    ];
  },
};

export default nextConfig;
