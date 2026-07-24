import "@testing-library/jest-dom/vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

let ScrollRoom: typeof import("./ScrollRoom").ScrollRoom;
let isWebGLAvailable: typeof import("./ScrollRoom").isWebGLAvailable;
let mediaMatches = true;

describe("ScrollRoom", () => {
  beforeEach(async () => {
    mediaMatches = true;
    const matchMedia = (query: string) => ({
      matches: mediaMatches,
      media: query,
      onchange: null,
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      addListener: vi.fn(),
      removeListener: vi.fn(),
      dispatchEvent: vi.fn(),
    });
    vi.stubGlobal("matchMedia", matchMedia);
    Object.defineProperty(window, "matchMedia", { configurable: true, value: matchMedia });
    ({ ScrollRoom, isWebGLAvailable } = await import("./ScrollRoom"));
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("renders an accessible image-backed fallback without a Spline URL", async () => {
    render(<ScrollRoom />);

    expect(
      screen.getByAltText("Bản xem trước kiến trúc với lớp tường, sàn và điểm nhấn đất nung"),
    ).toBeInTheDocument();
    expect(screen.getByText("Không phải kết quả AI")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Bỏ qua không gian chuyển động" })).toHaveAttribute(
      "href",
      "#how-it-works",
    );
    await waitFor(() => expect(screen.getByText("Bản xem trước tĩnh")).toBeInTheDocument());
  });

  it("keeps the fallback static when reduced motion is requested", async () => {
    mediaMatches = true;
    render(<ScrollRoom />);

    await waitFor(() => {
      expect(screen.getByText("Bản xem trước tĩnh")).toBeInTheDocument();
      expect(screen.getByText("Chế độ chuyển động tối giản")).toBeInTheDocument();
    });
  });

  it("detects when WebGL is unavailable before mounting a scene", () => {
    Object.defineProperty(HTMLCanvasElement.prototype, "getContext", {
      configurable: true,
      value: () => null,
    });

    expect(isWebGLAvailable()).toBe(false);
  });
});
