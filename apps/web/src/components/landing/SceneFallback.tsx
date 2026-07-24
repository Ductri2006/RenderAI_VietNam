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
      role="img"
      aria-label="Bản xem trước kiến trúc với các lớp mặt phẳng, đường phối cảnh và điểm nhấn màu đất nung"
      data-static={reducedMotion || reason !== "preview" ? "true" : "false"}
    >
      <div className="scene-fallback__ceiling" aria-hidden="true" />
      <div className="scene-fallback__wall scene-fallback__wall--back" aria-hidden="true" />
      <div className="scene-fallback__wall scene-fallback__wall--side" aria-hidden="true" />
      <div className="scene-fallback__floor" aria-hidden="true" />
      <div className="scene-fallback__grid" aria-hidden="true" />
      <div className="scene-fallback__frame" aria-hidden="true" />
      <div className="scene-fallback__accent" aria-hidden="true" />
      <div className="scene-fallback__label">
        <span>{reasonCopy[reason]}</span>
        <strong>Không phải kết quả AI</strong>
      </div>
    </div>
  );
}
