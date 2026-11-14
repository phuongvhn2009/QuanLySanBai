using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuanLySanBai.Data;
using QuanLySanBai.Models.Entities;
using QuanLySanBai.ViewModels;
namespace QuanLySanBai.Services;

public class BookingService(QuanLySanBaiContext db, IOptions<BookingOptions> options, RealtimeUpdates? updates = null)
{
    public IQueryable<San> PublicCourts => db.Sans.AsNoTracking().Where(s=>s.DangHoatDong && s.CoSo.DangHoatDong && s.CoSo.TrangThaiDuyet=="DA_DUYET" && s.LoaiSan.MonTheThao.TrangThai);
    public async Task<CourtCalendar?> Calendar(long courtId, DateOnly date)
    {
        var court=await PublicCourts.Include(s=>s.CoSo).Include(s=>s.LoaiSan).ThenInclude(t=>t.MonTheThao).SingleOrDefaultAsync(s=>s.Id==courtId);
        if(court is null) return null;
        var now=await ServerNow(); var today=DateOnly.FromDateTime(now); var weekday=VietnamTime.Weekday(date);
        var hours=await db.LichHoatDongCoSos.AsNoTracking().SingleOrDefaultAsync(h=>h.CoSoId==court.CoSoId && h.ThuTrongTuan==weekday);
        var slots=await db.KhungGioMacDinhs.AsNoTracking().Where(k=>k.CoSoId==court.CoSoId).OrderBy(k=>k.GioBatDau).ToListAsync();
        var prices=await db.BangGia.AsNoTracking().Where(p=>p.SanId==courtId && p.DangApDung && p.ThuTrongTuan==weekday && p.NgayHieuLucTu<=date && (p.NgayHieuLucDen==null || p.NgayHieuLucDen>=date)).ToListAsync();
        var policy=await db.ChinhSachHuyDois.AsNoTracking().Include(p=>p.MucPhiHuyDois).SingleOrDefaultAsync(p=>p.CoSoId==court.CoSoId && p.NgayHieuLucTu<=today && (p.NgayHieuLucDen==null || p.NgayHieuLucDen>=today));
        bool validPolicy=false;
        if(policy!=null) { try { FacilityService.ValidateFees(policy.MucPhiHuyDois.Select(f=>new FeeForm { From=f.SoGioTuTruocTran,Until=f.SoGioDenTruocTran,CancelPercent=f.TiLePhiHuy,ChangePercent=f.TiLePhiDoi }).ToList()); validPolicy=true; } catch(BusinessException) { } }
        var locks=await db.LichKhoaSans.AsNoTracking().Where(l=>l.SanId==courtId && l.TrangThai=="DANG_AP_DUNG" && l.BatDauLuc<date.AddDays(1).ToDateTime(TimeOnly.MinValue) && l.KetThucLuc>date.ToDateTime(TimeOnly.MinValue)).ToListAsync();
        var bookings=await db.ChiTietDatSans.AsNoTracking().Include(c=>c.DonDatSan).Where(c=>c.SanId==courtId && c.NgaySuDung==date && (c.TrangThaiSlot=="DA_XAC_NHAN" || c.TrangThaiSlot=="DA_CHECK_IN" || c.TrangThaiSlot=="HOAN_THANH" || c.TrangThaiSlot=="VANG_MAT" || c.TrangThaiSlot=="GIU_CHO" && c.DonDatSan.ThoiGianHetHanGiuCho>now)).ToListAsync();
        var result=new List<AvailableSlot>();
        foreach(var k in slots)
        {
            var price=prices.SingleOrDefault(p=>p.KhungGioId==k.Id); var start=VietnamTime.Start(date,k.GioBatDau); var end=VietnamTime.End(date,k.GioKetThuc);
            var booking=bookings.FirstOrDefault(b=>VietnamTime.Start(date,b.GioBatDau)<end && start<VietnamTime.End(date,b.GioKetThuc));
            var status=booking!=null ? (booking.TrangThaiSlot=="GIU_CHO" ? "Đang giữ chỗ" : "Đã đặt") : court.DangBaoTri || locks.Any(l=>l.BatDauLuc<end && start<l.KetThucLuc) ? "Khóa / bảo trì" : hours?.NghiCaNgay==true || k.GioBatDau<(hours?.GioMoCua ?? court.CoSo.GioMoCua) || end>VietnamTime.End(date,hours?.GioDongCua ?? court.CoSo.GioDongCua) ? "Ngoài giờ / nghỉ" : start<=now ? "Đã qua giờ" : price==null ? "Chưa có giá" : !validPolicy ? "Chưa đủ chính sách" : "Trống";
            result.Add(new(k.Id,k.GioBatDau,k.GioKetThuc,price?.GiaThue,price?.TienCocToiThieu,status,status=="Trống"));
        }
        return new(court,date,now,result,policy);
    }
    public Task<DateTime> ServerNow() => db.Database.SqlQueryRaw<DateTime>("SELECT CONVERT(datetime2(0),DATEADD(HOUR,7,SYSUTCDATETIME())) AS Value").SingleAsync();
    public async Task<long> Book(long actor, BookingForm form, CancellationToken ct=default)
    {
        if(db.Database.CurrentTransaction!=null || System.Transactions.Transaction.Current!=null) throw new BusinessException("Đặt sân phải chạy ngoài transaction bao ngoài.");
        var user=await db.NguoiDungs.AsNoTracking().SingleOrDefaultAsync(u=>u.Id==actor && u.TrangThai) ?? throw new FacilityAccessException();
        if(form.Date==default || form.CourtId<=0 || form.SlotId<=0) throw new BusinessException("Chọn sân, ngày và khung giờ hợp lệ.");
        if(!await PublicCourts.AnyAsync(s=>s.Id==form.CourtId,ct)) throw new BusinessException("Sân hoặc môn thể thao hiện không được công khai nhận đặt.");
        // The stored procedure rechecks price, account, hours, policy and collisions under its lock.
        await db.Database.OpenConnectionAsync(ct);
        try
        {
            await using var command=db.Database.GetDbConnection().CreateCommand(); command.CommandText="dbo.usp_DatSanMotKhung"; command.CommandType=CommandType.StoredProcedure;
            command.Parameters.Add(new SqlParameter("@san_id",SqlDbType.BigInt) { Value=form.CourtId });
            command.Parameters.Add(new SqlParameter("@khung_gio_id",SqlDbType.Int) { Value=form.SlotId });
            command.Parameters.Add(new SqlParameter("@ngay_su_dung",SqlDbType.Date) { Value=form.Date.ToDateTime(TimeOnly.MinValue) });
            command.Parameters.Add(new SqlParameter("@ten_khach_hang",SqlDbType.NVarChar,100) { Value=user.HoVaTen });
            command.Parameters.Add(new SqlParameter("@sdt_khach_hang",SqlDbType.NVarChar,20) { Value=user.SoDienThoai });
            command.Parameters.Add(new SqlParameter("@nguoi_dung_id",SqlDbType.BigInt) { Value=actor });
            command.Parameters.Add(new SqlParameter("@nguon_dat",SqlDbType.NVarChar,30) { Value="APP" });
            command.Parameters.Add(new SqlParameter("@so_phut_giu_cho",SqlDbType.Int) { Value=options.Value.HoldMinutes });
            command.Parameters.Add(new SqlParameter("@ghi_chu",SqlDbType.NVarChar,-1) { Value=(object?)form.Note ?? DBNull.Value });
            await using var reader=await command.ExecuteReaderAsync(ct);
            if(!await reader.ReadAsync(ct)) throw new BusinessException("Không nhận được kết quả đặt sân. Hãy kiểm tra Đơn của tôi trước khi thử lại.");
            var id=reader.GetInt64(reader.GetOrdinal("don_dat_san_id"));
            await reader.DisposeAsync();
            if(updates is not null) await updates.Calendar(form.CourtId);
            return id;
        }
        finally { await db.Database.CloseConnectionAsync(); }
    }
    public async Task Expire(CancellationToken ct=default)
    {
        var now=await ServerNow();
        var courts=updates is null ? [] : await db.ChiTietDatSans.Where(c=>c.TrangThaiSlot=="GIU_CHO" && c.DonDatSan.ThoiGianHetHanGiuCho<=now).Select(c=>c.SanId).Distinct().ToListAsync(ct);
        await db.Database.ExecuteSqlRawAsync("EXEC dbo.usp_GiaiPhongGiuChoHetHan",ct);
        if(updates is not null) foreach(var court in courts) await updates.Calendar(court);
    }
}

public class HoldExpiryWorker(IServiceScopeFactory scopes, IOptions<BookingOptions> options, ILogger<HoldExpiryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer=new PeriodicTimer(TimeSpan.FromSeconds(options.Value.ExpiryIntervalSeconds));
        do
        {
            try { using var scope=scopes.CreateScope(); await scope.ServiceProvider.GetRequiredService<BookingService>().Expire(stoppingToken); }
            catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested) { break; }
            catch(Exception ex) { logger.LogError(ex,"Hold expiry failed; next scheduled run will retry"); }
        } while(await timer.WaitForNextTickAsync(stoppingToken));
    }
}
