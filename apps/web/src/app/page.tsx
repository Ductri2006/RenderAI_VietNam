import { HeroCopy } from "@/components/landing/HeroCopy";
import { LandingSections } from "@/components/landing/LandingSections";
import { ScrollRoom } from "@/components/landing/ScrollRoom";
import Link from "next/link";

export default function Home() {
  return (
    <main className="site-shell">
      <nav className="site-nav page-width" aria-label="Điều hướng chính">
        <Link className="site-nav__brand" href="/" aria-label="RenderVN AI, trang chủ">
          RenderVN <span>AI</span>
        </Link>
        <div className="site-nav__links">
          <a href="#how-it-works">Quy trình</a>
          <a href="#styles">Phong cách</a>
          <a className="site-nav__cta" href="/app">
            Mở ứng dụng
          </a>
        </div>
      </nav>
      <section className="hero-wrap" aria-labelledby="hero-heading">
        <div className="hero-grid page-width">
          <HeroCopy />
          <ScrollRoom />
        </div>
      </section>
      <LandingSections />
    </main>
  );
}
