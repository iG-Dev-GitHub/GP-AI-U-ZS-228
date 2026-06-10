import { Ionicons } from "@expo/vector-icons";
import { useRouter } from "expo-router";
import { useEffect, useMemo } from "react";
import {
  FlatList,
  StyleSheet,
  Text,
  TouchableOpacity,
  View,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";

import { CountdownTimer } from "@/src/components/CountdownTimer";
import { TaskCard } from "@/src/components/TaskCard";
import {
  computeCleared,
  recordDayClear,
  removeTask,
  toggleTask,
  useAppState,
} from "@/src/store/state";
import { secondsUntilHHmm } from "@/src/utils/date";

export default function FullSprint() {
  const router = useRouter();
  const { tasks, dayFormat, settings } = useAppState();
  const done = useMemo(() => tasks.filter((t) => t.done).length, [tasks]);
  const total = tasks.length;
  const cleared = dayFormat === "full-sprint" && computeCleared("full-sprint", total, done);
  const secsLeft = secondsUntilHHmm(settings.sprintDeadline);
  const expired = secsLeft <= 0;

  useEffect(() => {
    if (cleared) recordDayClear("full-sprint");
  }, [cleared]);

  return (
    <SafeAreaView style={styles.container} edges={["top", "bottom"]} testID="full-sprint-screen">
      <View style={styles.header}>
        <TouchableOpacity
          testID="full-sprint-close"
          onPress={() => router.back()}
          style={styles.iconBtn}
        >
          <Ionicons name="close" color="#FFFFFF" size={22} />
        </TouchableOpacity>
        <Text style={styles.kicker}>Full Sprint</Text>
        <View style={styles.iconBtn} />
      </View>

      <View style={styles.timerWrap}>
        <Text style={styles.timerLabel}>
          {expired
            ? "Sprint window closed"
            : `Finish all by ${settings.sprintDeadline}`}
        </Text>
        <CountdownTimer
          targetHHmm={settings.sprintDeadline}
          color={expired ? "#94A3B8" : "#FCA5A5"}
          size="xl"
          testID="sprint-countdown-timer"
        />
        <View style={styles.progress}>
          <View
            style={[
              styles.progressFill,
              {
                width: total === 0 ? "0%" : `${Math.min(100, (done / total) * 100)}%`,
              },
            ]}
          />
        </View>
        <Text style={styles.progressText}>
          {done}/{total} tasks complete
        </Text>
      </View>

      {cleared ? (
        <View testID="sprint-cleared-banner" style={styles.clearedBanner}>
          <Ionicons name="trophy" color="#0B1020" size={20} />
          <Text style={styles.clearedText}>Day Clear! Sprint won.</Text>
        </View>
      ) : null}

      <FlatList
        data={tasks}
        keyExtractor={(t) => t.id}
        contentContainerStyle={styles.list}
        renderItem={({ item, index }) => (
          <TaskCard
            task={item}
            onToggle={() => toggleTask(item.id)}
            onDelete={() => removeTask(item.id)}
            testID={`sprint-task-${index}`}
            evening
          />
        )}
        ListEmptyComponent={
          <Text style={styles.empty}>Add tasks on the Today tab first.</Text>
        }
      />
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: "#1B0606", paddingHorizontal: 16 },
  header: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    paddingVertical: 8,
  },
  iconBtn: { width: 40, height: 40, borderRadius: 20, alignItems: "center", justifyContent: "center" },
  kicker: {
    color: "#FCA5A5",
    fontSize: 12,
    fontWeight: "800",
    letterSpacing: 2,
    textTransform: "uppercase",
  },
  timerWrap: { alignItems: "center", paddingVertical: 18 },
  timerLabel: {
    color: "#FCA5A5",
    fontSize: 12,
    fontWeight: "800",
    letterSpacing: 2,
    textTransform: "uppercase",
    marginBottom: 6,
  },
  progress: {
    width: "100%",
    height: 8,
    backgroundColor: "#3B0A0A",
    borderRadius: 4,
    marginTop: 14,
    overflow: "hidden",
  },
  progressFill: { height: "100%", backgroundColor: "#EF4444" },
  progressText: { color: "#FCA5A5", fontSize: 12, marginTop: 6, fontWeight: "700" },
  clearedBanner: {
    backgroundColor: "#FACC15",
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "center",
    gap: 8,
    padding: 12,
    borderRadius: 12,
    marginBottom: 10,
  },
  clearedText: { color: "#0B1020", fontWeight: "900", fontSize: 15 },
  list: { paddingBottom: 40 },
  empty: { color: "#FCA5A5", textAlign: "center", marginTop: 20 },
});
