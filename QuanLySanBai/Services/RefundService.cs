using Microsoft.EntityFrameworkCore;
using QuanLySanBai.Data;
using QuanLySanBai.Models.Entities;
using QuanLySanBai.ViewModels;

namespace QuanLySanBai.Services;

public class RefundService(
    QuanLySanBaiContext db,
    AccessService access,
    WalletService walletService,
    TimeProvider clock,
    RealtimeUpdates? updates = null)
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime.AddHours(7);

    private async Task<DateTime> ServerNowAsync() =>
        await db.Database.SqlQueryRaw<DateTime>("SELECT CONVERT(datetime2(0),DATEADD(HOUR,7,SYSUTCDATETIME())) AS Value").SingleAsync();

    public async Task<CancelPreviewVM> PreviewCancel(long actorUserId, long orderId)
    {
        var order = await db.DonDatSans.AsNoTracking()
            .Include(d => d.ChiTietDatSans).ThenInclude(c => c.ChinhSachHuyDoi).ThenInclude(p => p.MucPhiHuyDois)
            .Include(d => d.ThanhToanDons)
            .SingleOrDefaultAsync(d => d.Id == orderId)
            ?? throw new BusinessException("Không tìm thấy đơn đặt sân.");

        var isOwner = order.NguoiDungId == actorUserId;
        if (!isOwner && !await access.HasPermission(actorUserId, order.CoSoId, "DOI_HUY_DON"))
            throw new FacilityAccessException();

        var activeSlot = order.ChiTietDatSans.FirstOrDefault(c => c.TrangThaiSlot is not ("DA_DOI" or "DA_HUY"))
            ?? throw new BusinessException("Đơn không còn khung giờ hoạt động để hủy.");

        var now = await ServerNowAsync();
        var start = VietnamTime.Start(activeSlot.NgaySuDung, activeSlot.GioBatDau);
        var hours = Math.Max(0m, decimal.Round((decimal)(start - now).TotalHours, 2));
        var paid = order.ThanhToanDons.Where(p => p.TrangThai == "THANH_CONG").Sum(p => p.SoTien);

        var (percent, penalty, refund) = CalculateCancelAmounts(activeSlot.ChinhSachHuyDoi, hours, paid);

        return new CancelPreviewVM
        {
            OrderId = order.Id,
            MaDatSan = order.MaDatSan,
            TotalPaid = paid,
            HoursBeforeStart = hours,
            PenaltyPercent = percent,
            PenaltyAmount = penalty,
            RefundAmount = refund,
            WillRefundToWallet = order.NguoiDungId.HasValue,
            PolicyName = activeSlot.ChinhSachHuyDoi.TenChinhSach
        };
    }

    public async Task CancelWithRefund(long actorUserId, OrderActionForm form)
    {
        if (string.IsNullOrWhiteSpace(form.Reason) || form.Reason.Length > 1000)
            throw new BusinessException("Vui lòng nhập lý do hủy đơn hợp lệ.");

        if (db.ChangeTracker.HasChanges())
            throw new BusinessException("Có dữ liệu chưa lưu. Hãy tải lại trang.");
        db.ChangeTracker.Clear();

        List<long> affectedCourts = [];
        long? notifyUserId = null;

        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(
            "DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'QLSB:LICH_SAN',@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=10000; IF @r<0 THROW 51305,N'Lịch đang được thao tác, hãy thử lại.',1;");

        var order = await db.DonDatSans
            .Include(d => d.ChiTietDatSans).ThenInclude(c => c.ChinhSachHuyDoi).ThenInclude(p => p.MucPhiHuyDois)
            .Include(d => d.ThanhToanDons)
            .SingleOrDefaultAsync(d => d.Id == form.Id)
            ?? throw new BusinessException("Không tìm thấy đơn đặt sân.");

        var isOwner = order.NguoiDungId == actorUserId;
        if (!isOwner)
        {
            if (!await db.NguoiDungs.AsNoTracking().AnyAsync(u => u.Id == actorUserId && u.TrangThai) ||
                !await access.HasPermission(actorUserId, order.CoSoId, "DOI_HUY_DON"))
                throw new FacilityAccessException();
        }
        else if (!await db.NguoiDungs.AsNoTracking().AnyAsync(u => u.Id == actorUserId && u.TrangThai))
        {
            throw new FacilityAccessException();
        }

        if (!string.IsNullOrEmpty(form.Version) && Convert.ToBase64String(order.RowVersion) != form.Version)
            throw new BusinessException("Đơn đã thay đổi. Hãy tải lại trang trước khi hủy.");

        if (order.TrangThai is not ("CHO_THANH_TOAN" or "DA_XAC_NHAN"))
            throw new BusinessException("Chỉ có thể hủy đơn đang chờ thanh toán hoặc đã xác nhận.");

        var activeSlots = order.ChiTietDatSans.Where(c => c.TrangThaiSlot is not ("DA_DOI" or "DA_HUY")).ToList();
        if (activeSlots.Count == 0)
            throw new BusinessException("Đơn không còn khung giờ hiệu lực.");

        var now = await ServerNowAsync();
        if (activeSlots.Any(c => VietnamTime.Start(c.NgaySuDung, c.GioBatDau) <= now))
            throw new BusinessException("Đã đến hoặc quá giờ sử dụng, không thể hủy đơn.");

        var successPayments = order.ThanhToanDons
            .Where(p => p.TrangThai == "THANH_CONG")
            .OrderBy(p => p.Id)
            .ToList();

        var existingRefunded = await db.HoanTiens
            .Where(h => h.ThanhToanDon.DonDatSanId == order.Id && h.TrangThai != "DA_HUY" && h.TrangThai != "THAT_BAI")
            .SumAsync(h => (decimal?)h.SoTien) ?? 0m;

        var totalPaid = successPayments.Sum(p => p.SoTien);
        var primarySlot = activeSlots[0];
        var hoursBeforeStart = decimal.Round((decimal)(VietnamTime.Start(primarySlot.NgaySuDung, primarySlot.GioBatDau) - now).TotalHours, 2);
        var (penaltyPercent, penaltyAmount, refundAmount) = CalculateCancelAmounts(primarySlot.ChinhSachHuyDoi, hoursBeforeStart, totalPaid);

        if (refundAmount + existingRefunded > totalPaid)
            throw new BusinessException("Tổng tiền hoàn vượt quá số tiền đã thanh toán.");

        var cancelLog = new LichSuHuySan
        {
            ChiTietDatSanId = primarySlot.Id,
            NguoiYeuCauId = actorUserId,
            NguoiXuLyId = actorUserId,
            SoGioTruocTran = hoursBeforeStart,
            SoTienDaPhanBo = totalPaid,
            PhiPhatHuy = penaltyAmount,
            SoTienDuKienHoan = refundAmount,
            LyDoHuy = form.Reason.Trim(),
            NgayHuy = now
        };
        db.LichSuHuySans.Add(cancelLog);
        await db.SaveChangesAsync();

        if (refundAmount > 0 && successPayments.Count > 0)
        {
            ViNguoiDung? lockedWallet = null;
            if (order.NguoiDungId.HasValue)
                lockedWallet = await walletService.LockOrCreateWalletAsync(order.NguoiDungId.Value, now);

            var remainingToRefund = refundAmount;
            foreach (var p in successPayments)
            {
                if (remainingToRefund <= 0) break;
                var alreadyRefundedForPayment = await db.HoanTiens
                    .Where(h => h.ThanhToanDonId == p.Id && h.TrangThai != "DA_HUY" && h.TrangThai != "THAT_BAI")
                    .SumAsync(h => (decimal?)h.SoTien) ?? 0m;
                var maxForPayment = Math.Max(0m, p.SoTien - alreadyRefundedForPayment);
                var portion = Math.Min(remainingToRefund, maxForPayment);
                if (portion <= 0) continue;

                if (lockedWallet is not null)
                {
                    var key = $"REFUND:{order.Id}:{p.Id}:{cancelLog.Id}";
                    var ledger = WalletService.CreateLedgerEntry(
                        lockedWallet,
                        "HOAN",
                        portion,
                        key,
                        $"Hoàn tiền hủy đơn {order.MaDatSan}",
                        now);
                    db.GiaoDichVis.Add(ledger);
                    await db.SaveChangesAsync();

                    db.HoanTiens.Add(new HoanTien
                    {
                        MaHoanTien = $"HT{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}",
                        ThanhToanDonId = p.Id,
                        LichSuHuySanId = cancelLog.Id,
                        GiaoDichViId = ledger.Id,
                        NguoiDuyetId = actorUserId,
                        SoTien = portion,
                        PhuongThuc = "VI",
                        KhoaChongLap = key,
                        TrangThai = "THANH_CONG",
                        LyDo = form.Reason.Trim(),
                        NgayTao = now,
                        NgayHoanTien = now
                    });
                }
                else
                {
                    var key = $"REFUND_MANUAL:{order.Id}:{p.Id}:{cancelLog.Id}";
                    db.HoanTiens.Add(new HoanTien
                    {
                        MaHoanTien = $"HT{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}",
                        ThanhToanDonId = p.Id,
                        LichSuHuySanId = cancelLog.Id,
                        NguoiDuyetId = actorUserId,
                        SoTien = portion,
                        PhuongThuc = p.PhuongThuc == "TIEN_MAT" ? "TIEN_MAT" : "CHUYEN_KHOAN_TAI_QUAY",
                        KhoaChongLap = key,
                        TrangThai = "KHOI_TAO",
                        LyDo = form.Reason.Trim(),
                        NgayTao = now
                    });
                }
                remainingToRefund -= portion;
            }
        }

        // Log violation if last-minute cancellation (< 2 hours) by customer with existing payment/confirmed booking
        if (isOwner && order.NguoiDungId.HasValue && hoursBeforeStart < 2 && order.TrangThai == "DA_XAC_NHAN")
        {
            db.NhatKyViPhams.Add(new NhatKyViPham
            {
                NguoiDungId = order.NguoiDungId.Value,
                DonDatSanId = order.Id,
                LoaiViPham = "HUY_SAT_GIO",
                DiemUyTinBiTru = 5,
                NoiDungViPham = $"Hủy đơn {order.MaDatSan} sát giờ sử dụng ({hoursBeforeStart:0.##} giờ trước trận).",
                NgayGhiNhan = now
            });
        }

        affectedCourts = activeSlots.Select(c => c.SanId).Distinct().ToList();
        foreach (var s in activeSlots)
        {
            s.TrangThaiSlot = "DA_HUY";
            s.NgayCapNhat = now;
        }
        order.TrangThai = "DA_HUY";
        order.TongTienSan = 0m;
        order.TongTienDichVu = 0m;
        order.TienGiamGia = 0m;
        order.TienCocYeuCau = 0m;
        order.TongPhiPhat = penaltyAmount;
        order.NgayCapNhat = now;

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        notifyUserId = order.NguoiDungId;
        if (updates is not null)
        {
            foreach (var courtId in affectedCourts)
                await updates.Calendar(courtId);
            await updates.PaymentChanged(order.Id, notifyUserId);
        }
    }

    public async Task CompleteManualRefund(long staffUserId, long refundId, string? reference)
    {
        if (db.ChangeTracker.HasChanges())
            db.ChangeTracker.Clear();

        await using var tx = await db.Database.BeginTransactionAsync();
        var refund = await db.HoanTiens
            .Include(h => h.ThanhToanDon).ThenInclude(p => p.DonDatSan)
            .SingleOrDefaultAsync(h => h.Id == refundId)
            ?? throw new BusinessException("Không tìm thấy yêu cầu hoàn tiền.");

        if (!await access.HasPermission(staffUserId, refund.ThanhToanDon.DonDatSan.CoSoId, "THU_TIEN"))
            throw new FacilityAccessException();

        if (refund.TrangThai is not ("KHOI_TAO" or "DANG_XU_LY" or "THAT_BAI"))
            throw new BusinessException("Khoản hoàn tiền này đã được xử lý hoặc hủy.");

        if (refund.PhuongThuc is not ("TIEN_MAT" or "CHUYEN_KHOAN_TAI_QUAY"))
            throw new BusinessException("Chỉ hoàn tiền tại quầy mới xác nhận thủ công qua luồng này.");

        var now = await ServerNowAsync();
        refund.TrangThai = "THANH_CONG";
        refund.NguoiDuyetId = staffUserId;
        refund.MaThamChieuNgoai = reference?.Trim();
        refund.NgayHoanTien = now;

        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public static (decimal PenaltyPercent, decimal PenaltyAmount, decimal RefundAmount) CalculateCancelAmounts(
        ChinhSachHuyDoi policy, decimal hoursBeforeStart, decimal totalPaid)
    {
        if (totalPaid <= 0)
            return (0m, 0m, 0m);

        var tier = policy.MucPhiHuyDois
            .OrderBy(m => m.SoGioTuTruocTran)
            .FirstOrDefault(m => hoursBeforeStart >= m.SoGioTuTruocTran &&
                                 (m.SoGioDenTruocTran is null || hoursBeforeStart < m.SoGioDenTruocTran))
            ?? policy.MucPhiHuyDois.OrderBy(m => m.SoGioTuTruocTran).FirstOrDefault();

        var percent = tier?.TiLePhiHuy ?? 0m;
        var penalty = Math.Min(totalPaid, decimal.Round(totalPaid * percent / 100m, 2));
        var refund = Math.Max(0m, totalPaid - penalty);
        return (percent, penalty, refund);
    }
}
