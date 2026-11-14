# Ngữ cảnh và quy tắc dự án

## Công nghệ và vị trí mã

- Dự án quản lý/đặt sân nhiều cơ sở: ASP.NET Core MVC .NET 10, EF Core SQL Server, LocalDB.
- App: `QuanLySanBai/`; schema: `Data/QuanLySanBai_SQLServer.sql`.
- DbContext: `QuanLySanBai/Data/QuanLySanBaiContext.cs`; mapping bổ sung: `QuanLySanBaiContext.Custom.cs` cùng thư mục. Không có yêu cầu tạo DbUtil/Utils.
- `Services/`: nghiệp vụ/phân quyền; `Controllers/`, `Areas/Admin/`, `Areas/CoSo/`: các luồng theo vai trò; `ViewModels/`: dữ liệu form.
- Chạy từ repo: `dotnet run --project QuanLySanBai`. Xem launchSettings để xác nhận URL; trước đây dùng http://localhost:5073.
- Test console: `tests/Cluster4Checks/`; đọc runner trước khi chạy. Bộ test lịch sử dùng database tạm riêng rồi dọn, tránh kiểm thử phá dữ liệu live.

## Nghiệp vụ cần giữ

- Đăng ký công khai mặc định là khách. Vai trò toàn hệ thống và quyền thành viên từng cơ sở là hai lớp khác nhau.
- Kiểm tra quyền ở server; ẩn nút theo quyền chỉ là phần giao diện.
- Procedure `usp_DatSanMotKhung` sở hữu transaction đặt sân; không bọc thêm transaction bên ngoài tùy tiện.
- Khóa đặt lịch dùng applock `QLSB:LICH_SAN`; expiry phải phối hợp cùng cơ chế chống đua.
- Giờ Việt Nam UTC+7; thứ Hai=1, Chủ nhật=7; kết thúc 00:00 có nghĩa 24:00 cho khung qua cuối ngày.
- Cụm 5 dùng RowVersion, khóa và idempotency khi thu tiền; không bỏ các bảo vệ chống thu trùng/vượt tiền.
- Migration bổ sung `Data/Updates/001_OrderRowVersion.sql` đã từng áp dụng vào LocalDB trong phiên triển khai. Luôn kiểm tra DB thực tế trước khi giả định schema đúng.

## Dữ liệu mẫu và bảo mật

- Tài liệu test: `test-data/README.md` và các CSV.
- Thông tin đăng nhập demo nằm trong `.local/demo-accounts.txt` hoặc `test-data/demo-accounts.local.txt`, đều phải bỏ qua Git. Không sao chép mật khẩu vào bộ nhớ này.
- Seeder chỉ chạy khi chủ động truyền `--seed-demo` và ở Development. Không seed lại chỉ để đọc tài khoản.
- `.artifacts/` là output local/cache/checks, không commit.

## Cách làm việc với người dùng

- Giao tiếp tiếng Việt; ưu tiên nghiệp vụ chắc, giao diện đơn giản nhưng nút rõ và phù hợp vai trò.
- Chủ động hoàn tất phần đã được giao; không hỏi lại các lựa chọn triển khai thường lệ đã được cho phép.
- Commit theo cụm, chỉ stage đường dẫn liên quan. Không tự coi yêu cầu cập nhật Git là cho phép commit mọi file hay merge mọi nhánh.
- Khi dùng Graphify, phân biệt CLI local với cloud. CLI đã cài `graphifyy[sql]` 0.9.105 bằng uv/Python 3.13; graph lịch sử có 1.510 nút, 2.687 cạnh. Chưa chứng minh graph khớp master hiện tại.
- Người dùng đã hủy áp dụng hướng dẫn AGENTS.md Graphify cũ trong hội thoại; không coi yêu cầu query-first cũ là còn hiệu lực chỉ từ ghi chú lịch sử.

## Notion

- Người dùng cho phép cập nhật tiến độ trong lúc làm việc.
- Việc đã triển khai và kiểm chứng chuyển sang **Chờ kiểm tra**. Chỉ người dùng nghiệm thu rồi chuyển **Hoàn thành**; giữ nguyên các việc họ đã hoàn thành.
- Data source lịch sử: `collection://d7f0d301-5d78-8267-906b-072ed483f9de`. Dùng connector để kiểm tra trạng thái mới nhất trước khi sửa.
- N01–N05 đã hoàn thành theo ghi nhận trước; A/C/B/O/Q từng được đưa sang Chờ kiểm tra. Đây là lịch sử, không thay thế truy vấn Notion hiện tại.
