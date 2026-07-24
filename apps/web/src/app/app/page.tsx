import Link from "next/link";

export default function WorkspacePlaceholder() {
  return (
    <main className="workspace-placeholder">
      <section className="workspace-placeholder__panel" aria-labelledby="workspace-heading">
        <p className="eyebrow">RenderVN AI / bước tiếp theo</p>
        <h1 id="workspace-heading">Không gian làm việc đang được dựng.</h1>
        <p>
          Đây là bước phát triển kế tiếp của RenderVN AI. Bản hiện tại chưa có dashboard, đăng nhập
          hay công cụ dựng phối cảnh.
        </p>
        <Link className="button-primary" href="/">
          Trở về trang giới thiệu
        </Link>
      </section>
    </main>
  );
}
