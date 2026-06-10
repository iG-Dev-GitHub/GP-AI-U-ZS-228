import { Ionicons } from "@expo/vector-icons";
import { StyleSheet, Text, View } from "react-native";

import { AssetImage } from "./AssetImage";
import { DAY_FORMATS } from "@/src/utils/formats";
import { DayFormatId } from "@/src/store/types";

type Props = {
  formatId: DayFormatId;
  compact?: boolean;
  testID?: string;
};

export function DayFormatBadge({ formatId, compact, testID }: Props) {
  const f = DAY_FORMATS[formatId];
  return (
    <View
      testID={testID}
      style={[
        styles.card,
        compact && styles.cardCompact,
        { borderColor: f.color, backgroundColor: f.color + "22" },
      ]}
    >
      <View style={[styles.iconWrap, { backgroundColor: f.color }]}>
        {compact ? (
          <Ionicons name="flash" size={20} color="#FFFFFF" />
        ) : (
          <AssetImage
            name={f.assetKey}
            size={56}
            fallback={<Ionicons name="flash" size={28} color="#FFFFFF" />}
          />
        )}
      </View>
      <View style={styles.body}>
        <Text style={[styles.title, { color: f.color }]}>{f.title}</Text>
        <Text style={styles.desc}>{f.description}</Text>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    flexDirection: "row",
    alignItems: "center",
    padding: 14,
    borderRadius: 16,
    borderWidth: 2,
    gap: 12,
  },
  cardCompact: {
    padding: 10,
  },
  iconWrap: {
    width: 64,
    height: 64,
    borderRadius: 14,
    alignItems: "center",
    justifyContent: "center",
  },
  body: { flex: 1 },
  title: {
    fontSize: 18,
    fontWeight: "900",
    letterSpacing: 0.5,
  },
  desc: {
    color: "#E2E8F0",
    fontSize: 13,
    marginTop: 2,
  },
});
