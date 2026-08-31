import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Next 16 writes its own CLAUDE.md / AGENTS.md into frontend/ on every dev
  // start. A nested CLAUDE.md shadows the repo-root one for anything working in
  // this directory, so the generator is off and the root file governs.
  agentRules: false,
  /* config options here */
};

export default nextConfig;
