import { Ionicons } from "@expo/vector-icons";
import { useState } from "react";
import {
  Modal,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  TouchableOpacity,
  View,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";

import { resetAll, updateSettings, useAppState } from "@/src/store/state";
import { RiskLevel } from "@/src/store/types";

const TIMES = [
  "06:00", "07:00", "08:00", "09:00", "10:00",
  "16:00", "17:00", "18:00", "19:00", "20:00",
  "21:00", "22:00", "23:00",
];

export default function Settings() {
  const { settings } = useAppState();
  const [editing, setEditing] = useState<null | keyof typeof settings>(null);
  const [confirmReset, setConfirmReset] = useState(false);

  const openTimePicker = (key: keyof typeof settings) => setEditing(key);
  const pickTime = async (t: string) => {
    if (!editing) return;
    await updateSettings({ [editing]: t } as never);
    setEditing(null);
  };

  return (
    <SafeAreaView style={styles.container} edges={["top"]} testID="settings-screen">
      <ScrollView contentContainerStyle={styles.scroll}>
        <Text style={styles.kicker}>Preferences</Text>
        <Text style={styles.h1}>Settings</Text>

        <Text style={styles.section}>Times</Text>
        <Row
          testID="setting-morning"
          icon="sunny"
          label="Morning Drop"
          value={settings.morningDrop}
          onPress={() => openTimePicker("morningDrop")}
        />
        <Row
          testID="setting-sprint"
          icon="flame"
          label="Full Sprint Deadline"
          value={settings.sprintDeadline}
          onPress={() => openTimePicker("sprintDeadline")}
        />
        <Row
          testID="setting-evening"
          icon="moon"
          label="Evening Reminder"
          value={settings.eveningReminder}
          onPress={() => openTimePicker("eveningReminder")}
        />

        <Text style={styles.section}>Risk Level</Text>
        <View style={styles.riskRow}>
          {(["easy", "hardcore"] as RiskLevel[]).map((r) => {
            const active = settings.riskLevel === r;
            return (
              <TouchableOpacity
                key={r}
                testID={`setting-risk-${r}`}
                style={[
                  styles.riskChip,
                  active && { backgroundColor: r === "easy" ? "#10B981" : "#EF4444" },
                ]}
                onPress={() => updateSettings({ riskLevel: r })}
              >
                <Text
                  style={[
                    styles.riskText,
                    active && { color: "#0B1020" },
                  ]}
                >
                  {r === "easy" ? "Easy" : "Hardcore"}
                </Text>
              </TouchableOpacity>
            );
          })}
        </View>
        <Text style={styles.hint}>
          Easy biases the ball toward Steady & Quick Win. Hardcore tilts it to Deep Work & Full Sprint.
        </Text>

        <Text style={styles.section}>Data</Text>
        <TouchableOpacity
          testID="reset-button"
          style={styles.resetBtn}
          onPress={() => setConfirmReset(true)}
        >
          <Ionicons name="trash" color="#EF4444" size={18} />
          <Text style={styles.resetText}>Reset All Data</Text>
        </TouchableOpacity>

        <Text style={styles.foot}>Everything is stored locally on this device.</Text>
      </ScrollView>

      <Modal
        visible={editing !== null}
        transparent
        animationType="fade"
        onRequestClose={() => setEditing(null)}
      >
        <Pressable style={styles.backdrop} onPress={() => setEditing(null)}>
          <Pressable style={styles.sheet}>
            <Text style={styles.sheetTitle}>Pick time</Text>
            <ScrollView style={{ maxHeight: 320 }}>
              {TIMES.map((t) => (
                <TouchableOpacity
                  key={t}
                  testID={`time-${t}`}
                  style={styles.timeRow}
                  onPress={() => pickTime(t)}
                >
                  <Text style={styles.timeText}>{t}</Text>
                </TouchableOpacity>
              ))}
            </ScrollView>
          </Pressable>
        </Pressable>
      </Modal>

      <Modal
        visible={confirmReset}
        transparent
        animationType="fade"
        onRequestClose={() => setConfirmReset(false)}
      >
        <Pressable style={styles.backdrop} onPress={() => setConfirmReset(false)}>
          <Pressable style={styles.sheet}>
            <Text style={styles.sheetTitle}>Reset everything?</Text>
            <Text style={styles.confirmBody}>
              This deletes tasks, streaks, badges and history. Cannot be undone.
            </Text>
            <View style={{ flexDirection: "row", gap: 10, marginTop: 16 }}>
              <TouchableOpacity
                style={[styles.confirmBtn, { backgroundColor: "#1E243A" }]}
                onPress={() => setConfirmReset(false)}
              >
                <Text style={[styles.confirmBtnText, { color: "#94A3B8" }]}>Cancel</Text>
              </TouchableOpacity>
              <TouchableOpacity
                testID="confirm-reset-button"
                style={[styles.confirmBtn, { backgroundColor: "#EF4444" }]}
                onPress={async () => {
                  await resetAll();
                  setConfirmReset(false);
                }}
              >
                <Text style={[styles.confirmBtnText, { color: "#FFFFFF" }]}>Reset</Text>
              </TouchableOpacity>
            </View>
          </Pressable>
        </Pressable>
      </Modal>
    </SafeAreaView>
  );
}

function Row({
  icon,
  label,
  value,
  onPress,
  testID,
}: {
  icon: keyof typeof Ionicons.glyphMap;
  label: string;
  value: string;
  onPress: () => void;
  testID: string;
}) {
  return (
    <TouchableOpacity testID={testID} style={styles.row} onPress={onPress}>
      <Ionicons name={icon} color="#FACC15" size={20} />
      <Text style={styles.rowLabel}>{label}</Text>
      <Text style={styles.rowValue}>{value}</Text>
      <Ionicons name="chevron-forward" color="#475569" size={18} />
    </TouchableOpacity>
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
  section: {
    color: "#94A3B8",
    fontSize: 12,
    fontWeight: "800",
    letterSpacing: 2,
    textTransform: "uppercase",
    marginTop: 22,
    marginBottom: 10,
  },
  row: {
    flexDirection: "row",
    alignItems: "center",
    gap: 12,
    backgroundColor: "#1E243A",
    paddingVertical: 14,
    paddingHorizontal: 14,
    borderRadius: 12,
    marginBottom: 8,
  },
  rowLabel: { color: "#FFFFFF", fontSize: 15, fontWeight: "700", flex: 1 },
  rowValue: { color: "#FACC15", fontSize: 15, fontWeight: "800" },
  riskRow: { flexDirection: "row", gap: 10 },
  riskChip: {
    flex: 1,
    paddingVertical: 14,
    borderRadius: 999,
    alignItems: "center",
    backgroundColor: "#1E243A",
  },
  riskText: { color: "#FFFFFF", fontSize: 15, fontWeight: "900" },
  hint: { color: "#64748B", fontSize: 12, marginTop: 8, lineHeight: 18 },
  resetBtn: {
    flexDirection: "row",
    alignItems: "center",
    gap: 10,
    borderColor: "#EF4444",
    borderWidth: 1,
    borderRadius: 12,
    paddingVertical: 14,
    paddingHorizontal: 14,
  },
  resetText: { color: "#EF4444", fontSize: 15, fontWeight: "800" },
  foot: { color: "#475569", fontSize: 12, marginTop: 16, textAlign: "center" },
  backdrop: { flex: 1, backgroundColor: "rgba(0,0,0,0.7)", justifyContent: "center", padding: 24 },
  sheet: { backgroundColor: "#1E243A", borderRadius: 18, padding: 18 },
  sheetTitle: { color: "#FFFFFF", fontSize: 18, fontWeight: "900", marginBottom: 10 },
  timeRow: { paddingVertical: 12, borderBottomColor: "#2A3350", borderBottomWidth: 1 },
  timeText: { color: "#FFFFFF", fontSize: 16 },
  confirmBody: { color: "#94A3B8", fontSize: 14, marginTop: 6, lineHeight: 20 },
  confirmBtn: { flex: 1, paddingVertical: 14, borderRadius: 999, alignItems: "center" },
  confirmBtnText: { fontSize: 15, fontWeight: "900" },
});
