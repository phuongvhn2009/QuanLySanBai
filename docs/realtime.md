# Đồng bộ màn hình bằng SignalR

Endpoint `/updates`, SignalR JS 10.0.0 được lưu local trong `wwwroot/lib/signalr/`, kèm license MIT. Server dùng SignalR có sẵn trong ASP.NET Core, không cần thêm NuGet.

- Cấp/thu hồi quyền hoặc thêm thành viên: sau SaveChanges thành công, gửi `PermissionsChanged` tới UserIdentifier từ cookie của đúng người dùng. Không nhận user ID do browser gửi để chọn người nhận. Các tab của người đó tự tải lại, kể cả khi đang nhập form vì quyền truy cập đã thay đổi.
- Đặt sân qua BookingService: sau procedure hoàn tất, gửi `CalendarChanged` tới nhóm sân công khai tương ứng. Hub kiểm tra sân công khai trước khi cho tham gia. Thông báo chỉ yêu cầu tải lại, không chứa tên khách, mã đơn hoặc dữ liệu riêng tư.
- Hết hạn giữ chỗ: worker xử lý theo chu kỳ cấu hình rồi phát thông báo cho sân liên quan. Không phải đúng từng giây tại deadline.
- Khi kết nối trở lại hoặc tab hoạt động trở lại: tải dữ liệu mới. Form POST đang nhập được giữ, hiện nút tải lại thay vì tự xóa nội dung; thay đổi quyền luôn buộc tải lại.
- Lỗi gửi thông báo không làm báo thất bại một đơn đã commit; server ghi log. Cần tải lại nếu thông báo bị bỏ lỡ trong khi tab vẫn hoạt động.

## Kiểm tra thủ công

1. Khởi động lại ứng dụng với mã mới. Mở chủ sân trong trình duyệt thường, nhân viên trong cửa sổ ẩn danh hoặc trình duyệt khác.
2. Nhân viên mở Cơ sở làm việc. Chủ sân cấp quyền quản lý nhân viên và lưu: màn hình nhân viên tự tải lại và hiện nút.
3. Nhân viên vào trang quản lý. Chủ sân bỏ quyền và lưu: màn hình nhân viên tự tải lại và bị chặn. Kiểm tra URL và POST vẫn bị server từ chối.
4. Hai trình duyệt cùng mở lịch của một sân công khai, cùng ngày. Một bên đặt khung: bên kia tự tải lại, hiển thị giữ chỗ. Khi worker giải phóng giữ chỗ, lịch cập nhật lại.
5. Thử mất mạng rồi kết nối lại; lịch tải dữ liệu mới. Đang nhập ghi chú đặt sân thì có thông báo tải mới để tránh mất bản nháp.

## Giới hạn hiện tại

- Chỉ đồng bộ các luồng trên master đã nối thông báo: thành viên, đặt sân APP và hết hạn giữ chỗ. Các màn hình vận hành cụm 5, cập nhật giá/khóa sân và thao tác SQL trực tiếp chưa được nối sự kiện ở bản này.
- Cấu hình hiện dành cho một tiến trình ứng dụng. Nếu chạy nhiều instance cần backplane/Azure SignalR để chuyển thông báo giữa các instance.
- SignalR là cập nhật giao diện; kiểm tra quyền tại server và transaction SQL vẫn quyết định thao tác có hợp lệ hay không.
