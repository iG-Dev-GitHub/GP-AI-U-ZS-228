import { useRouter } from "expo-router";
import { useRef, useState } from "react";
import {
  Dimensions,
  ScrollView,
  StyleSheet,
  Text,
  TouchableOpacity,
  View,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";

import { PlinkoBoard } from "@/src/components/PlinkoBoard";
import { markOnboarded } from "@/src/store/state";

const { width: SCREEN_W } = Dimensions.get("window");

const SLIDES = [
  {
    title: "Add Your Tasks",
    body: "Drop in up to 10 things you want to get done today.",
    color: "#3B82F6",
  },
  {
    title: "Drop the Ball",
    body: "The Plinko ball picks your day format: Steady, Quick Win, Deep Work, or Full Sprint.",
    color: "#10B981",
  },
  {
    title: "Hit Full Sprint",
    body: "Close every task before 6 PM and earn the Day Clear badge.",
    color: "#EF4444",
  },
];

export default function Welcome() {
  const router = useRouter();
  const [page, setPage] = useState(0);
  const scrollRef = useRef<ScrollView>(null);

  const goNext = async () => {
    if (page < SLIDES.length - 1) {
      const next = page + 1;
      setPage(next);
      scrollRef.current?.scrollTo({ x: next * SCREEN_W, animated: true });
    } else {
      await markOnboarded();
      router.replace("/(tabs)");
    }
  };

  return (
    <SafeAreaView style={styles.container} edges={["top", "bottom"]} testID="welcome-screen">
      <ScrollView
        ref={scrollRef}
        horizontal
        pagingEnabled
        showsHorizontalScrollIndicator={false}
        onMomentumScrollEnd={(e) => {
          setPage(Math.round(e.nativeEvent.contentOffset.x / SCREEN_W));
        }}
      >
        {SLIDES.map((s, i) => (
          <View key={i} style={[styles.slide, { width: SCREEN_W }]}>
            <View style={styles.boardWrap}>
              <PlinkoBoard
                width={SCREEN_W - 80}
                height={(SCREEN_W - 80) * 1.1}
                targetCell={i === 0 ? null : i === 1 ? 3 : 0}
                animateDrop={false}
                redAlarm={i === 2}
              />
            </View>
            <View style={[styles.badge, { backgroundColor: s.color }]}>
              <Text style={styles.badgeText}>{`Step ${i + 1} of 3`}</Text>
            </View>
            <Text style={styles.title}>{s.title}</Text>
            <Text style={styles.body}>{s.body}</Text>
          </View>
        ))}
      </ScrollView>

      <View style={styles.footer}>
        <View style={styles.dots}>
          {SLIDES.map((_, i) => (
            <View
              key={i}
              style={[styles.dot, i === page && styles.dotActive]}
            />
          ))}
        </View>
        <TouchableOpacity
          testID="welcome-next-button"
          onPress={goNext}
          style={styles.cta}
          activeOpacity={0.85}
        >
          <Text style={styles.ctaText}>
            {page === SLIDES.length - 1 ? "Plan My Day" : "Next"}
          </Text>
        </TouchableOpacity>
      </View>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: "#090A10" },
  slide: {
    flex: 1,
    alignItems: "center",
    paddingHorizontal: 32,
    paddingTop: 12,
  },
  boardWrap: { marginTop: 8, marginBottom: 16 },
  badge: {
    paddingHorizontal: 14,
    paddingVertical: 6,
    borderRadius: 999,
    marginTop: 12,
  },
  badgeText: {
    color: "#FFFFFF",
    fontSize: 11,
    fontWeight: "900",
    letterSpacing: 2,
    textTransform: "uppercase",
  },
  title: {
    color: "#FFFFFF",
    fontSize: 30,
    fontWeight: "900",
    marginTop: 14,
    textAlign: "center",
  },
  body: {
    color: "#94A3B8",
    fontSize: 15,
    marginTop: 8,
    textAlign: "center",
    lineHeight: 22,
  },
  footer: { paddingHorizontal: 32, paddingBottom: 24, paddingTop: 12 },
  dots: { flexDirection: "row", justifyContent: "center", gap: 8, marginBottom: 16 },
  dot: {
    width: 8,
    height: 8,
    borderRadius: 4,
    backgroundColor: "#1E243A",
  },
  dotActive: { backgroundColor: "#FACC15", width: 22 },
  cta: {
    backgroundColor: "#FACC15",
    paddingVertical: 16,
    borderRadius: 999,
    alignItems: "center",
  },
  ctaText: { color: "#0B1020", fontSize: 17, fontWeight: "900", letterSpacing: 0.5 },
});
