# A01–A05: tài khoản và phân quyền

Nhánh: `MA-TaiKhoanPhanQuyen`. Thay đổi đang ở working tree, đã commit/push và merge theo cụm, xem Git log.

## Chức năng

- A01: `/Account/Register`, tạo KHACH_HANG cố định, hash bằng ASP.NET PasswordHasher, chuẩn hóa email, chặn trùng điện thoại/email ở ứng dụng và unique index SQL.
- A02: `/Account/Login`, `/Account/Profile`, POST `/Account/Logout`; cookie HttpOnly, SameSite Lax, thời hạn 8 giờ. Chỉ chấp nhận return URL nội bộ. Hồ sơ lấy ID từ danh tính đăng nhập. Kiểm tra trạng thái và vai trò trong DB mỗi request.
- A03: Admin yêu cầu vai trò ADMIN. Các action cơ sở kiểm tra người sở hữu/thành viên hoạt động, quyền chi tiết và ID cơ sở phía server; ID thành viên phải thuộc cơ sở đang thao tác.
- A04: `/CoSo` → cơ sở → quản lý thành viên; thêm tài khoản có sẵn qua số điện thoại, cấp/thu quyền, bật/tắt thành viên. Chỉ chủ sở hữu được bổ nhiệm quản lý. Không cho tự nâng quyền hoặc thay đổi chủ sân. Người được ủy quyền không được cấp quyền quản lý thành viên hoặc quyền ngoài phạm vi mình có.
- A05: `/Admin/Management/Users` tìm người dùng, khóa/mở khóa; `/Admin/Management/Facilities` duyệt/từ chối cơ sở. Không tự khóa tài khoản Admin đang dùng.

## Chạy và kiểm tra

Cần .NET 10 và SQL Server LocalDB với database `QuanLySanBai` đã cài từ `Data/QuanLySanBai_SQLServer.sql`. Chuỗi kết nối dùng `ConnectionStrings:DefaultConnection`.

```powershell
dotnet build QuanLySanBai/QuanLySanBai.csproj
dotnet run --project QuanLySanBai
# Bộ kiểm thử nghiệp vụ, rollback dữ liệu:
dotnet run --project tests/AccountChecks
# Có thể chạy HTTP trên cổng riêng, ở terminal khác:
dotnet run --project QuanLySanBai --no-launch-profile --urls http://127.0.0.1:5188
dotnet run --project tests/AccountChecks -- http://127.0.0.1:5188
```

Kiểm thử HTTP tạo tài khoản/cơ sở tạm và xóa trong finally. Chỉ chạy trên DB phát triển; không ngắt tiến trình khi đang dọn dữ liệu.

## Người dùng kiểm tra thủ công

1. Đăng ký khách hàng, thử trùng email/điện thoại, mật khẩu xác nhận sai.
2. Đăng nhập bằng email hoặc số điện thoại; cập nhật hồ sơ; đăng xuất.
3. Với dữ liệu cơ sở và tài khoản chủ sân đã chuẩn bị từ C01: vào Cơ sở của tôi, thêm nhân viên đã đăng ký và cấp quyền; đăng nhập nhân viên ở trình duyệt khác. Thu quyền rồi thao tác lại.
4. Sửa coSoId/memberId trên URL hoặc form để kiểm tra chặn truy cập cơ sở khác.
5. Dùng tài khoản ADMIN đã được cấp vai trò qua quy trình quản trị DB để duyệt/từ chối cơ sở, khóa tài khoản đang đăng nhập ở trình duyệt khác và xác nhận request tiếp theo bị đăng xuất.

Dữ liệu ban đầu không có tài khoản Admin/cơ sở; đăng ký công khai luôn chỉ cấp KHACH_HANG. Không có mật khẩu Admin mặc định. Việc tạo cơ sở thuộc C01, không thuộc A01–A05.

## Giới hạn môi trường và phạm vi kiểm chứng

- Trong sandbox Windows, LocalDB và DPAPI bảo vệ cookie bị lỗi truy cập. Chạy ngoài sandbox đã kết nối được; không thay đổi cơ chế bảo vệ khóa để né lỗi môi trường.
- NuGet báo NU1900 do không truy cập được dịch vụ dữ liệu lỗ hổng. Build dùng các package sẵn có thành công; chưa xác minh được dữ liệu lỗ hổng từ NuGet.
- Kiểm thử tự động xác minh hành vi nghiệp vụ và HTTP; việc duyệt giao diện trực quan, nghiệm thu cuối cùng do người dùng thực hiện.

## Kết quả thực tế

- Build ứng dụng: thành công, 0 lỗi, 0 cảnh báo trong lần build ban đầu sau triển khai.
- Bộ kiểm thử: 34/34 kiểm tra qua (21 kiểm tra nghiệp vụ/DB, 13 kiểm tra HTTP và token), đã rollback/xóa dữ liệu thử.
- HTTP bổ sung: Register/Login 200; Profile/Admin/CoSo chưa đăng nhập 302; POST Login thiếu CSRF 400.
- Tài khoản thường truy cập trang quản trị bị chuyển đến trang từ chối quyền; cấp ADMIN trong fixture kiểm thử cho phép tải danh sách người dùng/cơ sở.
- Lỗi khóa apphost khi build đồng thời ứng dụng đang chạy đã xử lý bằng build bộ kiểm thử riêng với `-p:BuildProjectReferences=false` và chạy `--no-build`. Khi tự build lại, dừng ứng dụng đang chạy trước.
