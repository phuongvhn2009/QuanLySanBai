# Cụm 4 — Xem lịch và đặt sân (B01–B05)

## Phạm vi triển khai

| Mã | Chức năng | Kết quả |
| --- | --- | --- |
| B01 | Tìm và xem sân | Lọc môn/địa chỉ/ngày còn chỗ trước phân trang; chỉ công khai cơ sở được duyệt và môn/cơ sở hoạt động |
| B02 | Lịch theo sân/ngày | Phân biệt trống, giữ chỗ, đã đặt, khóa, nghỉ và thiếu cấu hình; hiển thị giá/cọc đúng ngày |
| B03 | Tạo booking | Gọi procedure có tham số; danh tính và liên hệ lấy từ phiên; giá/cọc lấy từ DB; một đơn/một khung |
| B04 | Đơn của tôi, hết hạn | Chỉ đọc đơn đúng chủ; countdown theo thời gian server; job giải phóng giữ chỗ định kỳ |
| B05 | Đồng thời và biên thời gian | Chỉ một người đặt thành công khi tranh cùng khung; rollback không có đơn rác; thử quá khứ/nghỉ/bảo trì/khóa/00:00 |

## Luồng dữ liệu

1. Khách mở `/Courts`, chọn sân và ngày tại `/Courts/Details/{id}`.
2. Calendar đọc lịch hoạt động, khung, giá hiệu lực, chính sách, khóa và booking hiện có.
3. POST `/Bookings/Create` chỉ nhận sân/ngày/khung/ghi chú, có xác thực và chống CSRF.
4. BookingService lấy ID, tên, điện thoại từ người dùng hiện tại và gọi `usp_DatSanMotKhung` bằng SqlParameter.
5. Procedure tự mở transaction, lấy applock, kiểm tra lại điều kiện và tạo đơn/slot. Không bọc procedure bằng transaction bên ngoài.
6. Khách được chuyển đến đơn vừa tạo. `/Bookings` chỉ liệt kê các đơn của mình.
7. `HoldExpiryWorker` gọi `usp_GiaiPhongGiuChoHetHan`; cập nhật trạng thái đơn/slot và giải phóng chỗ hết hạn.

## Quy ước

- Giờ Việt Nam UTC+7; thứ Hai=1, Chủ nhật=7; kết thúc 00:00 là 24:00 của ngày chọn.
- `Booking:HoldMinutes`: mặc định 10, cho phép 1–60 phút.
- `Booking:ExpiryIntervalSeconds`: mặc định 30, cho phép 5–300 giây.
- Countdown dựa trên thời gian server tại lúc đọc, không tin thời gian người dùng gửi lên.
- Không nhận giá/danh tính thực hiện tùy ý từ form. Chỗ hiển thị trống vẫn phải kiểm tra lại khi lưu.
- Giá slot và chính sách đã gắn với đơn cũ được giữ làm căn cứ lịch sử.

## Mã và kiểm chứng

Mã chính: `BookingService.cs` (gồm worker), `BookingModels.cs`, `CourtsController.cs`, `BookingsController.cs`, Views/Courts và Views/Bookings.

Bộ kiểm thử chung tại `tests/Cluster4Checks` đã qua 74 kiểm tra ở lần bàn giao Git, bao gồm thử đặt đồng thời, giả mạo giá/danh tính, chặn xem đơn người khác, job thực sự chạy trên server HTTP và tính tiền computed. Test tự cài database riêng và xóa sau khi hoàn tất.

```powershell
dotnet build tests/Cluster4Checks -o .artifacts/checks
dotnet .artifacts/checks/Cluster4Checks.dll D:/QuanLysanBai
```

Kịch bản duyệt thủ công: [test-scenarios.csv](../test-data/test-scenarios.csv). Dùng ngày mai và hai trình duyệt/tài khoản khác nhau khi thử tranh cùng khung.

## Giới hạn và bước tiếp

Chưa có thanh toán, thu tiền, check-in, kết thúc buổi hoặc hủy/đổi có hoàn tiền. Khi làm các cụm tiếp theo phải kiểm thử cuộc đua thu tiền/hết hạn. Lọc ngày còn chỗ đang đọc lịch từng sân; cần tối ưu khi dữ liệu lớn. Nghiệm thu bố cục trực quan vẫn cần lượt duyệt của người dùng.

## Git và nghiệm thu

Nhánh `MB-XemLichDatSan`, commit [96819df](https://github.com/phuongvhn2009/QuanLySanBai/commit/96819df), đã push và merge vào master. B01–B05 được bàn giao Chờ kiểm tra trên Notion; merge code không tự thay thế xác nhận nghiệm thu.
