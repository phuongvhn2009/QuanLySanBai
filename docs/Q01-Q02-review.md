# Cụm 6 — Kiểm thử cốt lõi (Q01–Q02)

## Kết quả thực tế

Bộ kiểm thử SQL/HTTP mở rộng chạy qua 106 kiểm tra; build 0 lỗi và 0 cảnh báo. File tests/Cluster4Checks/OperationChecks.cs chứa các kiểm tra cụm 5–6, được gọi trong runner hiện có. Số đếm có thể chênh 1 giữa các lần do kiểm tra phụ thuộc người thắng tranh chỗ; đây là các assertion trong console runner, không phải 106 test case xUnit độc lập.

Q01 kiểm chứng:

- Người không có quyền không được thu tiền/đặt quầy/hủy; danh sách cơ sở khác bị chặn.
- Cùng yêu cầu thu tiền chạy song song chỉ ghi một khoản; cùng khóa nhưng khác nội dung bị từ chối.
- Hai khoản thu khác khóa với cùng phiên bản đơn không cùng sử dụng số dư cũ.
- Chặn thu vượt, cọc đầu thiếu, biểu mẫu cũ, nhận tiền sau hết hạn.
- Khi expiry thắng: đơn/slot hết hạn và không có khoản thu thành công. Khi tiền thắng: đơn/slot đã xác nhận không bị expiry giải phóng dù deadline đã qua.
- Cuộc đua hủy và thu tiền không để lại đơn hủy có khoản thu hoặc đơn xác nhận không có tiền.
- Mã/giờ check-in sai, check-in lặp, kết thúc sớm/thiếu tiền đều bị chặn; lịch sử lưu bền.
- Hủy đơn chưa thu tiền giải phóng slot và có lịch sử; có tiền bị chuyển sang yêu cầu quy trình hoàn.

Q02 kiểm chứng:

1. Khách đặt khung bằng BookingService/procedure thực.
2. Chủ cơ sở nhận đủ cọc, đơn và slot cùng được xác nhận.
3. Check-in bằng mã và lưu đúng nhân viên.
4. Thu bổ sung đủ tổng tiền, không vượt số tiền còn lại.
5. Sau giờ kết thúc hoàn thành đơn/slot.
6. Tạo DbContext mới đọc lại trạng thái, tổng khoản thu và lịch sử từ SQL Server.

Test check-in/kết thúc dùng TimeProvider giả lập giờ tiến tới buổi đặt, không thay giờ máy, không cho request của khách sửa đồng hồ. Ứng dụng thật dùng TimeProvider.System. POST thu tiền/xác nhận và gửi lại cùng khóa cũng được kiểm tra qua HTTP có cookie/CSRF thật.

## Chạy lại

```powershell
dotnet build tests/Cluster4Checks -o .artifacts/checks
dotnet .artifacts/checks/Cluster4Checks.dll D:/QuanLysanBai
```

Runner tạo database tên ngẫu nhiên QuanLySanBai_Cluster4Tests_<GUID>, cài script, mở server HTTP trên cổng riêng và dọn server/database trong finally. Không dùng dữ liệu local thật để thử thu tiền/đồng thời. Cần LocalDB và quyền tạo/xóa database thử.

## Còn cần người dùng duyệt

- Quy tắc check-in tạm 15 phút trước giờ bắt đầu; bắt buộc hết giờ và thu đủ trước khi hoàn thành.
- Giao diện trực quan và chạy demo thủ công với nhân viên thực.
- Cụm này chưa chứng minh vận hành production, khả năng chịu tải hoặc tích hợp thanh toán ngân hàng. Hoàn tiền/hủy có phí, ví và cổng thanh toán vẫn thuộc phần mở rộng.

Kết quả được bàn giao Chờ kiểm tra trên Notion, chưa tự xác nhận Hoàn thành thay người dùng. Code cụm 5–6 chưa commit/push.
