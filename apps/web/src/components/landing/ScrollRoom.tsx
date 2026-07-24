"use client";

import dynamic from "next/dynamic";
import type { ComponentType } from "react";
import { useEffect, useRef, useState } from "react";
import gsap from "gsap";
import { ScrollTrigger } from "gsap/ScrollTrigger";
import { SceneFallback } from "./SceneFallback";

gsap.registerPlugin(ScrollTrigger);

type SceneState = "sketch" | "build" | "scan" | "styles" | "reveal";

const sceneUrl = process.env.NEXT_PUBLIC_SPLINE_SCENE_URL;
type SplineSceneProps = {
  scene: string;
  onLoad?: () => void;
  onError?: () => void;
};

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

  useEffect(() => {
    const media = window.matchMedia("(prefers-reduced-motion: reduce)");
    const update = () => setReducedMotion(media.matches);
    update();
    media.addEventListener?.("change", update);
    return () => media.removeEventListener?.("change", update);
  }, []);

  useEffect(() => {
    if (!roomRef.current || reducedMotion) return;

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

  return (
    <div className="room-wrap">
      <div
        ref={roomRef}
        className="scroll-room"
        data-state={reducedMotion ? "reveal" : state}
        aria-label="Không gian mô phỏng năm bước của RenderVN AI"
      >
        <div className="scroll-room__topline" aria-hidden="true">
          <span>01</span>
          <span>05</span>
        </div>
        {SplineScene && sceneUrl && !reducedMotion && !sceneError ? (
          <SplineScene
            scene={sceneUrl as string}
            onLoad={() => setSceneError(false)}
            onError={() => setSceneError(true)}
          />
        ) : (
          <SceneFallback reducedMotion={reducedMotion} reason={sceneError ? "error" : "preview"} />
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
