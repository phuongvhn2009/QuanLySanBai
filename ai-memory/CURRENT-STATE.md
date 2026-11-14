# Trạng thái bàn giao

Cập nhật: 2026-11-14, Asia/Saigon. Đây là ảnh chụp trạng thái tại lúc ghi.

## Git

- Repo: https://github.com/phuongvhn2009/QuanLySanBai.git
- Nhánh đang làm: `master`, commit `0a8233f` (kế thừa `59083dd`, đã commit sửa phân quyền cơ sở, đồng bộ SignalR realtime, kiểm thử 101 assertions và tài liệu/ai-memory, đồng bộ `origin/master`).
- Master chứa cụm 1–4, sửa giao diện/đặt sân từ GitHub, sửa phân quyền tường minh và đồng bộ SignalR realtime. Program.cs trên master chưa đăng ký OperationService.
- Cụm 5: `MO-VanHanhDon`, commit `bab70be`, đã push, chưa merge master tại lần kiểm tra gần nhất.
- Cụm 6: `MQ-KiemThuCotLoi`, commit `86ef359`, kế thừa cụm 5, đã push, chưa merge master.
- Graphify: `Tools-Graphify`, commit `c85a38d`, kế thừa cụm 5–6, đã push, chưa merge master.
- Các nhánh cụm 1–4: MA-TaiKhoanPhanQuyen, MC-CauHinhCoSo, MB-XemLichDatSan. Các cụm này đã merge master; một số nhánh trên GitHub đã có commit mới hơn nhánh local.

## Trạng thái các tài liệu và cấu hình trên master

- Đã commit lên `master`: `README.md`, `docs/` (`N01-N05-review.md`, `A01-A05-review.md`, `C01-C06-review.md`, `B01-B05-review.md`, `cluster-1-4-review.md`, `cluster-3-4-acceptance.md`, `evidence/cluster34-2026-11-14.txt`, `realtime.md`, `repo-comparison.md`, `HANDOFF.md`, `README.md`), và `ai-memory/`.
- `graphify-out/` cache đã được thêm vào `.gitignore` trên `master` để tránh nhiễu khi chuyển nhánh; các file báo cáo/đồ thị chính của Graphify vẫn nằm trên nhánh `Tools-Graphify` chờ merge.
- Phiên trước từng thấy 8 file references của skill Graphify bị thiếu. Trạng thái đó chưa được chẩn đoán hay sửa; kiểm tra trên Tools-Graphify khi cần.

## Phạm vi đã triển khai

- Cụm 1: N01–N05 — nền tảng.
- Cụm 2: A01–A04 — đăng ký, đăng nhập/cookie/hồ sơ, phân quyền và nhân viên.
- Cụm 3: C01–C06 + A05 — cấu hình sân/cơ sở và quản trị.
- Cụm 4: B01–B05 — lịch trống, đặt sân, hết hạn giữ chỗ, chống trùng.
- Cụm 5 trên nhánh riêng: O01–O06 — đặt quầy, thu tiền, xác nhận cọc, check-in, hoàn tất, hủy và danh sách đơn.
- Cụm 6 trên nhánh riêng: Q01–Q02 — kiểm thử SQL/HTTP. Lần chạy lịch sử báo 106 assertion đạt; không phải 106 test case xUnit. Chưa chạy lại sau cập nhật master ngày 13/11.
- Cụm mở rộng thanh toán/hoàn tiền chưa triển khai đầy đủ. Đơn đã thu tiền không được hủy qua luồng hủy đơn chưa thanh toán.
- Check-in trước 15 phút là giả định triển khai đang chờ người dùng duyệt.

## Việc tiếp theo

Người dùng đang kiểm tra các việc Chờ kiểm tra trên Notion. Merge không đồng nghĩa nghiệm thu. Cụm 5–6 cần người dùng duyệt trước bước merge tiếp theo theo trao đổi gần nhất.

Folder ai-memory đã được commit và push lên `origin/master` ngày 2026-11-14.

## Sửa phân quyền ngày 2026-11-14 (đã commit `0a8233f` & push lên `origin/master`)

- Người dùng báo cấp quyền vận hành không hiện chức năng; thu hồi quyền quản lý nhân viên vẫn vào được.
- AccessService trước đây tự cho QUAN_LY và CHU_SAN nội bộ vượt mọi quyền chi tiết. Đã bỏ bypass: chỉ CoSo.ChuSoHuuId có toàn quyền; thành viên cần đang hoạt động và có grant cụ thể.
- HomeController dùng CanManage chung để hiển thị nút; Home/Members trả no-store để hạn chế trang quyền cũ từ cache.
- Thêm kiểm thử SQL và HTTP cho cấp/thu hồi trong cùng phiên, URL trực tiếp và POST giả mạo. Sửa test N04 cũ để kiểm tra redirect + thông báo an toàn theo BookingsController mới.
- Master chưa có màn hình vận hành cụm 5 nên các quyền TAO_DON/THU_TIEN/CHECK_IN không có nút tương ứng trên bản hiện tại. Không tự merge cụm 5 trong lần sửa này.
- Khi tích hợp cụm 5, cần kiểm tra lại quyền TAO_DON trong usp_DatSanMotKhung: SQL hiện vẫn có nhánh cho vai trò CHU_SAN/QUAN_LY; cần đồng bộ chính sách quyền tường minh trước nghiệm thu luồng đặt quầy.
- Xác minh cuối: build 0 lỗi/0 cảnh báo; toàn bộ 90 assertion SQL/HTTP đạt trên database tạm và database đã được dọn. Chưa thử trực tiếp trên trình duyệt của người dùng; cần khởi động lại ứng dụng để chạy code mới.

## SignalR ngày 2026-11-14 (đã commit `0a8233f` & push lên `origin/master`)

- Đã thêm UpdatesHub, RealtimeUpdates, `/updates`, JS SignalR 10.0.0 local và realtime.js dùng chung layout.
- Members Add/Update gửi PermissionsChanged tới đúng user sau khi lưu; BookingService gửi CalendarChanged sau đặt sân hoặc worker giải phóng giữ chỗ. Chỉ cho subscribe sân công khai, không gửi dữ liệu đơn/khách qua thông báo.
- Có tự kết nối lại, tải mới khi quay lại tab, giữ bản nháp form POST cho cập nhật lịch (thay đổi quyền buộc tải lại).
- Xác minh: build 0 lỗi/0 cảnh báo; 101 assertion SQL/HTTP/WebSocket đạt. Database tạm đã dọn. JS đã kiểm tra cú pháp; chưa xác minh trực quan trên hai trình duyệt.
- Đọc docs/realtime.md để biết cách thử và giới hạn: một instance, chưa nối sự kiện cấu hình giá/khóa sân hay vận hành cụm 5.

## Nghiệm thu tự động cụm 3–4 ngày 2026-11-14 (đã commit & push lên `origin/master`)

- Người dùng yêu cầu tự test cụm 3–4 và trả hướng dẫn giao diện web. Đã chạy lại trên master cùng các thay đổi quyền/realtime (commit `0a8233f`); restore/build 0 lỗi/cảnh báo; 101 assertion SQL/HTTP/WebSocket đạt. Không phải 101 test riêng cụm 3–4.
- Database tạm QuanLySanBai_Cluster4Tests_cd15850dc94d4e97bfe2d10c1cea2f61 đã dọn. Không ghi dữ liệu test vào DB live.
- Báo cáo và checklist đầy đủ: docs/cluster-3-4-acceptance.md; log: docs/evidence/cluster34-2026-11-14.txt. Chưa thực hiện kiểm tra trực quan bằng chuột/trình duyệt, tổ hợp bộ lọc và countdown cần nghiệm thu thêm theo checklist.
- Đã commit và push toàn bộ thay đổi ngày 2026-11-14 cùng tài liệu/ai-memory lên `origin/master` theo yêu cầu người dùng.
