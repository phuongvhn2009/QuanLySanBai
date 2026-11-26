# Hệ thống Thiết kế Giao diện Sân Bãi (Chuẩn màu 60-30-10)

Tài liệu quy chuẩn bảng màu, kiểu chữ và nguyên tắc sử dụng token cho toàn bộ ứng dụng **Sân Bãi** (`QuanLySanBai/wwwroot/css/site.css`, `_Layout.cshtml`, `_AccountNav.cshtml` và các view ASP.NET Core MVC + Bootstrap 5.3.3).

## 1. Bảng token màu theo tỉ lệ 60-30-10

| Tỉ lệ | Vai trò | Token CSS | Mã màu | Phạm vi áp dụng |
|---|---|---|---|---|
| **60%** | Nền trung tính (*Sân sáng*) | `--sb-bg` | `#F4F6F8` | Nền trang chính (`body`) |
| **60%** | Bề mặt thẻ / form | `--sb-surface` | `#FFFFFF` | Card, bảng, input, modal, dropdown |
| **60%** | Bề mặt phụ | `--sb-surface-soft` | `#EDF1F4` | Header bảng, khung tóm tắt, banner phụ, vùng kéo thả |
| **60%** | Viền phân tách | `--sb-border` | `#D5DDE3` | Viền card, input, nút phụ, phân cách |
| **30%** | Màu phụ (*Đêm sân — Navy*) | `--sb-navy` | `#0F2233` | Navbar, footer, khối hero tìm sân, panel auth, chữ chính (`--sb-ink`), trạng thái đang chọn của chip/date-pill/tab |
| **30%** | Navy nổi / hover | `--sb-navy-2` | `#1B3448` | Nút `.btn-secondary`, hover card tương tác |
| **30%** | Navy nhạt | `--sb-navy-soft` | `#E6ECF1` | Nền mục đang chọn (`.sb-slot-row.is-selected`, `.choice-card.is-selected`), pill thông tin |
| **30%** | Liên kết & focus | `--sb-link` | `#1D4E79` | Thẻ `<a>`, viền focus `:focus-visible`, focus input |
| **30%** | Chữ bậc 2 / Chữ phụ | `--sb-ink-2` / `--sb-muted` | `#33495B` / `#52616E` | Tiêu đề bảng, mô tả phụ, nhãn |
| **10%** | Điểm nhấn (*Đỏ thương hiệu*) | `--sb-accent` | `#D7141E` | Chỉ dùng cho điểm nhấn chủ đạo (xem mục 2) |
| **10%** | Đỏ hover | `--sb-accent-hover` | `#B3101A` | Trạng thái hover/active của CTA chính |
| **10%** | Đỏ nhạt | `--sb-accent-soft` | `#FDECEE` | Subtle primary (`--bs-primary-bg-subtle`) |

## 2. Quy tắc nghiêm ngặt cho 10% Đỏ thương hiệu (`--sb-accent`)

Để giữ đúng tỉ lệ 10% và giúp hành động chính nổi bật tức thì, **chỉ** sử dụng `--sb-accent` tại:

1. Nút hành động chính tô đầy `.btn-primary` (Tìm sân, Tạo tài khoản, Xác nhận đặt sân, Xem cơ sở...).
2. Biểu trưng thương hiệu `.sb-brand-mark` trên thanh điều hướng.
3. Vạch chỉ báo mục đang hoạt động trên thanh điều hướng (`.app-nav .nav-link.active`).
4. Bước hiện tại trong quy trình đặt sân (`.booking-step.is-active`).
5. Đồng hồ đếm ngược giữ chỗ thanh toán (`.sb-countdown-big`).
6. Vạch nhấn trang đăng nhập / đăng ký (`.sb-auth-accent-line`).

Mọi trạng thái lựa chọn khác (chip môn thể thao, thẻ chọn ngày, khung giờ đang chọn, nút `.btn-outline-primary`, `.nav-pills .nav-link.active`, tiêu đề bảng, `.text-primary`) đều dùng tông **Navy (`--sb-navy` / `--sb-navy-soft`)**.

## 3. Màu ngữ nghĩa (Semantic Status — ngoài 60-30-10)

Chỉ dùng để truyền đạt trạng thái nghiệp vụ, không dùng trang trí:

| Ngữ nghĩa | Chữ / Viền | Nền nhạt | Ghi chú |
|---|---|---|---|
| **Lỗi / Cảnh báo nguy hiểm** | `--sb-danger` (`#9F1239`) | `--sb-danger-soft` (`#FCE8EE`) | Đỏ hồng sậm tách biệt hẳn khỏi đỏ thương hiệu `#D7141E` |
| **Thành công / Còn trống** | `--sb-success` (`#166342`) | `--sb-success-soft` (`#E7F5EC`) | Khung giờ trống, đã xác nhận, thanh toán đủ |
| **Chờ xử lý / Giữ chỗ** | `--sb-warning` (`#8A5300`) | `--sb-warning-soft` (`#FFF3D6`) | Chờ thanh toán, đang giữ chỗ (`--sb-warning-dot: #D99B26`) |
| **Thông tin / Tự động** | `--sb-navy` (`#0F2233`) | `--sb-navy-soft` (`#E6ECF1`) | `.sb-pill-info`, `.sb-info-banner`, `--bs-info-bg-subtle` |

Các biến Bootstrap 5.3.3 (`--bs-primary-rgb`, `--bs-secondary-rgb`, `--bs-success-rgb`, `--bs-warning-rgb`, `--bs-danger-rgb`, `--bs-info-rgb`, `--bs-*-bg-subtle`, `--bs-*-text-emphasis`) đã được ánh xạ đồng bộ trong `site.css` để các lớp `.alert-*`, `.badge`, `.btn-*` tự động tuân thủ bảng màu mới.

## 4. Kiểu chữ (Typography)

- **Chữ thân & UI**: `'Be Vietnam Pro'` (trọng số 400, 500, 600, 700), cỡ gốc `16px`, `line-height: 1.5`.
- **Kiểu chữ hiển thị (*Bảng tỉ số sân vận động*)**: `'Barlow Condensed'` (600, 700, có hỗ trợ tiếng Việt), chỉ áp dụng tại:
  - `h1` (`2.25rem`, `line-height: 1.1`)
  - `.sb-kpi-value`
  - `.sb-countdown-big`
  - `.date-pill strong`
  - `.slot-card h2`
  - `.sb-price` (kèm `font-variant-numeric: tabular-nums`)
- `.eyebrow` giữ chữ thường (sentence case, không `text-transform: uppercase`).

## 5. Bảng kiểm tra tương phản WCAG 2.1 AA (tối thiểu 4.5:1)

| Cặp màu (Chữ trên Nền) | Tỉ lệ đo thực tế | Chuẩn AA |
|---|---|---|
| `#FFFFFF` trên `#D7141E` (`--sb-accent`) | **5.24:1** | Đạt |
| `#FFFFFF` trên `#B3101A` (`--sb-accent-hover`) | **6.99:1** | Đạt |
| `#0F2233` trên `#F4F6F8` (`--sb-ink` / `--sb-bg`) | **14.95:1** | Đạt |
| `#52616E` trên `#FFFFFF` (`--sb-muted` / `--sb-surface`) | **6.38:1** | Đạt |
| `#52616E` trên `#F4F6F8` (`--sb-muted` / `--sb-bg`) | **5.89:1** | Đạt |
| `#FFFFFF` trên `#0F2233` (Chữ trắng trên `--sb-navy`) | **16.19:1** | Đạt |
| `rgba(255,255,255,.72)` (`#BCC1C6`) trên `#0F2233` (Footer) | **8.93:1** | Đạt |
| `rgba(255,255,255,.78)` (`#CACED2`) trên `#0F2233` (Nav link) | **10.23:1** | Đạt |
| `#1D4E79` trên `#FFFFFF` (`--sb-link` / `--sb-surface`) | **8.68:1** | Đạt |
| `#9F1239` trên `#FCE8EE` (`--sb-danger` / `--sb-danger-soft`) | **6.84:1** | Đạt |
| `#166342` trên `#E7F5EC` (`--sb-success` / `--sb-success-soft`) | **6.44:1** | Đạt |
| `#8A5300` trên `#FFF3D6` (`--sb-warning` / `--sb-warning-soft`) | **5.74:1** | Đạt |
| `#0F2233` trên `#E6ECF1` (`--sb-navy` / `--sb-navy-soft`) | **13.60:1** | Đạt |
| `#B3101A` trên `#FDECEE` (`--sb-accent-hover` / `--sb-accent-soft`) | **6.12:1** | Đạt |

## 6. Quy tắc viết mã View & Follow-up

- **Không viết thẳng mã hex trong các file `.cshtml`**: luôn dùng biến `--sb-*` trong CSS hoặc các class ngữ nghĩa (`.sb-search-hero`, `.sb-summary-box`, `.sb-lightbox-content`, `.sb-price`, `.status-dot.is-*`, `.sb-pill-*`, `.w-fit`).
- **Ghi chú đồng bộ Figma (Follow-up)**: Plugin Figma tại `tools/figma/plugin/code.js` hiện vẫn lưu bộ token xanh dương cũ và chưa được đồng bộ với bảng màu 60-30-10 mới trong đợt cập nhật này.
