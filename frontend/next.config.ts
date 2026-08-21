import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Emits a self-contained server bundle, so the runtime image needs neither node_modules
  // nor a package install.
  output: "standalone",
};

export default nextConfig;
