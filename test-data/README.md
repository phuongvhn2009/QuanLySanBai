# Dữ liệu mẫu để kiểm tra

## Tài khoản demo

| Vai trò | Số điện thoại | Email |
| --- | --- | --- |
| Admin | 0991111101 | admin@qlsb.example.test |
| Chủ cơ sở | 0991111102 | owner@qlsb.example.test |
| Khách hàng | 0991111103 | customer@qlsb.example.test |

Mật khẩu từng tài khoản có trong `demo-accounts.local.txt` cùng thư mục. File mật khẩu đã được Git bỏ qua. Có thể đăng nhập bằng số điện thoại hoặc email. Dữ liệu đã được tạo trong database QuanLySanBai trên LocalDB; không cần import lại.

## Cơ sở và sân mẫu

- Cơ sở Demo tại TP. Hồ Chí Minh, đã duyệt và đang hoạt động.
- Sân demo 1, môn cầu lông, sức chứa 4 người.
- Giờ mở cửa 06:00–24:00.
- Khung đặt: 18:00–19:00 và 23:00–24:00.
- Giá mỗi khung: 150.000đ; cọc: 50.000đ; đủ cả 7 ngày trong tuần.
- Chính sách demo: không thu phí hủy/đổi, cho phép đổi lịch. Đây chỉ là cấu hình mẫu; thao tác thanh toán/hủy/đổi thuộc giai đoạn tiếp theo.
- Giữ chỗ mặc định 10 phút; tác vụ hết hạn chạy định kỳ 30 giây khi ứng dụng hoạt động.

## Chạy thử

1. Khởi động ứng dụng và vào http://localhost:5073/Account/Login (đổi cổng nếu ứng dụng chạy cổng khác).
2. Dùng cửa sổ ẩn danh hoặc trình duyệt khác nhau cho từng tài khoản để tránh ghi đè phiên đăng nhập.
3. Chọn ngày mai khi đặt sân để tránh giờ đã qua. Mở `test-scenarios.csv` để xem các trường hợp thử.
4. Muốn thử nhân viên: đăng ký một tài khoản khách mới; đăng nhập chủ cơ sở → Quản lý cơ sở → Quản lý nhân viên → thêm số điện thoại vừa đăng ký. Cấp/thu quyền rồi đăng nhập tài khoản đó ở cửa sổ khác. Không tạo sẵn vai trò nhân viên toàn hệ thống thay cho quyền từng cơ sở.

## Tạo lại dữ liệu khi cài database mới

Từ thư mục gốc dự án:

```powershell
dotnet build QuanLySanBai/QuanLySanBai.csproj -o .artifacts/local
dotnet .artifacts/local/QuanLySanBai.dll --contentRoot D:/QuanLysanBai/QuanLySanBai --environment Development --seed-demo
```

Seed chỉ được chạy trong Development, không tạo bản sao hoặc đổi mật khẩu nếu demo đã tồn tại. Mật khẩu sinh lần đầu nằm ở `.local/demo-accounts.txt`; sao chép sang `test-data/demo-accounts.local.txt` khi cần. Không chạy lại script cài schema trên database đã có dữ liệu; không xóa dữ liệu chỉ để reset một lần đặt thử.

File CSV trong thư mục là tài liệu tra cứu/tình huống, không phải script import. Bộ kiểm thử tự động đầy đủ nằm ở `tests/Cluster4Checks` và tự dùng database riêng.
