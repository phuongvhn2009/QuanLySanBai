using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Data.SqlClient;
using System.Data;
using QuanLySanBai.Data;
using QuanLySanBai.Models.Entities;
using QuanLySanBai.ViewModels;
namespace QuanLySanBai.Services;

public class OperationService(QuanLySanBaiContext db,AccessService access,TimeProvider clock,IOptions<OperationOptions> options)
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime.AddHours(7);
    public async Task Require(long actor,long facility,string right)
    {
        if(!await db.NguoiDungs.AsNoTracking().AnyAsync(u=>u.Id==actor && u.TrangThai) || !await access.HasPermission(actor,facility,right)) throw new FacilityAccessException();
    }
    private async Task Locked(Func<Task> work)
    {
        if(db.ChangeTracker.HasChanges()) throw new BusinessException("Có dữ liệu chưa lưu. Hãy tải lại trang.");
        db.ChangeTracker.Clear();
        await using var tx=await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'QLSB:LICH_SAN',@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=10000; IF @r<0 THROW 51305,N'Lịch đang được thao tác, hãy thử lại.',1;");
        await work(); await db.SaveChangesAsync(); await tx.CommitAsync();
    }
    private async Task<DonDatSan> Load(long id) => await db.DonDatSans.Include(d=>d.ChiTietDatSans).SingleOrDefaultAsync(d=>d.Id==id) ?? throw new BusinessException("Không tìm thấy đơn.");
    private static void Version(DonDatSan d,string version)
    {
        if(Convert.ToBase64String(d.RowVersion)!=version) throw new BusinessException("Đơn đã thay đổi. Tải lại trang trước khi thao tác.");
    }
    private async Task<decimal> Paid(long id) => await db.ThanhToanDons.Where(p=>p.DonDatSanId==id && p.TrangThai=="THANH_CONG").SumAsync(p=>(decimal?)p.SoTien) ?? 0m;
    private static void Transition(DonDatSan d,string order,string slot,DateTime now)
    { d.TrangThai=order;d.NgayCapNhat=now;foreach(var c in d.ChiTietDatSans){c.TrangThaiSlot=slot;c.NgayCapNhat=now;} }
    private async Task ConfirmPending(DonDatSan d,decimal paid,DateTime now)
    {
        if(d.TrangThai!="CHO_THANH_TOAN" || d.ChiTietDatSans.Count!=1 || d.ChiTietDatSans.Any(c=>c.TrangThaiSlot!="GIU_CHO")) throw new BusinessException("Đơn không còn ở trạng thái giữ chỗ.");
        if(d.ThoiGianHetHanGiuCho is null || d.ThoiGianHetHanGiuCho<=now) throw new BusinessException("Đã hết hạn giữ chỗ; không được nhận tiền/xác nhận đơn này.");
        if(paid<d.TienCocYeuCau) throw new BusinessException("Lần thu đầu phải đạt mức cọc yêu cầu để xác nhận đơn.");
        Transition(d,"DA_XAC_NHAN","DA_XAC_NHAN",now); await Task.CompletedTask;
    }
    public Task Collect(long actor,CollectForm f) => Locked(async()=>
    {
        var d=await Load(f.Id);await Require(actor,d.CoSoId,"THU_TIEN");
        if(f.Amount<=0 || f.Amount>999999999999.99m || decimal.Round(f.Amount,2)!=f.Amount || f.Method is not("TIEN_MAT" or "CHUYEN_KHOAN_TAI_QUAY") || f.Purpose is not("COC" or "THANH_TOAN" or "BO_SUNG") || string.IsNullOrWhiteSpace(f.Key) || f.Key.Length>100 || f.Reference?.Length>100) throw new BusinessException("Thông tin khoản thu không hợp lệ.");
        var prior=await db.ThanhToanDons.SingleOrDefaultAsync(p=>p.KhoaChongLap==f.Key);
        if(prior!=null) { if(prior.DonDatSanId!=d.Id || prior.NhanVienThuId!=actor || prior.SoTien!=f.Amount || prior.PhuongThuc!=f.Method || prior.MucDich!=f.Purpose || prior.MaThamChieuNgoai!=f.Reference) throw new BusinessException("Khóa thu tiền đã được dùng cho yêu cầu khác.");return; }
        Version(d,f.Version);
        if(d.TrangThai is not("CHO_THANH_TOAN" or "DA_XAC_NHAN" or "DANG_SU_DUNG")) throw new BusinessException("Không thể thu tiền ở trạng thái hiện tại.");
        var paid=await Paid(d.Id);if(paid+f.Amount>d.TongTienPhaiTra) throw new BusinessException("Khoản thu vượt số tiền còn phải trả.");
        var now=await db.Database.SqlQueryRaw<DateTime>("SELECT CONVERT(datetime2(0),DATEADD(HOUR,7,SYSUTCDATETIME())) AS Value").SingleAsync();
        if(d.TrangThai=="CHO_THANH_TOAN") await ConfirmPending(d,paid+f.Amount,now);
        else { d.NgayCapNhat=now;db.Entry(d).Property(x=>x.NgayCapNhat).IsModified=true; }
        db.ThanhToanDons.Add(new ThanhToanDon { MaThanhToan=Guid.NewGuid().ToString("N"),DonDatSanId=d.Id,NguoiThanhToanId=d.NguoiDungId,NhanVienThuId=actor,PhuongThuc=f.Method,MucDich=f.Purpose,SoTien=f.Amount,MaThamChieuNgoai=f.Reference,KhoaChongLap=f.Key,TrangThai="THANH_CONG",NgayTao=now,NgayThanhToan=now });
    });
    public Task Confirm(long actor,OrderActionForm f) => Locked(async()=> { var d=await Load(f.Id);await Require(actor,d.CoSoId,"THU_TIEN");Version(d,f.Version);var now=await db.Database.SqlQueryRaw<DateTime>("SELECT CONVERT(datetime2(0),DATEADD(HOUR,7,SYSUTCDATETIME())) AS Value").SingleAsync(); await ConfirmPending(d,await Paid(d.Id),now); });
    public Task CheckIn(long actor,OrderActionForm f) => Locked(async()=>
    {
        var d=await Load(f.Id);await Require(actor,d.CoSoId,"CHECK_IN");Version(d,f.Version);
        if(d.TrangThai!="DA_XAC_NHAN" || d.ChiTietDatSans.Count!=1 || d.ChiTietDatSans.Any(c=>c.TrangThaiSlot!="DA_XAC_NHAN")) throw new BusinessException("Chỉ check-in đơn đã xác nhận và chưa check-in.");
        if(string.IsNullOrWhiteSpace(f.Code) || !(string.Equals(f.Code.Trim(),d.MaDatSan,StringComparison.Ordinal) || string.Equals(f.Code.Trim(),d.MaQrCheckin,StringComparison.Ordinal))) throw new BusinessException("Mã đặt sân/QR không đúng đơn.");
        var c=d.ChiTietDatSans.Single();var now=Now;
        if(now<VietnamTime.Start(c.NgaySuDung,c.GioBatDau).AddMinutes(-options.Value.CheckInEarlyMinutes) || now>=VietnamTime.End(c.NgaySuDung,c.GioKetThuc)) throw new BusinessException("Ngoài thời gian được check-in.");
        db.LichSuCheckIns.Add(new LichSuCheckIn { ChiTietDatSanId=c.Id,NhanVienCheckinId=actor,ThoiGianCheckin=now,PhuongThuc=f.Code.Trim()==d.MaQrCheckin ? "QR":"THU_CONG" });
        Transition(d,"DANG_SU_DUNG","DA_CHECK_IN",now);
    });
    public Task Complete(long actor,OrderActionForm f) => Locked(async()=>
    {
        var d=await Load(f.Id);await Require(actor,d.CoSoId,"CHECK_IN");Version(d,f.Version);
        if(d.TrangThai!="DANG_SU_DUNG" || d.ChiTietDatSans.Count!=1 || d.ChiTietDatSans.Any(c=>c.TrangThaiSlot!="DA_CHECK_IN")) throw new BusinessException("Đơn chưa check-in hoặc đã kết thúc.");
        if(d.ChiTietDatSans.Any(c=>VietnamTime.End(c.NgaySuDung,c.GioKetThuc)>Now)) throw new BusinessException("Chưa đến giờ kết thúc buổi.");
        if(await Paid(d.Id)<d.TongTienPhaiTra) throw new BusinessException("Chưa thu đủ tiền.");
        Transition(d,"HOAN_THANH","HOAN_THANH",Now);
    });
    public Task Cancel(long actor,OrderActionForm f) => Locked(async()=>
    {
        var d=await Load(f.Id);if(d.NguoiDungId!=actor) await Require(actor,d.CoSoId,"DOI_HUY_DON");else if(!await db.NguoiDungs.AnyAsync(u=>u.Id==actor && u.TrangThai)) throw new FacilityAccessException();
        Version(d,f.Version);
        if(d.TrangThai is not("CHO_THANH_TOAN" or "DA_XAC_NHAN") || string.IsNullOrWhiteSpace(f.Reason) || f.Reason.Length>1000) throw new BusinessException("Đơn không thể hủy hoặc chưa nhập lý do hợp lệ.");
        if(await db.ThanhToanDons.AnyAsync(p=>p.DonDatSanId==d.Id && (p.TrangThai=="THANH_CONG" || p.TrangThai=="DANG_XU_LY"))) throw new BusinessException("Đơn đã thu tiền hoặc đang xử lý tiền; phải đi qua quy trình hoàn tiền.");
        if(d.ChiTietDatSans.Any(c=>VietnamTime.Start(c.NgaySuDung,c.GioBatDau)<=Now)) throw new BusinessException("Đã đến giờ sử dụng, không thể hủy theo luồng chưa thu tiền.");
        foreach(var c in d.ChiTietDatSans) db.LichSuHuySans.Add(new LichSuHuySan { ChiTietDatSanId=c.Id,NguoiYeuCauId=actor,NguoiXuLyId=actor,SoGioTruocTran=decimal.Round((decimal)(VietnamTime.Start(c.NgaySuDung,c.GioBatDau)-Now).TotalHours,2),LyDoHuy=f.Reason.Trim(),NgayHuy=Now });
        Transition(d,"DA_HUY","DA_HUY",Now);d.TongTienSan=0;d.TongTienDichVu=0;d.TongPhiPhat=0;d.TienGiamGia=0;d.TienCocYeuCau=0;
    });
    public async Task<long> Counter(long actor,CounterForm f)
    {
        await Require(actor,f.FacilityId,"TAO_DON");
        if(!await db.Sans.AnyAsync(s=>s.Id==f.CourtId && s.CoSoId==f.FacilityId) || string.IsNullOrWhiteSpace(f.Name) || f.Name.Length>100 || !System.Text.RegularExpressions.Regex.IsMatch(f.Phone??"",@"^0[0-9]{9}$")) throw new BusinessException("Sân hoặc thông tin khách không hợp lệ.");
        await db.Database.OpenConnectionAsync();try {
            await using var cmd=db.Database.GetDbConnection().CreateCommand();cmd.CommandText="dbo.usp_DatSanMotKhung";cmd.CommandType=CommandType.StoredProcedure;
            foreach(var p in new[] { new SqlParameter("@san_id",SqlDbType.BigInt){Value=f.CourtId},new SqlParameter("@khung_gio_id",SqlDbType.Int){Value=f.SlotId},new SqlParameter("@ngay_su_dung",SqlDbType.Date){Value=f.Date.ToDateTime(TimeOnly.MinValue)},new SqlParameter("@ten_khach_hang",SqlDbType.NVarChar,100){Value=f.Name.Trim()},new SqlParameter("@sdt_khach_hang",SqlDbType.NVarChar,20){Value=f.Phone},new SqlParameter("@nhan_vien_tao_id",SqlDbType.BigInt){Value=actor},new SqlParameter("@nguon_dat",SqlDbType.NVarChar,30){Value="TAI_QUAY"} }) cmd.Parameters.Add(p);
            await using var reader=await cmd.ExecuteReaderAsync();if(!await reader.ReadAsync())throw new BusinessException("Chưa nhận kết quả, kiểm tra danh sách đơn trước khi đặt lại.");return reader.GetInt64(0);
        } finally { await db.Database.CloseConnectionAsync(); }
    }
    public async Task<OperationDetails> Details(long actor,long id)
    {
        var d=await db.DonDatSans.AsNoTracking().Include(d=>d.ChiTietDatSans).ThenInclude(c=>c.San).Include(d=>d.ThanhToanDons).Include(d=>d.ChiTietDatSans).ThenInclude(c=>c.LichSuCheckIns).Include(d=>d.ChiTietDatSans).ThenInclude(c=>c.LichSuHuySan).SingleOrDefaultAsync(d=>d.Id==id) ?? throw new BusinessException("Không tìm thấy đơn.");
        await Require(actor,d.CoSoId,"XEM_LICH");return new() { Order=d,Paid=await Paid(id),Collect=await access.HasPermission(actor,d.CoSoId,"THU_TIEN"),CheckIn=await access.HasPermission(actor,d.CoSoId,"CHECK_IN"),Cancel=await access.HasPermission(actor,d.CoSoId,"DOI_HUY_DON") };
    }
}

