# Cụm 7 — Thanh toán, VietQR SePay, Ví điện tử, Hoàn tiền & Đổi lịch (F01–F05)

Nhánh: `MF-ThanhToanHoanTien`.

## Phạm vi đã triển khai

| Mã | Nội dung | Kết quả |
| --- | --- | --- |
| F01 | Tích hợp VietQR MB Bank + SePay Webhook + Nâng cấp Tiền mặt | Sinh QR động theo `SePayOptions`, API `POST /api/webhook/sepay` xác thực `Authorization: Apikey`, đối soát tài khoản nhận, mã thanh toán, số tiền, chống lặp bằng `UX_Giao_Dich_Cong_Thanh_Toan_sepay_transaction_id`, nhật ký `Nhat_Ky_Webhook`, cập nhật UI realtime qua SignalR (`WatchOrder` / `PaymentChanged`). Tiền mặt hỗ trợ nhập tiền khách đưa, tính tiền thừa và in hóa đơn `/ThanhToan/HoaDon/{id}`. |
| F02 | Ví điện tử, nạp tiền & thanh toán bằng ví | Trang `/Wallet` xem số dư khả dụng/đóng băng và lịch sử bút toán bất biến (`TR_GiaoDichVi_BatBien`). Nạp tiền qua VietQR + Webhook SePay. Thanh toán đơn bằng ví khóa dòng `Vi_Nguoi_Dung WITH (UPDLOCK, HOLDLOCK)` chống chi vượt đồng thời. |
| F03 | Hủy có phí, hoàn tiền & xử lý vi phạm | `RefundService` tính tỷ lệ phạt theo `ChinhSachHuyDoi`/`MucPhiHuyDoi`, tạo `LichSuHuySan` và `HoanTien`, tự động hoàn về ví với đơn online hoặc tạo yêu cầu hoàn thủ công tại quầy cho khách vãng lai; ghi `NhatKyViPham` khi hủy sát giờ (< 2 giờ). |
| F04 | Yêu cầu đổi lịch & bù trừ chênh lệch | `RescheduleService` + `/Bookings/DoiLich/{id}` kiểm tra chính sách `ChoPhepDoiLich`, hoán đổi slot nguyên tử (`DA_DOI` + slot mới có `ChiTietThayTheChoId`), bù chênh lệch/phí qua ví hoặc hoàn phần dư về ví; lỗi slot trùng rollback giữ nguyên slot cũ. |
| F05 | Rút tiền & kiểm thử tài chính | Yêu cầu rút tiền đóng băng số dư (`GIU_TIEN`), màn hình Admin `/Admin/Payouts` duyệt (`RUT`) hoặc từ chối (`GIAI_TOA`) kèm xem nhật ký Webhook SePay. |

## Cập nhật Database

- Migration bổ sung idempotent: `Data/Updates/002_Cluster7_PaymentWebhook.sql` (đã áp dụng vào LocalDB `QuanLySanBai`).
- Bổ sung bảng `dbo.Nhat_Ky_Webhook`, cột `so_tien_khach_dua`/`tien_thua` trên `Thanh_Toan_Don`, và `don_dat_san_id`/`thoi_gian_het_han_qr`/`noi_dung_chuyen_khoan`/`sepay_transaction_id` (unique filtered index) trên `Giao_Dich_Cong_Thanh_Toan`.

## Kiểm chứng

- `dotnet build QuanLySanBai.slnx -p:NuGetAudit=false`: **0 Error(s), 0 Warning(s)**.
- `Cluster4Checks` + `FinanceChecks` (trên disposable SQL Server DB): **192/192 assertions PASS** (bao gồm 17 kịch bản thanh toán/Webhook SePay/lỗi DB/concurrency, nạp & chi tiêu ví song song, hoàn tiền, đổi lịch nguyên tử, rút tiền, HTTP API & SignalR `PaymentChanged`).
- `AccountChecks`: **21/21 checks PASS**.
- Lưu ý: Giao dịch chuyển khoản MB Bank qua SePay ở bước này mới kiểm chứng bằng payload giả lập đúng chuẩn SePay API; cần điền cấu hình thật qua User Secrets để test tiền thật trên SePay.
