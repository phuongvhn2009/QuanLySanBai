# Trạng thái bàn giao

Cập nhật: 2026-11-17, Asia/Saigon.

## Git & Tích hợp

- Repo: https://github.com/phuongvhn2009/QuanLySanBai.git
- Nhánh chính: `master` (đã hợp nhất đầy đủ nhánh Cụm 5 `MO-VanHanhDon` và Cụm 6 `MQ-KiemThuCotLoi`, đồng bộ `origin/master`).
- Kết quả kiểm thử tự động ngày 2026-11-17:
  - Trên `MQ-KiemThuCotLoi`: **106/106 assertions** `Cluster4Checks` + **21/21 assertions** `AccountChecks` đạt 100%.
  - Trên `master`: **149/149 assertions** `Cluster4Checks` (gồm nền tảng, duyệt cơ sở, SignalR WebSocket và nghiệp vụ vận hành quầy) đạt 100%.
- Đồng bộ Notion ngày 2026-11-17:
  - Toàn bộ 8 tác vụ Cụm 5 & 6 (`O01–O06`, `Q01–Q02`) đã được nghiệm thu và cập nhật trạng thái **Hoàn thành** trực tiếp qua Notion API.
  - Tổng số việc hoàn thành trên Notion: **29/49 tác vụ (59%)**.
  - Tác vụ còn lại: 20 tác vụ thuộc các cụm mở rộng (`F01–F05`, `X01–X05`, `R01–R03`, `G01–G04`, `Q03–Q05`).

## Trạng thái các tài liệu và cấu hình trên master

- Đã commit lên `master`: `README.md`, `docs/` (`N01-N05-review.md`, `A01-A05-review.md`, `C01-C06-review.md`, `B01-B05-review.md`, `cluster-1-4-review.md`, `cluster-3-4-acceptance.md`, `evidence/cluster34-2026-11-14.txt`, `realtime.md`, `repo-comparison.md`, `HANDOFF.md`, `README.md`), và `ai-memory/`.
- `graphify-out/` cache đã được thêm vào `.gitignore` trên `master` để tránh nhiễu khi chuyển nhánh; các file báo cáo/đồ thị chính của Graphify vẫn nằm trên nhánh `Tools-Graphify` chờ merge.
- Phiên trước từng thấy 8 file references của skill Graphify bị thiếu. Trạng thái đó chưa được chẩn đoán hay sửa; kiểm tra trên Tools-Graphify khi cần.

## Phạm vi đã triển khai

- Cụm 1: N01–N05 — nền tảng.
- Cụm 2: A01–A04 — đăng ký, đăng nhập/cookie/hồ sơ, phân quyền và nhân viên.
- Cụm 3: C01–C06 + A05 — cấu hình sân/cơ sở và quản trị.
- Cụm 4: B01–B05 — lịch trống, đặt sân, hết hạn giữ chỗ, chống trùng.
- Cụm 5 (đã merge vào `master`): O01–O06 — đặt quầy, thu tiền, xác nhận cọc, check-in, hoàn tất, hủy và danh sách đơn.
- Cụm 6 (đã merge vào `master`): Q01–Q02 — kiểm thử SQL/HTTP. Lần chạy trên `MQ-KiemThuCotLoi` (`86ef359`) đạt 106/106 assertions; sau khi merge vào `master` đạt 148/148 assertions (gồm cả nền tảng, duyệt cơ sở, SignalR và vận hành; đây là assertion trong console runner, không phải test case xUnit).
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

## Sửa luồng duyệt cơ sở (2026-11-14, nhánh fix-coso-cho-duyet)
- CHO_DUYET/TU_CHOI chỉ hiện Đơn đăng ký cơ sở và cho chủ xem/cập nhật hồ sơ; không mở quản lý sân/cấu hình/nhân viên.
- AccessService yêu cầu DA_DUYET. FacilityService.Owned mặc định yêu cầu đã duyệt; chỉ GET/POST hồ sơ dùng registrationOnly:true, vẫn kiểm tra đúng chủ.
- Lưu hồ sơ chuyển về /CoSo; đơn bị từ chối khi lưu chuyển CHO_DUYET. Đổi tên/địa chỉ cơ sở đã duyệt yêu cầu duyệt lại.
- Admin Review và SaveFacility gửi SignalR tới chủ/thành viên để cập nhật giao diện.
- Build 0 lỗi/cảnh báo; 115 assertion SQL/HTTP/WebSocket đạt trên database tạm đã dọn. Kiểm tra cả hồ sơ chờ/từ chối, URL/POST bị chặn và mở lại sau duyệt. Chưa kiểm tra trực quan trên browser.
- Thứ tự nghiệm thu: đăng ký → admin duyệt → cấu hình sân/nhân viên. Kết quả 101 checks là lịch sử trước bản sửa này.

## Tích hợp Cụm 5–6 vào master (2026-11-16)

- **Kiểm thử & cập nhật nhánh `MQ-KiemThuCotLoi`**:
  - Chạy `Cluster4Checks` trên `MQ-KiemThuCotLoi` (`86ef359`, kế thừa `MO-VanHanhDon` `bab70be`): build 0 lỗi/0 cảnh báo, đạt **106/106 assertions** (database tạm `QuanLySanBai_Cluster4Tests_08ea4bcf04ed4813bfc8ee8d59e6acd2` đã xóa).
  - Fast-forward `MO-VanHanhDon` (`f058a96`) và `MQ-KiemThuCotLoi` (`0467b71`); trên `MQ-KiemThuCotLoi`, cập nhật `Data/QuanLySanBai_SQLServer.sql` (`usp_DatSanMotKhung`) để kiểm tra tường minh `c.chu_so_huu_id = @nhan_vien_tao_id` hoặc quyền `TAO_DON` (bỏ bypass `CHU_SAN`/`QUAN_LY` nội bộ) và cập nhật trạng thái trong `docs/O01-O06-review.md`, `docs/Q01-Q02-review.md`. Đã commit `8abc588` và push lên `origin/MQ-KiemThuCotLoi`.
- **Merge `MQ-KiemThuCotLoi` vào `master` (kế thừa `56e4cc3`)**:
  - Giữ `Server=(localdb)\MSSQLLocalDB` trong `QuanLySanBai/appsettings.json`.
  - Hợp nhất `Areas/CoSo/Views/Home/Facilities.cshtml` (giữ kiểm tra `DA_DUYET` và chỉ hiện nút `Lịch và đơn đặt` sau khi cơ sở đã duyệt + có quyền `XEM_LICH`).
  - Hợp nhất `Controllers/BookingsController.cs` và `Views/Bookings/Details.cshtml` (giữ xử lý lỗi/redirect và badge của `master`, bổ sung action `Cancel` và khung hủy đơn chưa thu tiền).
  - Cập nhật `Services/OperationService.cs` nhận `RealtimeUpdates? updates = null` và gửi `CalendarChanged` sau khi đặt quầy (`Counter`) hoặc hủy đơn (`Cancel`).
  - Hợp nhất `tests/Cluster4Checks/Program.cs` gồm đầy đủ test duyệt cơ sở (`CHO_DUYET`/`TU_CHOI`), SignalR WebSocket, `OperationChecks` và HTTP `O01/O02/O03/O06`.
- **Xác minh sau merge trên `master`**:
  - Build `tests/Cluster4Checks/Cluster4Checks.csproj` (`-p:NuGetAudit=false`): **0 Warning(s), 0 Error(s)**.
  - Chạy `Cluster4Checks`: đạt **148/148 assertions** (SQL + concurrency + HTTP + SignalR WebSocket) trên database tạm `QuanLySanBai_Cluster4Tests_c73b219e48744f10bceb9b6237954501`, đã dọn sạch. Chưa kiểm thử trực quan trên trình duyệt.
- **Lưu ý triển khai**: Database local/production đã cài trước đó cần đảm bảo đã áp dụng `Data/Updates/001_OrderRowVersion.sql` và cập nhật lại stored procedure `dbo.usp_DatSanMotKhung` theo `Data/QuanLySanBai_SQLServer.sql` mới nhất.

## Nghiên cứu tham khảo AloBo (2026-11-16)

- Đã đọc trang công khai, App Store và wiki hướng dẫn AloBo; đây là nghiên cứu ý tưởng, **không có thay đổi code hay xác nhận sản phẩm**. Chi tiết và thứ tự ưu tiên ở [ALOBO-IDEAS.md](ALOBO-IDEAS.md).
- Gợi ý cần cân nhắc trước: bảng lịch vận hành theo sân × giờ; tìm sân theo giờ và khoảng giá (giá lọc phải dựa trên slot/ngày thật); sau đó mới tích hợp QR/cổng thanh toán tự xác nhận.
- Dự án đã có lọc môn/khu vực/ngày, trang lịch có giá/cọc/chính sách, giữ chỗ có hạn, lịch/đơn vận hành, đặt tại quầy, ghi thu thủ công và check-in. Không ghi các gợi ý trên như tính năng đã triển khai.
- Ưu tiên sau khi luồng lõi ổn định: gắn dịch vụ bán thêm vào đơn; để kho, hội viên, sự kiện/social và báo cáo nâng cao ở backlog mở rộng.
- AloBo tự công bố hơn 2.500 chủ sân; App Store hiển thị khoảng 22k lượt đánh giá. Đây là chỉ dấu công khai, không phải xác minh độc lập về quy mô hay thời gian hoạt động.
