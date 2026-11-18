using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using QuanLySanBai.Data;
using QuanLySanBai.Models.Entities;
using QuanLySanBai.ViewModels;

namespace QuanLySanBai.Services;

public class WebhookService(
    QuanLySanBaiContext db,
    SePayService sePay,
    WalletService walletService,
    RealtimeUpdates? updates = null)
{
    private async Task<DateTime> ServerNowAsync() =>
        await db.Database.SqlQueryRaw<DateTime>("SELECT CONVERT(datetime2(0),DATEADD(HOUR,7,SYSUTCDATETIME())) AS Value").SingleAsync();

    public async Task<WebhookProcessResult> ProcessSePayWebhook(string rawBody, string? authorizationHeader)
    {
        var now = await ServerNowAsync();

        if (!sePay.VerifyAuthorization(authorizationHeader))
        {
            var authNote = string.IsNullOrWhiteSpace(authorizationHeader)
                ? "Thiếu header Authorization."
                : $"Sai Apikey (nhận {authorizationHeader.Length} ký tự).";
            await SaveStandaloneLogAsync("SEPAY", null, null, null, null, null, null, null, "SAI_CHU_KY", authNote, rawBody, now);
            return new WebhookProcessResult
            {
                StatusCode = 401,
                Success = false,
                ProcessingStatus = "SAI_CHU_KY",
                Message = "Unauthorized"
            };
        }

        var payload = sePay.ParsePayload(rawBody);
        if (payload is null || payload.Id <= 0 || payload.TransferAmount <= 0)
        {
            await SaveStandaloneLogAsync("SEPAY", payload?.Id, null, null, payload?.TransferType, payload?.AccountNumber, payload?.TransferAmount, payload?.Content, "LOI_XU_LY", "Payload JSON không hợp lệ.", rawBody, now);
            return new WebhookProcessResult
            {
                StatusCode = 400,
                Success = false,
                ProcessingStatus = "LOI_XU_LY",
                Message = "Invalid payload"
            };
        }

        var extractedCode = sePay.ExtractPaymentCode(payload);

        if (!string.Equals(payload.TransferType, "in", StringComparison.OrdinalIgnoreCase))
        {
            await SaveStandaloneLogAsync("SEPAY", payload.Id, extractedCode, null, payload.TransferType, payload.AccountNumber, payload.TransferAmount, payload.Content, "BO_QUA_TIEN_RA", "Bỏ qua giao dịch không phải tiền vào.", rawBody, now);
            return new WebhookProcessResult
            {
                StatusCode = 200,
                Success = true,
                ProcessingStatus = "BO_QUA_TIEN_RA",
                Message = "Ignored outgoing transfer"
            };
        }

        if (!sePay.AccountMatches(payload.AccountNumber, payload.SubAccount))
        {
            await SaveStandaloneLogAsync("SEPAY", payload.Id, extractedCode, null, payload.TransferType, payload.AccountNumber, payload.TransferAmount, payload.Content, "SAI_TAI_KHOAN", "Số tài khoản nhận không khớp cấu hình.", rawBody, now);
            return new WebhookProcessResult
            {
                StatusCode = 200,
                Success = true,
                ProcessingStatus = "SAI_TAI_KHOAN",
                Message = "Account mismatch"
            };
        }

        if (db.ChangeTracker.HasChanges())
            db.ChangeTracker.Clear();

        long? notifyOrderId = null;
        long? notifyUserId = null;
        long? notifyCourtId = null;

        try
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            await db.Database.ExecuteSqlRawAsync(
                "DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'QLSB:LICH_SAN',@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=10000; IF @r<0 THROW 51305,N'Hệ thống đang bận, vui lòng thử lại.',1;");

            var sepayIdStr = payload.Id.ToString();
            var existingBySepay = await db.GiaoDichCongThanhToans
                .SingleOrDefaultAsync(g => g.SepayTransactionId == payload.Id || (g.CongThanhToan == "SEPAY" && g.MaGiaoDichNganHang == sepayIdStr));

            if (existingBySepay is not null)
            {
                db.NhatKyWebhooks.Add(BuildLog(payload, extractedCode, existingBySepay.Id, "TRUNG_LAP", "Giao dịch SePay đã được xử lý trước đó.", rawBody, now));
                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return new WebhookProcessResult
                {
                    StatusCode = 200,
                    Success = true,
                    ProcessingStatus = "TRUNG_LAP",
                    Message = "Duplicate transaction ignored",
                    GatewayTxId = existingBySepay.Id
                };
            }

            if (string.IsNullOrEmpty(extractedCode))
            {
                db.NhatKyWebhooks.Add(BuildLog(payload, null, null, "KHONG_TIM_THAY_MA", "Không tìm thấy mã thanh toán QSB trong nội dung chuyển khoản.", rawBody, now));
                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return new WebhookProcessResult
                {
                    StatusCode = 200,
                    Success = true,
                    ProcessingStatus = "KHONG_TIM_THAY_MA",
                    Message = "Payment code not found"
                };
            }

            var gatewayTx = await db.GiaoDichCongThanhToans
                .FromSqlInterpolated($"SELECT * FROM dbo.Giao_Dich_Cong_Thanh_Toan WITH (UPDLOCK, HOLDLOCK) WHERE ma_don_hang_cong = {extractedCode}")
                .SingleOrDefaultAsync();

            if (gatewayTx is null)
            {
                db.NhatKyWebhooks.Add(BuildLog(payload, extractedCode, null, "KHONG_TIM_THAY_MA", "Không tìm thấy yêu cầu thanh toán khớp mã.", rawBody, now));
                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return new WebhookProcessResult
                {
                    StatusCode = 200,
                    Success = true,
                    ProcessingStatus = "KHONG_TIM_THAY_MA",
                    Message = "Gateway order not found"
                };
            }

            if (gatewayTx.TrangThai == "THANH_CONG")
            {
                db.NhatKyWebhooks.Add(BuildLog(payload, extractedCode, gatewayTx.Id, "TRUNG_LAP", "Yêu cầu thanh toán này đã thành công trước đó.", rawBody, now));
                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return new WebhookProcessResult
                {
                    StatusCode = 200,
                    Success = true,
                    ProcessingStatus = "TRUNG_LAP",
                    Message = "Already succeeded",
                    GatewayTxId = gatewayTx.Id
                };
            }

            if (payload.TransferAmount < gatewayTx.SoTien)
            {
                db.NhatKyWebhooks.Add(BuildLog(payload, extractedCode, gatewayTx.Id, "THIEU_TIEN", $"Chuyển thiếu: nhận {payload.TransferAmount:N0}, yêu cầu {gatewayTx.SoTien:N0}.", rawBody, now));
                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return new WebhookProcessResult
                {
                    StatusCode = 200,
                    Success = true,
                    ProcessingStatus = "THIEU_TIEN",
                    Message = "Insufficient transfer amount",
                    GatewayTxId = gatewayTx.Id
                };
            }

            // Case 1: Wallet Deposit
            var deposit = await db.NapTienVis.SingleOrDefaultAsync(n => n.GiaoDichCongId == gatewayTx.Id);
            if (deposit is not null)
            {
                if (deposit.TrangThai == "THANH_CONG")
                {
                    db.NhatKyWebhooks.Add(BuildLog(payload, extractedCode, gatewayTx.Id, "TRUNG_LAP", "Giao dịch nạp ví đã hoàn tất trước đó.", rawBody, now));
                    await db.SaveChangesAsync();
                    await tx.CommitAsync();
                    return new WebhookProcessResult { StatusCode = 200, Success = true, ProcessingStatus = "TRUNG_LAP", Message = "Already deposited", GatewayTxId = gatewayTx.Id };
                }

                var lockedWallet = await walletService.LockWalletByIdAsync(deposit.ViId);
                var ledgerEntry = WalletService.CreateLedgerEntry(
                    lockedWallet,
                    "NAP",
                    payload.TransferAmount,
                    $"SEPAY_DEP:{payload.Id}",
                    $"Nạp ví qua SePay ({extractedCode})",
                    now);
                db.GiaoDichVis.Add(ledgerEntry);
                await db.SaveChangesAsync();

                deposit.TrangThai = "THANH_CONG";
                deposit.GiaoDichViId = ledgerEntry.Id;
                deposit.NgayHoanTat = now;

                MarkGatewaySuccess(gatewayTx, payload, rawBody, now);
                var status = payload.TransferAmount > gatewayTx.SoTien ? "THUA_TIEN_DA_XU_LY" : "HOP_LE_DA_XU_LY";
                db.NhatKyWebhooks.Add(BuildLog(payload, extractedCode, gatewayTx.Id, status, "Nạp tiền vào ví thành công.", rawBody, now));
                await db.SaveChangesAsync();
                await tx.CommitAsync();

                notifyUserId = lockedWallet.NguoiDungId;
                if (updates is not null)
                    await updates.PaymentChanged(null, notifyUserId);

                return new WebhookProcessResult
                {
                    StatusCode = 200,
                    Success = true,
                    ProcessingStatus = status,
                    Message = "Wallet deposit confirmed",
                    UserId = notifyUserId,
                    GatewayTxId = gatewayTx.Id
                };
            }

            // Case 2: Booking Payment
            if (!gatewayTx.DonDatSanId.HasValue)
            {
                db.NhatKyWebhooks.Add(BuildLog(payload, extractedCode, gatewayTx.Id, "LOI_XU_LY", "Giao dịch cổng không gắn đơn đặt sân hay nạp ví.", rawBody, now));
                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return new WebhookProcessResult { StatusCode = 200, Success = true, ProcessingStatus = "LOI_XU_LY", Message = "Orphan gateway transaction", GatewayTxId = gatewayTx.Id };
            }

            var order = await db.DonDatSans
                .FromSqlInterpolated($"SELECT * FROM dbo.Don_Dat_San WITH (UPDLOCK, HOLDLOCK) WHERE id = {gatewayTx.DonDatSanId.Value}")
                .SingleOrDefaultAsync();

            if (order is null)
            {
                db.NhatKyWebhooks.Add(BuildLog(payload, extractedCode, gatewayTx.Id, "LOI_XU_LY", "Không tìm thấy đơn đặt sân.", rawBody, now));
                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return new WebhookProcessResult { StatusCode = 200, Success = true, ProcessingStatus = "LOI_XU_LY", Message = "Order not found" };
            }

            await db.Entry(order).Collection(d => d.ChiTietDatSans).LoadAsync();

            if (order.TrangThai == "DA_HUY")
            {
                gatewayTx.TrangThai = "DA_HUY";
                gatewayTx.SepayTransactionId = payload.Id;
                gatewayTx.MaGiaoDichNganHang = sepayIdStr;
                gatewayTx.MaSuKienCong = payload.ReferenceCode;
                gatewayTx.DuLieuWebhook = rawBody;
                gatewayTx.NgayCapNhat = now;
                db.NhatKyWebhooks.Add(BuildLog(payload, extractedCode, gatewayTx.Id, "DON_DA_HUY", "Đơn đã bị hủy trước khi nhận webhook; không tự động xác nhận.", rawBody, now));
                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return new WebhookProcessResult { StatusCode = 200, Success = true, ProcessingStatus = "DON_DA_HUY", Message = "Order already cancelled", OrderId = order.Id, GatewayTxId = gatewayTx.Id };
            }

            var paid = await OrderFinance.NetPaidAsync(db, order.Id);

            var isExpiredOrder = order.TrangThai == "HET_HAN" ||
                (order.TrangThai == "CHO_THANH_TOAN" && order.ThoiGianHetHanGiuCho.HasValue && order.ThoiGianHetHanGiuCho.Value <= now);

            var activeOrExpiredSlots = order.ChiTietDatSans
                .Where(c => c.TrangThaiSlot is not ("DA_DOI" or "DA_HUY"))
                .ToList();

            if (isExpiredOrder)
            {
                if (activeOrExpiredSlots.Count == 0)
                {
                    gatewayTx.TrangThai = "THAT_BAI";
                    gatewayTx.SepayTransactionId = payload.Id;
                    gatewayTx.MaGiaoDichNganHang = sepayIdStr;
                    gatewayTx.DuLieuWebhook = rawBody;
                    gatewayTx.NgayCapNhat = now;
                    db.NhatKyWebhooks.Add(BuildLog(payload, extractedCode, gatewayTx.Id, "LOI_XU_LY", "Đơn hết hạn không còn chi tiết slot.", rawBody, now));
                    await db.SaveChangesAsync();
                    await tx.CommitAsync();
                    return new WebhookProcessResult { StatusCode = 200, Success = true, ProcessingStatus = "LOI_XU_LY", Message = "Expired order has no slots", OrderId = order.Id };
                }

                // Check if slots are still free before late confirmation
                foreach (var slot in activeOrExpiredSlots)
                {
                    var slotStart = VietnamTime.Start(slot.NgaySuDung, slot.GioBatDau);
                    var slotEnd = VietnamTime.End(slot.NgaySuDung, slot.GioKetThuc);

                    var otherBookings = await db.ChiTietDatSans.Include(c => c.DonDatSan)
                        .Where(c => c.DonDatSanId != order.Id && c.SanId == slot.SanId && c.NgaySuDung == slot.NgaySuDung &&
                            (c.TrangThaiSlot == "DA_XAC_NHAN" || c.TrangThaiSlot == "DA_CHECK_IN" || c.TrangThaiSlot == "HOAN_THANH" || c.TrangThaiSlot == "VANG_MAT" ||
                            (c.TrangThaiSlot == "GIU_CHO" && c.DonDatSan.ThoiGianHetHanGiuCho > now)))
                        .ToListAsync();

                    var hasOverlap = otherBookings.Any(b => VietnamTime.Start(b.NgaySuDung, b.GioBatDau) < slotEnd && slotStart < VietnamTime.End(b.NgaySuDung, b.GioKetThuc));
                    var hasLock = await db.LichKhoaSans.AnyAsync(l => l.SanId == slot.SanId && l.TrangThai == "DANG_AP_DUNG" && l.BatDauLuc < slotEnd && slotStart < l.KetThucLuc);

                    if (hasOverlap || hasLock)
                    {
                        gatewayTx.TrangThai = "THAT_BAI";
                        gatewayTx.SepayTransactionId = payload.Id;
                        gatewayTx.MaGiaoDichNganHang = sepayIdStr;
                        gatewayTx.DuLieuWebhook = rawBody;
                        gatewayTx.NgayCapNhat = now;
                        db.NhatKyWebhooks.Add(BuildLog(payload, extractedCode, gatewayTx.Id, "LOI_XU_LY", "Webhook đến muộn và khung giờ đã có người đặt/khóa; cần hoàn tiền.", rawBody, now));
                        await db.SaveChangesAsync();
                        await tx.CommitAsync();
                        return new WebhookProcessResult { StatusCode = 200, Success = true, ProcessingStatus = "LOI_XU_LY", Message = "Late webhook slot conflict", OrderId = order.Id, GatewayTxId = gatewayTx.Id };
                    }
                }

                // Slot is still free: restore order prices if zeroed by HoldExpiryWorker
                if (order.TrangThai == "HET_HAN" && order.TongTienSan == 0)
                {
                    order.TongTienSan = activeOrExpiredSlots.Sum(s => s.GiaThue);
                    var bangGia = await db.BangGia.AsNoTracking().SingleOrDefaultAsync(b => b.Id == activeOrExpiredSlots[0].BangGiaId);
                    var maxDeposit = Math.Max(0m, order.TongTienSan + order.TongTienDichVu + order.TongPhiPhat - order.TienGiamGia);
                    order.TienCocYeuCau = Math.Min(maxDeposit, bangGia?.TienCocToiThieu ?? Math.Min(gatewayTx.SoTien, maxDeposit));
                }
            }

            var totalDue = Math.Max(0m, order.TongTienSan + order.TongTienDichVu + order.TongPhiPhat - order.TienGiamGia);
            var remaining = Math.Max(0m, totalDue - paid);

            if (order.TrangThai == "HOAN_THANH" || remaining <= 0)
            {
                gatewayTx.TrangThai = "DA_HUY";
                gatewayTx.SepayTransactionId = payload.Id;
                gatewayTx.MaGiaoDichNganHang = sepayIdStr;
                gatewayTx.DuLieuWebhook = rawBody;
                gatewayTx.NgayCapNhat = now;
                db.NhatKyWebhooks.Add(BuildLog(payload, extractedCode, gatewayTx.Id, "DON_DA_THANH_TOAN", "Đơn đã thanh toán đủ trước đó; không xác nhận hai lần.", rawBody, now));
                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return new WebhookProcessResult { StatusCode = 200, Success = true, ProcessingStatus = "DON_DA_THANH_TOAN", Message = "Order already paid", OrderId = order.Id, GatewayTxId = gatewayTx.Id };
            }

            var appliedAmount = Math.Min(payload.TransferAmount, remaining);
            var excessAmount = payload.TransferAmount - appliedAmount;

            var mucDich = (order.TrangThai is "CHO_THANH_TOAN" or "HET_HAN") && paid == 0 && appliedAmount < remaining
                ? "COC"
                : (paid > 0 ? "BO_SUNG" : "THANH_TOAN");

            MarkGatewaySuccess(gatewayTx, payload, rawBody, now);

            var payment = new ThanhToanDon
            {
                MaThanhToan = Guid.NewGuid().ToString("N"),
                DonDatSanId = order.Id,
                NguoiThanhToanId = order.NguoiDungId,
                GiaoDichCongId = gatewayTx.Id,
                PhuongThuc = "CONG_THANH_TOAN",
                MucDich = mucDich,
                SoTien = appliedAmount,
                MaThamChieuNgoai = payload.ReferenceCode ?? sepayIdStr,
                KhoaChongLap = $"SEPAY:{payload.Id}",
                TrangThai = "THANH_CONG",
                NgayTao = now,
                NgayThanhToan = now
            };
            db.ThanhToanDons.Add(payment);

            if (order.TrangThai is "CHO_THANH_TOAN" or "HET_HAN")
            {
                if (paid + appliedAmount >= order.TienCocYeuCau)
                {
                    order.TrangThai = "DA_XAC_NHAN";
                    order.ThoiGianHetHanGiuCho = null;
                    order.NgayCapNhat = now;
                    foreach (var s in activeOrExpiredSlots)
                    {
                        s.TrangThaiSlot = "DA_XAC_NHAN";
                        s.NgayCapNhat = now;
                    }
                    notifyCourtId = activeOrExpiredSlots.FirstOrDefault()?.SanId;
                }
            }
            else
            {
                order.NgayCapNhat = now;
            }

            if (excessAmount > 0 && order.NguoiDungId.HasValue)
            {
                await db.SaveChangesAsync();
                var lockedWallet = await walletService.LockOrCreateWalletAsync(order.NguoiDungId.Value, now);
                var excessLedger = WalletService.CreateLedgerEntry(
                    lockedWallet,
                    "NAP",
                    excessAmount,
                    $"SEPAY_EXCESS:{payload.Id}",
                    $"Tiền chuyển thừa đơn {order.MaDatSan} tự động cộng ví",
                    now);
                db.GiaoDichVis.Add(excessLedger);
            }

            var logStatus = excessAmount > 0 ? "THUA_TIEN_DA_XU_LY" : "HOP_LE_DA_XU_LY";
            var logNote = isExpiredOrder
                ? "Webhook đến muộn (QR/giữ chỗ hết hạn), slot vẫn trống nên đã xác nhận đơn."
                : (excessAmount > 0 ? $"Chuyển thừa {excessAmount:N0} đ; phần hợp lệ đã ghi nhận." : "Thanh toán đơn thành công.");

            db.NhatKyWebhooks.Add(BuildLog(payload, extractedCode, gatewayTx.Id, logStatus, logNote, rawBody, now));
            await db.SaveChangesAsync();
            await tx.CommitAsync();

            notifyOrderId = order.Id;
            notifyUserId = order.NguoiDungId;

            if (updates is not null)
            {
                if (notifyCourtId.HasValue)
                    await updates.Calendar(notifyCourtId.Value);
                await updates.PaymentChanged(notifyOrderId, notifyUserId);
            }

            return new WebhookProcessResult
            {
                StatusCode = 200,
                Success = true,
                ProcessingStatus = logStatus,
                Message = "Order payment confirmed",
                OrderId = notifyOrderId,
                UserId = notifyUserId,
                CourtId = notifyCourtId,
                GatewayTxId = gatewayTx.Id
            };
        }
        catch (Exception ex) when (ex is DbUpdateException or SqlException && IsUniqueViolation(ex))
        {
            db.ChangeTracker.Clear();
            await SaveStandaloneLogAsync("SEPAY", payload.Id, extractedCode, null, payload.TransferType, payload.AccountNumber, payload.TransferAmount, payload.Content, "TRUNG_LAP", "Chặn xử lý trùng bởi ràng buộc duy nhất SQL.", rawBody, now);
            return new WebhookProcessResult
            {
                StatusCode = 200,
                Success = true,
                ProcessingStatus = "TRUNG_LAP",
                Message = "Duplicate prevented by database constraint"
            };
        }
    }

    private static void MarkGatewaySuccess(GiaoDichCongThanhToan gw, SePayWebhookPayload payload, string rawBody, DateTime now)
    {
        gw.TrangThai = "THANH_CONG";
        gw.SepayTransactionId = payload.Id;
        gw.MaGiaoDichNganHang = payload.Id.ToString();
        gw.MaSuKienCong = payload.ReferenceCode;
        gw.DuLieuWebhook = rawBody;
        gw.NgayThanhToan = now;
        gw.NgayCapNhat = now;
    }

    private static NhatKyWebhook BuildLog(
        SePayWebhookPayload? payload,
        string? code,
        long? gatewayTxId,
        string status,
        string note,
        string rawBody,
        DateTime now) => new()
    {
        CongThanhToan = "SEPAY",
        SepayTransactionId = payload?.Id,
        MaThanhToan = code,
        GiaoDichCongId = gatewayTxId,
        LoaiChuyenTien = payload?.TransferType,
        SoTaiKhoanNhan = payload?.AccountNumber,
        SoTien = payload?.TransferAmount,
        NoiDung = payload?.Content,
        TrangThaiXuLy = status,
        GhiChu = note.Length > 500 ? note[..500] : note,
        DuLieuGoc = TruncateRawBody(rawBody),
        NgayTao = now
    };

    private async Task SaveStandaloneLogAsync(
        string gateway, long? sepayId, string? code, long? gatewayTxId, string? transferType,
        string? accountNumber, decimal? amount, string? content, string status, string note, string rawBody, DateTime now)
    {
        try
        {
            db.ChangeTracker.Clear();
            db.NhatKyWebhooks.Add(new NhatKyWebhook
            {
                CongThanhToan = gateway,
                SepayTransactionId = sepayId,
                MaThanhToan = code,
                GiaoDichCongId = gatewayTxId,
                LoaiChuyenTien = transferType,
                SoTaiKhoanNhan = accountNumber,
                SoTien = amount,
                NoiDung = content,
                TrangThaiXuLy = status,
                GhiChu = note.Length > 500 ? note[..500] : note,
                DuLieuGoc = TruncateRawBody(rawBody),
                NgayTao = now
            });
            await db.SaveChangesAsync();
        }
        catch
        {
            // Logging webhook failures must never mask primary response
        }
    }

    private static string TruncateRawBody(string rawBody) =>
        rawBody.Length > 4000 ? rawBody[..4000] : rawBody;

    private static bool IsUniqueViolation(Exception ex)
    {
        var sql = ex as SqlException ?? (ex as DbUpdateException)?.InnerException as SqlException;
        return sql is not null && (sql.Number == 2601 || sql.Number == 2627);
    }
}
