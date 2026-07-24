import "@testing-library/jest-dom/vitest";
import type { ReactElement } from "react";
import { act, cleanup, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

type DynamicLoading = (props: { error?: Error | null }) => ReactElement;
type IntersectionCallback = (entries: Array<{ isIntersecting: boolean }>) => void;

const harness = vi.hoisted(() => ({
  throwSpline: false,
  dynamicLoading: undefined as DynamicLoading | undefined,
  intersectionCallback: undefined as IntersectionCallback | undefined,
  idleCallback: undefined as (() => void) | undefined,
  timelineOptions: undefined as { scrollTrigger?: { pin?: boolean } } | undefined,
  contextRevert: vi.fn(),
  observerDisconnect: vi.fn(),
  observerObserve: vi.fn(),
  timelineTo: vi.fn(),
}));

vi.mock("next/dynamic", async () => {
  const React = await import("react");
  return {
    default: vi.fn(
      (
        _loader: unknown,
        options?: { loading?: DynamicLoading },
      ) => {
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
      timeline: vi.fn((options: { scrollTrigger?: { pin?: boolean } }) => {
        harness.timelineOptions = options;
        return timeline;
      }),
    },
  };
});

vi.mock("gsap/ScrollTrigger", () => ({ ScrollTrigger: {} }));

let ScrollRoom: typeof import("./ScrollRoom").ScrollRoom;
let getSceneState: typeof import("./ScrollRoom").getSceneState;

describe("ScrollRoom lifecycle", () => {
  beforeEach(async () => {
    vi.resetModules();
    vi.clearAllMocks();
    harness.throwSpline = false;
    harness.dynamicLoading = undefined;
    harness.intersectionCallback = undefined;
    harness.idleCallback = undefined;
    harness.timelineOptions = undefined;
    process.env.NEXT_PUBLIC_SPLINE_SCENE_URL = "test-room.splinecode";

    const matchMedia = (query: string) => ({
      matches: false,
      media: query,
      onchange: null,
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      addListener: vi.fn(),
      removeListener: vi.fn(),
      dispatchEvent: vi.fn(),
    });
    Object.defineProperty(window, "matchMedia", { configurable: true, value: matchMedia });
    Object.defineProperty(window, "innerWidth", { configurable: true, value: 1200 });
    Object.defineProperty(HTMLCanvasElement.prototype, "getContext", {
      configurable: true,
      value: () => ({}),
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

    ({ ScrollRoom, getSceneState } = await import("./ScrollRoom"));
  });

  afterEach(() => {
    cleanup();
    delete process.env.NEXT_PUBLIC_SPLINE_SCENE_URL;
    vi.unstubAllGlobals();
  });

  it("defers Spline until intersection and idle readiness, then cleans up", async () => {
    const { unmount } = render(<ScrollRoom />);

    expect(
      screen.getByRole("region", { name: "Không gian mô phỏng năm bước của RenderVN AI" }),
    ).toBeInTheDocument();
    expect(screen.queryByTestId("spline-scene")).not.toBeInTheDocument();

    await waitFor(() => expect(harness.intersectionCallback).toBeTypeOf("function"));
    expect(screen.queryByTestId("spline-scene")).not.toBeInTheDocument();

    act(() => harness.intersectionCallback?.([{ isIntersecting: true }]));
    expect(screen.getByRole("status", { name: "" })).toHaveTextContent("Đang dựng không gian");
    expect(screen.queryByTestId("spline-scene")).not.toBeInTheDocument();

    act(() => harness.idleCallback?.());
    await waitFor(() => expect(screen.getByTestId("spline-scene")).toBeInTheDocument());
    expect(screen.getByTestId("spline-scene")).toHaveAttribute("data-scene", "test-room.splinecode");
    expect(harness.timelineOptions?.scrollTrigger?.pin).toBe(true);
    expect(harness.timelineTo).toHaveBeenCalledTimes(5);

    unmount();
    expect(harness.contextRevert).toHaveBeenCalledTimes(1);
    expect(harness.observerDisconnect).toHaveBeenCalled();
  });

  it("falls back for dynamic chunk and Spline runtime failures", async () => {
    const dynamicFallback = harness.dynamicLoading?.({ error: new Error("chunk failed") });
    expect(dynamicFallback).toBeDefined();
    const loadingView = render(dynamicFallback as ReactElement);
    expect(screen.getByText("Bản xem trước tĩnh")).toBeInTheDocument();
    loadingView.unmount();

    harness.throwSpline = true;
    const consoleError = vi.spyOn(console, "error").mockImplementation(() => undefined);
    render(<ScrollRoom />);
    await waitFor(() => expect(harness.intersectionCallback).toBeTypeOf("function"));
    act(() => harness.intersectionCallback?.([{ isIntersecting: true }]));
    act(() => harness.idleCallback?.());

    await waitFor(() => expect(screen.getByText("Bản xem trước tĩnh")).toBeInTheDocument());
    expect(screen.queryByTestId("spline-scene")).not.toBeInTheDocument();
    consoleError.mockRestore();
  });

  it("maps scroll progress across exactly five scene states", () => {
    expect(getSceneState(0)).toBe("sketch");
    expect(getSceneState(0.2)).toBe("build");
    expect(getSceneState(0.4)).toBe("scan");
    expect(getSceneState(0.6)).toBe("styles");
    expect(getSceneState(0.8)).toBe("reveal");
    expect(getSceneState(1)).toBe("reveal");
  });
});
