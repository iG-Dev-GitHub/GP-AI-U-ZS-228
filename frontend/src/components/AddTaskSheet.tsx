import { useState } from "react";
import {
  Keyboard,
  Modal,
  Pressable,
  StyleSheet,
  Text,
  TextInput,
  TouchableOpacity,
  TouchableWithoutFeedback,
  View,
} from "react-native";

import { Priority } from "@/src/store/types";

type Props = {
  visible: boolean;
  onClose: () => void;
  onSubmit: (title: string, priority: Priority) => void;
};

const PRIORITIES: { id: Priority; label: string; color: string }[] = [
  { id: "low", label: "Low", color: "#3B82F6" },
  { id: "med", label: "Med", color: "#F59E0B" },
  { id: "high", label: "High", color: "#EF4444" },
];

export function AddTaskSheet({ visible, onClose, onSubmit }: Props) {
  const [title, setTitle] = useState("");
  const [priority, setPriority] = useState<Priority>("med");

  const submit = () => {
    const trimmed = title.trim();
    if (!trimmed) return;
    onSubmit(trimmed, priority);
    setTitle("");
    setPriority("med");
    onClose();
  };

  return (
    <Modal
      visible={visible}
      transparent
      animationType="slide"
      onRequestClose={onClose}
      testID="add-task-modal"
    >
      <TouchableWithoutFeedback onPress={() => { Keyboard.dismiss(); onClose(); }}>
        <View style={styles.backdrop}>
          <TouchableWithoutFeedback>
            <View style={styles.sheet}>
              <Text style={styles.title}>New Task</Text>
              <TextInput
                testID="new-task-input"
                style={styles.input}
                placeholder="What do you want to do?"
                placeholderTextColor="#64748B"
                value={title}
                onChangeText={setTitle}
                autoFocus
                returnKeyType="done"
                onSubmitEditing={submit}
                maxLength={120}
              />
              <Text style={styles.label}>Priority</Text>
              <View style={styles.row}>
                {PRIORITIES.map((p) => {
                  const active = p.id === priority;
                  return (
                    <TouchableOpacity
                      testID={`priority-${p.id}`}
                      key={p.id}
                      onPress={() => setPriority(p.id)}
                      style={[
                        styles.chip,
                        {
                          borderColor: p.color,
                          backgroundColor: active ? p.color : "transparent",
                        },
                      ]}
                    >
                      <Text
                        style={[
                          styles.chipText,
                          { color: active ? "#0B1020" : p.color },
                        ]}
                      >
                        {p.label}
                      </Text>
                    </TouchableOpacity>
                  );
                })}
              </View>
              <View style={styles.actions}>
                <Pressable
                  testID="cancel-task-button"
                  style={[styles.btn, styles.btnGhost]}
                  onPress={onClose}
                >
                  <Text style={[styles.btnText, { color: "#94A3B8" }]}>Cancel</Text>
                </Pressable>
                <Pressable
                  testID="submit-task-button"
                  style={[styles.btn, styles.btnPrimary]}
                  onPress={submit}
                >
                  <Text style={[styles.btnText, { color: "#0B1020" }]}>Add</Text>
                </Pressable>
              </View>
            </View>
          </TouchableWithoutFeedback>
        </View>
      </TouchableWithoutFeedback>
    </Modal>
  );
}

const styles = StyleSheet.create({
  backdrop: {
    flex: 1,
    backgroundColor: "rgba(0,0,0,0.65)",
    justifyContent: "flex-end",
  },
  sheet: {
    backgroundColor: "#1E243A",
    padding: 20,
    paddingBottom: 32,
    borderTopLeftRadius: 24,
    borderTopRightRadius: 24,
  },
  title: {
    color: "#FFFFFF",
    fontSize: 22,
    fontWeight: "900",
    marginBottom: 14,
  },
  input: {
    backgroundColor: "#0B1020",
    color: "#FFFFFF",
    fontSize: 16,
    paddingHorizontal: 14,
    paddingVertical: 14,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: "#2A3350",
  },
  label: {
    color: "#94A3B8",
    fontSize: 12,
    fontWeight: "800",
    letterSpacing: 2,
    textTransform: "uppercase",
    marginTop: 16,
    marginBottom: 8,
  },
  row: { flexDirection: "row", gap: 10 },
  chip: {
    flex: 1,
    paddingVertical: 12,
    borderRadius: 999,
    borderWidth: 2,
    alignItems: "center",
  },
  chipText: { fontWeight: "800", fontSize: 14 },
  actions: { flexDirection: "row", gap: 10, marginTop: 22 },
  btn: {
    flex: 1,
    paddingVertical: 14,
    borderRadius: 999,
    alignItems: "center",
  },
  btnGhost: { backgroundColor: "#0B1020" },
  btnPrimary: { backgroundColor: "#FACC15" },
  btnText: { fontSize: 16, fontWeight: "900" },
});
