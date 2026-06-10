import { Ionicons } from "@expo/vector-icons";
import { useState } from "react";
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
import { PlinkoBoard } from "@/src/components/PlinkoBoard";
import {
  addTomorrowTask,
  removeTomorrowTask,
  useAppState,
} from "@/src/store/state";

const { width: SCREEN_W } = Dimensions.get("window");

export default function Tomorrow() {
  const { tomorrowTasks } = useAppState();
  const [showAdd, setShowAdd] = useState(false);

  return (
    <SafeAreaView style={styles.container} edges={["top"]} testID="tomorrow-screen">
      <FlatList
        data={tomorrowTasks}
        keyExtractor={(t) => t.id}
        contentContainerStyle={styles.list}
        ListHeaderComponent={
          <View>
            <Text style={styles.kicker}>Pre-plan</Text>
            <Text style={styles.h1}>Tomorrow</Text>
            <Text style={styles.sub}>
              Stage tasks for tomorrow. They will merge with carried-over tasks at midnight.
            </Text>

            <View style={styles.boardWrap}>
              <PlinkoBoard
                width={SCREEN_W - 40}
                height={(SCREEN_W - 40) * 0.7}
                targetCell={null}
                animateDrop={false}
              />
              <View style={styles.inactiveOverlay}>
                <Ionicons name="lock-closed" color="#94A3B8" size={22} />
                <Text style={styles.inactiveText}>Drop unlocks tomorrow morning</Text>
              </View>
            </View>

            <TouchableOpacity
              testID="add-tomorrow-task-button"
              style={styles.addBtn}
              onPress={() => setShowAdd(true)}
            >
              <Ionicons name="add" color="#0B1020" size={20} />
              <Text style={styles.addBtnText}>Add Task</Text>
            </TouchableOpacity>

            <Text style={styles.section}>Staged Tasks ({tomorrowTasks.length})</Text>
          </View>
        }
        renderItem={({ item, index }) => (
          <View
            testID={`tomorrow-task-${index}`}
            style={styles.row}
          >
            <View style={styles.dot} />
            <Text style={styles.rowTitle}>{item.title}</Text>
            <TouchableOpacity onPress={() => removeTomorrowTask(item.id)}>
              <Ionicons name="trash-outline" color="#94A3B8" size={18} />
            </TouchableOpacity>
          </View>
        )}
        ListEmptyComponent={
          <Text style={styles.empty}>Nothing staged for tomorrow yet.</Text>
        }
      />
      <AddTaskSheet
        visible={showAdd}
        onClose={() => setShowAdd(false)}
        onSubmit={(t, p) => addTomorrowTask(t, p)}
      />
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: "#090A10" },
  list: { padding: 20, paddingBottom: 60 },
  kicker: {
    color: "#64748B",
    fontSize: 11,
    fontWeight: "800",
    letterSpacing: 2,
    textTransform: "uppercase",
  },
  h1: { color: "#FFFFFF", fontSize: 30, fontWeight: "900", marginTop: 2 },
  sub: { color: "#94A3B8", fontSize: 14, marginTop: 6, marginBottom: 14 },
  boardWrap: { alignItems: "center", position: "relative", marginBottom: 18 },
  inactiveOverlay: {
    position: "absolute",
    top: 0, left: 0, right: 0, bottom: 0,
    backgroundColor: "rgba(9,10,16,0.75)",
    alignItems: "center",
    justifyContent: "center",
    borderRadius: 24,
    gap: 8,
  },
  inactiveText: { color: "#94A3B8", fontSize: 13, fontWeight: "700" },
  addBtn: {
    backgroundColor: "#FACC15",
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "center",
    gap: 8,
    paddingVertical: 14,
    borderRadius: 999,
  },
  addBtnText: { color: "#0B1020", fontSize: 15, fontWeight: "900" },
  section: {
    color: "#94A3B8",
    fontSize: 12,
    fontWeight: "800",
    letterSpacing: 2,
    textTransform: "uppercase",
    marginTop: 18,
    marginBottom: 10,
  },
  row: {
    flexDirection: "row",
    alignItems: "center",
    backgroundColor: "#1E243A",
    paddingVertical: 12,
    paddingHorizontal: 14,
    borderRadius: 12,
    marginBottom: 8,
    gap: 12,
  },
  dot: {
    width: 8,
    height: 8,
    borderRadius: 4,
    backgroundColor: "#FACC15",
  },
  rowTitle: { color: "#FFFFFF", fontSize: 15, fontWeight: "600", flex: 1 },
  empty: { color: "#64748B", textAlign: "center", marginTop: 24 },
});
