// Hook for AI-generated assets. Fetches missing assets from the backend
// (which uses Gemini Nano Banana), caches them in AsyncStorage as data URIs.
// After first launch the app is fully offline.

import { useEffect, useState } from "react";

import { getState, saveAsset, useAppState } from "@/src/store/state";

const REQUIRED_ASSETS = [
  "ball-clock",
  "badge-day-clear",
  "badge-on-fire",
  "badge-week-champion",
  "card-steady-day",
  "card-quick-win",
  "card-deep-work",
  "card-full-sprint",
];

const BACKEND_URL = process.env.EXPO_PUBLIC_BACKEND_URL;

async function generate(name: string): Promise<string | null> {
  if (!BACKEND_URL) return null;
  try {
    const res = await fetch(`${BACKEND_URL}/api/assets/generate`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name }),
    });
    if (!res.ok) {
      console.warn(`[assets] generate ${name} failed:`, res.status);
      return null;
    }
    const data = await res.json();
    if (!data?.data) return null;
    const mime = data.mime_type || "image/png";
    // Store as a complete data URI so the rendering layer doesn't need to
    // know which mime type Gemini decided to return.
    return `data:${mime};base64,${data.data}`;
  } catch (e) {
    console.warn(`[assets] generate ${name} error`, e);
    return null;
  }
}

let inflight: Promise<void> | null = null;

export async function ensureAssets(): Promise<void> {
  if (inflight) return inflight;
  inflight = (async () => {
    for (const name of REQUIRED_ASSETS) {
      if (getState().assets[name]) continue;
      const data = await generate(name);
      if (data) await saveAsset(name, data);
    }
  })();
  try {
    await inflight;
  } finally {
    inflight = null;
  }
}

export function useAsset(name: string): string | null {
  const { assets } = useAppState();
  return assets[name] ?? null;
}

export function assetUri(stored: string | null): string | undefined {
  if (!stored) return undefined;
  // Back-compat: previous versions saved bare base64 with no prefix.
  if (stored.startsWith("data:")) return stored;
  return `data:image/png;base64,${stored}`;
}

export function useAssetsProgress() {
  const { assets } = useAppState();
  const have = REQUIRED_ASSETS.filter((k) => !!assets[k]).length;
  return { have, total: REQUIRED_ASSETS.length, done: have === REQUIRED_ASSETS.length };
}

// Triggers background generation on mount (fire-and-forget).
export function useEnsureAssets() {
  const [, force] = useState(0);
  useEffect(() => {
    let cancelled = false;
    ensureAssets().then(() => {
      if (!cancelled) force((n) => n + 1);
    });
    return () => {
      cancelled = true;
    };
  }, []);
}
