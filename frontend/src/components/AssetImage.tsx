import { Image, View, StyleSheet } from "react-native";

import { assetUri, useAsset } from "@/src/utils/assets";

type Props = {
  name: string;
  size: number;
  fallback?: React.ReactNode;
  testID?: string;
};

export function AssetImage({ name, size, fallback, testID }: Props) {
  const b64 = useAsset(name);
  if (!b64) {
    return (
      <View
        testID={testID}
        style={[styles.fallback, { width: size, height: size, borderRadius: size / 2 }]}
      >
        {fallback}
      </View>
    );
  }
  return (
    <Image
      testID={testID}
      source={{ uri: assetUri(b64) }}
      style={{ width: size, height: size, resizeMode: "contain" }}
    />
  );
}

const styles = StyleSheet.create({
  fallback: {
    backgroundColor: "rgba(255,255,255,0.06)",
    alignItems: "center",
    justifyContent: "center",
  },
});
