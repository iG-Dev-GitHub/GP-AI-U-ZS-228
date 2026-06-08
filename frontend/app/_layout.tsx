import { Stack } from "expo-router";
import { SafeAreaProvider } from "react-native-safe-area-context";
import { StatusBar } from "expo-status-bar";
import * as SplashScreen from "expo-splash-screen";
import { useEffect } from "react";

import { useIconFonts } from "@/src/hooks/use-icon-fonts";
import { loadState, useAppState } from "@/src/store/state";
import { useEnsureAssets } from "@/src/utils/assets";

// Keep the native splash visible from cold start until icon fonts register.
SplashScreen.preventAutoHideAsync();

export default function RootLayout() {
  const [loaded, error] = useIconFonts();
  const { ready } = useAppState();

  useEffect(() => {
    loadState();
  }, []);

  useEnsureAssets();

  useEffect(() => {
    if ((loaded || error) && ready) {
      SplashScreen.hideAsync();
    }
  }, [loaded, error, ready]);

  if ((!loaded && !error) || !ready) return null;

  return (
    <SafeAreaProvider>
      <StatusBar style="light" />
      <Stack screenOptions={{ headerShown: false, contentStyle: { backgroundColor: "#090A10" } }}>
        <Stack.Screen name="index" />
        <Stack.Screen name="welcome" options={{ animation: "fade" }} />
        <Stack.Screen name="(tabs)" />
        <Stack.Screen name="drop" options={{ presentation: "modal", animation: "fade" }} />
        <Stack.Screen name="full-sprint" options={{ presentation: "modal", animation: "fade" }} />
      </Stack>
    </SafeAreaProvider>
  );
}
