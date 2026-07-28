"use client";

import api, { ApiError } from "../../lib/api";
import Image from "next/image";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { FormEvent, useRef, useState } from "react";

type AuthMode = "login" | "register";

interface AuthFormProps {
  mode: AuthMode;
}

interface FieldErrors {
  email?: string;
  password?: string;
  confirmation?: string;
}

const API_ERROR_MESSAGES: Record<string, string> = {
  duplicate_email: "Email này đã được đăng ký.",
  grant_failed: "Chưa thể cấp 20 credit cho tài khoản. Vui lòng thử lại.",
  invalid_credentials: "Email hoặc mật khẩu chưa đúng.",
  registration_failed: "Chưa thể đăng ký. Vui lòng kiểm tra yêu cầu mật khẩu và thử lại.",
};

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

function validate(
  mode: AuthMode,
  email: string,
  password: string,
  confirmation: string,
): FieldErrors {
  const errors: FieldErrors = {};

  if (!email) errors.email = "Vui lòng nhập email.";
  else if (!EMAIL_PATTERN.test(email)) errors.email = "Email chưa đúng định dạng.";

  if (!password) errors.password = "Vui lòng nhập mật khẩu.";
  else if (mode === "register" && password.length < 8) {
    errors.password = "Mật khẩu cần có ít nhất 8 ký tự.";
  }

  if (mode === "register") {
    if (!confirmation) errors.confirmation = "Vui lòng xác nhận mật khẩu.";
    else if (confirmation !== password) errors.confirmation = "Mật khẩu xác nhận chưa khớp.";
  }

  return errors;
}

function getApiErrorMessage(error: unknown, mode: AuthMode) {
  if (error instanceof ApiError && API_ERROR_MESSAGES[error.code]) {
    return API_ERROR_MESSAGES[error.code];
  }

  return mode === "login"
    ? "Chưa thể đăng nhập. Vui lòng thử lại."
    : "Chưa thể đăng ký. Vui lòng thử lại.";
}

export function AuthForm({ mode }: AuthFormProps) {
  const router = useRouter();
  const submittingRef = useRef(false);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmation, setConfirmation] = useState("");
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [apiError, setApiError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const isLogin = mode === "login";
  const title = isLogin ? "Tiếp tục thiết kế không gian." : "Tạo tài khoản cho dự án đầu tiên.";
  const description = isLogin
    ? "Đăng nhập để trở lại dự án và lịch sử phối cảnh của bạn."
    : "Đăng ký để nhận 20 credit và bắt đầu dựng phương án đầu tiên.";
  const submitLabel = isLogin ? "Đăng nhập" : "Tạo tài khoản";
  const loadingLabel = isLogin ? "Đang đăng nhập..." : "Đang tạo tài khoản...";

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (submittingRef.current) return;

    const normalizedEmail = email.trim();
    const errors = validate(mode, normalizedEmail, password, confirmation);
    setFieldErrors(errors);
    setApiError(null);
    if (Object.keys(errors).length > 0) return;

    submittingRef.current = true;
    setIsSubmitting(true);

    try {
      if (isLogin) await api.login(normalizedEmail, password);
      else await api.register(normalizedEmail, password);
      router.replace("/app");
    } catch (error) {
      setApiError(getApiErrorMessage(error, mode));
      submittingRef.current = false;
      setIsSubmitting(false);
    }
  }

  return (
    <main className="auth-shell">
      <aside className="auth-visual" aria-label="Xem trước không gian RenderVN AI">
        <Image
          className="auth-visual__image"
          src="/room-preview.webp"
          alt="Phối cảnh phòng khách được tạo với RenderVN AI"
          fill
          priority
          sizes="(max-width: 900px) 100vw, 52vw"
        />
        <div className="auth-visual__wash" />
        <Link className="auth-brand" href="/" aria-label="RenderVN AI, trang chủ">
          RenderVN <span>AI</span>
        </Link>
        <div className="auth-visual__copy">
          <p>Phối cảnh có chủ đích</p>
          <h2>Biến nét phác thành không gian dễ trình bày.</h2>
        </div>
      </aside>

      <section className="auth-panel" aria-labelledby="auth-heading">
        <div className="auth-panel__inner">
          <Link className="auth-back" href="/">
            Trở về trang giới thiệu
          </Link>
          <div className="auth-heading">
            <h1 id="auth-heading">{title}</h1>
            <p>{description}</p>
          </div>

          <form className="auth-form" noValidate onSubmit={handleSubmit}>
            {apiError ? <p className="auth-alert" role="alert">{apiError}</p> : null}

            <div className="auth-field">
              <label htmlFor="email">Email</label>
              <input
                id="email"
                name="email"
                type="email"
                autoComplete="email"
                inputMode="email"
                value={email}
                disabled={isSubmitting}
                aria-describedby={fieldErrors.email ? "email-error" : undefined}
                aria-invalid={Boolean(fieldErrors.email)}
                aria-required="true"
                onChange={(event) => setEmail(event.target.value)}
              />
              {fieldErrors.email ? <p id="email-error" className="auth-field__error">{fieldErrors.email}</p> : null}
            </div>

            <div className="auth-field">
              <label htmlFor="password">Mật khẩu</label>
              <input
                id="password"
                name="password"
                type="password"
                autoComplete={isLogin ? "current-password" : "new-password"}
                value={password}
                disabled={isSubmitting}
                aria-describedby={fieldErrors.password ? "password-error" : undefined}
                aria-invalid={Boolean(fieldErrors.password)}
                aria-required="true"
                onChange={(event) => setPassword(event.target.value)}
              />
              {fieldErrors.password ? <p id="password-error" className="auth-field__error">{fieldErrors.password}</p> : null}
            </div>

            {!isLogin ? (
              <div className="auth-field">
                <label htmlFor="confirmation">Xác nhận mật khẩu</label>
                <input
                  id="confirmation"
                  name="confirmation"
                  type="password"
                  autoComplete="new-password"
                  value={confirmation}
                  disabled={isSubmitting}
                  aria-describedby={fieldErrors.confirmation ? "confirmation-error" : undefined}
                  aria-invalid={Boolean(fieldErrors.confirmation)}
                  aria-required="true"
                  onChange={(event) => setConfirmation(event.target.value)}
                />
                {fieldErrors.confirmation ? (
                  <p id="confirmation-error" className="auth-field__error">{fieldErrors.confirmation}</p>
                ) : null}
              </div>
            ) : null}

            <button className="auth-submit" type="submit" disabled={isSubmitting}>
              {isSubmitting ? loadingLabel : submitLabel}
            </button>
          </form>

          <p className="auth-switch">
            {isLogin ? "Chưa có tài khoản?" : "Đã có tài khoản?"}{" "}
            <Link href={isLogin ? "/register" : "/login"}>
              {isLogin ? "Đăng ký" : "Đăng nhập"}
            </Link>
          </p>
        </div>
      </section>
    </main>
  );
}
