import "@testing-library/jest-dom/vitest";
import { render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

let ScrollRoom: typeof import("./ScrollRoom").ScrollRoom;

describe("ScrollRoom", () => {
  beforeEach(() => {
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
    vi.stubGlobal("matchMedia", matchMedia);
    Object.defineProperty(window, "matchMedia", { configurable: true, value: matchMedia });
  });

  beforeEach(async () => {
    ({ ScrollRoom } = await import("./ScrollRoom"));
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("renders an accessible preview when no Spline scene URL is configured", () => {
    render(<ScrollRoom />);

    expect(
      screen.getByRole("img", {
        name: /bản xem trước kiến trúc với các lớp mặt phẳng/i,
      }),
    ).toBeInTheDocument();
    expect(screen.getByText("Không phải kết quả AI")).toBeInTheDocument();
  });
});
