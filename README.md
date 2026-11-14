# Quản lý sân bãi

Ứng dụng web quản lý cơ sở thể thao, cấu hình sân và đặt sân, xây dựng bằng ASP.NET Core MVC. Hệ thống có tài khoản khách hàng, chủ sân, quản trị viên và quyền nhân viên theo từng cơ sở.

## Chức năng hiện có

- Đăng ký, đăng nhập bằng email hoặc số điện thoại, quản lý tài khoản và phân quyền.
- Quản trị danh mục môn thể thao, loại sân, tài khoản và duyệt cơ sở.
- Quản lý cơ sở, sân, nhân viên, giờ hoạt động, khung giờ, bảng giá, chính sách hủy/đổi và lịch khóa sân.
- Xem lịch sân, chọn khung giờ, đặt sân và tự động giải phóng giữ chỗ hết hạn.

Schema có cả các bảng phục vụ nghiệp vụ mở rộng; có bảng dữ liệu không đồng nghĩa chức năng tương ứng đã được triển khai. Xem [tài liệu dự án](docs/README.md) để theo dõi phạm vi và trạng thái từng phần.

## Công nghệ và yêu cầu

- .NET SDK 10, ASP.NET Core MVC, Entity Framework Core 10 và xác thực bằng cookie.
- SQL Server 2019 trở lên; cấu hình phát triển mặc định dùng SQL Server LocalDB trên Windows.
- SQL Server Management Studio (SSMS) hoặc `sqlcmd` để cài cơ sở dữ liệu.

Các lệnh bên dưới chạy bằng PowerShell tại thư mục gốc repository.

## Cài đặt và chạy

### 1. Tạo cơ sở dữ liệu

Mở [Data/QuanLySanBai_SQLServer.sql](Data/QuanLySanBai_SQLServer.sql) bằng SSMS và chạy toàn bộ script, hoặc dùng:

```powershell
sqlcmd -S '(localdb)\MSSQLLocalDB' -E -C -i Data/QuanLySanBai_SQLServer.sql -b
```

Script tạo database `QuanLySanBai`, schema và dữ liệu danh mục ban đầu. Chỉ dùng cho database mới, chưa có bảng ứng dụng; không chạy lại để cập nhật hoặc đặt lại dữ liệu. Ứng dụng không tự tạo database khi khởi động.

### 2. Cấu hình kết nối

Kết nối mặc định trong [appsettings.json](QuanLySanBai/appsettings.json):

```text
Server=(localdb)\MSSQLLocalDB;Database=QuanLySanBai;Trusted_Connection=True;TrustServerCertificate=True;
```

Nếu dùng SQL Server khác, đặt biến môi trường trước khi chạy, ví dụ với Windows Authentication:

```powershell
$env:ConnectionStrings__DefaultConnection = 'Server=localhost;Database=QuanLySanBai;Trusted_Connection=True;TrustServerCertificate=True;'
```

Không đưa mật khẩu kết nối thực tế vào repository.

### 3. Khởi động ứng dụng

```powershell
dotnet restore QuanLySanBai.slnx
dotnet build QuanLySanBai.slnx
dotnet run --project QuanLySanBai --launch-profile http
```

Truy cập **http://localhost:5073**. Trang đăng nhập: **http://localhost:5073/Account/Login**.

Để chạy HTTPS ở môi trường phát triển:

```powershell
dotnet dev-certs https --trust
dotnet run --project QuanLySanBai --launch-profile https
```

Địa chỉ HTTPS theo cấu hình hiện tại: **https://localhost:7237**.

## Dữ liệu demo

Sau khi cài schema, chạy lệnh sau để tạo tài khoản và một cơ sở có sân, khung giờ, bảng giá mẫu:

```powershell
dotnet run --project QuanLySanBai --launch-profile http -- --seed-demo
```

Lệnh chỉ chạy trong môi trường `Development` và kết thúc sau khi tạo dữ liệu. Chạy lại lệnh khởi động ứng dụng ở trên để mở web.

| Vai trò | Số điện thoại | Email |
| --- | --- | --- |
| Quản trị viên | 0991111101 | admin@qlsb.example.test |
| Chủ sân | 0991111102 | owner@qlsb.example.test |
| Khách hàng | 0991111103 | customer@qlsb.example.test |

Mật khẩu được sinh ngẫu nhiên và lưu tại `.local/demo-accounts.txt`, được Git bỏ qua. Seed không đổi mật khẩu hoặc tạo bản sao khi demo đã tồn tại; nếu mất file mật khẩu, chạy seed lại không khôi phục được mật khẩu cũ.

Xem [hướng dẫn dữ liệu mẫu và kịch bản thử](test-data/README.md). Nên chọn ngày mai khi thử đặt sân để tránh khung giờ đã qua.

## Kiểm thử

Bộ kiểm thử tích hợp chính chạy bằng:

```powershell
dotnet run --project tests/Cluster4Checks -- .
```

Bộ kiểm thử cần LocalDB, tự tạo database riêng có tên `QuanLySanBai_Cluster4Tests_...` và xóa sau khi chạy. Tài khoản SQL cần quyền tạo/xóa database. Các kiểm tra bao gồm nghiệp vụ, phân quyền và luồng HTTP.

Bộ kiểm tra tài khoản có thể chạy sau khi cài database mặc định:

```powershell
dotnet run --project tests/AccountChecks
```

`AccountChecks` kết nối trực tiếp database `QuanLySanBai` trên LocalDB, tạo dữ liệu kiểm tra trong transaction rồi rollback. Chỉ chạy trên môi trường phát triển.

## Cấu trúc thư mục

```text
QuanLySanBai.slnx          Solution
QuanLySanBai/
  Areas/Admin/            Giao diện và controller quản trị
  Areas/CoSo/             Quản lý cơ sở và nhân viên
  Controllers/            Tài khoản, sân, đặt sân, trang chủ
  Data/                   DbContext và cấu hình ánh xạ
  Models/                 Mô hình dữ liệu
  Services/               Nghiệp vụ và tác vụ hết hạn giữ chỗ
  ViewModels/             Dữ liệu biểu mẫu và giao diện
  Views/                  Giao diện Razor
  wwwroot/                Tài nguyên tĩnh
Data/                     Script cài SQL Server
tests/                    Các chương trình kiểm thử
test-data/                Hướng dẫn và kịch bản thử dữ liệu mẫu
docs/                     Tài liệu triển khai và bàn giao
```

## Quy ước dữ liệu

- Thời gian nghiệp vụ dùng giờ Việt Nam (UTC+7).
- Giờ kết thúc `00:00` của khung giờ được hiểu là `24:00` cùng ngày.
- Giữ chỗ mặc định 10 phút; tác vụ giải phóng chỗ hết hạn chạy mỗi 30 giây khi ứng dụng hoạt động.
- Ánh xạ EF tùy chỉnh nằm trong `QuanLySanBai/Data/QuanLySanBaiContext.Custom.cs`.

Tham khảo thêm [luồng dữ liệu và cách kiểm tra](docs/cluster-1-4-review.md) và [ghi chú bàn giao](docs/HANDOFF.md).
