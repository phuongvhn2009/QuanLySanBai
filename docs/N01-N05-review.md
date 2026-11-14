# Cụm 1 — Nền tảng (N01–N05)

## Phạm vi và kết quả

| Mã | Nội dung | Kết quả hiện có |
| --- | --- | --- |
| N01 | Solution .NET và database | ASP.NET Core MVC .NET 10; kết nối SQL Server LocalDB; đối chiếu schema và routine thực tế |
| N02 | Git và môi trường | Nhánh chính master; cấu hình kết nối có thể override qua biến môi trường; bỏ qua build và dữ liệu nhạy cảm local |
| N03 | EF Core Database First | Entity và DbContext scaffold; mapping bổ sung tách riêng; tương thích trigger và cột computed |
| N04 | Cấu trúc và xử lý lỗi | Controllers, Services, ViewModels, Areas; BusinessErrorFilter; logging tác vụ nền |
| N05 | Layout cơ bản | Layout dùng chung; menu theo vai trò/quyền; form và nút Bootstrap; giao diện quản trị/cơ sở/khách |

## Các file chính

- `QuanLySanBai/QuanLySanBai.csproj`, `Program.cs`.
- `Data/QuanLySanBai_SQLServer.sql`: script cài database mới.
- `QuanLySanBai/Data/QuanLySanBaiContext.cs`, `Models/Entities`: mã scaffold.
- `QuanLySanBai/Data/QuanLySanBaiContext.Custom.cs`: mapping bổ sung, tránh sửa file sinh tự động.
- `QuanLySanBai/Services/BusinessRules.cs`: lỗi nghiệp vụ và quy ước thời gian.
- `QuanLySanBai/Views/Shared/_Layout.cshtml`, `_AccountNav.cshtml`: giao diện và menu chung.

## Kiểm chứng

Database thực tế đã đối chiếu: 48 bảng, 102 khóa ngoại, 13 trigger, 3 procedure, 2 view. Tổng 21 định nghĩa hàm/procedure/trigger/view khớp script. Kiểm thử SQL xác minh ghi qua EF, xử lý trigger và đọc tổng tiền computed.

```powershell
dotnet build tests/Cluster4Checks -o .artifacts/checks
dotnet .artifacts/checks/Cluster4Checks.dll D:/QuanLysanBai --audit-live
```

Lệnh audit chỉ đọc database đang dùng. Bộ kiểm thử đầy đủ tạo database riêng rồi dọn sau khi chạy. Chi tiết môi trường, cách chạy và giới hạn kiểm chứng ở [báo cáo tổng hợp](cluster-1-4-review.md).

## Lưu ý tiếp tục phát triển

- Không chạy script cài mới lên database đã có bảng; thay đổi schema cần migration riêng.
- Không commit `.local`, `.artifacts` hoặc `test-data/*.local.txt`.
- Không ghi cột computed trực tiếp; mapping tùy chỉnh đặt trong file partial.
- Dừng phiên ứng dụng trước khi build vào cùng thư mục để tránh khóa file.
- Menu theo vai trò không thay thế kiểm tra quyền phía server.

## Git và nghiệm thu

Nền tảng gốc: commit `1e3ff8a`. Các bổ sung layout, lỗi chung và mapping được giao cùng các nhánh A/C/B và đã merge vào master. N01–N05 có trạng thái Hoàn thành từ trước trên Notion; các chỉnh sửa giao diện bổ sung đã được ghi chú để người dùng duyệt.
