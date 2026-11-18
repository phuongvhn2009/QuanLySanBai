using Microsoft.EntityFrameworkCore;
using QuanLySanBai.Data;
using QuanLySanBai.Models.Entities;
using QuanLySanBai.ViewModels;

namespace QuanLySanBai.Services;

public class RescheduleService(
    QuanLySanBaiContext db,
    AccessService access,
    WalletService walletService,
    RealtimeUpdates? updates = null)
{
    private async Task<DateTime> ServerNowAsync() =>
        await db.Database.SqlQueryRaw<DateTime>("SELECT CONVERT(datetime2(0),DATEADD(HOUR,7,SYSUTCDATETIME())) AS Value").SingleAsync();

    public async Task<ReschedulePreviewVM> GetRescheduleOptions(long actorUserId, long orderId, DateOnly? targetDate = null)
    {
        var order = await db.DonDatSans.AsNoTracking()
            .Include(d => d.CoSo)
            .Include(d => d.ChiTietDatSans).ThenInclude(c => c.San)
            .Include(d => d.ChiTietDatSans).ThenInclude(c => c.ChinhSachHuyDoi).ThenInclude(p => p.MucPhiHuyDois)
            .Include(d => d.ThanhToanDons)
            .SingleOrDefaultAsync(d => d.Id == orderId)
            ?? throw new BusinessException("Không tìm thấy đơn đặt sân.");

        var isOwner = order.NguoiDungId == actorUserId;
        if (!isOwner && !await access.HasPermission(actorUserId, order.CoSoId, "DOI_HUY_DON"))
            throw new FacilityAccessException();

        var currentSlot = order.ChiTietDatSans.SingleOrDefault(c => c.TrangThaiSlot is not ("DA_DOI" or "DA_HUY"))
            ?? throw new BusinessException("Đơn không còn khung giờ có thể đổi.");

        var now = await ServerNowAsync();
        var today = DateOnly.FromDateTime(now);
        var selectedDate = targetDate ?? currentSlot.NgaySuDung;
        if (selectedDate < today) selectedDate = today;

        var paid = order.ThanhToanDons.Where(p => p.TrangThai == "THANH_CONG").Sum(p => p.SoTien);
        var hoursBeforeStart = Math.Max(0m, decimal.Round((decimal)(VietnamTime.Start(currentSlot.NgaySuDung, currentSlot.GioBatDau) - now).TotalHours, 2));

        var allowChange = order.TrangThai is ("CHO_THANH_TOAN" or "DA_XAC_NHAN")
            && currentSlot.ChinhSachHuyDoi.ChoPhepDoiLich
            && VietnamTime.Start(currentSlot.NgaySuDung, currentSlot.GioBatDau) > now;

        string? disallowReason = null;
        if (!currentSlot.ChinhSachHuyDoi.ChoPhepDoiLich)
            disallowReason = "Chính sách của đơn không cho phép đổi lịch.";
        else if (order.TrangThai is not ("CHO_THANH_TOAN" or "DA_XAC_NHAN"))
            disallowReason = "Chỉ có thể đổi lịch đơn đang chờ thanh toán hoặc đã xác nhận.";
        else if (VietnamTime.Start(currentSlot.NgaySuDung, currentSlot.GioBatDau) <= now)
            disallowReason = "Đã đến giờ sử dụng, không thể đổi lịch.";

        var tier = currentSlot.ChinhSachHuyDoi.MucPhiHuyDois
            .OrderBy(m => m.SoGioTuTruocTran)
            .FirstOrDefault(m => hoursBeforeStart >= m.SoGioTuTruocTran && (m.SoGioDenTruocTran is null || hoursBeforeStart < m.SoGioDenTruocTran))
            ?? currentSlot.ChinhSachHuyDoi.MucPhiHuyDois.OrderBy(m => m.SoGioTuTruocTran).FirstOrDefault();

        var feePercent = tier?.TiLePhiDoi ?? 0m;
        var feeAmount = decimal.Round(currentSlot.GiaThue * feePercent / 100m, 2);

        var options = new List<RescheduleSlotOption>();
        if (allowChange)
        {
            var weekday = VietnamTime.Weekday(selectedDate);
            var hours = await db.LichHoatDongCoSos.AsNoTracking()
                .SingleOrDefaultAsync(h => h.CoSoId == order.CoSoId && h.ThuTrongTuan == weekday);

            if (hours?.NghiCaNgay != true)
            {
                var courts = await db.Sans.AsNoTracking()
                    .Where(s => s.CoSoId == order.CoSoId && s.DangHoatDong && !s.DangBaoTri && s.LoaiSan.MonTheThao.TrangThai)
                    .OrderBy(s => s.TenSan)
                    .ToListAsync();
                var slots = await db.KhungGioMacDinhs.AsNoTracking()
                    .Where(k => k.CoSoId == order.CoSoId)
                    .OrderBy(k => k.GioBatDau)
                    .ToListAsync();
                var prices = await db.BangGia.AsNoTracking()
                    .Where(p => p.San.CoSoId == order.CoSoId && p.DangApDung && p.ThuTrongTuan == weekday &&
                        p.NgayHieuLucTu <= selectedDate && (p.NgayHieuLucDen == null || p.NgayHieuLucDen >= selectedDate))
                    .ToListAsync();
                var bookings = await db.ChiTietDatSans.AsNoTracking()
                    .Include(c => c.DonDatSan)
                    .Where(c => c.San.CoSoId == order.CoSoId && c.NgaySuDung == selectedDate &&
                        (c.TrangThaiSlot == "DA_XAC_NHAN" || c.TrangThaiSlot == "DA_CHECK_IN" || c.TrangThaiSlot == "HOAN_THANH" || c.TrangThaiSlot == "VANG_MAT" ||
                        (c.TrangThaiSlot == "GIU_CHO" && c.DonDatSan.ThoiGianHetHanGiuCho > now)))
                    .ToListAsync();
                var locks = await db.LichKhoaSans.AsNoTracking()
                    .Where(l => l.San.CoSoId == order.CoSoId && l.TrangThai == "DANG_AP_DUNG" &&
                        l.BatDauLuc < selectedDate.AddDays(1).ToDateTime(TimeOnly.MinValue) &&
                        l.KetThucLuc > selectedDate.ToDateTime(TimeOnly.MinValue))
                    .ToListAsync();

                var openTime = hours?.GioMoCua ?? order.CoSo.GioMoCua;
                var closeTime = VietnamTime.End(selectedDate, hours?.GioDongCua ?? order.CoSo.GioDongCua);

                foreach (var court in courts)
                {
                    foreach (var slot in slots)
                    {
                        if (court.Id == currentSlot.SanId && slot.Id == currentSlot.KhungGioId && selectedDate == currentSlot.NgaySuDung)
                            continue;

                        var start = VietnamTime.Start(selectedDate, slot.GioBatDau);
                        var end = VietnamTime.End(selectedDate, slot.GioKetThuc);
                        if (start <= now || slot.GioBatDau < openTime || end > closeTime)
                            continue;

                        var price = prices.SingleOrDefault(p => p.SanId == court.Id && p.KhungGioId == slot.Id);
                        if (price is null) continue;

                        var conflict = bookings.Any(b => b.SanId == court.Id && VietnamTime.Start(selectedDate, b.GioBatDau) < end && start < VietnamTime.End(selectedDate, b.GioKetThuc))
                            || locks.Any(l => l.SanId == court.Id && l.BatDauLuc < end && start < l.KetThucLuc);
                        if (conflict) continue;

                        options.Add(new RescheduleSlotOption
                        {
                            CourtId = court.Id,
                            CourtName = court.TenSan,
                            SlotId = slot.Id,
                            Start = slot.GioBatDau,
                            End = slot.GioKetThuc,
                            NewPrice = price.GiaThue
                        });
                    }
                }
            }
        }

        decimal walletBalance = 0m;
        if (order.NguoiDungId.HasValue)
        {
            var w = await walletService.GetOrCreateWallet(order.NguoiDungId.Value);
            walletBalance = w.SoDuKhaDung;
        }

        return new ReschedulePreviewVM
        {
            Order = order,
            CurrentSlot = currentSlot,
            SelectedDate = selectedDate,
            AvailableSlots = options,
            ChangeFeePercent = feePercent,
            ChangeFeeAmount = feeAmount,
            PaidAmount = paid,
            AllowChange = allowChange,
            DisallowReason = disallowReason,
            WalletBalance = walletBalance
        };
    }

    public async Task ExecuteReschedule(long actorUserId, RescheduleForm form)
    {
        if (db.ChangeTracker.HasChanges())
            throw new BusinessException("Có dữ liệu chưa lưu. Hãy tải lại trang.");
        db.ChangeTracker.Clear();

        long oldCourtId = 0;
        long? notifyUserId = null;

        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(
            "DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'QLSB:LICH_SAN',@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=10000; IF @r<0 THROW 51305,N'Lịch đang được thao tác, hãy thử lại.',1;");

        var order = await db.DonDatSans
            .Include(d => d.CoSo)
            .Include(d => d.ChiTietDatSans).ThenInclude(c => c.ChinhSachHuyDoi).ThenInclude(p => p.MucPhiHuyDois)
            .Include(d => d.ThanhToanDons)
            .SingleOrDefaultAsync(d => d.Id == form.OrderId)
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
            throw new BusinessException("Đơn đã thay đổi. Hãy tải lại trang trước khi đổi lịch.");

        if (order.TrangThai is not ("CHO_THANH_TOAN" or "DA_XAC_NHAN"))
            throw new BusinessException("Chỉ có thể đổi lịch đơn đang chờ thanh toán hoặc đã xác nhận.");

        var oldSlot = order.ChiTietDatSans.SingleOrDefault(c => c.TrangThaiSlot is not ("DA_DOI" or "DA_HUY"))
            ?? throw new BusinessException("Đơn không còn khung giờ hợp lệ để đổi.");

        if (!oldSlot.ChinhSachHuyDoi.ChoPhepDoiLich)
            throw new BusinessException("Chính sách của đơn không cho phép đổi lịch.");

        var now = await ServerNowAsync();
        if (VietnamTime.Start(oldSlot.NgaySuDung, oldSlot.GioBatDau) <= now)
            throw new BusinessException("Đã đến giờ sử dụng, không thể đổi lịch.");

        if (form.NewCourtId == oldSlot.SanId && form.NewSlotId == oldSlot.KhungGioId && form.NewDate == oldSlot.NgaySuDung)
            throw new BusinessException("Vui lòng chọn khung giờ hoặc sân khác với lịch hiện tại.");

        var newCourt = await db.Sans.Include(s => s.LoaiSan).ThenInclude(t => t.MonTheThao)
            .SingleOrDefaultAsync(s => s.Id == form.NewCourtId && s.CoSoId == order.CoSoId && s.DangHoatDong && !s.DangBaoTri && s.LoaiSan.MonTheThao.TrangThai)
            ?? throw new BusinessException("Sân mới không khả dụng hoặc không thuộc cơ sở.");

        var newSlotDef = await db.KhungGioMacDinhs
            .SingleOrDefaultAsync(k => k.Id == form.NewSlotId && k.CoSoId == order.CoSoId)
            ?? throw new BusinessException("Khung giờ mới không hợp lệ.");

        var newStart = VietnamTime.Start(form.NewDate, newSlotDef.GioBatDau);
        var newEnd = VietnamTime.End(form.NewDate, newSlotDef.GioKetThuc);
        if (newStart <= now)
            throw new BusinessException("Không thể đổi sang thời điểm trong quá khứ.");

        var weekday = VietnamTime.Weekday(form.NewDate);
        var hours = await db.LichHoatDongCoSos.SingleOrDefaultAsync(h => h.CoSoId == order.CoSoId && h.ThuTrongTuan == weekday);
        if (hours?.NghiCaNgay == true)
            throw new BusinessException("Cơ sở nghỉ vào ngày đã chọn.");
        var openTime = hours?.GioMoCua ?? order.CoSo.GioMoCua;
        var closeTime = VietnamTime.End(form.NewDate, hours?.GioDongCua ?? order.CoSo.GioDongCua);
        if (newSlotDef.GioBatDau < openTime || newEnd > closeTime)
            throw new BusinessException("Khung giờ mới nằm ngoài giờ hoạt động của cơ sở.");

        var newPrice = await db.BangGia.SingleOrDefaultAsync(p =>
            p.SanId == newCourt.Id && p.KhungGioId == newSlotDef.Id && p.ThuTrongTuan == weekday && p.DangApDung &&
            p.NgayHieuLucTu <= form.NewDate && (p.NgayHieuLucDen == null || p.NgayHieuLucDen >= form.NewDate))
            ?? throw new BusinessException("Chưa có bảng giá cho khung giờ mới.");

        var sameDayBookings = await db.ChiTietDatSans.Include(c => c.DonDatSan)
            .Where(c => c.Id != oldSlot.Id && c.SanId == newCourt.Id && c.NgaySuDung == form.NewDate &&
                (c.TrangThaiSlot == "DA_XAC_NHAN" || c.TrangThaiSlot == "DA_CHECK_IN" || c.TrangThaiSlot == "HOAN_THANH" || c.TrangThaiSlot == "VANG_MAT" ||
                (c.TrangThaiSlot == "GIU_CHO" && c.DonDatSan.ThoiGianHetHanGiuCho > now)))
            .ToListAsync();

        if (sameDayBookings.Any(b => VietnamTime.Start(form.NewDate, b.GioBatDau) < newEnd && newStart < VietnamTime.End(form.NewDate, b.GioKetThuc)))
            throw new BusinessException("Khung giờ mới đã có người đặt.");

        if (await db.LichKhoaSans.AnyAsync(l => l.SanId == newCourt.Id && l.TrangThai == "DANG_AP_DUNG" && l.BatDauLuc < newEnd && newStart < l.KetThucLuc))
            throw new BusinessException("Khung giờ mới đang bị khóa hoặc bảo trì.");

        var today = DateOnly.FromDateTime(now);
        var activePolicy = await db.ChinhSachHuyDois
            .Where(p => p.CoSoId == order.CoSoId && p.NgayHieuLucTu <= today && (p.NgayHieuLucDen == null || p.NgayHieuLucDen >= today))
            .OrderByDescending(p => p.NgayHieuLucTu)
            .FirstOrDefaultAsync();
        var policyIdForNewSlot = activePolicy?.Id ?? oldSlot.ChinhSachHuyDoiId;

        var hoursBeforeOld = Math.Max(0m, decimal.Round((decimal)(VietnamTime.Start(oldSlot.NgaySuDung, oldSlot.GioBatDau) - now).TotalHours, 2));
        var tier = oldSlot.ChinhSachHuyDoi.MucPhiHuyDois
            .OrderBy(m => m.SoGioTuTruocTran)
            .FirstOrDefault(m => hoursBeforeOld >= m.SoGioTuTruocTran && (m.SoGioDenTruocTran is null || hoursBeforeOld < m.SoGioDenTruocTran))
            ?? oldSlot.ChinhSachHuyDoi.MucPhiHuyDois.OrderBy(m => m.SoGioTuTruocTran).FirstOrDefault();

        var feePercent = tier?.TiLePhiDoi ?? 0m;
        var changeFee = decimal.Round(oldSlot.GiaThue * feePercent / 100m, 2);
        var priceDiff = newPrice.GiaThue - oldSlot.GiaThue;

        var successPayments = order.ThanhToanDons.Where(p => p.TrangThai == "THANH_CONG").OrderBy(p => p.Id).ToList();
        var paid = successPayments.Sum(p => p.SoTien);

        oldCourtId = oldSlot.SanId;
        var oldSlotStatus = oldSlot.TrangThaiSlot;

        // Swap slots atomically
        oldSlot.TrangThaiSlot = "DA_DOI";
        oldSlot.NgayCapNhat = now;

        var newDetail = new ChiTietDatSan
        {
            DonDatSanId = order.Id,
            SanId = newCourt.Id,
            NgaySuDung = form.NewDate,
            GioBatDau = newSlotDef.GioBatDau,
            GioKetThuc = newSlotDef.GioKetThuc,
            GiaThue = newPrice.GiaThue,
            TrangThaiSlot = oldSlotStatus,
            KhungGioId = newSlotDef.Id,
            BangGiaId = newPrice.Id,
            ChiTietThayTheChoId = oldSlot.Id,
            ChinhSachHuyDoiId = policyIdForNewSlot,
            NgayTao = now,
            NgayCapNhat = now
        };
        db.ChiTietDatSans.Add(newDetail);
        await db.SaveChangesAsync();

        var newTotalSan = newPrice.GiaThue;
        var newTotalPhiPhat = order.TongPhiPhat + changeFee;
        var newTotalDue = Math.Max(0m, newTotalSan + order.TongTienDichVu + newTotalPhiPhat - order.TienGiamGia);
        order.TongTienSan = newTotalSan;
        order.TongPhiPhat = newTotalPhiPhat;
        order.TienCocYeuCau = Math.Min(newTotalDue, Math.Max(order.TienCocYeuCau, newPrice.TienCocToiThieu));
        order.NgayCapNhat = now;

        var rescheduleReq = new YeuCauDoiLich
        {
            ChiTietDatSanCuId = oldSlot.Id,
            SanMoiId = newCourt.Id,
            NgaySuDungMoi = form.NewDate,
            GioBatDauMoi = newSlotDef.GioBatDau,
            GioKetThucMoi = newSlotDef.GioKetThuc,
            PhiDoiLich = changeFee,
            ChenhLechGia = priceDiff,
            TrangThai = "DA_DUYET",
            LyDo = form.Reason?.Trim(),
            NgayTao = now,
            NguoiYeuCauId = actorUserId,
            NguoiXuLyId = actorUserId,
            KhungGioMoiId = newSlotDef.Id,
            ChiTietDatSanMoiId = newDetail.Id,
            NgayXuLy = now
        };
        db.YeuCauDoiLiches.Add(rescheduleReq);
        await db.SaveChangesAsync();

        // Handle financial difference
        var delta = newTotalDue - paid;
        if (delta > 0 && form.PayExtraByWallet && order.NguoiDungId.HasValue && isOwner)
        {
            // Only collect extra automatically via wallet if already confirmed or deposit increased
            decimal neededNow = order.TrangThai == "DA_XAC_NHAN"
                ? Math.Max(0m, (priceDiff > 0 ? priceDiff : 0m) + changeFee)
                : Math.Max(0m, order.TienCocYeuCau - paid);
            neededNow = Math.Min(delta, neededNow);

            if (neededNow > 0)
            {
                var lockedWallet = await walletService.LockOrCreateWalletAsync(order.NguoiDungId.Value, now);
                if (lockedWallet.SoDuKhaDung < neededNow)
                    throw new BusinessException($"Số dư ví ({lockedWallet.SoDuKhaDung:N0} đ) không đủ bù chênh lệch đổi lịch ({neededNow:N0} đ).");

                var key = $"RESCHED:{order.Id}:{rescheduleReq.Id}";
                var ledger = WalletService.CreateLedgerEntry(
                    lockedWallet,
                    "CHI_TRA",
                    neededNow,
                    key,
                    $"Bù chênh lệch/phí đổi lịch đơn {order.MaDatSan}",
                    now);
                db.GiaoDichVis.Add(ledger);
                await db.SaveChangesAsync();

                db.ThanhToanDons.Add(new ThanhToanDon
                {
                    MaThanhToan = Guid.NewGuid().ToString("N"),
                    DonDatSanId = order.Id,
                    NguoiThanhToanId = order.NguoiDungId,
                    GiaoDichViId = ledger.Id,
                    PhuongThuc = "VI",
                    MucDich = "BO_SUNG",
                    SoTien = neededNow,
                    KhoaChongLap = key,
                    TrangThai = "THANH_CONG",
                    NgayTao = now,
                    NgayThanhToan = now
                });
            }
        }
        else if (delta < 0 && successPayments.Count > 0)
        {
            var excessRefund = Math.Abs(delta);
            ViNguoiDung? lockedWallet = null;
            if (order.NguoiDungId.HasValue)
                lockedWallet = await walletService.LockOrCreateWalletAsync(order.NguoiDungId.Value, now);

            var remainingToRefund = excessRefund;
            foreach (var p in successPayments)
            {
                if (remainingToRefund <= 0) break;
                var portion = Math.Min(remainingToRefund, p.SoTien);
                if (portion <= 0) continue;

                var key = $"REFUND_RESCHED:{order.Id}:{p.Id}:{rescheduleReq.Id}";
                if (lockedWallet is not null)
                {
                    var ledger = WalletService.CreateLedgerEntry(
                        lockedWallet,
                        "HOAN",
                        portion,
                        key,
                        $"Hoàn chênh lệch đổi lịch đơn {order.MaDatSan}",
                        now);
                    db.GiaoDichVis.Add(ledger);
                    await db.SaveChangesAsync();

                    db.HoanTiens.Add(new HoanTien
                    {
                        MaHoanTien = $"HT{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}",
                        ThanhToanDonId = p.Id,
                        YeuCauDoiLichId = rescheduleReq.Id,
                        GiaoDichViId = ledger.Id,
                        NguoiDuyetId = actorUserId,
                        SoTien = portion,
                        PhuongThuc = "VI",
                        KhoaChongLap = key,
                        TrangThai = "THANH_CONG",
                        LyDo = $"Hoàn chênh lệch đổi lịch đơn {order.MaDatSan}",
                        NgayTao = now,
                        NgayHoanTien = now
                    });
                }
                else
                {
                    db.HoanTiens.Add(new HoanTien
                    {
                        MaHoanTien = $"HT{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}",
                        ThanhToanDonId = p.Id,
                        YeuCauDoiLichId = rescheduleReq.Id,
                        NguoiDuyetId = actorUserId,
                        SoTien = portion,
                        PhuongThuc = p.PhuongThuc == "TIEN_MAT" ? "TIEN_MAT" : "CHUYEN_KHOAN_TAI_QUAY",
                        KhoaChongLap = key,
                        TrangThai = "KHOI_TAO",
                        LyDo = $"Hoàn chênh lệch đổi lịch đơn {order.MaDatSan}",
                        NgayTao = now
                    });
                }
                remainingToRefund -= portion;
            }
        }

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        notifyUserId = order.NguoiDungId;
        if (updates is not null)
        {
            await updates.Calendar(oldCourtId);
            if (newCourt.Id != oldCourtId)
                await updates.Calendar(newCourt.Id);
            await updates.PaymentChanged(order.Id, notifyUserId);
        }
    }
}
