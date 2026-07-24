"use client";

import dynamic from "next/dynamic";
import type { ComponentType, ReactNode } from "react";
import { Component, useEffect, useRef, useState } from "react";
import gsap from "gsap";
import { ScrollTrigger } from "gsap/ScrollTrigger";
import { SceneFallback } from "./SceneFallback";

gsap.registerPlugin(ScrollTrigger);

type SceneState = "sketch" | "build" | "scan" | "styles" | "reveal";
type SplineSceneProps = { scene: string };

const sceneUrl = process.env.NEXT_PUBLIC_SPLINE_SCENE_URL;
const SplineScene = sceneUrl
  ? dynamic<SplineSceneProps>(
      () =>
        import("@splinetool/react-spline").then(
          (module) => module.default as unknown as ComponentType<SplineSceneProps>,
        ),
      {
        ssr: false,
        loading: () => <SceneLoading />,
      },
    )
  : null;

class SplineErrorBoundary extends Component<
  { children: ReactNode; onError: () => void },
  { hasError: boolean }
> {
  state = { hasError: false };

  static getDerivedStateFromError() {
    return { hasError: true };
  }

  componentDidCatch() {
    this.props.onError();
  }

  render() {
    return this.state.hasError ? null : this.props.children;
  }
}

export function isWebGLAvailable() {
  if (typeof document === "undefined") return false;

  try {
    const canvas = document.createElement("canvas");
    return Boolean(canvas.getContext("webgl2") || canvas.getContext("webgl"));
  } catch {
    return false;
  }
}

function isNarrowDevice() {
  return window.matchMedia("(max-width: 720px)").matches || window.innerWidth <= 720;
}

function SceneLoading() {
  return (
    <div className="scene-loading" role="status" aria-live="polite">
      <span className="scene-loading__bar" />
      <span>Đang dựng không gian</span>
    </div>
  );
}

function getSceneState(progress: number): SceneState {
  if (progress < 0.2) return "sketch";
  if (progress < 0.4) return "build";
  if (progress < 0.6) return "scan";
  if (progress < 0.8) return "styles";
  return "reveal";
}

export function ScrollRoom() {
  const roomRef = useRef<HTMLDivElement>(null);
  const [state, setState] = useState<SceneState>("sketch");
  const [reducedMotion, setReducedMotion] = useState(false);
  const [sceneError, setSceneError] = useState(false);
  const [sceneReady, setSceneReady] = useState(false);
  const [preparingScene, setPreparingScene] = useState(false);
  const [capability, setCapability] = useState<{ webgl: boolean; narrow: boolean } | null>(null);

  useEffect(() => {
    const media = window.matchMedia("(prefers-reduced-motion: reduce)");
    const update = () => setReducedMotion(media.matches);
    update();
    media.addEventListener?.("change", update);
    return () => media.removeEventListener?.("change", update);
  }, []);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setCapability({ webgl: isWebGLAvailable(), narrow: isNarrowDevice() });
    }, 0);
    return () => window.clearTimeout(timer);
  }, []);

  useEffect(() => {
    if (!capability) return;
    const { narrow: isNarrow, webgl: hasWebgl } = capability;

    if (!sceneUrl || reducedMotion || isNarrow || !hasWebgl || !roomRef.current) return;

    const room = roomRef.current;
    let idleHandle: number | undefined;
    let timeoutHandle: number | undefined;
    let cancelled = false;
    const observer = new IntersectionObserver(
      ([entry]) => {
        if (!entry.isIntersecting || cancelled) return;
        observer.disconnect();
        setPreparingScene(true);
        const reveal = () => {
          if (!cancelled) {
            setSceneReady(true);
            setPreparingScene(false);
          }
        };
        const idleWindow = window as Window & {
          requestIdleCallback?: (callback: () => void, options?: { timeout: number }) => number;
          cancelIdleCallback?: (handle: number) => void;
        };
        if (typeof idleWindow.requestIdleCallback === "function") {
          idleHandle = idleWindow.requestIdleCallback(reveal, { timeout: 1200 });
        } else {
          timeoutHandle = window.setTimeout(reveal, 260);
        }
      },
      { rootMargin: "160px 0px" },
    );
    observer.observe(room);

    return () => {
      cancelled = true;
      observer.disconnect();
      const idleWindow = window as Window & { cancelIdleCallback?: (handle: number) => void };
      if (idleHandle !== undefined) idleWindow.cancelIdleCallback?.(idleHandle);
      if (timeoutHandle !== undefined) window.clearTimeout(timeoutHandle);
    };
  }, [capability, reducedMotion]);

  useEffect(() => {
    if (!roomRef.current || reducedMotion || window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;

    const context = gsap.context(() => {
      const timeline = gsap.timeline({
        defaults: { ease: "none" },
        scrollTrigger: {
          trigger: roomRef.current,
          start: "top top",
          end: () => `+=${window.innerHeight * 5}`,
          pin: true,
          scrub: 0.8,
          invalidateOnRefresh: true,
          onUpdate: (trigger) => setState(getSceneState(trigger.progress)),
        },
      });

      timeline.to(roomRef.current, { backgroundPosition: "50% 42%", duration: 1 });
      timeline.to(roomRef.current, { backgroundPosition: "58% 48%", duration: 1 });
      timeline.to(roomRef.current, { backgroundPosition: "46% 52%", duration: 1 });
      timeline.to(roomRef.current, { backgroundPosition: "50% 50%", duration: 1 });
      timeline.to(roomRef.current, { backgroundPosition: "50% 50%", duration: 1 });
    }, roomRef);

    return () => context.revert();
  }, [reducedMotion]);

  const fallbackReason = sceneError
    ? "error"
    : reducedMotion
      ? "reduced-motion"
      : "preview";
  const sceneBlocked = !sceneUrl || reducedMotion || !capability?.webgl || capability.narrow;

  return (
    <div className="room-wrap">
      <a className="scene-bypass" href="#how-it-works">
        Bỏ qua không gian chuyển động
      </a>
      <div
        ref={roomRef}
        className="scroll-room"
        data-state={reducedMotion ? "reveal" : state}
        aria-labelledby="scroll-room-title"
      >
        <span id="scroll-room-title" className="sr-only">
          Không gian mô phỏng năm bước của RenderVN AI
        </span>
        <div className="scroll-room__topline" aria-hidden="true">
          <span>01</span>
          <span>05</span>
        </div>
        {SplineScene && sceneUrl && sceneReady && !sceneBlocked && !sceneError ? (
          <SplineErrorBoundary onError={() => setSceneError(true)}>
            <SplineScene scene={sceneUrl} />
          </SplineErrorBoundary>
        ) : preparingScene && !sceneError ? (
          <SceneLoading />
        ) : (
          <SceneFallback reducedMotion={reducedMotion} reason={fallbackReason} />
        )}
        <div className="scroll-room__state" aria-live="polite">
          <span className="scroll-room__state-dot" aria-hidden="true" />
          <span>{state}</span>
        </div>
        <div className="scroll-room__caption">
          {reducedMotion ? "Chế độ chuyển động tối giản" : "Kéo qua để thấy không gian thành hình"}
        </div>
      </div>
    </div>
  );
}
