import { Redirect } from "expo-router";

import { useAppState } from "@/src/store/state";

export default function Index() {
  const { ready, onboarded } = useAppState();
  if (!ready) return null;
  if (!onboarded) return <Redirect href="/welcome" />;
  return <Redirect href="/(tabs)" />;
}
