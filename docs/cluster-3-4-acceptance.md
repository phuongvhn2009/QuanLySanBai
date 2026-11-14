# Kết quả kiểm thử và hướng dẫn nghiệm thu cụm 3–4

Ngày chạy: 2026-11-14. Nhánh: master, commit `0a8233f` (kế thừa `59083dd`, đã bao gồm sửa phân quyền và SignalR).

## Kết quả tự động

- Restore/build thành công: 0 lỗi, 0 cảnh báo. NuGet audit bị tắt trong lệnh restore này; không phải kết quả kiểm toán lỗ hổng thư viện.
- Toàn bộ 101 assertion SQL/HTTP/WebSocket đạt. Tổng này gồm cả kiểm tra nền tảng, tài khoản và realtime; không phải 101 test riêng cho cụm 3–4.
- Tự tạo database `QuanLySanBai_Cluster4Tests_cd15850dc94d4e97bfe2d10c1cea2f61`, tự sinh tài khoản/cơ sở/sân/lịch/giá/chính sách, chạy server HTTP tạm rồi dọn server và database. Không dùng database live để ghi dữ liệu kiểm thử.
- Log: [cluster34-2026-11-14.txt](evidence/cluster34-2026-11-14.txt).
- Không phát hiện lỗi trong các tình huống đã chạy. Chưa kiểm tra trực quan, thao tác chuột hay bố cục trên hai trình duyệt; HTTP render không thay thế kiểm tra giao diện.

| Mã | Bằng chứng tình huống đã chạy đạt |
| --- | --- |
| C01 | Cơ sở mới chờ duyệt, tạo liên kết chủ sở hữu và vai trò; chủ khác bị chặn sửa; trang thông tin/cấu hình trả HTTP thành công. |
| C02 | Lưu sân; sân bảo trì bị chặn đặt; môn ngừng hoạt động bị chặn cả request giả mạo; trang danh mục admin và POST ngừng môn hoạt động. |
| C03 | Khung chồng lấn/giờ ngược bị SQL chặn; lịch nghỉ cả ngày hiển thị và không đặt được; khung 23:00–00:00 đặt được. |
| C04 | Cọc vượt giá, giá chồng hiệu lực, tiền quá hai số thập phân bị chặn; tổng 123456.78 và cọc 50000.25 lưu đúng; không sửa được giá đã có lịch sử. |
| C05 | Mức phí bị hở khoảng bị từ chối; chính sách hợp lệ lưu được; thiếu chính sách không đặt được; phí đã dùng không sửa được. |
| C06 | Lịch khóa hiển thị, chặn đặt; gỡ khóa dùng lại được; không khóa đè buổi đã giữ chỗ; seed demo chạy lặp không nhân đôi dữ liệu. |
| B01 | Cơ sở chưa duyệt không có trong PublicCourts; danh sách sân public render qua HTTP. Từng tổ hợp bộ lọc cần thử bổ sung theo checklist web. |
| B02 | Giá/khung hiển thị đúng trong service; phân biệt giữ chỗ, khóa, nghỉ; trang lịch render qua HTTP. |
| B03 | Gọi procedure thật; request HTTP không thể giả giá/người đặt; người chưa đăng nhập bị chặn; lỗi đặt sân quay lại lịch với thông báo an toàn. |
| B04 | Hết hạn cập nhật đơn và slot, giải phóng khung; chạy expiry lặp an toàn; worker nền hoạt động; người khác không xem được chi tiết đơn. Countdown cần xem trực tiếp trên web. |
| B05 | Hai yêu cầu song song chỉ một thành công, không có đơn mồ côi; ngày quá khứ/nghỉ/bảo trì bị chặn; thứ và biên 00:00 đúng. |

Ngoài ra: quyền nhân viên được thu hồi ngay trong cùng phiên; thông báo SignalR đặt sân/hết hạn và cấp/thu hồi quyền được kiểm chứng qua WebSocket thật, kết nối không liên quan không nhận thông báo quyền.

## Chuẩn bị để thử trên web

1. Khởi động lại app để dùng mã local mới: `dotnet run --project QuanLySanBai --launch-profile http`. Mở http://localhost:5073. Nếu app báo lỗi DB, kiểm tra connection string phù hợp máy trước; không đổi về LocalDB chỉ vì bộ test dùng LocalDB.
2. Dùng dữ liệu Development/demo. Tài khoản mẫu được mô tả ở `test-data/README.md`; mật khẩu xem `.local/demo-accounts.txt` hoặc `test-data/demo-accounts.local.txt`, không dán vào báo cáo lỗi.
3. Cần một admin, hai chủ cơ sở để kiểm tra truy cập chéo, hai tài khoản khách khác nhau để kiểm tra tranh chỗ. Có thể tự đăng ký khách và cơ sở test riêng; đặt tên dễ nhận ra, ví dụ `TEST-C34-1411`.
4. Dùng hai trình duyệt hoặc thường/ẩn danh. Hai tab trong cùng hồ sơ trình duyệt thường dùng chung phiên, không phù hợp mô phỏng hai người.
5. Chọn ngày tương lai D (ví dụ ngày mai), ghi lại thứ của D: Hai=1…Chủ nhật=7. Khung mẫu 18:00–19:00; cơ sở mở 06:00–00:00; giá 150000, cọc 50000. Dùng ngày D và D+7 cho các ca khác nhau, tránh đặt hết mọi khung trước khi thử.
6. Giá và chính sách demo cũ có thể hết hiệu lực theo ngày. Luôn kiểm tra ngày hiệu lực, đừng coi không đặt được do hết giá là lỗi hệ thống.

## Checklist thao tác giao diện

### C01 — Tạo/sửa cơ sở

- [ ] Chủ sân vào Quản lý cơ sở → Thêm cơ sở (tài khoản chưa có cơ sở dùng menu hồ sơ → Đăng ký cơ sở kinh doanh). Nhập tên test, địa chỉ và giờ hợp lệ, lưu: cơ sở mới ở Chờ duyệt.
- [ ] Admin vào Duyệt cơ sở, duyệt cơ sở test. Chủ sân tải lại: hiển thị Đã duyệt.
- [ ] Chủ sân sửa tên/địa chỉ: cơ sở quay lại Chờ duyệt. Admin duyệt lại để tiếp tục các ca đặt sân.
- [ ] Sao chép URL sửa cơ sở, mở bằng tài khoản chủ khác: bị từ chối, không lộ dữ liệu chỉnh sửa.

### C02 — Danh mục và sân

- [ ] Admin vào Danh mục, tạo môn/loại sân test nếu thiếu. Chủ sân vào cơ sở → Quản lý sân và cấu hình → Sân → Thêm sân, chọn đúng loại, lưu.
- [ ] Sửa tên/loại sân rồi mở lại: dữ liệu được lưu đúng. Thử ngừng hoạt động: khách không tìm/đặt được sân đó. Bật lại để thử tiếp.
- [ ] Admin ngừng môn thể thao: sân thuộc môn đó không nhận đặt. Bật lại sau thử. Chỉ thao tác môn test để tránh ảnh hưởng các sân khác.

### C03 — Lịch tuần và khung giờ

- [ ] Trong cấu hình, mở Lịch tuần; thiết lập thứ của D mở 06:00–00:00.
- [ ] Mở Khung giờ, thêm 18:00–19:00 và 23:00–00:00: hợp lệ.
- [ ] Thử thêm 18:30–19:30 trùng khung hoặc 19:00–18:00: bị từ chối, không phát sinh dòng hợp lệ mới.
- [ ] Đánh dấu thứ của D Nghỉ cả ngày; phía khách thấy không đặt được. Bỏ ngày nghỉ để tiếp tục.

### C04 — Giá và cọc

- [ ] Mở Giá và tiền cọc, chọn sân/khung/thứ của D; giá 150000, cọc 50000, hiệu lực bao phủ D, lưu. Lịch khách hiển thị đúng.
- [ ] Thử cọc 160000 với giá 150000 hoặc tạo thêm giá cùng sân/khung/thứ có thời gian hiệu lực chồng nhau: bị từ chối.
- [ ] Khi cần thay giá, dùng Tắt giá rồi tạo giá mới; đơn đã đặt phải giữ giá cũ. Không xóa lịch sử đơn để đổi giá.

### C05 — Chính sách

- [ ] Mở Chính sách hủy/đổi → Thêm chính sách. Nếu chưa có chính sách test, dùng một mức: Từ giờ=0, Đến giờ để trống, phí hủy/đổi trong 0–100; hiệu lực từ hôm nay, lưu.
- [ ] Với chính sách mới độc lập, thử mức bắt đầu từ 2 giờ (thiếu khoảng 0–2), hoặc hai mức 0–2 và 3–vô hạn: bị từ chối.
- [ ] Không tạo phiên bản chồng chính sách đang hiệu lực. Dùng cơ sở test khác hoặc ngày hiệu lực không chồng nếu cần thử thêm.
- [ ] Lưu ý: cấu hình chính sách ở cụm 3 chưa có nghĩa luồng hoàn tiền/đổi đơn mở rộng đã triển khai.

### C06 — Khóa/bảo trì

- [ ] Dùng một khung chưa có đơn; mở Khóa sân, khóa 18:00–19:00 ngày D, ghi lý do. Khách tải lại lịch: khung bị khóa, không đặt được.
- [ ] Gỡ khóa, tải lại lịch: khung có thể đặt nếu đủ giá/chính sách và chưa có người đặt.
- [ ] Đánh dấu sân Đang bảo trì: bị chặn đặt. Bỏ bảo trì sau thử.
- [ ] Thử khóa đè một khung đang giữ chỗ/đã đặt: bị từ chối.
- [ ] Thay đổi cấu hình khóa/giá chưa được nối SignalR trong bản này: tải lại trang khách bằng tay khi kiểm tra.

### B01 — Tìm sân

- [ ] Khách vào Tìm sân, thử lọc riêng môn, khu vực, ngày, rồi kết hợp các bộ lọc. Kết quả đúng dữ liệu cơ sở test; trường hợp không khớp hiển thị danh sách rỗng hợp lý.
- [ ] Cơ sở chờ duyệt/ngừng hoạt động và sân ngừng hoạt động không xuất hiện công khai.
- [ ] Bấm vào sân: tên, cơ sở, môn/loại và lịch đúng sân đã chọn.

### B02 — Lịch

- [ ] Chọn D, đối chiếu khung/giá/cọc với cấu hình; đổi ngày để kiểm tra thứ và hiệu lực giá.
- [ ] Lần lượt quan sát trống, giữ chỗ, khóa/bảo trì; khung không hợp lệ không có nút đặt khả dụng.
- [ ] Mở cùng lịch bằng trình duyệt thứ hai. Khi bên thứ nhất đặt thành công, bên thứ hai tự cập nhật nhờ SignalR.

### B03 — Đặt sân

- [ ] Khi chưa đăng nhập, thử đặt: yêu cầu đăng nhập.
- [ ] Khách đăng nhập, chọn khung hợp lệ và đặt: tới chi tiết đơn, đúng sân/ngày/giờ/tổng/cọc, trạng thái giữ chỗ.
- [ ] Kiểm tra Lịch đặt của tôi có đơn mới, không bị tạo thêm đơn khi thao tác đã thất bại.
- [ ] Trường hợp sửa request để giả giá/người đặt đã được kiểm tra tự động; không cần sửa DB để thử lại trên UI.

### B04 — Hết hạn và bảo mật đơn

- [ ] Mở chi tiết đơn mới: thấy thời hạn và đếm ngược. Chờ hết hạn thực, không chỉnh đồng hồ máy để mô phỏng.
- [ ] Worker chạy theo chu kỳ nên có thể chậm thêm một chu kỳ sau deadline. Tải lại lịch/đơn: trạng thái hết hạn và khung được giải phóng.
- [ ] Dùng khách thứ hai mở URL chi tiết đơn của khách thứ nhất: không xem được đơn.
- [ ] Không sửa thời hạn trực tiếp trên database live để tăng tốc. Bộ test tự động đã mô phỏng hết hạn trên database tạm.

### B05 — Tranh chỗ và biên giờ

- [ ] Hai khách khác nhau mở cùng sân/ngày/khung còn trống. Cùng bấm đặt: chỉ một người thành công; người còn lại nhận lỗi hợp lý. SignalR có thể cập nhật sớm và vô hiệu hóa nút của bên còn lại; ca cạnh tranh thật đã được chạy song song trong bộ test.
- [ ] Thử ngày quá khứ, ngày nghỉ và sân bảo trì: không tạo được đơn.
- [ ] Tạo đủ giá/chính sách cho khung 23:00–00:00 ngày tương lai, thử đặt: khung kết thúc vào 00:00 ngày hôm sau, thời lượng một giờ.

## Ghi kết quả

Với mỗi mã ghi: Đạt/Lỗi/Chưa thử; vai trò dùng; sân/ngày/khung; thao tác; kết quả mong muốn và thực tế; ảnh hoặc URL liên quan (không gửi mật khẩu/cookie). Chỉ chuyển Hoàn thành trên Notion khi người nghiệm thu xác nhận. Lần kiểm thử tự động này không tự thay trạng thái nghiệm thu của 11 công việc.
