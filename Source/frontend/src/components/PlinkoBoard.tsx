import { useEffect, useMemo } from "react";
import { StyleSheet, View } from "react-native";
import Animated, {
  Easing,
  cancelAnimation,
  runOnJS,
  useAnimatedStyle,
  useSharedValue,
  withTiming,
  withSequence,
} from "react-native-reanimated";

import { AssetImage } from "./AssetImage";
import { CELL_FORMATS, DAY_FORMATS } from "@/src/utils/formats";

// Plinko board geometry
//   rows         peg rows of pegs (each row has rowIdx+3 pegs, row 0 has 3 pegs)
//   bottomCells  = 7 catcher slots
//
// Top of the board is row 0, bottom (row index = ROWS-1) sits above the cells.

const ROWS = 9; // peg rows; rowIdx 0..ROWS-1
const BOTTOM_CELLS = 7;

type Props = {
  width: number;
  height: number;
  // When set, the ball drops to land in this cell index (0..6). Null = no ball.
  targetCell: number | null;
  // Whether the entire board should pulse red (Full Sprint hit).
  redAlarm?: boolean;
  // Called when the ball lands in its cell.
  onLanded?: (cellIndex: number) => void;
  // Animate ball drop (false = static board only)
  animateDrop?: boolean;
  testID?: string;
};

export function PlinkoBoard({
  width,
  height,
  targetCell,
  redAlarm,
  onLanded,
  animateDrop = true,
  testID,
}: Props) {
  const ballSize = Math.max(22, Math.round(width * 0.07));
  const pegSize = Math.max(8, Math.round(width * 0.022));
  const cellWidth = width / BOTTOM_CELLS;

  // Vertical spacing between peg rows
  const topInset = 24;
  const bottomCellsHeight = Math.max(64, height * 0.13);
  const pegArea = height - topInset - bottomCellsHeight - 12;
  const rowGap = pegArea / (ROWS + 1);

  // Compute peg positions
  const pegs = useMemo(() => {
    const out: { x: number; y: number }[] = [];
    for (let r = 0; r < ROWS; r++) {
      const count = r + 3; // row 0 -> 3 pegs, row 1 -> 4 pegs, etc.
      const totalSpan = (count - 1) * rowGap * 1.05;
      const startX = (width - totalSpan) / 2;
      const y = topInset + (r + 1) * rowGap;
      for (let c = 0; c < count; c++) {
        out.push({ x: startX + c * rowGap * 1.05, y });
      }
    }
    return out;
  }, [width, rowGap]);

  // Ball position
  const ballX = useSharedValue(width / 2 - ballSize / 2);
  const ballY = useSharedValue(topInset - ballSize);
  const ballScale = useSharedValue(1);

  // Cell glow shared value (use single shared value, switch which cell glows in JS)
  // For simplicity each cell gets its own JS state-driven glow via animateDrop completion.

  useEffect(() => {
    if (!animateDrop || targetCell == null) return;

    // Compute a bouncy zig-zag path from top center to the target cell center.
    const startX = width / 2 - ballSize / 2;
    const startY = topInset - ballSize;
    const endX = targetCell * cellWidth + (cellWidth - ballSize) / 2;
    const endY = topInset + pegArea + bottomCellsHeight - ballSize - 8;

    ballX.value = startX;
    ballY.value = startY;

    // Generate ROWS+1 keyframes - x oscillates left/right with random jitter,
    // converging linearly toward endX.
    const xKeys: number[] = [];
    const yKeys: number[] = [];
    const stepDur = 220; // ms per row
    for (let r = 0; r <= ROWS; r++) {
      const t = (r + 1) / (ROWS + 1);
      const linearX = startX + (endX - startX) * t;
      // jitter scaled down at the end so we always converge
      const jitterAmp = (1 - t) * cellWidth * 0.6;
      const jitter = (Math.random() - 0.5) * 2 * jitterAmp;
      xKeys.push(linearX + jitter);
      yKeys.push(topInset + (r + 1) * rowGap - ballSize / 2);
    }
    // Final landing into the cell
    xKeys.push(endX);
    yKeys.push(endY);

    // Compose timing animations sequentially via withSequence
    const xSeq: ReturnType<typeof withTiming>[] = xKeys.map((v, i) =>
      withTiming(v, {
        duration: i === xKeys.length - 1 ? 280 : stepDur,
        easing: i === xKeys.length - 1 ? Easing.in(Easing.quad) : Easing.inOut(Easing.quad),
      }),
    );
    const ySeq: ReturnType<typeof withTiming>[] = yKeys.map((v, i) =>
      withTiming(v, {
        duration: i === yKeys.length - 1 ? 280 : stepDur,
        easing: i === yKeys.length - 1 ? Easing.in(Easing.quad) : Easing.in(Easing.quad),
      }),
    );

    cancelAnimation(ballX);
    cancelAnimation(ballY);
    cancelAnimation(ballScale);

    // @ts-expect-error withSequence accepts variadic timings
    ballX.value = withSequence(...xSeq);
    // @ts-expect-error withSequence accepts variadic timings
    ballY.value = withSequence(
      ...ySeq.slice(0, -1),
      withTiming(yKeys[yKeys.length - 1], { duration: 280, easing: Easing.in(Easing.quad) }, (finished) => {
        if (finished && onLanded) {
          runOnJS(onLanded)(targetCell);
        }
      }),
    );
    ballScale.value = withSequence(
      withTiming(1.05, { duration: 200 }),
      withTiming(1, { duration: 200 }),
    );
  }, [animateDrop, targetCell, width, height]);

  const ballStyle = useAnimatedStyle(() => ({
    transform: [{ translateX: ballX.value }, { translateY: ballY.value }, { scale: ballScale.value }],
  }));

  return (
    <View
      testID={testID}
      style={[
        styles.board,
        { width, height, backgroundColor: redAlarm ? "#3B0A0A" : "#13182B" },
      ]}
    >
      {/* Pegs */}
      {pegs.map((p, i) => (
        <View
          key={`peg-${i}`}
          style={[
            styles.peg,
            {
              width: pegSize,
              height: pegSize,
              borderRadius: pegSize / 2,
              left: p.x - pegSize / 2,
              top: p.y - pegSize / 2,
            },
          ]}
        />
      ))}

      {/* Bottom cells */}
      <View
        style={[
          styles.cellsRow,
          { height: bottomCellsHeight, width, bottom: 0 },
        ]}
      >
        {CELL_FORMATS.map((fmt, idx) => {
          const f = DAY_FORMATS[fmt];
          const isTarget = targetCell === idx;
          return (
            <View
              key={`cell-${idx}`}
              testID={`day-format-cell-${idx}`}
              style={[
                styles.cell,
                {
                  backgroundColor: f.color,
                  opacity: isTarget ? 1 : 0.75,
                  borderTopWidth: isTarget ? 3 : 0,
                  borderColor: "#FFFFFF",
                },
              ]}
            />
          );
        })}
      </View>

      {/* Ball */}
      {targetCell != null ? (
        <Animated.View
          testID="plinko-ball"
          style={[
            styles.ball,
            { width: ballSize, height: ballSize, borderRadius: ballSize / 2 },
            ballStyle,
          ]}
        >
          <AssetImage name="ball-clock" size={ballSize} />
        </Animated.View>
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  board: {
    borderRadius: 24,
    overflow: "hidden",
    position: "relative",
  },
  peg: {
    position: "absolute",
    backgroundColor: "#FFFFFF",
    shadowColor: "#FFFFFF",
    shadowOpacity: 0.8,
    shadowRadius: 4,
    shadowOffset: { width: 0, height: 0 },
    elevation: 4,
  },
  cellsRow: {
    position: "absolute",
    flexDirection: "row",
  },
  cell: {
    flex: 1,
    marginHorizontal: 2,
    borderTopLeftRadius: 8,
    borderTopRightRadius: 8,
  },
  ball: {
    position: "absolute",
    backgroundColor: "#FFFFFF",
    shadowColor: "#FFFFFF",
    shadowOpacity: 0.9,
    shadowRadius: 12,
    shadowOffset: { width: 0, height: 0 },
    elevation: 8,
    alignItems: "center",
    justifyContent: "center",
  },
});
