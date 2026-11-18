using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuanLySanBai.Data;
using QuanLySanBai.Models.Entities;
using QuanLySanBai.ViewModels;

namespace QuanLySanBai.Services;

public class WalletService(
    QuanLySanBaiContext db,
    VietQrService vietQr,
    TimeProvider clock,
    IOptions<SePayOptions> sepayOptions,
    RealtimeUpdates? updates = null)
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime.AddHours(7);

    private async Task<DateTime> ServerNowAsync() =>
        await db.Database.SqlQueryRaw<DateTime>("SELECT CONVERT(datetime2(0),DATEADD(HOUR,7,SYSUTCDATETIME())) AS Value").SingleAsync();

    public async Task<ViNguoiDung> GetOrCreateWallet(long userId)
    {
        var wallet = await db.ViNguoiDungs.SingleOrDefaultAsync(w => w.NguoiDungId == userId);
        if (wallet is not null)
            return wallet;

        if (!await db.NguoiDungs.AnyAsync(u => u.Id == userId && u.TrangThai))
            throw new FacilityAccessException();

        var now = await ServerNowAsync();
        wallet = new ViNguoiDung
        {
            NguoiDungId = userId,
            SoDuKhaDung = 0m,
            SoDuDongBang = 0m,
            TrangThai = "HOAT_DONG",
            NgayTao = now,
            NgayCapNhat = now
        };
        db.ViNguoiDungs.Add(wallet);
        try
        {
            await db.SaveChangesAsync();
            return wallet;
        }
        catch (DbUpdateException)
        {
            db.Entry(wallet).State = EntityState.Detached;
            return await db.ViNguoiDungs.SingleAsync(w => w.NguoiDungId == userId);
        }
    }

    public async Task<WalletIndexVM> GetWalletIndex(long userId)
    {
        var wallet = await GetOrCreateWallet(userId);
        var transactions = await db.GiaoDichVis.AsNoTracking()
            .Where(t => t.ViId == wallet.Id)
            .OrderByDescending(t => t.Id)
            .Take(50)
            .ToListAsync();
        var deposits = await db.NapTienVis.AsNoTracking()
            .Include(d => d.GiaoDichCong)
            .Where(d => d.ViId == wallet.Id)
            .OrderByDescending(d => d.Id)
            .Take(20)
            .ToListAsync();
        var withdrawals = await db.YeuCauRutTiens.AsNoTracking()
            .Where(w => w.ViId == wallet.Id)
            .OrderByDescending(w => w.Id)
            .Take(20)
            .ToListAsync();
        var refunds = await db.HoanTiens.AsNoTracking()
            .Include(h => h.ThanhToanDon).ThenInclude(p => p.DonDatSan)
            .Where(h => h.ThanhToanDon.NguoiThanhToanId == userId || h.ThanhToanDon.DonDatSan.NguoiDungId == userId)
            .OrderByDescending(h => h.Id)
            .Take(20)
            .ToListAsync();

        return new WalletIndexVM
        {
            Wallet = wallet,
            Transactions = transactions,
            Deposits = deposits,
            Withdrawals = withdrawals,
            Refunds = refunds,
            SePayConfigured = vietQr.IsConfigured,
            CanWithdraw = await CanUserWithdrawAsync(userId)
        };
    }

    public Task<bool> CanUserWithdrawAsync(long userId) =>
        db.CoSos.AnyAsync(c => c.ChuSoHuuId == userId && c.TrangThaiDuyet == "DA_DUYET");

    public async Task<long> CreateDepositQr(long userId, decimal amount)
    {
        if (amount < 1000 || amount > 100000000 || decimal.Round(amount, 2) != amount)
            throw new BusinessException("Số tiền nạp ví phải từ 1.000 đến 100.000.000 đ.");

        var wallet = await GetOrCreateWallet(userId);
        if (wallet.TrangThai != "HOAT_DONG")
            throw new BusinessException("Ví hiện đang bị khóa, không thể nạp tiền.");

        if (!vietQr.IsConfigured && !sepayOptions.Value.AllowTestWithoutSecrets)
            throw new BusinessException("Cổng VietQR SePay chưa được cấu hình thông tin tài khoản nhận.");

        var now = await ServerNowAsync();
        var expireMinutes = Math.Clamp(sepayOptions.Value.QrExpireMinutes, 1, 60);
        var paymentCode = $"QSBW{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}";

        await using var tx = await db.Database.BeginTransactionAsync();
        var gatewayTx = new GiaoDichCongThanhToan
        {
            MaDonHangCong = paymentCode,
            NguoiDungId = userId,
            CongThanhToan = "SEPAY",
            SoTien = amount,
            LoaiThanhToan = "THU_TIEN",
            TrangThai = "KHOI_TAO",
            NgayTao = now,
            NgayCapNhat = now,
            ThoiGianHetHanQr = now.AddMinutes(expireMinutes),
            NoiDungChuyenKhoan = paymentCode
        };
        db.GiaoDichCongThanhToans.Add(gatewayTx);
        await db.SaveChangesAsync();

        db.NapTienVis.Add(new NapTienVi
        {
            ViId = wallet.Id,
            GiaoDichCongId = gatewayTx.Id,
            SoTien = amount,
            TrangThai = "KHOI_TAO",
            NgayTao = now
        });
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return gatewayTx.Id;
    }

    public async Task<long> PayBookingWithWallet(long userId, PayWalletForm form)
    {
        if (string.IsNullOrWhiteSpace(form.Key) || form.Key.Length > 100)
            throw new BusinessException("Khóa giao dịch không hợp lệ.");
        if (form.Purpose is not ("COC" or "THANH_TOAN" or "BO_SUNG"))
            throw new BusinessException("Mục đích thanh toán không hợp lệ.");

        await GetOrCreateWallet(userId);
        if (db.ChangeTracker.HasChanges())
            throw new BusinessException("Có dữ liệu chưa lưu. Hãy tải lại trang.");
        db.ChangeTracker.Clear();

        long? courtIdToNotify = null;
        long paymentId = 0;

        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(
            "DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'QLSB:LICH_SAN',@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=10000; IF @r<0 THROW 51305,N'Lịch đang được thao tác, hãy thử lại.',1;");

        var existingPayment = await db.ThanhToanDons.SingleOrDefaultAsync(p => p.KhoaChongLap == form.Key);
        if (existingPayment is not null)
        {
            if (existingPayment.DonDatSanId != form.OrderId || existingPayment.NguoiThanhToanId != userId || existingPayment.PhuongThuc != "VI")
                throw new BusinessException("Khóa thanh toán đã được dùng cho yêu cầu khác.");
            await tx.CommitAsync();
            return existingPayment.Id;
        }

        var order = await db.DonDatSans.Include(d => d.ChiTietDatSans)
            .SingleOrDefaultAsync(d => d.Id == form.OrderId)
            ?? throw new BusinessException("Không tìm thấy đơn đặt sân.");

        if (order.NguoiDungId != userId)
            throw new FacilityAccessException();

        if (!string.IsNullOrEmpty(form.Version) && Convert.ToBase64String(order.RowVersion) != form.Version)
            throw new BusinessException("Đơn đã thay đổi. Vui lòng tải lại trang trước khi thanh toán.");

        if (order.TrangThai is not ("CHO_THANH_TOAN" or "DA_XAC_NHAN" or "DANG_SU_DUNG"))
            throw new BusinessException("Trạng thái đơn hiện tại không thể thanh toán.");

        var now = await ServerNowAsync();
        if (order.TrangThai == "CHO_THANH_TOAN" && (order.ThoiGianHetHanGiuCho is null || order.ThoiGianHetHanGiuCho <= now))
            throw new BusinessException("Đơn đã hết hạn giữ chỗ.");

        var paid = await OrderFinance.NetPaidAsync(db, order.Id);
        var totalDue = order.TongTienPhaiTra ?? 0m;
        var remaining = totalDue - paid;
        if (remaining <= 0)
            throw new BusinessException("Đơn đã thanh toán đủ.");

        decimal amountToPay = form.Purpose == "COC" && order.TrangThai == "CHO_THANH_TOAN" && order.TienCocYeuCau > paid
            ? Math.Min(remaining, order.TienCocYeuCau - paid)
            : remaining;
        if (amountToPay <= 0)
            amountToPay = remaining;

        if (order.TrangThai == "CHO_THANH_TOAN" && paid + amountToPay < order.TienCocYeuCau)
            throw new BusinessException("Lần thanh toán đầu phải đạt ít nhất tiền cọc yêu cầu.");

        var lockedWallet = await LockWalletByUserIdAsync(userId);
        if (lockedWallet.TrangThai != "HOAT_DONG")
            throw new BusinessException("Ví đang bị khóa.");
        if (lockedWallet.SoDuKhaDung < amountToPay)
            throw new InsufficientWalletBalanceException(lockedWallet.SoDuKhaDung, amountToPay);

        var ledgerEntry = CreateLedgerEntry(
            lockedWallet,
            "CHI_TRA",
            amountToPay,
            $"W:{form.Key}",
            $"Thanh toán đơn {order.MaDatSan}",
            now);
        db.GiaoDichVis.Add(ledgerEntry);
        await db.SaveChangesAsync();

        var activeSlots = order.ChiTietDatSans.Where(c => c.TrangThaiSlot is not ("DA_DOI" or "DA_HUY")).ToList();
        if (order.TrangThai == "CHO_THANH_TOAN" && paid + amountToPay >= order.TienCocYeuCau)
        {
            order.TrangThai = "DA_XAC_NHAN";
            order.ThoiGianHetHanGiuCho = null;
            order.NgayCapNhat = now;
            foreach (var c in activeSlots)
            {
                c.TrangThaiSlot = "DA_XAC_NHAN";
                c.NgayCapNhat = now;
            }
            courtIdToNotify = activeSlots.FirstOrDefault()?.SanId;
        }
        else
        {
            order.NgayCapNhat = now;
        }

        var payment = new ThanhToanDon
        {
            MaThanhToan = Guid.NewGuid().ToString("N"),
            DonDatSanId = order.Id,
            NguoiThanhToanId = userId,
            GiaoDichViId = ledgerEntry.Id,
            PhuongThuc = "VI",
            MucDich = form.Purpose,
            SoTien = amountToPay,
            KhoaChongLap = form.Key,
            TrangThai = "THANH_CONG",
            NgayTao = now,
            NgayThanhToan = now
        };
        db.ThanhToanDons.Add(payment);
        await db.SaveChangesAsync();
        paymentId = payment.Id;

        await tx.CommitAsync();

        if (updates is not null)
        {
            if (courtIdToNotify.HasValue)
                await updates.Calendar(courtIdToNotify.Value);
            await updates.PaymentChanged(order.Id, userId);
        }

        return paymentId;
    }

    public async Task<long> RequestWithdrawal(long userId, WithdrawForm form)
    {
        if (!await CanUserWithdrawAsync(userId))
            throw new BusinessException("Tính năng rút tiền hiện chỉ dành cho Chủ sân có cơ sở đã duyệt.");
        if (form.Amount < 10000 || decimal.Round(form.Amount, 2) != form.Amount)
            throw new BusinessException("Số tiền rút tối thiểu 10.000 đ và tối đa 2 chữ số thập phân.");
        if (string.IsNullOrWhiteSpace(form.BankName) || string.IsNullOrWhiteSpace(form.AccountNumber) || string.IsNullOrWhiteSpace(form.AccountHolder))
            throw new BusinessException("Vui lòng nhập đầy đủ thông tin tài khoản ngân hàng nhận tiền.");

        await GetOrCreateWallet(userId);
        if (db.ChangeTracker.HasChanges())
            throw new BusinessException("Có dữ liệu chưa lưu. Hãy tải lại trang.");
        db.ChangeTracker.Clear();

        await using var tx = await db.Database.BeginTransactionAsync();
        var lockedWallet = await LockWalletByUserIdAsync(userId);
        if (lockedWallet.TrangThai != "HOAT_DONG")
            throw new BusinessException("Ví đang bị khóa.");
        if (lockedWallet.SoDuKhaDung < form.Amount)
            throw new BusinessException($"Số dư khả dụng ({lockedWallet.SoDuKhaDung:N0} đ) không đủ để rút.");

        var now = await ServerNowAsync();
        var holdEntry = CreateLedgerEntry(
            lockedWallet,
            "GIU_TIEN",
            form.Amount,
            $"HOLD:{Guid.NewGuid():N}",
            $"Tạm giữ tiền cho yêu cầu rút về {form.BankName.Trim()} - {form.AccountNumber.Trim()}",
            now);
        db.GiaoDichVis.Add(holdEntry);
        await db.SaveChangesAsync();

        var request = new YeuCauRutTien
        {
            ViId = lockedWallet.Id,
            SoTien = form.Amount,
            TenNganHang = form.BankName.Trim(),
            SoTaiKhoan = form.AccountNumber.Trim(),
            TenChuTaiKhoan = form.AccountHolder.Trim(),
            TrangThai = "CHO_DUYET",
            NgayTao = now,
            GiaoDichGiuTienId = holdEntry.Id
        };
        db.YeuCauRutTiens.Add(request);
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return request.Id;
    }

    public async Task ProcessWithdrawal(long adminId, ProcessWithdrawalForm form)
    {
        if (!await db.NguoiDungs.AnyAsync(u => u.Id == adminId && u.TrangThai && u.NguoiDungVaiTros.Any(r => r.VaiTro.MaVaiTro == "ADMIN")))
            throw new FacilityAccessException();

        if (!form.Approve && string.IsNullOrWhiteSpace(form.RejectReason))
            throw new BusinessException("Vui lòng nhập lý do từ chối yêu cầu rút tiền.");

        if (db.ChangeTracker.HasChanges())
            throw new BusinessException("Có dữ liệu chưa lưu. Hãy tải lại trang.");
        db.ChangeTracker.Clear();

        await using var tx = await db.Database.BeginTransactionAsync();
        var request = await db.YeuCauRutTiens
            .FromSqlInterpolated($"SELECT * FROM dbo.Yeu_Cau_Rut_Tien WITH (UPDLOCK, HOLDLOCK) WHERE id = {form.RequestId}")
            .SingleOrDefaultAsync()
            ?? throw new BusinessException("Không tìm thấy yêu cầu rút tiền.");

        if (request.TrangThai != "CHO_DUYET")
            throw new BusinessException("Yêu cầu rút tiền này đã được xử lý trước đó.");

        var lockedWallet = await db.ViNguoiDungs
            .FromSqlInterpolated($"SELECT * FROM dbo.Vi_Nguoi_Dung WITH (UPDLOCK, HOLDLOCK) WHERE id = {request.ViId}")
            .SingleAsync();

        if (lockedWallet.SoDuDongBang < request.SoTien)
            throw new BusinessException("Số dư đóng băng của ví không đủ để xử lý yêu cầu rút.");

        var now = await ServerNowAsync();
        var ledgerType = form.Approve ? "RUT" : "GIAI_TOA";
        var note = form.Approve
            ? $"Chuyển khoản rút tiền #{request.Id} về {request.TenNganHang} - {request.SoTaiKhoan}"
            : $"Từ chối rút tiền #{request.Id}: {form.RejectReason?.Trim()}";

        var endEntry = CreateLedgerEntry(
            lockedWallet,
            ledgerType,
            request.SoTien,
            $"WD:{request.Id}:{ledgerType}",
            note,
            now);
        db.GiaoDichVis.Add(endEntry);
        await db.SaveChangesAsync();

        request.NhanVienDuyetId = adminId;
        request.NgayXuLy = now;
        request.GiaoDichKetThucId = endEntry.Id;
        request.TrangThai = form.Approve ? "THANH_CONG" : "TU_CHOI";
        if (!form.Approve)
            request.LyDoTuChoi = form.RejectReason?.Trim();

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        if (updates is not null)
            await updates.PaymentChanged(null, lockedWallet.NguoiDungId);
    }

    public async Task<ViNguoiDung> LockOrCreateWalletAsync(long userId, DateTime now)
    {
        var wallet = await db.ViNguoiDungs
            .FromSqlInterpolated($"SELECT * FROM dbo.Vi_Nguoi_Dung WITH (UPDLOCK, HOLDLOCK) WHERE nguoi_dung_id = {userId}")
            .SingleOrDefaultAsync();
        if (wallet is not null)
            return wallet;

        wallet = new ViNguoiDung
        {
            NguoiDungId = userId,
            SoDuKhaDung = 0m,
            SoDuDongBang = 0m,
            TrangThai = "HOAT_DONG",
            NgayTao = now,
            NgayCapNhat = now
        };
        db.ViNguoiDungs.Add(wallet);
        await db.SaveChangesAsync();
        return wallet;
    }

    public Task<ViNguoiDung> LockWalletByUserIdAsync(long userId) =>
        db.ViNguoiDungs
            .FromSqlInterpolated($"SELECT * FROM dbo.Vi_Nguoi_Dung WITH (UPDLOCK, HOLDLOCK) WHERE nguoi_dung_id = {userId}")
            .SingleAsync();

    public Task<ViNguoiDung> LockWalletByIdAsync(long walletId) =>
        db.ViNguoiDungs
            .FromSqlInterpolated($"SELECT * FROM dbo.Vi_Nguoi_Dung WITH (UPDLOCK, HOLDLOCK) WHERE id = {walletId}")
            .SingleAsync();

    public static GiaoDichVi CreateLedgerEntry(
        ViNguoiDung wallet,
        string type,
        decimal amount,
        string idempotencyKey,
        string? note,
        DateTime now)
    {
        if (amount <= 0)
            throw new BusinessException("Số tiền giao dịch ví phải lớn hơn 0.");

        var availBefore = wallet.SoDuKhaDung;
        var frozenBefore = wallet.SoDuDongBang;
        decimal availAfter = availBefore;
        decimal frozenAfter = frozenBefore;

        switch (type)
        {
            case "NAP":
            case "HOAN":
                availAfter = availBefore + amount;
                break;
            case "CHI_TRA":
                if (availBefore < amount)
                    throw new BusinessException("Số dư khả dụng không đủ.");
                availAfter = availBefore - amount;
                break;
            case "GIU_TIEN":
                if (availBefore < amount)
                    throw new BusinessException("Số dư khả dụng không đủ để tạm giữ.");
                availAfter = availBefore - amount;
                frozenAfter = frozenBefore + amount;
                break;
            case "GIAI_TOA":
                if (frozenBefore < amount)
                    throw new BusinessException("Số dư đóng băng không đủ để giải tỏa.");
                availAfter = availBefore + amount;
                frozenAfter = frozenBefore - amount;
                break;
            case "RUT":
                if (frozenBefore < amount)
                    throw new BusinessException("Số dư đóng băng không đủ để hoàn tất rút tiền.");
                frozenAfter = frozenBefore - amount;
                break;
            default:
                throw new BusinessException("Loại giao dịch ví không hợp lệ.");
        }

        wallet.SoDuKhaDung = availAfter;
        wallet.SoDuDongBang = frozenAfter;
        wallet.NgayCapNhat = now;

        return new GiaoDichVi
        {
            ViId = wallet.Id,
            MaGiaoDich = Guid.NewGuid().ToString("N"),
            LoaiGiaoDich = type,
            SoTien = amount,
            SoDuTruoc = availBefore,
            SoDuSau = availAfter,
            SoDuDongBangTruoc = frozenBefore,
            SoDuDongBangSau = frozenAfter,
            NoiDung = note,
            TrangThai = "DA_GHI_SO",
            NgayTao = now,
            KhoaChongLap = idempotencyKey
        };
    }
}
