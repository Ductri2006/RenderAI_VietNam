import "@testing-library/jest-dom/vitest";
import { act, cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "../../lib/api";
import { AuthForm } from "./AuthForm";

const mocks = vi.hoisted(() => ({
  login: vi.fn<(email: string, password: string) => Promise<void>>(),
  register: vi.fn<(email: string, password: string) => Promise<unknown>>(),
  replace: vi.fn<(href: string) => void>(),
}));

vi.mock("../../lib/api", async (importOriginal) => {
  const actual = await importOriginal<typeof import("../../lib/api")>();

  return {
    ...actual,
    default: {
      ...actual.default,
      login: mocks.login,
      register: mocks.register,
    },
  };
});

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace: mocks.replace }),
}));

function fillLogin(email = "architect@example.com", password = "StrongPass123!") {
  fireEvent.change(screen.getByLabelText("Email"), { target: { value: email } });
  fireEvent.change(screen.getByLabelText("Mật khẩu"), { target: { value: password } });
}

function fillRegister(
  email = "architect@example.com",
  password = "StrongPass123!",
  confirmation = password,
) {
  fillLogin(email, password);
  fireEvent.change(screen.getByLabelText("Xác nhận mật khẩu"), {
    target: { value: confirmation },
  });
}

function deferred<T>() {
  let resolve!: (value: T | PromiseLike<T>) => void;
  let reject!: (reason?: unknown) => void;
  const promise = new Promise<T>((resolvePromise, rejectPromise) => {
    resolve = resolvePromise;
    reject = rejectPromise;
  });

  return { promise, reject, resolve };
}

describe("AuthForm", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    cleanup();
  });

  it("matches the room preview image sizes to the auth layout breakpoint", () => {
    render(<AuthForm mode="login" />);

    expect(screen.getByAltText(/RenderVN AI/)).toHaveAttribute(
      "sizes",
      "(max-width: 900px) 100vw, 52vw",
    );
  });

  it("requires email and password before login", () => {
    render(<AuthForm mode="login" />);

    fireEvent.click(screen.getByRole("button", { name: "Đăng nhập" }));

    expect(screen.getByText("Vui lòng nhập email.")).toBeInTheDocument();
    expect(screen.getByText("Vui lòng nhập mật khẩu.")).toBeInTheDocument();
    expect(mocks.login).not.toHaveBeenCalled();
  });

  it("rejects an invalid email before login", () => {
    render(<AuthForm mode="login" />);
    fillLogin("khong-phai-email");

    fireEvent.click(screen.getByRole("button", { name: "Đăng nhập" }));

    expect(screen.getByText("Email chưa đúng định dạng.")).toBeInTheDocument();
    expect(mocks.login).not.toHaveBeenCalled();
  });

  it("enforces register password length and confirmation", () => {
    render(<AuthForm mode="register" />);
    fillRegister("architect@example.com", "1234567", "1234568");

    fireEvent.click(screen.getByRole("button", { name: "Tạo tài khoản" }));

    expect(screen.getByText("Mật khẩu cần có ít nhất 8 ký tự.")).toBeInTheDocument();
    expect(screen.getByText("Mật khẩu xác nhận chưa khớp.")).toBeInTheDocument();
    expect(mocks.register).not.toHaveBeenCalled();
  });

  it("logs in once with a trimmed email, preserves the password, and replaces the route", async () => {
    mocks.login.mockResolvedValue(undefined);
    render(<AuthForm mode="login" />);
    fillLogin("  architect@example.com  ", "  StrongPass123!  ");

    fireEvent.click(screen.getByRole("button", { name: "Đăng nhập" }));

    await waitFor(() => {
      expect(mocks.login).toHaveBeenCalledTimes(1);
      expect(mocks.login).toHaveBeenCalledWith(
        "architect@example.com",
        "  StrongPass123!  ",
      );
      expect(mocks.replace).toHaveBeenCalledWith("/app");
    });
  });

  it("registers once and replaces the route", async () => {
    mocks.register.mockResolvedValue({
      email: "architect@example.com",
      availableCredits: 20,
      reservedCredits: 0,
    });
    render(<AuthForm mode="register" />);
    fillRegister();

    fireEvent.click(screen.getByRole("button", { name: "Tạo tài khoản" }));

    await waitFor(() => {
      expect(mocks.register).toHaveBeenCalledTimes(1);
      expect(mocks.register).toHaveBeenCalledWith("architect@example.com", "StrongPass123!");
      expect(mocks.replace).toHaveBeenCalledWith("/app");
    });
  });

  it.each([
    ["login", "invalid_credentials", "Email hoặc mật khẩu chưa đúng."],
    ["register", "duplicate_email", "Email này đã được đăng ký."],
    [
      "register",
      "registration_failed",
      "Chưa thể đăng ký. Vui lòng kiểm tra yêu cầu mật khẩu và thử lại.",
    ],
    [
      "register",
      "grant_failed",
      "Chưa thể cấp 20 credit cho tài khoản. Vui lòng thử lại.",
    ],
  ] as const)("maps %s error %s to a Vietnamese alert", async (mode, code, message) => {
    const request = mode === "login" ? mocks.login : mocks.register;
    request.mockRejectedValueOnce(new ApiError(code, "Unsafe upstream detail", 400, "req-123"));
    render(<AuthForm mode={mode} />);
    if (mode === "login") fillLogin();
    else fillRegister();

    fireEvent.click(
      screen.getByRole("button", { name: mode === "login" ? "Đăng nhập" : "Tạo tài khoản" }),
    );

    expect(await screen.findByRole("alert")).toHaveTextContent(message);
  });

  it("disables the form and blocks a second submit while login is pending", async () => {
    const loginRequest = deferred<void>();
    mocks.login.mockReturnValue(loginRequest.promise);
    render(<AuthForm mode="login" />);
    fillLogin();
    const email = screen.getByLabelText("Email");
    const password = screen.getByLabelText("Mật khẩu");
    const button = screen.getByRole("button", { name: "Đăng nhập" });
    const form = button.closest("form");
    expect(form).not.toBeNull();

    act(() => {
      form?.dispatchEvent(new Event("submit", { bubbles: true, cancelable: true }));
      form?.dispatchEvent(new Event("submit", { bubbles: true, cancelable: true }));
    });

    expect(mocks.login).toHaveBeenCalledTimes(1);
    expect(email).toBeDisabled();
    expect(password).toBeDisabled();
    expect(screen.getByRole("button", { name: "Đang đăng nhập..." })).toBeDisabled();

    await act(async () => {
      loginRequest.resolve();
      await loginRequest.promise;
    });
    await waitFor(() => expect(mocks.replace).toHaveBeenCalledWith("/app"));
  });

  it("restores the login form after an API failure so the user can retry", async () => {
    mocks.login.mockRejectedValueOnce(new Error("network down")).mockResolvedValueOnce(undefined);
    render(<AuthForm mode="login" />);
    fillLogin();

    fireEvent.click(screen.getByRole("button", { name: "Đăng nhập" }));

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Chưa thể đăng nhập. Vui lòng thử lại.",
    );
    expect(screen.getByLabelText("Email")).toBeEnabled();
    expect(screen.getByLabelText("Mật khẩu")).toBeEnabled();
    const retryButton = screen.getByRole("button", { name: "Đăng nhập" });
    expect(retryButton).toBeEnabled();

    fireEvent.click(retryButton);

    await waitFor(() => {
      expect(mocks.login).toHaveBeenCalledTimes(2);
      expect(mocks.replace).toHaveBeenCalledWith("/app");
    });
  });
});
