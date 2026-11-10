# Bàn giao cụm 1–4

Phạm vi: N01–N05, A01–A05, C01–C06, B01–B05. Nhánh `MA-TaiKhoanPhanQuyen`; thay đổi đang ở working tree, đã commit/push và merge theo cụm, xem Git log. Giao diện Bootstrap phục vụ thao tác.

## Luồng dữ liệu

1. Người dùng đăng ký nhận KHACH_HANG. Đăng ký cơ sở tạo Co_So ở CHO_DUYET, thành viên CHU_SAN và vai trò CHU_SAN trong cùng transaction. Không thể đổi chủ sở hữu từ form. Đổi tên/địa chỉ cơ sở yêu cầu duyệt lại.
2. Chủ cơ sở cấu hình sân, lịch tuần, khung giờ, bảng giá/cọc, chính sách và khóa sân. Admin quản lý danh mục và duyệt cơ sở. Người khác không sửa được bản ghi cơ sở. Không xóa sân có lịch sử; dùng ngừng hoạt động.
3. Cơ sở đã duyệt/đang hoạt động và môn đang hoạt động mới được công khai. Khách xem ngày, giá, cọc, chính sách và trạng thái Trống/Giữ chỗ/Đã đặt/Khóa/Nghỉ/Thiếu cấu hình. Lọc địa chỉ và môn; lọc ngày còn chỗ trước khi phân trang.
4. POST đặt sân lấy ID, tên, điện thoại từ tài khoản hiện tại. Giá/cọc/chính sách lấy từ SQL; không nhận giá hoặc danh tính từ form. BookingService gọi usp_DatSanMotKhung bằng tham số và không bọc transaction bao ngoài.
5. Procedure lấy applock trước khi kiểm tra và tạo đơn/slot. Chống trùng được kiểm tra ở SQL. Lỗi rollback không để lại đơn rác. Đặt một đơn/một khung.
6. Worker gọi usp_GiaiPhongGiuChoHetHan định kỳ, có log và thử lại ở lần chạy sau. Khách chỉ xem đơn thuộc mình; countdown dựa trên chênh lệch thời gian server và đồng hồ monotonic của trình duyệt.

## Quy ước nghiệp vụ

- Giờ lưu/hiển thị Việt Nam UTC+7; không phụ thuộc múi giờ hệ điều hành. Thứ Hai=1, Chủ nhật=7. Giờ kết thúc 00:00 là 24:00 của ngày sử dụng.
- Giữ chỗ mặc định 10 phút theo procedure hiện có; cấu hình `Booking:HoldMinutes` (1–60). Worker mặc định 30 giây; `Booking:ExpiryIntervalSeconds` (5–300).
- Giá/cọc là decimal, tối đa hai số thập phân. Tiền cọc không vượt giá; giá hiệu lực theo sân/khung/thứ/ngày, không giao nhau.
- Chính sách áp dụng theo ngày đặt hiện tại, theo đúng procedure. Mức phí phải liên tục từ 0 đến vô hạn. Chủ cơ sở nhập tỷ lệ thực tế; phí demo 0% chỉ dành cho demo.
- Giá đã dùng không sửa nội dung; tắt giá cũ rồi thêm giá mới. Chính sách đã dùng giữ mức phí cũ; có thể đóng ngày hiệu lực để tạo phiên bản sau.
- Chưa triển khai thu tiền, thanh toán, hủy/đổi có hoàn tiền trong phạm vi này. Không có chuyển trạng thái tùy ý hoặc sửa ví qua CRUD.

## Các trang thao tác

- `/Courts`: tìm sân; `/Courts/Details/{id}`: lịch và đặt.
- `/Bookings`: đơn của tôi.
- `/CoSo`: cơ sở của tài khoản; `/CoSo/Configuration/Facility`: đăng ký cơ sở.
- `/CoSo/Configuration/Index/{id}`: quản lý cấu hình; `/CoSo/Members?coSoId={id}`: nhân viên.
- `/Admin/Catalog`: môn/loại sân; `/Admin/Management/Facilities`: duyệt; `/Admin/Management/Users`: khóa/mở tài khoản.

## Chạy và dữ liệu demo

Dừng phiên ứng dụng đang chạy trước khi build bình thường để tránh khóa file. Nếu cần build riêng:

```powershell
dotnet build QuanLySanBai/QuanLySanBai.csproj -o .artifacts/local
# Từ thư mục gốc repository:
dotnet .artifacts/local/QuanLySanBai.dll --contentRoot D:/QuanLysanBai/QuanLySanBai --environment Development --urls http://localhost:5073
```

Dữ liệu demo đã được tạo trong LocalDB Development: Admin `0991111101`, chủ sân `0991111102`, khách `0991111103`; mật khẩu ngẫu nhiên trong `.local/demo-accounts.txt`. File này và thư mục .artifacts được Git bỏ qua. Có một cơ sở đã duyệt, sân cầu lông, khung 18:00–19:00 và 23:00–24:00, giá/cọc đủ 7 ngày và chính sách liên tục. Tài khoản có sẵn của người dùng được giữ nguyên.

Seed chỉ chạy khi chỉ định `--seed-demo` và environment Development, thực thi transaction, không đổi mật khẩu hay tạo bản sao khi đã có dữ liệu demo:

```powershell
dotnet .artifacts/local/QuanLySanBai.dll --contentRoot D:/QuanLysanBai/QuanLySanBai --environment Development --seed-demo
```

## Bằng chứng kiểm tra

- Database đang dùng: 48 bảng, 102 FK, 13 trigger, 3 procedure, 2 view; 21 định nghĩa hàm/procedure/trigger/view khớp script.
- Bộ kiểm thử cụm 4: lần chạy mở rộng qua 63 kiểm tra; gồm quyền sở hữu, ghi EF/trigger/computed, giá decimal, cấu hình trùng, chính sách thiếu/gãy, môn ngừng hoạt động, ngày quá khứ, ngày nghỉ, bảo trì, khóa sân, 00:00, đặt đồng thời, rollback, giữ chỗ hết hạn, job nền, quyền đơn, giả mạo giá/danh tính, HTTP và các trang cấu hình.
- Kiểm thử tự tạo database có tên QuanLySanBai_Cluster4Tests_<GUID>, khởi động server HTTP riêng và xóa database thử trong finally. Không dùng DB phát triển cho phép thử đồng thời. Chạy từng lượt để tránh hai lần build chung output.
- Bộ kiểm thử A01–A05 trước đó: 34 kiểm tra qua. N01–N05 đã Hoàn thành trên Notion; chỉ bổ sung kết quả rà soát, không tự đổi xác nhận của người dùng. C/B chuyển Chờ kiểm tra để người dùng duyệt.
- Build mới: 0 lỗi, 0 cảnh báo code. Dịch vụ audit lỗ hổng NuGet từng không truy cập được; lần build test tắt audit qua tham số lệnh để kiểm chứng code, chưa kết luận về tình trạng lỗ hổng package.

```powershell
dotnet build tests/Cluster4Checks -o .artifacts/checks
dotnet .artifacts/checks/Cluster4Checks.dll D:/QuanLysanBai
# Chỉ đọc đối chiếu database hiện tại:
dotnet .artifacts/checks/Cluster4Checks.dll D:/QuanLysanBai --audit-live
```

## Tiếp nối

Bắt đầu cụm 5 O01–O06 sau khi người dùng duyệt. Cần chốt quy tắc thu tiền/xác nhận, check-in, hoàn thành và hủy. Khi làm tài chính phải kiểm thử cuộc đua thu tiền/hết hạn và không coi procedure expiry hiện tại là bộ máy xử lý thanh toán. Các danh sách hiện dùng phân trang đơn giản; lọc ngày còn chỗ đọc lịch theo từng sân, cần tối ưu query nếu số lượng sân lớn. Kiểm thử hành vi HTTP đã thực hiện; kiểm tra giao diện trực quan và nghiệm thu cuối cùng còn thuộc lượt duyệt của người dùng.

## Điều chỉnh giao diện theo vai trò

- Một thanh điều hướng chung cho cả ba layout, bỏ menu lặp và trang Welcome mặc định. Trang gốc điều hướng Admin vào tổng quan quản trị, thành viên cơ sở vào cơ sở làm việc, khách vào tìm sân.
- Admin chỉ thấy điều hướng quản trị; khách thấy tìm sân/lịch đặt của tôi. Đăng ký cơ sở nằm trong menu tài khoản khách. Menu cơ sở lấy theo quan hệ sở hữu/thành viên đang hoạt động, không dựa riêng vào vai trò toàn cục.
- Từng cơ sở chỉ có nút cấu hình cho chủ sở hữu và nút nhân viên cho người có quyền quản lý thành viên. Trang nhân viên không hiện biểu mẫu sửa chủ sân/chính mình; quản lý không thấy lựa chọn tự nâng quyền hoặc cấp vai trò quản lý.
- Trang cấu hình chia sáu mục mở/đóng; danh mục và nhân viên mở biểu mẫu khi cần. Bảng người dùng ẩn nút tự khóa; duyệt cơ sở ẩn thao tác trùng trạng thái. Khóa tài khoản/từ chối cơ sở có xác nhận trên trình duyệt.
- HTTP/SQL regression mở rộng qua 74 kiểm tra, có kiểm tra riêng menu khách/chủ sân/nhân viên/Admin, một navigation duy nhất và server từ chối quyền tương ứng. Build 0 lỗi/0 cảnh báo. Chưa có công cụ trình duyệt khả dụng để xác nhận ảnh render; cần người dùng duyệt bố cục trực quan.
