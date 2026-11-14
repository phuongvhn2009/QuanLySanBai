# So sánh hai dự án quản lý đặt sân

## Phạm vi kiểm tra

Repo đối chiếu: TrTung216/QuanLy_DatSan_UNETI5_TI17A4HN, commit e33067f9626a99f40ddd7a11e19d5aabd0726f29. Dự án hiện tại: phuongvhn2009/QuanLySanBai, master 9e94b0e. Đọc mã nguồn, model, controller, service, repository và test. Chưa build/chạy repo đối chiếu, chưa chạy test của họ, chưa xác nhận bố cục trực quan hoặc kết nối ngân hàng. Không coi số file/test hay số bảng là thước đo chất lượng.

Kết luận: repo đối chiếu có tổ chức và có nhiều phần xử lý thực chất, đáng tham khảo. Họ đi xa hơn về luồng vận hành đơn/dịch vụ/cọc/thống kê; dự án hiện tại có mô hình nhiều cơ sở và quyền từng cơ sở chi tiết hơn. Không có cơ sở kết luận một repo tốt hơn ở tất cả tiêu chí.

## Bảng so sánh

| Tiêu chí | Repo đối chiếu | Dự án hiện tại |
| --- | --- | --- |
| Kiến trúc | Controller, Service, Repository, Unit of Work, interface và DTO; tách nhiều nghiệp vụ | Service nghiệp vụ và EF/SQL procedure; một số controller còn truy cập DB trực tiếp |
| Phạm vi đã làm | Xác nhận/từ chối/hủy/hoàn thành đơn, dịch vụ, tính tiền, thống kê; có cọc mô phỏng | Đến cụm 4; chưa có các chức năng vận hành/tài chính hoàn chỉnh |
| Vai trò | Admin, NhanVien, KhachHang toàn hệ thống | Chủ sở hữu và thành viên/quyền riêng theo cơ sở; Admin riêng |
| Chống đặt trùng | Transaction và khóa dòng sân UPDLOCK, kiểm tra lại sau khóa | Procedure/trigger với applock toàn lịch; đã thử đồng thời trên SQL |
| Sửa đồng thời | RowVersion cho đơn và kiểm tra phiên bản gửi từ form | Có transaction/constraint cho nhiều thao tác; chưa kiểm soát phiên bản toàn diện trên các form cấu hình |
| Giữ chỗ | Đơn ChoXuLy được tính vào xung đột; chưa thấy worker hết hạn tự động trong mã đã đọc | Hạn giữ chỗ và worker giải phóng định kỳ |
| Giá | Có hàm cao điểm/cuối tuần; có dấu hiệu chưa đồng bộ vào luồng lưu đơn | Bảng giá theo sân/khung/thứ/ngày; snapshot khi đặt |
| Test | Nhiều project xUnit, test service/HTTP/SQL và tranh chấp cọc | Console integration suite SQL/HTTP đã chạy qua 74 kiểm tra tại lần bàn giao |
| Đăng nhập | PasswordHasher, cookie/session, đọc lại trạng thái, rate limit | PasswordHasher, cookie và đọc lại trạng thái; chưa có rate limit đăng nhập |
| Dữ liệu | EF migration, model nhỏ hơn theo phạm vi | Database First, schema lớn hơn; nhiều bảng thuộc chức năng chưa triển khai |

## Điểm nên học từ repo đối chiếu

1. Phân tách hợp đồng nghiệp vụ, DTO và truy cập dữ liệu rõ, dễ thay thế phụ thuộc khi test. Repository không tự commit; Unit of Work quản transaction, rollback và tháo tracking khi thất bại.
2. RowVersion giúp từ chối biểu mẫu cũ khi đơn bị người khác sửa. Đây là thứ dự án hiện tại cần trước các thao tác thu tiền/xác nhận.
3. Khóa theo sân có phạm vi hẹp hơn khóa lịch toàn hệ thống hiện tại, thuận lợi hơn khi nhiều sân được đặt độc lập. Chưa benchmark nên đây là nhận định thiết kế.
4. Có rate limit thử đăng nhập và cookie Secure Always ngoài Development; có test đồng thời cọc/xác nhận/hủy.
5. Truy vấn khả dụng theo lô; danh sách có count, tổng trang và giới hạn trang. Dự án hiện tại còn đọc Calendar từng sân và một số danh sách chỉ Take số lượng cố định.

## Điểm cần kiểm tra trước khi học theo

### 1. Nguy cơ lệch giá báo và giá đơn

GiaThueSan.TinhTien dùng giá cuối tuần hoặc phân đoạn 18:00–21:00. Tuy nhiên DatSanService.TaoDonAsync lưu DonGia = san.DonGia; TinhTienDonAsync gọi TinhTienService với giá đã lưu và thời lượng, không dùng giá cao điểm/cuối tuần.

Ví dụ đọc từ logic: giá thường 100.000/giờ, cuối tuần 150.000/giờ, thuê 1 giờ cuối tuần: hàm giá tham khảo cho 150.000, còn đường lưu/tính đơn có thể dùng 100.000. Đây là phân tích tĩnh cần tái hiện end-to-end, chưa phải kết quả chạy.

Nguồn: [giá theo khung](https://github.com/TrTung216/QuanLy_DatSan_UNETI5_TI17A4HN/blob/e33067f9626a99f40ddd7a11e19d5aabd0726f29/QuanLyDatSan_UNETI5_DHTI17A4HN/Services/GiaThueSan.cs), [tạo đơn](https://github.com/TrTung216/QuanLy_DatSan_UNETI5_TI17A4HN/blob/e33067f9626a99f40ddd7a11e19d5aabd0726f29/QuanLyDatSan_UNETI5_DHTI17A4HN/Services/DatSanService.cs), [tính tiền đơn](https://github.com/TrTung216/QuanLy_DatSan_UNETI5_TI17A4HN/blob/e33067f9626a99f40ddd7a11e19d5aabd0726f29/QuanLyDatSan_UNETI5_DHTI17A4HN/Services/DatSan/DatSanService.Tien.cs).

### 2. Chính sách cọc mới chưa bật trong tạo đơn

TaoDonAsync ghi rõ chưa kích hoạt chính sách cọc 1; không đặt PhienBanChinhSachCoc/TienCocYeuCau khi tạo. Code cọc mô phỏng kiểm tra phiên bản, môi trường Development và cấu hình. Không nên mô tả là đã tích hợp thanh toán ngân hàng hoặc luồng cọc mới hoàn chỉnh chỉ vì đã có màn hình/test.

Nguồn: [tạo đơn](https://github.com/TrTung216/QuanLy_DatSan_UNETI5_TI17A4HN/blob/e33067f9626a99f40ddd7a11e19d5aabd0726f29/QuanLyDatSan_UNETI5_DHTI17A4HN/Services/DatSanService.cs), [cọc mô phỏng](https://github.com/TrTung216/QuanLy_DatSan_UNETI5_TI17A4HN/blob/e33067f9626a99f40ddd7a11e19d5aabd0726f29/QuanLyDatSan_UNETI5_DHTI17A4HN/Services/DatSan/DatSanService.Coc.cs).

### 3. Chưa có mô hình chủ/quyền theo cơ sở như yêu cầu của dự án hiện tại

Enum chỉ có ba vai trò; model sân không gắn chủ/cơ sở. Service quản lý chấp nhận Admin hoặc NhanVien và truy vấn đơn quản lý toàn hệ thống. Phù hợp một mô hình vận hành tập trung, nhưng không thể mang nguyên sang yêu cầu nhiều chủ độc lập. Đây là khác phạm vi, không tự coi là lỗ hổng trong đề tài của họ.

Nguồn: [vai trò](https://github.com/TrTung216/QuanLy_DatSan_UNETI5_TI17A4HN/blob/e33067f9626a99f40ddd7a11e19d5aabd0726f29/QuanLyDatSan_UNETI5_DHTI17A4HN/Enums/VaiTro.cs), [sân](https://github.com/TrTung216/QuanLy_DatSan_UNETI5_TI17A4HN/blob/e33067f9626a99f40ddd7a11e19d5aabd0726f29/QuanLyDatSan_UNETI5_DHTI17A4HN/Models/SanTheThao.cs), [truy vấn đơn](https://github.com/TrTung216/QuanLy_DatSan_UNETI5_TI17A4HN/blob/e33067f9626a99f40ddd7a11e19d5aabd0726f29/QuanLyDatSan_UNETI5_DHTI17A4HN/Repositories/QuanLyDatSan/DatSanRepository.cs).

### 4. Giới hạn giờ và lịch sử dịch vụ

Kiểm tra khoảng giờ yêu cầu kết thúc cùng ngày và sau bắt đầu; chưa hỗ trợ quy ước 23:00–00:00 như dự án hiện tại. DonTrungLich giữ cả đơn chờ, chưa thấy tác vụ tự hết hạn trong mã đã rà. Service dịch vụ cho chỉnh ở ChoXuLy/DangXuLy và hủy bằng xóa dòng: cần đối chiếu yêu cầu giữ lịch sử trước khi áp dụng.

Nguồn: [khả dụng](https://github.com/TrTung216/QuanLy_DatSan_UNETI5_TI17A4HN/blob/e33067f9626a99f40ddd7a11e19d5aabd0726f29/QuanLyDatSan_UNETI5_DHTI17A4HN/Services/DatSan/DatSanService.KhaDung.cs), [dịch vụ trong đơn](https://github.com/TrTung216/QuanLy_DatSan_UNETI5_TI17A4HN/blob/e33067f9626a99f40ddd7a11e19d5aabd0726f29/QuanLyDatSan_UNETI5_DHTI17A4HN/Services/QuanLyDichVuTrongDon/DichVuTrongDonService.cs).

## Điểm cần cải thiện của dự án hiện tại

- Chưa có rate limiting đăng nhập; cookie production nên yêu cầu HTTPS.
- Khóa QLSB:LICH_SAN là khóa chung, nhiều sân độc lập phải chờ nhau; không kết luận chịu tải cao khi chưa benchmark.
- Một số form quản trị chưa có RowVersion để báo sửa dữ liệu cũ.
- Lọc lịch trống hiện gọi Calendar từng sân và đọc toàn bộ ứng viên trước phân trang; cần truy vấn theo lô.
- Chuyển test sang cấu trúc xUnit có test case độc lập sẽ dễ báo cáo và đưa vào CI hơn. 74 kiểm tra console không đồng nghĩa 74 test độc lập.
- Database có bảng ví, ghép trận, đối soát không có nghĩa đã triển khai được những nghiệp vụ đó.
- Cần hoàn thiện cụm vận hành/thu tiền, giữ lịch sử và kiểm thử lại cuộc đua thu tiền/hết hạn trước khi so sánh mức hoàn thiện chức năng với họ.

## Khuyến nghị

Tiếp tục mô hình nhiều cơ sở của dự án hiện tại. Học cách tổ chức test, DTO, RowVersion, giới hạn đăng nhập và truy vấn theo lô của repo đối chiếu. Không thay toàn bộ schema/code chỉ vì họ có nhiều màn hình hơn. Trước khi áp dụng luồng tài chính từ repo khác, tái hiện giá cao điểm/cuối tuần và luồng cọc của đơn mới.

