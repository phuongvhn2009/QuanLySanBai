using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuanLySanBai.Data;
using QuanLySanBai.Models.Entities;
using QuanLySanBai.ViewModels;

namespace QuanLySanBai.Services;

public class PaymentService(
    QuanLySanBaiContext db,
    AccessService access,
    VietQrService vietQr,
    WalletService walletService,
    TimeProvider clock,
    IOptions<SePayOptions> sepayOptions,
    RealtimeUpdates? updates = null)
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime.AddHours(7);

    private async Task<DateTime> ServerNowAsync() =>
        await db.Database.SqlQueryRaw<DateTime>("SELECT CONVERT(datetime2(0),DATEADD(HOUR,7,SYSUTCDATETIME())) AS Value").SingleAsync();

    public async Task<TaoThanhToanVM> GetPaymentSelection(long actorUserId, long orderId)
    {
        var order = await db.DonDatSans.AsNoTracking()
            .Include(d => d.CoSo)
            .Include(d => d.ChiTietDatSans).ThenInclude(c => c.San)
            .SingleOrDefaultAsync(d => d.Id == orderId)
            ?? throw new BusinessException("Không tìm thấy đơn đặt sân.");

        var isOwner = order.NguoiDungId == actorUserId;
        var canCollect = await access.HasPermission(actorUserId, order.CoSoId, "THU_TIEN");
        var canView = isOwner || canCollect || await access.HasPermission(actorUserId, order.CoSoId, "XEM_LICH");
        if (!canView)
            throw new FacilityAccessException();

        var paid = await OrderFinance.NetPaidAsync(db, orderId);

        decimal walletBalance = 0m;
        if (isOwner)
        {
            var wallet = await walletService.GetOrCreateWallet(actorUserId);
            walletBalance = wallet.SoDuKhaDung;
        }

        var slot = order.ChiTietDatSans.FirstOrDefault(c => c.TrangThaiSlot is not ("DA_DOI" or "DA_HUY"))
            ?? order.ChiTietDatSans.FirstOrDefault();

        return new TaoThanhToanVM
        {
            OrderId = order.Id,
            MaDatSan = order.MaDatSan,
            TenCoSo = order.CoSo.TenCoSo,
            TenSan = slot?.San.TenSan ?? "",
            NgaySuDung = slot?.NgaySuDung ?? default,
            GioBatDau = slot?.GioBatDau ?? default,
            GioKetThuc = slot?.GioKetThuc ?? default,
            TrangThaiDon = order.TrangThai,
            TongTienSan = order.TongTienSan,
            TongTienDichVu = order.TongTienDichVu,
            TongPhiPhat = order.TongPhiPhat,
            TienGiamGia = order.TienGiamGia,
            TongTienPhaiTra = order.TongTienPhaiTra ?? 0m,
            TienCocYeuCau = order.TienCocYeuCau,
            DaThanhToan = paid,
            SoDuViKhaDung = walletBalance,
            IsStaff = canCollect,
            CanCollectCash = canCollect,
            SePayConfigured = vietQr.IsConfigured,
            Version = Convert.ToBase64String(order.RowVersion),
            ThoiGianHetHanGiuCho = order.ThoiGianHetHanGiuCho
        };
    }

    public async Task<long> CreateBookingQr(long actorUserId, long orderId, string purpose)
    {
        if (purpose is not ("COC" or "THANH_TOAN" or "BO_SUNG"))
            purpose = "THANH_TOAN";

        if (!vietQr.IsConfigured && !sepayOptions.Value.AllowTestWithoutSecrets)
            throw new BusinessException("Chưa cấu hình tài khoản MB Bank nhận tiền (SePay:AccountNumber).");

        var order = await db.DonDatSans.AsNoTracking()
            .SingleOrDefaultAsync(d => d.Id == orderId)
            ?? throw new BusinessException("Không tìm thấy đơn đặt sân.");

        var isOwner = order.NguoiDungId == actorUserId;
        var canCollect = await access.HasPermission(actorUserId, order.CoSoId, "THU_TIEN");
        if (!isOwner && !canCollect)
            throw new FacilityAccessException();

        if (order.TrangThai is not ("CHO_THANH_TOAN" or "DA_XAC_NHAN" or "DANG_SU_DUNG"))
            throw new BusinessException("Trạng thái đơn hiện tại không thể tạo mã thanh toán.");

        var now = await ServerNowAsync();
        if (order.TrangThai == "CHO_THANH_TOAN" && (order.ThoiGianHetHanGiuCho is null || order.ThoiGianHetHanGiuCho <= now))
            throw new BusinessException("Đơn đã hết hạn giữ chỗ.");

        var paid = await OrderFinance.NetPaidAsync(db, orderId);
        var totalDue = order.TongTienPhaiTra ?? 0m;
        var remaining = totalDue - paid;
        if (remaining <= 0)
            throw new BusinessException("Đơn đã thanh toán đủ.");

        decimal amountToCollect = purpose == "COC" && order.TrangThai == "CHO_THANH_TOAN" && order.TienCocYeuCau > paid
            ? Math.Min(remaining, order.TienCocYeuCau - paid)
            : remaining;
        if (amountToCollect <= 0)
            amountToCollect = remaining;

        if (order.TrangThai == "CHO_THANH_TOAN" && paid + amountToCollect < order.TienCocYeuCau)
            throw new BusinessException("Số tiền QR phải đạt tối thiểu tiền cọc yêu cầu.");

        var expireMinutes = Math.Clamp(sepayOptions.Value.QrExpireMinutes, 1, 60);
        var defaultExpiry = now.AddMinutes(expireMinutes);
        var qrExpiry = order.TrangThai == "CHO_THANH_TOAN" && order.ThoiGianHetHanGiuCho.HasValue && order.ThoiGianHetHanGiuCho.Value < defaultExpiry
            ? order.ThoiGianHetHanGiuCho.Value
            : defaultExpiry;

        var paymentCode = $"QSBO{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}";

        var gatewayTx = new GiaoDichCongThanhToan
        {
            MaDonHangCong = paymentCode,
            DonDatSanId = order.Id,
            NguoiDungId = order.NguoiDungId ?? actorUserId,
            CongThanhToan = "SEPAY",
            SoTien = amountToCollect,
            LoaiThanhToan = "THU_TIEN",
            TrangThai = "KHOI_TAO",
            NgayTao = now,
            NgayCapNhat = now,
            ThoiGianHetHanQr = qrExpiry,
            NoiDungChuyenKhoan = paymentCode
        };
        db.GiaoDichCongThanhToans.Add(gatewayTx);
        await db.SaveChangesAsync();

        return gatewayTx.Id;
    }

    public async Task<(long? PaymentId, long? GatewayTxId, string? InfoMessage)> PayWithWalletOrQrAsync(long userId, PayWalletForm form)
    {
        try
        {
            var paymentId = await walletService.PayBookingWithWallet(userId, form);
            return (paymentId, null, null);
        }
        catch (InsufficientWalletBalanceException ex)
        {
            db.ChangeTracker.Clear();
            var gatewayTxId = await CreateBookingQr(userId, form.OrderId, form.Purpose);
            return (null, gatewayTxId, $"{ex.Message} Hệ thống đã tự động chuyển sang tạo mã chuyển khoản VietQR cho đơn này.");
        }
    }

    public async Task<ThanhToanQrVM> GetQrDetails(long actorUserId, long gatewayTxId)
    {
        var gw = await db.GiaoDichCongThanhToans.AsNoTracking()
            .Include(g => g.DonDatSan)
            .Include(g => g.ThanhToanDon)
            .Include(g => g.NapTienVi)
            .SingleOrDefaultAsync(g => g.Id == gatewayTxId)
            ?? throw new BusinessException("Không tìm thấy yêu cầu thanh toán QR.");

        bool authorized = gw.NguoiDungId == actorUserId;
        if (!authorized && gw.DonDatSan is not null)
        {
            authorized = gw.DonDatSan.NguoiDungId == actorUserId
                || await access.HasPermission(actorUserId, gw.DonDatSan.CoSoId, "XEM_LICH")
                || await access.HasPermission(actorUserId, gw.DonDatSan.CoSoId, "THU_TIEN");
        }
        if (!authorized)
            throw new FacilityAccessException();

        var now = await ServerNowAsync();
        var content = gw.NoiDungChuyenKhoan ?? gw.MaDonHangCong;
        var qrUrl = vietQr.IsConfigured || sepayOptions.Value.AllowTestWithoutSecrets
            ? vietQr.BuildQrUrl(gw.SoTien, content)
            : "";

        string? warning = null;
        if (!vietQr.IsConfigured)
            warning = "Chưa cấu hình SePay:AccountNumber / SePay:BankCode trong cấu hình hệ thống.";

        return new ThanhToanQrVM
        {
            GatewayTxId = gw.Id,
            OrderId = gw.DonDatSanId,
            MaDatSan = gw.DonDatSan?.MaDatSan,
            IsWalletDeposit = gw.NapTienVi is not null,
            PaymentCode = gw.MaDonHangCong,
            TransferContent = content,
            QrImageUrl = qrUrl,
            BankName = sepayOptions.Value.BankName,
            BankCode = sepayOptions.Value.BankCode,
            AccountNumber = sepayOptions.Value.AccountNumber,
            AccountName = sepayOptions.Value.AccountName,
            Amount = gw.SoTien,
            Status = gw.TrangThai,
            OrderStatus = gw.DonDatSan?.TrangThai ?? (gw.NapTienVi?.TrangThai ?? ""),
            ExpiresAt = gw.ThoiGianHetHanQr ?? gw.NgayTao.AddMinutes(15),
            ServerNow = now,
            PaymentId = gw.ThanhToanDon?.Id,
            SePayConfigured = vietQr.IsConfigured,
            WarningMessage = warning
        };
    }

    public async Task<long> CollectCash(long actorUserId, ThanhToanTienMatVM form)
    {
        if (form.Amount <= 0 || form.Amount > 999999999999.99m || decimal.Round(form.Amount, 2) != form.Amount)
            throw new BusinessException("Số tiền cần thu không hợp lệ.");
        if (form.CashReceived < form.Amount || decimal.Round(form.CashReceived, 2) != form.CashReceived)
            throw new BusinessException("Số tiền khách đưa phải lớn hơn hoặc bằng số tiền cần thanh toán.");
        if (form.Purpose is not ("COC" or "THANH_TOAN" or "BO_SUNG"))
            throw new BusinessException("Mục đích thu tiền không hợp lệ.");
        if (string.IsNullOrWhiteSpace(form.Key) || form.Key.Length > 100)
            throw new BusinessException("Khóa chống lặp không hợp lệ.");

        if (db.ChangeTracker.HasChanges())
            throw new BusinessException("Có dữ liệu chưa lưu. Hãy tải lại trang.");
        db.ChangeTracker.Clear();

        long paymentId = 0;
        long? courtId = null;
        long? customerId = null;

        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(
            "DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'QLSB:LICH_SAN',@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=10000; IF @r<0 THROW 51305,N'Lịch đang được thao tác, hãy thử lại.',1;");

        var prior = await db.ThanhToanDons.SingleOrDefaultAsync(p => p.KhoaChongLap == form.Key);
        if (prior is not null)
        {
            if (prior.DonDatSanId != form.OrderId || prior.NhanVienThuId != actorUserId || prior.SoTien != form.Amount || prior.PhuongThuc != "TIEN_MAT")
                throw new BusinessException("Khóa thu tiền đã được dùng cho yêu cầu khác.");
            await tx.CommitAsync();
            return prior.Id;
        }

        var order = await db.DonDatSans.Include(d => d.ChiTietDatSans)
            .SingleOrDefaultAsync(d => d.Id == form.OrderId)
            ?? throw new BusinessException("Không tìm thấy đơn đặt sân.");

        if (!await db.NguoiDungs.AsNoTracking().AnyAsync(u => u.Id == actorUserId && u.TrangThai) ||
            !await access.HasPermission(actorUserId, order.CoSoId, "THU_TIEN"))
            throw new FacilityAccessException();

        if (!string.IsNullOrEmpty(form.Version) && Convert.ToBase64String(order.RowVersion) != form.Version)
            throw new BusinessException("Đơn đã thay đổi. Vui lòng tải lại trang trước khi thu tiền.");

        if (order.TrangThai is not ("CHO_THANH_TOAN" or "DA_XAC_NHAN" or "DANG_SU_DUNG"))
            throw new BusinessException("Không thể thu tiền ở trạng thái hiện tại.");

        var paid = await OrderFinance.NetPaidAsync(db, order.Id);
        var totalDue = order.TongTienPhaiTra ?? 0m;
        if (paid + form.Amount > totalDue)
            throw new BusinessException("Khoản thu vượt quá số tiền còn phải trả.");

        var now = await ServerNowAsync();
        var activeSlots = order.ChiTietDatSans.Where(c => c.TrangThaiSlot is not ("DA_DOI" or "DA_HUY")).ToList();

        if (order.TrangThai == "CHO_THANH_TOAN")
        {
            if (activeSlots.Count != 1 || activeSlots.Any(c => c.TrangThaiSlot != "GIU_CHO"))
                throw new BusinessException("Đơn không còn ở trạng thái giữ chỗ.");
            if (order.ThoiGianHetHanGiuCho is null || order.ThoiGianHetHanGiuCho <= now)
                throw new BusinessException("Đã hết hạn giữ chỗ; không được nhận tiền/xác nhận đơn này.");
            if (paid + form.Amount < order.TienCocYeuCau)
                throw new BusinessException("Lần thu đầu phải đạt mức cọc yêu cầu để xác nhận đơn.");

            order.TrangThai = "DA_XAC_NHAN";
            order.ThoiGianHetHanGiuCho = null;
            order.NgayCapNhat = now;
            foreach (var c in activeSlots)
            {
                c.TrangThaiSlot = "DA_XAC_NHAN";
                c.NgayCapNhat = now;
            }
            courtId = activeSlots.FirstOrDefault()?.SanId;
        }
        else
        {
            order.NgayCapNhat = now;
        }

        var change = decimal.Round(form.CashReceived - form.Amount, 2);
        var payment = new ThanhToanDon
        {
            MaThanhToan = Guid.NewGuid().ToString("N"),
            DonDatSanId = order.Id,
            NguoiThanhToanId = order.NguoiDungId,
            NhanVienThuId = actorUserId,
            PhuongThuc = "TIEN_MAT",
            MucDich = form.Purpose,
            SoTien = form.Amount,
            SoTienKhachDua = form.CashReceived,
            TienThua = change,
            KhoaChongLap = form.Key,
            TrangThai = "THANH_CONG",
            NgayTao = now,
            NgayThanhToan = now
        };
        db.ThanhToanDons.Add(payment);
        await db.SaveChangesAsync();
        paymentId = payment.Id;
        customerId = order.NguoiDungId;

        await tx.CommitAsync();

        if (updates is not null)
        {
            if (courtId.HasValue)
                await updates.Calendar(courtId.Value);
            await updates.PaymentChanged(order.Id, customerId);
        }

        return paymentId;
    }

    public async Task<KetQuaThanhToanVM> GetReceipt(long actorUserId, long paymentId)
    {
        var payment = await db.ThanhToanDons.AsNoTracking()
            .Include(p => p.NhanVienThu)
            .Include(p => p.DonDatSan).ThenInclude(d => d.CoSo)
            .Include(p => p.DonDatSan).ThenInclude(d => d.ChiTietDatSans).ThenInclude(c => c.San)
            .SingleOrDefaultAsync(p => p.Id == paymentId)
            ?? throw new BusinessException("Không tìm thấy hóa đơn thanh toán.");

        var order = payment.DonDatSan;
        var authorized = order.NguoiDungId == actorUserId
            || await access.HasPermission(actorUserId, order.CoSoId, "XEM_LICH")
            || await access.HasPermission(actorUserId, order.CoSoId, "THU_TIEN");
        if (!authorized)
            throw new FacilityAccessException();

        var totalPaid = await OrderFinance.NetPaidAsync(db, order.Id);

        var slot = order.ChiTietDatSans.FirstOrDefault(c => c.TrangThaiSlot is not ("DA_DOI" or "DA_HUY"))
            ?? order.ChiTietDatSans.FirstOrDefault();

        return new KetQuaThanhToanVM
        {
            PaymentId = payment.Id,
            MaThanhToan = payment.MaThanhToan,
            OrderId = order.Id,
            MaDatSan = order.MaDatSan,
            TenCoSo = order.CoSo.TenCoSo,
            DiaChiCoSo = order.CoSo.DiaChi,
            TenKhachHang = order.TenKhachHang,
            SdtKhachHang = order.SdtKhachHang,
            TenSan = slot?.San.TenSan ?? "",
            NgaySuDung = slot?.NgaySuDung ?? default,
            GioBatDau = slot?.GioBatDau ?? default,
            GioKetThuc = slot?.GioKetThuc ?? default,
            PhuongThuc = payment.PhuongThuc,
            MucDich = payment.MucDich,
            TongTienSan = order.TongTienSan,
            TongPhiPhat = order.TongPhiPhat,
            TienGiamGia = order.TienGiamGia,
            TongTienPhaiTra = order.TongTienPhaiTra ?? 0m,
            SoTienThanhToan = payment.SoTien,
            SoTienKhachDua = payment.SoTienKhachDua,
            TienThua = payment.TienThua,
            TongDaThu = totalPaid,
            TenNhanVienThu = payment.NhanVienThu?.HoVaTen,
            MaThamChieuNgoai = payment.MaThamChieuNgoai,
            NgayThanhToan = payment.NgayThanhToan ?? payment.NgayTao,
            TrangThaiDon = order.TrangThai
        };
    }
}
