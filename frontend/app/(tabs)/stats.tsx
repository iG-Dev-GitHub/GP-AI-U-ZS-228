import { Ionicons } from "@expo/vector-icons";
import { ScrollView, StyleSheet, Text, View } from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";

import { AssetImage } from "@/src/components/AssetImage";
import { useAppState } from "@/src/store/state";
import { DAY_FORMATS } from "@/src/utils/formats";

export default function Stats() {
  const { streak, badges, history } = useAppState();

  return (
    <SafeAreaView style={styles.container} edges={["top"]} testID="stats-screen">
      <ScrollView contentContainerStyle={styles.scroll}>
        <Text style={styles.kicker}>Your Progress</Text>
        <Text style={styles.h1}>Stats</Text>

        <View style={styles.metricsRow}>
          <Metric label="Day Clear" value={badges.dayClearCount} icon="checkmark-circle" color="#10B981" testID="stat-day-clear" />
          <Metric label="Streak" value={streak} icon="flame" color="#F59E0B" testID="stat-streak" />
          <Metric label="Sprints Won" value={badges.fullSprintWins} icon="flash" color="#EF4444" testID="stat-sprints" />
        </View>

        <Text style={styles.section}>Badges</Text>
        <View style={styles.badgesRow}>
          <BadgeTile
            unlocked={badges.dayClearCount > 0}
            asset="badge-day-clear"
            title="Day Clear"
            testID="badge-day-clear"
          />
          <BadgeTile
            unlocked={badges.onFire}
            asset="badge-on-fire"
            title="On Fire"
            testID="badge-on-fire"
          />
          <BadgeTile
            unlocked={badges.weekChampion}
            asset="badge-week-champion"
            title="Week Champ"
            testID="badge-week-champion"
          />
        </View>

        <Text style={styles.section}>History</Text>
        {history.length === 0 ? (
          <Text style={styles.empty}>No completed days yet. Keep going.</Text>
        ) : (
          history.slice(0, 30).map((h) => {
            const fmt = DAY_FORMATS[h.formatId];
            return (
              <View key={h.date} style={styles.historyRow} testID={`history-${h.date}`}>
                <View style={[styles.historyDot, { backgroundColor: fmt.color }]} />
                <View style={{ flex: 1 }}>
                  <Text style={styles.historyTitle}>{fmt.title}</Text>
                  <Text style={styles.historyDate}>{h.date}</Text>
                </View>
                <Text style={styles.historyTasks}>
                  {h.doneTasks}/{h.totalTasks}
                </Text>
                {h.cleared ? (
                  <Ionicons name="checkmark-circle" color="#10B981" size={22} />
                ) : (
                  <Ionicons name="ellipse-outline" color="#475569" size={22} />
                )}
              </View>
            );
          })
        )}
      </ScrollView>
    </SafeAreaView>
  );
}

function Metric({
  label,
  value,
  icon,
  color,
  testID,
}: {
  label: string;
  value: number;
  icon: keyof typeof Ionicons.glyphMap;
  color: string;
  testID: string;
}) {
  return (
    <View testID={testID} style={styles.metric}>
      <Ionicons name={icon} color={color} size={22} />
      <Text style={styles.metricValue}>{value}</Text>
      <Text style={styles.metricLabel}>{label}</Text>
    </View>
  );
}

function BadgeTile({
  unlocked,
  asset,
  title,
  testID,
}: {
  unlocked: boolean;
  asset: string;
  title: string;
  testID: string;
}) {
  return (
    <View testID={testID} style={[styles.badgeTile, !unlocked && styles.badgeLocked]}>
      <AssetImage
        name={asset}
        size={64}
        fallback={<Ionicons name="trophy" color={unlocked ? "#FACC15" : "#475569"} size={28} />}
      />
      <Text style={[styles.badgeTitle, !unlocked && { color: "#64748B" }]}>{title}</Text>
      {!unlocked ? <Text style={styles.badgeLockText}>Locked</Text> : null}
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: "#090A10" },
  scroll: { padding: 20, paddingBottom: 60 },
  kicker: {
    color: "#64748B",
    fontSize: 11,
    fontWeight: "800",
    letterSpacing: 2,
    textTransform: "uppercase",
  },
  h1: { color: "#FFFFFF", fontSize: 30, fontWeight: "900", marginTop: 2 },
  metricsRow: { flexDirection: "row", gap: 10, marginTop: 18 },
  metric: {
    flex: 1,
    backgroundColor: "#1E243A",
    padding: 14,
    borderRadius: 16,
    alignItems: "flex-start",
  },
  metricValue: {
    color: "#FFFFFF",
    fontSize: 28,
    fontWeight: "900",
    marginTop: 8,
  },
  metricLabel: {
    color: "#94A3B8",
    fontSize: 11,
    fontWeight: "700",
    letterSpacing: 1,
    textTransform: "uppercase",
    marginTop: 2,
  },
  section: {
    color: "#94A3B8",
    fontSize: 12,
    fontWeight: "800",
    letterSpacing: 2,
    textTransform: "uppercase",
    marginTop: 24,
    marginBottom: 10,
  },
  badgesRow: { flexDirection: "row", gap: 10 },
  badgeTile: {
    flex: 1,
    backgroundColor: "#1E243A",
    padding: 14,
    borderRadius: 16,
    alignItems: "center",
  },
  badgeLocked: { opacity: 0.5 },
  badgeTitle: {
    color: "#FFFFFF",
    fontSize: 13,
    fontWeight: "800",
    marginTop: 8,
    textAlign: "center",
  },
  badgeLockText: { color: "#64748B", fontSize: 10, marginTop: 2 },
  historyRow: {
    flexDirection: "row",
    alignItems: "center",
    backgroundColor: "#1E243A",
    paddingVertical: 12,
    paddingHorizontal: 14,
    borderRadius: 12,
    marginBottom: 8,
    gap: 12,
  },
  historyDot: { width: 10, height: 10, borderRadius: 5 },
  historyTitle: { color: "#FFFFFF", fontSize: 14, fontWeight: "800" },
  historyDate: { color: "#94A3B8", fontSize: 11, marginTop: 2 },
  historyTasks: { color: "#FACC15", fontSize: 14, fontWeight: "900" },
  empty: { color: "#64748B", textAlign: "center", marginTop: 12 },
});
