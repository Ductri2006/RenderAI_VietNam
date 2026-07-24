import "@testing-library/jest-dom/vitest";
import type { ReactElement } from "react";
import { act, cleanup, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

type DynamicLoading = (props: { error?: Error | null }) => ReactElement;
type IntersectionCallback = (entries: Array<{ isIntersecting: boolean }>) => void;
type GateOptions = {
  sceneUrl?: boolean;
  narrow?: boolean;
  webgl?: boolean;
  reducedMotion?: boolean;
};
type TimelineOptions = {
  scrollTrigger?: {
    pin?: boolean;
    onUpdate?: (trigger: { progress: number }) => void;
  };
};

const harness = vi.hoisted(() => ({
  throwSpline: false,
  reducedMotion: false,
  narrow: false,
  dynamicLoading: undefined as DynamicLoading | undefined,
  intersectionCallback: undefined as IntersectionCallback | undefined,
  idleCallback: undefined as (() => void) | undefined,
  reducedMotionListener: undefined as (() => void) | undefined,
  narrowListener: undefined as (() => void) | undefined,
  timelineOptions: undefined as TimelineOptions | undefined,
  contextRevert: vi.fn(),
  observerDisconnect: vi.fn(),
  observerObserve: vi.fn(),
  timelineTo: vi.fn(),
}));

vi.mock("next/dynamic", async () => {
  const React = await import("react");
  return {
    default: vi.fn(
      (_loader: unknown, options?: { loading?: DynamicLoading }) => {
        harness.dynamicLoading = options?.loading;
        return function MockSpline({ scene }: { scene: string }) {
          if (harness.throwSpline) throw new Error("Spline runtime failed");
          return React.createElement("div", { "data-testid": "spline-scene", "data-scene": scene });
        };
      },
    ),
  };
});

vi.mock("gsap", () => {
  const timeline = { to: harness.timelineTo };
  harness.timelineTo.mockImplementation(() => timeline);
  return {
    default: {
      registerPlugin: vi.fn(),
      context: vi.fn((callback: () => void) => {
        callback();
        return { revert: harness.contextRevert };
      }),
      timeline: vi.fn((options: TimelineOptions) => {
        harness.timelineOptions = options;
        return timeline;
      }),
    },
  };
});

vi.mock("gsap/ScrollTrigger", () => ({ ScrollTrigger: {} }));

async function loadScrollRoom({
  sceneUrl = true,
  narrow = false,
  webgl = true,
  reducedMotion = false,
}: GateOptions = {}) {
  vi.resetModules();
  harness.reducedMotion = reducedMotion;
  harness.narrow = narrow;
  if (sceneUrl) process.env.NEXT_PUBLIC_SPLINE_SCENE_URL = "test-room.splinecode";
  else delete process.env.NEXT_PUBLIC_SPLINE_SCENE_URL;

  const matchMedia = (query: string) => ({
    get matches() {
      if (query.includes("prefers-reduced-motion")) return harness.reducedMotion;
      if (query.includes("max-width")) return harness.narrow;
      return false;
    },
    media: query,
    onchange: null,
    addEventListener: (_type: string, listener: () => void) => {
      if (query.includes("prefers-reduced-motion")) harness.reducedMotionListener = listener;
      if (query.includes("max-width")) harness.narrowListener = listener;
    },
    removeEventListener: vi.fn(),
    addListener: vi.fn(),
    removeListener: vi.fn(),
    dispatchEvent: vi.fn(),
  });
  Object.defineProperty(window, "matchMedia", { configurable: true, value: matchMedia });
  Object.defineProperty(window, "innerWidth", { configurable: true, value: narrow ? 390 : 1200 });
  Object.defineProperty(HTMLCanvasElement.prototype, "getContext", {
    configurable: true,
    value: () => (webgl ? {} : null),
  });

  class MockIntersectionObserver {
    constructor(callback: IntersectionCallback) {
      harness.intersectionCallback = callback;
    }

    observe = harness.observerObserve;
    disconnect = harness.observerDisconnect;
  }

  vi.stubGlobal("IntersectionObserver", MockIntersectionObserver);
  Object.defineProperty(window, "requestIdleCallback", {
    configurable: true,
    value: (callback: () => void) => {
      harness.idleCallback = callback;
      return 17;
    },
  });
  Object.defineProperty(window, "cancelIdleCallback", {
    configurable: true,
    value: vi.fn(),
  });

  return import("./ScrollRoom");
}

function flushCapabilityCheck() {
  act(() => vi.runOnlyPendingTimers());
}

describe("ScrollRoom lifecycle", () => {
  beforeEach(() => {
    vi.useFakeTimers();
    vi.clearAllMocks();
    harness.throwSpline = false;
    harness.reducedMotion = false;
    harness.narrow = false;
    harness.dynamicLoading = undefined;
    harness.intersectionCallback = undefined;
    harness.idleCallback = undefined;
    harness.reducedMotionListener = undefined;
    harness.narrowListener = undefined;
    harness.timelineOptions = undefined;
  });

  afterEach(() => {
    cleanup();
    delete process.env.NEXT_PUBLIC_SPLINE_SCENE_URL;
    vi.unstubAllGlobals();
    vi.useRealTimers();
  });

  it("defers Spline until intersection and idle readiness, then cleans up", async () => {
    const { ScrollRoom } = await loadScrollRoom();
    const { unmount } = render(<ScrollRoom />);

    expect(
      screen.getByRole("region", { name: "Không gian mô phỏng năm bước của RenderVN AI" }),
    ).toBeInTheDocument();
    expect(screen.queryByTestId("spline-scene")).not.toBeInTheDocument();

    flushCapabilityCheck();
    expect(harness.intersectionCallback).toBeTypeOf("function");
    expect(screen.queryByTestId("spline-scene")).not.toBeInTheDocument();
    expect(screen.getByText("Phác thảo")).toBeInTheDocument();
    act(() => harness.timelineOptions?.scrollTrigger?.onUpdate?.({ progress: 0.4 }));
    expect(screen.getByText("Quét không gian")).toBeInTheDocument();

    act(() => harness.intersectionCallback?.([{ isIntersecting: true }]));
    expect(screen.getByRole("status")).toHaveTextContent("Đang dựng không gian");
    expect(screen.queryByTestId("spline-scene")).not.toBeInTheDocument();

    act(() => harness.idleCallback?.());
    expect(screen.getByTestId("spline-scene")).toHaveAttribute("data-scene", "test-room.splinecode");
    expect(harness.timelineOptions?.scrollTrigger?.pin).toBe(true);
    expect(harness.timelineTo).toHaveBeenCalledTimes(5);

    unmount();
    expect(harness.contextRevert).toHaveBeenCalledTimes(1);
    expect(harness.observerDisconnect).toHaveBeenCalled();
  });

  it("falls back for dynamic chunk and Spline runtime failures", async () => {
    const { ScrollRoom } = await loadScrollRoom();
    const dynamicFallback = harness.dynamicLoading?.({ error: new Error("chunk failed") });
    expect(dynamicFallback).toBeDefined();
    const loadingView = render(dynamicFallback as ReactElement);
    expect(screen.getByText("Bản xem trước tĩnh")).toBeInTheDocument();
    loadingView.unmount();

    harness.throwSpline = true;
    const consoleError = vi.spyOn(console, "error").mockImplementation(() => undefined);
    render(<ScrollRoom />);
    flushCapabilityCheck();
    act(() => harness.intersectionCallback?.([{ isIntersecting: true }]));
    act(() => harness.idleCallback?.());

    expect(screen.getByText("Bản xem trước tĩnh")).toBeInTheDocument();
    expect(screen.queryByTestId("spline-scene")).not.toBeInTheDocument();
    consoleError.mockRestore();
  });

  it("maps scroll progress across exactly five scene states", async () => {
    const { getSceneState } = await loadScrollRoom();
    expect(getSceneState(0)).toBe("sketch");
    expect(getSceneState(0.2)).toBe("build");
    expect(getSceneState(0.4)).toBe("scan");
    expect(getSceneState(0.6)).toBe("styles");
    expect(getSceneState(0.8)).toBe("reveal");
    expect(getSceneState(1)).toBe("reveal");
  });

  it("renders the fallback when no Spline URL is configured", async () => {
    const { ScrollRoom } = await loadScrollRoom({ sceneUrl: false });
    render(<ScrollRoom />);
    flushCapabilityCheck();

    expect(screen.getByAltText(/Bản xem trước kiến trúc/)).toBeInTheDocument();
    expect(screen.queryByTestId("spline-scene")).not.toBeInTheDocument();
    expect(harness.observerObserve).not.toHaveBeenCalled();
  });

  it("keeps narrow devices on the static fallback", async () => {
    const { ScrollRoom } = await loadScrollRoom({ narrow: true });
    render(<ScrollRoom />);
    flushCapabilityCheck();

    expect(screen.getByAltText(/Bản xem trước kiến trúc/)).toBeInTheDocument();
    expect(screen.queryByRole("status")).not.toBeInTheDocument();
    expect(harness.observerObserve).not.toHaveBeenCalled();
    expect(harness.timelineTo).not.toHaveBeenCalled();
  });

  it("updates the scene lifecycle when the narrow media query changes", async () => {
    const { ScrollRoom } = await loadScrollRoom();
    render(<ScrollRoom />);
    flushCapabilityCheck();

    expect(harness.timelineTo).toHaveBeenCalledTimes(5);
    expect(harness.observerObserve).toHaveBeenCalledTimes(1);

    harness.narrow = true;
    act(() => harness.narrowListener?.());
    expect(harness.contextRevert).toHaveBeenCalledTimes(1);
    expect(harness.observerDisconnect).toHaveBeenCalled();
    expect(harness.timelineTo).toHaveBeenCalledTimes(5);
    expect(screen.queryByRole("status")).not.toBeInTheDocument();

    harness.narrow = false;
    act(() => harness.narrowListener?.());
    expect(harness.timelineTo).toHaveBeenCalledTimes(10);
    expect(harness.observerObserve).toHaveBeenCalledTimes(2);
  });

  it("keeps devices without WebGL on the static fallback", async () => {
    const { ScrollRoom } = await loadScrollRoom({ webgl: false });
    render(<ScrollRoom />);
    flushCapabilityCheck();

    expect(screen.getByAltText(/Bản xem trước kiến trúc/)).toBeInTheDocument();
    expect(screen.queryByTestId("spline-scene")).not.toBeInTheDocument();
    expect(harness.observerObserve).not.toHaveBeenCalled();
  });

  it("exits deferred loading when reduced motion is enabled mid-preparation", async () => {
    const { ScrollRoom } = await loadScrollRoom();
    render(<ScrollRoom />);
    flushCapabilityCheck();
    act(() => harness.intersectionCallback?.([{ isIntersecting: true }]));
    expect(screen.getByRole("status")).toHaveTextContent("Đang dựng không gian");

    harness.reducedMotion = true;
    act(() => harness.reducedMotionListener?.());
    flushCapabilityCheck();

    expect(screen.queryByRole("status")).not.toBeInTheDocument();
    expect(screen.getByText("Bản xem trước tĩnh")).toBeInTheDocument();
    expect(harness.contextRevert).toHaveBeenCalledTimes(1);
    expect(harness.timelineTo).toHaveBeenCalledTimes(5);
    act(() => harness.idleCallback?.());
    expect(screen.queryByTestId("spline-scene")).not.toBeInTheDocument();
  });
});
