# Ý tưởng tham khảo AloBo — backlog chưa triển khai

Cập nhật: 2026-11-16. Đây là ghi chú nghiên cứu sản phẩm, không phải cam kết triển khai hay xác nhận nghiệm thu. Trước khi nhận việc cần xem Git/code, Notion và ưu tiên hiện tại.

## Hiện trạng đã đối chiếu

- Dự án có tìm sân theo môn, khu vực/địa chỉ và ngày; lịch chi tiết theo sân/ngày có trạng thái slot, giá, cọc và chính sách hủy/đổi.
- Có giữ chỗ hết hạn, đơn của khách, lịch/đơn vận hành theo ngày, đặt tại quầy, thu tiền thủ công, check-in và cập nhật realtime lịch.
- Tài liệu O01–O06 mô tả thao tác nghiệp vụ tại sân; hiện trang vận hành thiên về bảng đơn. Lịch theo tuần/tháng chưa được xác nhận trong UI hiện có.
- Chưa có xác nhận tích hợp ngân hàng/cổng thanh toán online tự động. Ghi nhận chuyển khoản tại quầy là thao tác thủ công.

## Đề xuất theo thứ tự ưu tiên

### P1 — Lịch vận hành trực quan cho chủ sân/nhân viên

Tham khảo cách AloBo cho xem sân trống theo giờ hoặc giờ trống theo sân và nhiều phạm vi lịch. Cân nhắc màn hình ngày dạng lưới **sân × khung giờ**, màu trạng thái nhất quán, thao tác mở đơn/đặt tại quầy/check-in từ ô lịch và chuyển nhanh ngày. Chỉ thêm tuần/tháng nếu nghiệp vụ thật sự cần. Tận dụng cập nhật SignalR hiện có sau khi xác nhận sự kiện nào cần phát.

### P1 — Tìm kiếm có thời gian và giá

Màn `/Courts` đã có môn, khu vực và ngày. Cân nhắc thêm khung giờ bắt đầu/kết thúc, khoảng giá và nút ngày nhanh (Hôm nay/Ngày mai). Giá thay đổi theo khung giờ nên phải lọc theo giá slot còn trống trong ngày/giờ người dùng chọn; không dùng giá trung bình cơ sở gây kết quả sai. Một đánh giá App Store của AloBo cũng yêu cầu lọc theo ngày/giờ, giá và khu vực — coi đây là tín hiệu nhu cầu của một người dùng, không phải nghiên cứu định lượng.

### P2 — Thanh toán online qua QR/cổng

Tách hai mức:

1. QR chỉ điền sẵn số tiền/nội dung chuyển khoản: khách vẫn chuyển tiền, nhân viên/chủ sân cần xác nhận thủ công.
2. Cổng thanh toán có webhook xác thực: hệ thống mới có thể tự cập nhật giao dịch/đơn sau khi xác minh chữ ký, mã giao dịch, số tiền, trạng thái và xử lý callback lặp.

Chưa chọn nhà cung cấp hay thiết kế tích hợp. Không tự chuyển đơn sang đã thanh toán chỉ vì khách mở/quét QR hoặc gửi ảnh biên nhận.

### P2 — Làm rõ bước xác nhận booking

Giữ các phần đã có: giá/cọc, chính sách hủy/đổi trên lịch và đồng hồ giữ chỗ ở trang đơn. Có thể làm nổi bật tổng tiền, cọc cần trả, thời hạn giữ slot, bước thanh toán tiếp theo và điều kiện hủy ngay trước/sau khi đặt. Không cần thêm voucher trước khi luồng thanh toán và chính sách được duyệt rõ.

### Backlog mở rộng — chỉ sau khi luồng lõi ổn định

- Thêm dịch vụ (nước, thuê dụng cụ, phụ phí) vào đơn đặt/quầy và thu cùng hóa đơn.
- Sau đó mới cân nhắc kho, hội viên/gói lượt, sự kiện giao lưu/bán vé, hóa đơn điện tử và báo cáo doanh thu/lợi nhuận.
- Không suy luận rằng entity/bảng DB sẵn có đồng nghĩa những luồng này đã triển khai.

## Nguồn AloBo đã xem

- [Trang chính thức](https://www.alobo.vn/) — nội dung quản lý trạng thái sân, lịch, dịch vụ, khách hàng và các số liệu AloBo tự công bố.
- [App đặt sân trên App Store](https://apps.apple.com/vn/app/alobo-sports-field-booking/id6479625228) — mô tả tìm kiếm theo địa điểm/môn/giờ, tình trạng realtime, thanh toán và đánh giá; có ý kiến người dùng về bộ lọc và đăng nhập.
- [Hướng dẫn đặt lịch và duyệt đơn](https://wiki.alobo.vn/article/huong-dan-dat-lich-online-va-duyet-don/) — có bước voucher/dịch vụ; phân biệt chuyển khoản chủ sân xác nhận với cổng BIDV tự xác nhận.
- [Bật đặt online và QR](https://wiki.alobo.vn/article/huong-dan-mo-dat-lich-online/) — QR có số tiền đơn; cần cấu hình ngân hàng; có link đặt sân chia sẻ.
- [Tính năng vận hành](https://wiki.alobo.vn/article/bat-tat-dat-lich-online/) — mô tả lịch, social, hội viên, bán hàng/kho và báo cáo. Mô tả nhà cung cấp, không phải kiểm thử độc lập.

Quy mô “hơn 2.500 chủ sân” là tuyên bố marketing trên website AloBo; App Store hiển thị khoảng 22k đánh giá tại thời điểm xem. Chưa xác minh độc lập số liệu hoặc ngày bắt đầu hoạt động chính xác của AloBo.
