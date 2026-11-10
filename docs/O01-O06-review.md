# Cụm 5 — Vận hành tại sân (O01–O06)

## Phạm vi

| Mã | Đã triển khai |
| --- | --- |
| O01 | Danh sách/lịch theo ngày sử dụng; đặt TAI_QUAY cho khách vãng lai, lấy nhân viên từ phiên; kiểm tra TAO_DON và cơ sở |
| O02 | Ghi nhận tiền mặt/chuyển khoản tại quầy; mục đích cọc/thanh toán/bổ sung; lưu người thu/tham chiếu; khóa chống lặp; chặn thu vượt |
| O03 | Thu đủ điều kiện cọc xác nhận đơn và slot cùng transaction; form cũ bị từ chối qua row_version; phối hợp khóa lịch với expiry |
| O04 | Check-in bằng mã đặt hoặc nội dung QR; kiểm tra CHECK_IN, cơ sở, giờ và trạng thái; lưu lịch sử; kết thúc khi hết giờ và thu đủ |
| O05 | Chủ đơn hoặc nhân viên có DOI_HUY_DON hủy trước giờ sử dụng khi chưa thu tiền; lưu lịch sử và giải phóng slot; có tiền phải qua hoàn tiền |
| O06 | Danh sách 20 đơn/trang, lọc ngày/trạng thái/điện thoại; chi tiết khoản thu/lịch sử và các nút theo quyền |

Mã chính: OperationService.cs, OperationModels.cs, OrdersController.cs, Views/Orders. BookingsController/Details bổ sung hủy cho chính khách đặt.

## Cách sử dụng

1. Đăng nhập chủ sân demo (xem test-data/README.md), vào Cơ sở của tôi → Lịch và đơn đặt.
2. Chọn Đặt tại quầy hoặc mở đơn khách đã đặt.
3. Mở Ghi nhận khoản thu, nhập số tiền và phương thức. Chỉ xác nhận sau khi thực tế nhận tiền.
4. Lần thu đầu phải đạt cọc yêu cầu. Hệ thống tự xác nhận đơn/slot cùng giao dịch. Đơn cọc 0 có nút xác nhận riêng.
5. Check-in: nhập mã đặt sân hoặc chuỗi nội dung QR khách cung cấp. Bản hiện tại không tích hợp camera quét QR.
6. Thu phần còn lại và hoàn thành buổi sau giờ kết thúc.

Nhân viên cần XEM_LICH để mở danh sách/chi tiết; TAO_DON để đặt tại quầy; THU_TIEN để thu/xác nhận; CHECK_IN để check-in/kết thúc; DOI_HUY_DON để hủy chưa thu. Chủ sở hữu có quyền tại cơ sở mình. Admin không tự động có quyền vận hành cơ sở người khác.

## Quy tắc đang áp dụng để người dùng duyệt

- Giữ chỗ theo hạn server; cùng khóa QLSB:LICH_SAN cho thu/xác nhận/hủy và procedure hết hạn.
- Không nhận một phần cọc thấp hơn yêu cầu khi đơn còn giữ chỗ, tránh nhận tiền rồi slot hết hạn. Đơn đã xác nhận cho phép thu bổ sung trong giới hạn còn phải trả.
- Tạm dùng check-in từ 15 phút trước giờ bắt đầu đến trước giờ kết thúc. Cấu hình Operations:CheckInEarlyMinutes (0–120). Đã hỏi người dùng, chưa nhận lựa chọn; cần duyệt quy tắc này.
- Chỉ hoàn thành khi đã check-in, hết giờ và thu đủ; không cho thao tác lặp.
- Khóa thu tiền chống lặp gắn với toàn bộ nội dung yêu cầu; gửi lại cùng khóa/nội dung trả thành công nhưng không ghi khoản mới. Khác nội dung bị từ chối.
- row_version phát hiện biểu mẫu cũ; tải lại để nhận phiên bản mới.
- Chuyển khoản tại quầy là ghi nhận thủ công sau khi nhân viên kiểm chứng; chưa phải tích hợp ngân hàng/webhook.

## Cập nhật database

Database mới: dùng script Data/QuanLySanBai_SQLServer.sql đã có row_version.
Database có sẵn: chạy script bổ sung trước khi mở bản ứng dụng mới:

```powershell
sqlcmd -S '(localdb)\MSSQLLocalDB' -E -C -d QuanLySanBai -i Data/Updates/001_OrderRowVersion.sql -b
```

Đã áp dụng trên LocalDB hiện tại. Script chỉ thêm cột nếu thiếu, không xóa dữ liệu. Mapping bổ sung nằm trong partial, không sửa entity scaffold gốc.

## Kiểm chứng

Bộ SQL/HTTP chung qua 106 kiểm tra trong lần chạy mở rộng, gồm nghiệp vụ cụm 5. Chi tiết ở Q01-Q02-review.md. Giao diện đã kiểm tra đáp ứng HTTP, chưa kiểm tra ảnh render trực quan. Chờ người dùng duyệt và xác nhận trên Notion. Thay đổi cụm 5–6 chưa commit/push.
