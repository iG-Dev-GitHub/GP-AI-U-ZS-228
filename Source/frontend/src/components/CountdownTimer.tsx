import { useEffect, useState } from "react";
import { StyleSheet, Text } from "react-native";

import { fmtCountdown, secondsUntilHHmm } from "@/src/utils/date";

type Props = {
  targetHHmm: string;
  color?: string;
  size?: "lg" | "xl";
  testID?: string;
};

export function CountdownTimer({ targetHHmm, color = "#FFFFFF", size = "lg", testID }: Props) {
  const [secs, setSecs] = useState(secondsUntilHHmm(targetHHmm));
  useEffect(() => {
    setSecs(secondsUntilHHmm(targetHHmm));
    const id = setInterval(() => setSecs(secondsUntilHHmm(targetHHmm)), 1000);
    return () => clearInterval(id);
  }, [targetHHmm]);

  return (
    <Text
      testID={testID}
      style={[
        size === "xl" ? styles.xl : styles.lg,
        { color },
      ]}
    >
      {fmtCountdown(secs)}
    </Text>
  );
}

const styles = StyleSheet.create({
  lg: {
    fontSize: 40,
    fontWeight: "800",
    letterSpacing: 2,
    fontVariant: ["tabular-nums"],
  },
  xl: {
    fontSize: 72,
    fontWeight: "900",
    letterSpacing: 4,
    fontVariant: ["tabular-nums"],
  },
});
