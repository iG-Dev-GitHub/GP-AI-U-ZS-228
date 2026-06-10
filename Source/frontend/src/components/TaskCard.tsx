import { Ionicons } from "@expo/vector-icons";
import { StyleSheet, Text, TouchableOpacity, View } from "react-native";

import { Priority, Task } from "@/src/store/types";

type Props = {
  task: Task;
  onToggle: () => void;
  onDelete: () => void;
  evening?: boolean;
  testID?: string;
};

const PRIORITY_COLOR: Record<Priority, string> = {
  high: "#EF4444",
  med: "#F59E0B",
  low: "#3B82F6",
};

const PRIORITY_LABEL: Record<Priority, string> = {
  high: "High",
  med: "Med",
  low: "Low",
};

export function TaskCard({ task, onToggle, onDelete, evening, testID }: Props) {
  const bg = evening ? "#451A03" : "#0F1E3D";
  const border = task.done ? "#475569" : PRIORITY_COLOR[task.priority];
  return (
    <View testID={testID} style={[styles.card, { backgroundColor: bg, borderLeftColor: border }]}>
      <TouchableOpacity
        testID={`${testID}-checkbox`}
        onPress={onToggle}
        style={[styles.checkbox, { borderColor: border, backgroundColor: task.done ? border : "transparent" }]}
      >
        {task.done ? <Ionicons name="checkmark" size={18} color="#0B1020" /> : null}
      </TouchableOpacity>
      <View style={styles.body}>
        <Text
          testID={`${testID}-title`}
          style={[styles.title, task.done && styles.done]}
          numberOfLines={2}
        >
          {task.title}
        </Text>
        <View style={[styles.tag, { backgroundColor: PRIORITY_COLOR[task.priority] + "33" }]}>
          <Text style={[styles.tagText, { color: PRIORITY_COLOR[task.priority] }]}>
            {PRIORITY_LABEL[task.priority]}
          </Text>
        </View>
      </View>
      <TouchableOpacity testID={`${testID}-delete`} onPress={onDelete} style={styles.deleteBtn}>
        <Ionicons name="trash-outline" size={18} color="#94A3B8" />
      </TouchableOpacity>
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    flexDirection: "row",
    alignItems: "center",
    paddingVertical: 14,
    paddingHorizontal: 14,
    marginBottom: 10,
    borderRadius: 14,
    borderLeftWidth: 5,
  },
  checkbox: {
    width: 26,
    height: 26,
    borderRadius: 13,
    borderWidth: 2,
    alignItems: "center",
    justifyContent: "center",
    marginRight: 12,
  },
  body: { flex: 1 },
  title: {
    color: "#FFFFFF",
    fontSize: 16,
    fontWeight: "700",
  },
  done: {
    textDecorationLine: "line-through",
    color: "#94A3B8",
  },
  tag: {
    alignSelf: "flex-start",
    marginTop: 6,
    paddingHorizontal: 8,
    paddingVertical: 2,
    borderRadius: 6,
  },
  tagText: {
    fontSize: 11,
    fontWeight: "800",
    letterSpacing: 1,
  },
  deleteBtn: {
    padding: 6,
    marginLeft: 4,
  },
});
