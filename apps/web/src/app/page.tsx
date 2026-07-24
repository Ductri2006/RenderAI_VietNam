import { HeroCopy } from "@/components/landing/HeroCopy";
import { LandingSections } from "@/components/landing/LandingSections";
import { ScrollRoom } from "@/components/landing/ScrollRoom";

export default function Home() {
  return (
    <main className="site-shell">
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
