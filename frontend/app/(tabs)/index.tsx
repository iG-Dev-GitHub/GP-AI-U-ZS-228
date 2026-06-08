import { Ionicons } from "@expo/vector-icons";
import { useRouter } from "expo-router";
import { useEffect, useMemo, useState } from "react";
import {
  Dimensions,
  FlatList,
  StyleSheet,
  Text,
  TouchableOpacity,
  View,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";

import { AddTaskSheet } from "@/src/components/AddTaskSheet";
import { DayFormatBadge } from "@/src/components/DayFormatBadge";
import { PlinkoBoard } from "@/src/components/PlinkoBoard";
import { TaskCard } from "@/src/components/TaskCard";
import {
  addTask,
  computeCleared,
  recordDayClear,
  removeTask,
  toggleTask,
  useAppState,
} from "@/src/store/state";
import { DAY_FORMATS } from "@/src/utils/formats";
import { isMorning } from "@/src/utils/date";

const { width: SCREEN_W } = Dimensions.get("window");

export default function Today() {
  const router = useRouter();
  const { tasks, dayFormat } = useAppState();
  const [showAdd, setShowAdd] = useState(false);

  const evening = !isMorning();
  const done = useMemo(() => tasks.filter((t) => t.done).length, [tasks]);
  const total = tasks.length;

  // Auto record day clear when condition is met.
  useEffect(() => {
    if (!dayFormat) return;
    if (computeCleared(dayFormat, total, done)) {
      recordDayClear(dayFormat);
    }
  }, [dayFormat, total, done]);

  const fmt = dayFormat ? DAY_FORMATS[dayFormat] : null;
  const requiredText = fmt
    ? fmt.required === "all"
      ? `${done}/${total} (all)`
      : `${done}/${fmt.required}`
    : `${done}/${total}`;

  return (
    <SafeAreaView style={styles.container} edges={["top"]} testID="today-screen">
      <FlatList
        data={tasks}
        keyExtractor={(item) => item.id}
        contentContainerStyle={styles.list}
        ListHeaderComponent={
          <View>
            <View style={styles.headerRow}>
              <View>
                <Text style={styles.kicker}>{evening ? "Evening" : "Morning"} session</Text>
                <Text style={styles.h1}>{fmt ? fmt.title : "Today"}</Text>
              </View>
              <View style={styles.progressPill}>
                <Text style={styles.progressText}>{requiredText}</Text>
              </View>
            </View>

            {fmt ? (
              <View style={{ marginTop: 14 }}>
                <DayFormatBadge formatId={fmt.id} testID="today-format-card" />
              </View>
            ) : (
              <View style={styles.boardWrap}>
                <PlinkoBoard
                  width={SCREEN_W - 40}
                  height={(SCREEN_W - 40) * 0.7}
                  targetCell={null}
                  animateDrop={false}
                />
              </View>
            )}

            <View style={styles.ctaRow}>
              {fmt && fmt.id === "full-sprint" ? (
                <TouchableOpacity
                  testID="open-full-sprint-button"
                  style={[styles.cta, { backgroundColor: "#EF4444" }]}
                  onPress={() => router.push("/full-sprint")}
                >
                  <Ionicons name="flame" color="#FFFFFF" size={18} />
                  <Text style={styles.ctaText}>Full Sprint</Text>
                </TouchableOpacity>
              ) : (
                <TouchableOpacity
                  testID="drop-for-today-button"
                  style={[
                    styles.cta,
                    { backgroundColor: dayFormat ? "#1E243A" : "#FACC15" },
                  ]}
                  onPress={() => router.push("/drop")}
                  disabled={!!dayFormat}
                >
                  <Ionicons
                    name={dayFormat ? "checkmark-circle" : "rocket"}
                    color={dayFormat ? "#94A3B8" : "#0B1020"}
                    size={18}
                  />
                  <Text
                    style={[
                      styles.ctaText,
                      { color: dayFormat ? "#94A3B8" : "#0B1020" },
                    ]}
                  >
                    {dayFormat ? "Day Format Set" : "Drop for Today"}
                  </Text>
                </TouchableOpacity>
              )}
              <TouchableOpacity
                testID="add-task-button"
                style={styles.addBtn}
                onPress={() => setShowAdd(true)}
                disabled={tasks.length >= 10}
              >
                <Ionicons name="add" color="#FFFFFF" size={22} />
              </TouchableOpacity>
            </View>

            <Text style={styles.sectionTitle}>Tasks ({tasks.length}/10)</Text>
          </View>
        }
        renderItem={({ item, index }) => (
          <TaskCard
            task={item}
            evening={evening}
            onToggle={() => toggleTask(item.id)}
            onDelete={() => removeTask(item.id)}
            testID={`task-${index}`}
          />
        )}
        ListEmptyComponent={
          <View style={styles.empty}>
            <Ionicons name="list-outline" size={40} color="#475569" />
            <Text style={styles.emptyText}>
              No tasks yet. Tap + to add up to 10 tasks for today.
            </Text>
          </View>
        }
      />

      <AddTaskSheet
        visible={showAdd}
        onClose={() => setShowAdd(false)}
        onSubmit={(t, p) => addTask(t, p)}
      />
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: "#090A10" },
  list: { padding: 20, paddingBottom: 60 },
  headerRow: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
  },
  kicker: {
    color: "#64748B",
    fontSize: 11,
    fontWeight: "800",
    letterSpacing: 2,
    textTransform: "uppercase",
  },
  h1: { color: "#FFFFFF", fontSize: 30, fontWeight: "900", marginTop: 2 },
  progressPill: {
    backgroundColor: "#1E243A",
    paddingHorizontal: 12,
    paddingVertical: 8,
    borderRadius: 999,
  },
  progressText: { color: "#FACC15", fontSize: 14, fontWeight: "900" },
  boardWrap: { marginTop: 14, alignItems: "center" },
  ctaRow: {
    flexDirection: "row",
    gap: 10,
    marginTop: 16,
    marginBottom: 18,
  },
  cta: {
    flex: 1,
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "center",
    gap: 8,
    paddingVertical: 16,
    borderRadius: 999,
  },
  ctaText: { color: "#0B1020", fontSize: 16, fontWeight: "900" },
  addBtn: {
    width: 56,
    height: 56,
    borderRadius: 28,
    backgroundColor: "#1E243A",
    alignItems: "center",
    justifyContent: "center",
  },
  sectionTitle: {
    color: "#94A3B8",
    fontSize: 12,
    fontWeight: "800",
    letterSpacing: 2,
    textTransform: "uppercase",
    marginBottom: 10,
  },
  empty: { alignItems: "center", paddingVertical: 40 },
  emptyText: {
    color: "#64748B",
    fontSize: 14,
    marginTop: 10,
    textAlign: "center",
    paddingHorizontal: 30,
  },
});
