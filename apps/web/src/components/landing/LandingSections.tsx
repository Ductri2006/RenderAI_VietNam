const processSteps = [
  {
    number: "01",
    title: "Đặt nét đầu tiên",
    body: "Tải bản vẽ, ảnh hiện trạng hoặc ghi chú nhanh về căn phòng.",
  },
  {
    number: "02",
    title: "Giữ đúng ý đồ",
    body: "Chọn vật liệu, ánh sáng và phong cách mà xưởng của bạn muốn theo.",
  },
  {
    number: "03",
    title: "Trình bày để chốt",
    body: "Xuất phối cảnh có câu chuyện, sẵn sàng gửi khách hoặc đưa vào hồ sơ.",
  },
];

const styles = ["Tĩnh lặng", "Ấm vật liệu", "Đô thị", "Tối giản"];

export function LandingSections() {
  return (
    <>
      <section className="landing-section landing-section--tint" aria-labelledby="problem-heading">
        <div className="page-width">
          <div className="section-heading">
            <p className="eyebrow">Khoảng cách thường bị bỏ quên</p>
            <h2 id="problem-heading">Ý tưởng tốt không nên mắc kẹt ở bản nháp.</h2>
          </div>
          <div className="problem-grid">
            <p className="problem-copy">
              Khách hàng không mua một bản vẽ. Họ mua cảm giác được sống trong không gian đó.
            </p>
            <div className="problem-list" aria-label="Những điểm nghẽn phổ biến">
              <p>Chỉnh phối cảnh thủ công khiến mỗi vòng duyệt kéo dài thêm vài ngày.</p>
              <p>Ý đồ vật liệu bị loãng khi chuyển từ mặt bằng sang hình ảnh.</p>
              <p>Xưởng nhỏ thiếu một cách trình bày vừa nhanh vừa có gu riêng.</p>
            </div>
          </div>
        </div>
      </section>

      <section id="quy-trinh" className="landing-section" aria-labelledby="process-heading">
        <div className="page-width process-layout">
          <div className="section-heading">
            <p className="eyebrow">Quy trình gọn hơn</p>
            <h2 id="process-heading">Ba bước từ ý tưởng đến buổi duyệt.</h2>
            <p>Không cần đổi cách bạn làm việc. Chỉ cần thêm một lớp nhìn thấy được.</p>
          </div>
          <div className="process-list">
            {processSteps.map((step) => (
              <article className="process-step" key={step.number}>
                <span className="process-number">{step.number}</span>
                <div>
                  <h3>{step.title}</h3>
                  <p>{step.body}</p>
                </div>
              </article>
            ))}
          </div>
        </div>
      </section>

      <section className="landing-section landing-section--tint" aria-labelledby="styles-heading">
        <div className="page-width styles-layout">
          <div className="section-heading">
            <p className="eyebrow">Bốn hướng thẩm mỹ</p>
            <h2 id="styles-heading">Giữ chất riêng, không giữ một công thức.</h2>
          </div>
          <div className="style-list">
            {styles.map((style, index) => (
              <article className="style-option" key={style}>
                <span>0{index + 1} / mood</span>
                <h3>{style}</h3>
              </article>
            ))}
          </div>
        </div>
      </section>

      <section className="landing-section" aria-labelledby="result-heading">
        <div className="page-width result-layout">
          <div className="result-frame" role="img" aria-label="Mẫu hướng phối cảnh nội thất với lớp ánh sáng và vật liệu đất nung">
            <span className="result-label">Mẫu hướng / 01</span>
          </div>
          <div className="result-copy">
            <p className="eyebrow">Hướng kết quả mẫu</p>
            <h2 id="result-heading">Một căn phòng có thể kể chuyện trước khi xây.</h2>
            <p>Ánh sáng, tỷ lệ và vật liệu cùng đứng về một phía để khách hàng dễ hình dung hơn.</p>
          </div>
        </div>
      </section>

      <section className="final-cta" aria-labelledby="cta-heading">
        <div className="page-width final-cta-inner">
          <h2 id="cta-heading">Để nét vẽ đi xa hơn bản nháp.</h2>
          <div>
            <p>Thử tạo không gian đầu tiên của bạn và xem ý tưởng thay đổi cách được nhìn thấy.</p>
            <a className="button-primary" href="/app">
              Mở RenderVN AI
            </a>
          </div>
        </div>
      </section>

      <footer className="page-width site-footer">
        <span>RenderVN AI / 2026</span>
        <span>Thiết kế cho người làm không gian</span>
      </footer>
    </>
  );
}
