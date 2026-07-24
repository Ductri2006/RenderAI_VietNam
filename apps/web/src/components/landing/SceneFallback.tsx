type SceneFallbackProps = {
  reducedMotion?: boolean;
  reason?: "preview" | "reduced-motion" | "error";
};

const reasonCopy = {
  preview: "Bản xem trước không gian",
  "reduced-motion": "Bản xem trước tĩnh",
  error: "Bản xem trước tĩnh",
} as const;

export function SceneFallback({ reducedMotion = false, reason = "preview" }: SceneFallbackProps) {
  return (
    <div
      className="scene-fallback"
      role="group"
      aria-labelledby="scene-fallback-title"
      data-static={reducedMotion || reason !== "preview" ? "true" : "false"}
    >
      <Image
        className="scene-fallback__image"
        src="/room-preview.webp"
        alt="Bản xem trước kiến trúc với lớp tường, sàn và điểm nhấn đất nung"
        fill
        sizes="(max-width: 900px) 100vw, 55vw"
      />
      <div className="scene-fallback__ceiling" aria-hidden="true" />
      <div className="scene-fallback__wall scene-fallback__wall--back" aria-hidden="true" />
      <div className="scene-fallback__wall scene-fallback__wall--side" aria-hidden="true" />
      <div className="scene-fallback__floor" aria-hidden="true" />
      <div className="scene-fallback__grid" aria-hidden="true" />
      <div className="scene-fallback__frame" aria-hidden="true" />
      <div className="scene-fallback__accent" aria-hidden="true" />
      <div className="scene-fallback__label">
        <span id="scene-fallback-title">{reasonCopy[reason]}</span>
        <strong>Không phải kết quả AI</strong>
      </div>
    </div>
  );
}
import Image from "next/image";
