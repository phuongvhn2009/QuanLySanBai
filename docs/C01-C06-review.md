# Cụm 3 — Cấu hình cơ sở (C01–C06, A05)

## Phạm vi triển khai

| Mã | Chức năng | Kiểm soát nghiệp vụ |
| --- | --- | --- |
| C01 | Tạo/sửa cơ sở, liên kết chủ | Tạo cơ sở chờ duyệt, thành viên và vai trò chủ sân cùng transaction; không sửa cơ sở người khác; đổi tên/địa chỉ phải duyệt lại |
| C02 | Môn, loại sân và sân | Admin quản lý danh mục; chủ quản lý sân của mình; ngừng hoạt động thay cho xóa lịch sử |
| C03 | Lịch tuần và khung giờ | Nghỉ theo thứ; chặn giờ ngược và khung chồng lấn; kết thúc 00:00 hiểu là 24:00 |
| C04 | Giá và cọc | Decimal tối đa hai số thập phân; cọc không vượt giá; giá theo sân/khung/thứ/ngày; chặn hiệu lực giao nhau |
| C05 | Chính sách hủy/đổi | Mức phí liên tục từ 0 giờ đến vô hạn; giữ điều khoản đã dùng; đóng phiên bản cũ để tạo phiên bản sau |
| C06 | Khóa sân, bảo trì, demo | Không đặt vào giờ khóa/bảo trì; không khóa đè booking còn hiệu lực; seed chỉ bật rõ ràng trong Development |
| A05 | Admin quản trị | Duyệt/từ chối cơ sở, khóa/mở tài khoản; kiểm tra ADMIN phía server; không tự khóa tài khoản hiện tại |

## Luồng cấu hình trước khi bán

1. Tài khoản đăng ký cơ sở, được liên kết làm chủ sở hữu; cơ sở ở trạng thái Chờ duyệt.
2. Admin tạo danh mục môn và loại sân. Một môn có thể có nhiều loại sân; chỉ tạo môn chưa đủ để chủ chọn loại sân.
3. Chủ cơ sở tạo sân và cấu hình khung giờ, lịch hoạt động, giá/cọc đủ các thứ cần bán.
4. Chủ tạo chính sách hiện hành với các mức phí liên tục.
5. Admin duyệt cơ sở. Khách chỉ thấy sân khi cơ sở và môn đang hoạt động.
6. Khóa sân hoặc bật bảo trì khi cần; SQL từ chối xung đột với booking đang có hiệu lực.

## Các trang thao tác

- `/CoSo`: chọn cơ sở theo quyền tài khoản.
- `/CoSo/Configuration/Facility`: tạo cơ sở mới.
- `/CoSo/Configuration/Index/{id}`: sáu mục cấu hình mở/đóng.
- `/CoSo/Configuration/Court?facilityId={id}`: thêm sân; truyền thêm id để sửa.
- `/Admin/Catalog`: môn và loại sân.
- `/Admin/Management/Facilities`: duyệt cơ sở.
- `/Admin/Management/Users`: quản lý trạng thái tài khoản.

Mã chính: `FacilityService.cs`, `DevelopmentSeeder.cs`, `ConfigurationController.cs`, `CatalogController.cs`, `ManagementController.cs`, `FacilityModels.cs`, `CatalogModels.cs` và Views tương ứng.

## Kiểm thử và dữ liệu mẫu

Kiểm thử tích hợp đã xác minh quyền chủ, transaction tạo cơ sở, khung chồng lấn, cọc sai, giá trùng, chính sách gãy/thiếu, bất biến giá/phí đã dùng, khóa sân và seed chạy lặp không tạo bản sao. Các kiểm tra này nằm trong bộ SQL/HTTP chung đã qua 74 kiểm tra tại lần bàn giao Git.

Dữ liệu demo có cơ sở đã duyệt, sân cầu lông, hai khung và giá/cọc đủ tuần. Xem [hướng dẫn dữ liệu mẫu](../test-data/README.md); không đưa mật khẩu vào tài liệu Git.

## Git và nghiệm thu

Nhánh `MC-CauHinhCoSo`, commit [08de2ea](https://github.com/phuongvhn2009/QuanLySanBai/commit/08de2ea), đã push và merge vào master. Công việc được bàn giao Chờ kiểm tra trên Notion; người dùng xác nhận Hoàn thành sau khi duyệt. Cấu hình chính sách ở cụm này chưa bao gồm thao tác thực hiện hủy/đổi hoặc hoàn tiền.
