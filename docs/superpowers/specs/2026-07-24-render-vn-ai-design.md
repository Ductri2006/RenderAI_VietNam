# RenderVN AI - Đặc tả thiết kế bản hackathon

## 1. Mục tiêu

RenderVN AI là prototype full-stack cho phép kiến trúc sư, xưởng mộc và đơn vị nội thất tại Việt Nam tải ảnh hiện trạng hoặc bản phác thảo phối cảnh, chọn phong cách, rồi nhận các phương án phối cảnh nội thất do AI tạo ra.

Mục tiêu của bản hackathon sau hai tháng là chứng minh một luồng sản phẩm hoàn chỉnh có thể chạy trực tiếp: đăng ký, tạo dự án, tải ảnh, chọn phong cách, tạo render, trừ credit, xem lịch sử và trình bày trải nghiệm landing page 3D.

Sản phẩm được định vị là công cụ hỗ trợ tư vấn ý tưởng, không phải công cụ tạo mô hình 3D hoặc bản vẽ thi công chính xác.

## 2. Phạm vi MVP

### Luồng người dùng

1. Người dùng đăng ký và nhận 20 credit.
2. Người dùng tạo dự án và chọn loại phòng.
3. Người dùng chọn một trong hai cách tạo đầu vào: tải ảnh/sketch hoặc tự vẽ trên canvas.
4. Nếu vẽ, người dùng dùng bút, tẩy, chọn độ dày nét, hoàn tác/làm lại, xóa canvas và xuất bản vẽ PNG.
5. Người dùng chọn Modern, Japandi, Indochine hoặc Minimalist.
6. Người dùng nhập màu sắc, vật liệu và yêu cầu ngắn.
7. Hệ thống tạo một `RenderJob`, tạm giữ 4 credit và hiển thị tiến độ.
8. AI tạo hai phương án render.
9. Người dùng xem so sánh trước-sau, tải ảnh và xem lại lịch sử.
10. Người dùng có thể nạp credit qua VNPay sandbox hoặc luồng mô phỏng được đánh dấu rõ ràng.

### Các màn hình

- Landing page với cảnh 3D scroll-driven.
- Đăng ký và đăng nhập.
- Dashboard dự án.
- Màn hình tạo thiết kế.
- Canvas vẽ ý tưởng với bút, tẩy, undo/redo, xóa canvas và xuất PNG.
- Màn hình theo dõi tác vụ render.
- Màn hình kết quả và so sánh ảnh.
- Lịch sử thiết kế.
- Gói credit và thanh toán sandbox.
- Quản trị tối thiểu cho người dùng, tác vụ và lỗi.

### Giới hạn cố ý

- Chỉ hỗ trợ phòng khách, phòng ngủ và phòng bếp.
- Ảnh hiện trạng và sketch phối cảnh là đầu vào chính; mặt bằng 2D chỉ tạo ý tưởng, không đảm bảo kích thước.
- Canvas chỉ là bảng vẽ raster cho nét phác thảo; không có layer, snap, đo kích thước, vector editing hoặc công cụ CAD.
- Thời gian render mục tiêu là 30-120 giây.
- Không huấn luyện model riêng.
- Không làm mobile app, thanh toán production, subscription định kỳ, editor 3D hay hệ thống chịu tải lớn.

## 3. Trải nghiệm landing page 3D

Cảnh 3D được ghim trong khoảng 4-5 màn hình cuộn. Spline dựng scene; GSAP ScrollTrigger liên kết cuộn với camera, vật liệu và các lớp nội dung.

Các trạng thái:

1. Căn phòng xuất hiện bằng nét phác thảo.
2. Tường, cửa và đồ nội thất được dựng lên.
3. Hiệu ứng quét thể hiện AI đang xử lý.
4. Căn phòng chuyển giữa Modern, Japandi và Indochine.
5. Camera lùi ra, hiện ảnh trước-sau và CTA "Tạo thiết kế đầu tiên".

Model 3D mục tiêu dưới 5-8 MB. 3D được tải lazy sau nội dung chính; mobile hoặc thiết bị yếu dùng ảnh/video dự phòng. Landing hỗ trợ `prefers-reduced-motion` và có nút bỏ qua hiệu ứng.

## 4. Kiến trúc

```text
Next.js + Spline/GSAP
          |
          v
ASP.NET Core Web API ---- PostgreSQL
          |                      |
          v                      v
       FastAPI                  Cloudinary
          |
          v
        Replicate
```

### Frontend

Next.js chịu trách nhiệm landing, dashboard, form tạo thiết kế, canvas vẽ bằng Fabric.js, trạng thái render, lịch sử và kết quả. Các yêu cầu `/api` được chuyển tiếp tới ASP.NET để giữ cookie đăng nhập first-party và giảm lỗi CORS.

### Core API

ASP.NET Core Web API dùng ASP.NET Identity cho tài khoản, quản lý project, credit, giao dịch, trạng thái render và quyền truy cập. API là nơi quyết định credit, không để frontend tự cộng hoặc trừ.

### AI service

FastAPI kiểm tra ảnh, chuẩn hóa kích thước, chọn pipeline ControlNet, tạo prompt từ style preset, gọi nhà cung cấp AI và trả trạng thái cùng ảnh kết quả.

### Hạ tầng

- Vercel cho Next.js.
- Render cho ASP.NET Core và FastAPI.
- Supabase PostgreSQL.
- Cloudinary cho ảnh.
- Replicate cho inference.
- Sentry cho lỗi.

Không dùng Kubernetes, RabbitMQ hoặc Redis trong MVP. Bảng `RenderJobs` trong PostgreSQL được dùng như hàng đợi đơn giản; sau hackathon có thể thay bằng worker và queue chuyên dụng.

## 5. Luồng render và AI

1. Frontend tải ảnh lên storage; canvas được xuất thành PNG trước khi gửi.
2. Frontend gửi URL ảnh cùng `sourceType` (`upload` hoặc `canvas`) tới Core API.
3. Core API kiểm tra tài khoản, tạo job và tạm giữ 4 credit.
4. FastAPI phân loại ảnh, chuẩn hóa ảnh và chọn Depth/Canny cho ảnh thật hoặc Scribble/MLSD cho sketch/canvas.
5. FastAPI ghép prompt ẩn theo loại phòng, phong cách, vật liệu, ánh sáng và yêu cầu người dùng.
6. Nhà cung cấp AI tạo hai ảnh với seed khác nhau.
7. Kết quả được lưu cùng model, seed, prompt version, thời gian và chi phí.
8. Core API cập nhật job thành công và trừ credit; lỗi sau lần retry thì hoàn credit.

Bộ kiểm thử AI gồm 20-30 ảnh. Mỗi kết quả chấm theo giữ bố cục, đúng phong cách, chất lượng hình ảnh và thời gian/chi phí. Mục tiêu là ít nhất 70% kết quả đạt tổng điểm từ 15/20.

## 6. Dữ liệu và credit

Các bảng chính:

- `Users` và bảng của ASP.NET Identity.
- `Projects`.
- `SourceImages`.
- `RenderJobs`.
- `RenderResults`.
- `CreditWallets`.
- `CreditTransactions`.
- `PaymentOrders`.
- `StylePresets`.
- `AuditEvents`.

`SourceImages.sourceType` nhận `upload` hoặc `canvas`. Với nguồn canvas, hệ thống lưu PNG cuối cùng; dữ liệu JSON của Fabric.js chỉ lưu khi cần khôi phục bản vẽ trong phiên hiện tại, không dùng làm đầu vào trực tiếp cho AI.

Một lần render tạo hai ảnh và dùng 4 credit. Credit được tạm giữ khi bắt đầu, trừ khi thành công và hoàn khi thất bại. Mỗi request có idempotency key để tránh trừ hai lần.

Các gói hiển thị trong bản thi:

- Miễn phí: 20 credit.
- Cá nhân: 50.000 đồng / 100 credit.
- Chuyên nghiệp: 199.000 đồng / 500 credit.

Gói Studio 299.000 đồng/tháng chỉ là hướng phát triển sau hackathon, không triển khai subscription định kỳ trong MVP.

## 7. An toàn và lỗi

- Mật khẩu do ASP.NET Identity quản lý.
- Cookie đăng nhập bảo mật; không lưu secret trong localStorage.
- API key chỉ nằm trên server.
- Ảnh riêng tư dùng URL có thời hạn.
- Kiểm tra MIME type, kích thước và nội dung file.
- Rate limit đăng nhập, upload và render.
- Không ghi token hoặc dữ liệu thanh toán nhạy cảm vào log.
- Lỗi AI tạm thời được retry một lần.
- Timeout hoặc lỗi cuối cùng hoàn credit.
- Thanh toán chỉ cộng credit sau khi xác minh callback server-to-server.
- Spline có ảnh/video fallback.

## 8. Kiểm thử và demo

Kiểm thử gồm logic credit, tích hợp Core-FastAPI-AI, luồng người dùng end-to-end, responsive landing và bộ đánh giá AI. Các tình huống phải thử gồm hết credit, file lỗi, AI timeout, gửi request trùng, thanh toán đóng sớm, mất mạng và Spline không tải được.

Demo có ba lớp:

1. Tác vụ live gọi AI.
2. Tác vụ đã hoàn thành trong tài khoản demo nếu API chậm.
3. Video quay sẵn nếu internet hoặc nhà cung cấp AI gặp sự cố.

Kịch bản thuyết trình dài 3-5 phút: vấn đề -> landing 3D -> tạo dự án -> render live -> kết quả trước-sau -> credit -> tác động kinh doanh -> hướng phát triển.

## 9. Tiêu chí nghiệm thu

- Ứng dụng công khai chạy được.
- Luồng đăng ký đến nhận ảnh hoạt động.
- Credit không bị trừ sai trong các trường hợp retry hoặc lỗi.
- Có ít nhất 20 bộ ảnh kiểm thử và bảng điểm.
- Landing có scroll 3D, fallback mobile và nút bỏ qua.
- Có tài khoản demo, video dự phòng và dữ liệu mẫu.
- Bài thuyết trình giải thích rõ giới hạn: đây là phối cảnh ý tưởng, không phải mô hình 3D kỹ thuật.
