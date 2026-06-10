import { Ionicons } from "@expo/vector-icons";
import { useRouter } from "expo-router";
import { useState } from "react";
import {
  Dimensions,
  Pressable,
  StyleSheet,
  Text,
  TouchableOpacity,
  View,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";

import { DayFormatBadge } from "@/src/components/DayFormatBadge";
import { PlinkoBoard } from "@/src/components/PlinkoBoard";
import { setDayFormat, updateSettings, useAppState } from "@/src/store/state";
import { RiskLevel } from "@/src/store/types";
import { formatForCell, pickTargetCell } from "@/src/utils/formats";

const { width: SCREEN_W } = Dimensions.get("window");

type Stage = "ready" | "dropping" | "landed";

export default function DropScreen() {
  const router = useRouter();
  const { settings } = useAppState();
  const [target, setTarget] = useState<number | null>(null);
  const [stage, setStage] = useState<Stage>("ready");

  const boardW = SCREEN_W - 32;
  const boardH = boardW * 1.2;

  const onDrop = () => {
    const cell = pickTargetCell(settings.riskLevel);
    setTarget(cell);
    setStage("dropping");
  };

  const onLanded = async (cell: number) => {
    const fmt = formatForCell(cell);
    await setDayFormat(fmt.id);
    setStage("landed");
  };

  const fmt = target != null ? formatForCell(target) : null;
  const isFullSprint = fmt?.id === "full-sprint";

  return (
    <SafeAreaView
      style={[styles.container, isFullSprint && stage === "landed" && styles.containerRed]}
      edges={["top", "bottom"]}
      testID="drop-screen"
    >
      <View style={styles.topBar}>
        <TouchableOpacity
          testID="drop-close-button"
          onPress={() => router.back()}
          style={styles.iconBtn}
        >
          <Ionicons name="close" color="#FFFFFF" size={22} />
        </TouchableOpacity>
        <Text style={styles.kicker}>Drop the ball</Text>
        <View style={styles.iconBtn} />
      </View>

      <View style={{ alignItems: "center" }}>
        <PlinkoBoard
          width={boardW}
          height={boardH}
          targetCell={target}
          animateDrop={stage === "dropping" || stage === "landed"}
          redAlarm={isFullSprint && stage === "landed"}
          onLanded={onLanded}
          testID="drop-plinko-board"
        />
      </View>

      <View style={styles.bottom}>
        {stage === "ready" ? (
          <>
            <View style={styles.riskGroup}>
              <Text style={styles.label}>Risk Level</Text>
              <View style={styles.riskRow}>
                {(["easy", "hardcore"] as RiskLevel[]).map((r) => {
                  const active = settings.riskLevel === r;
                  return (
                    <Pressable
                      key={r}
                      testID={`risk-${r}`}
                      style={[
                        styles.riskChip,
                        active && {
                          backgroundColor: r === "easy" ? "#10B981" : "#EF4444",
                        },
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
                    </Pressable>
                  );
                })}
              </View>
            </View>

            <TouchableOpacity
              testID="drop-ball-button"
              style={styles.dropBtn}
              onPress={onDrop}
              activeOpacity={0.85}
            >
              <Ionicons name="rocket" color="#0B1020" size={20} />
              <Text style={styles.dropText}>Drop</Text>
            </TouchableOpacity>
          </>
        ) : null}

        {stage === "dropping" ? (
          <View style={styles.droppingMsg}>
            <Text style={styles.droppingText}>Dropping…</Text>
          </View>
        ) : null}

        {stage === "landed" && fmt ? (
          <View style={{ gap: 12 }}>
            <DayFormatBadge formatId={fmt.id} testID="result-format-card" />
            <TouchableOpacity
              testID="continue-button"
              style={[
                styles.dropBtn,
                { backgroundColor: isFullSprint ? "#EF4444" : "#FACC15" },
              ]}
              onPress={() => {
                router.back();
                if (isFullSprint) {
                  setTimeout(() => router.push("/full-sprint"), 200);
                }
              }}
            >
              <Ionicons
                name={isFullSprint ? "flame" : "checkmark"}
                color={isFullSprint ? "#FFFFFF" : "#0B1020"}
                size={20}
              />
              <Text
                style={[
                  styles.dropText,
                  isFullSprint && { color: "#FFFFFF" },
                ]}
              >
                {isFullSprint ? "Start Full Sprint" : "Let's go"}
              </Text>
            </TouchableOpacity>
          </View>
        ) : null}
      </View>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: "#090A10", paddingHorizontal: 16 },
  containerRed: { backgroundColor: "#1B0606" },
  topBar: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    paddingVertical: 8,
    marginBottom: 8,
  },
  iconBtn: { width: 40, height: 40, borderRadius: 20, alignItems: "center", justifyContent: "center" },
  kicker: {
    color: "#94A3B8",
    fontSize: 12,
    fontWeight: "800",
    letterSpacing: 2,
    textTransform: "uppercase",
  },
  bottom: { padding: 4, marginTop: 16 },
  riskGroup: { marginBottom: 14 },
  label: {
    color: "#94A3B8",
    fontSize: 11,
    fontWeight: "800",
    letterSpacing: 2,
    textTransform: "uppercase",
    marginBottom: 8,
  },
  riskRow: { flexDirection: "row", gap: 10 },
  riskChip: {
    flex: 1,
    paddingVertical: 12,
    borderRadius: 999,
    alignItems: "center",
    backgroundColor: "#1E243A",
  },
  riskText: { color: "#FFFFFF", fontSize: 14, fontWeight: "900" },
  dropBtn: {
    backgroundColor: "#FACC15",
    paddingVertical: 16,
    borderRadius: 999,
    alignItems: "center",
    flexDirection: "row",
    justifyContent: "center",
    gap: 8,
  },
  dropText: { color: "#0B1020", fontSize: 17, fontWeight: "900" },
  droppingMsg: { alignItems: "center", paddingVertical: 16 },
  droppingText: { color: "#94A3B8", fontSize: 14, fontWeight: "700" },
});
