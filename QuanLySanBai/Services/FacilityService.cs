using Microsoft.EntityFrameworkCore;
using QuanLySanBai.Data;
using QuanLySanBai.Models.Entities;
using QuanLySanBai.ViewModels;
namespace QuanLySanBai.Services;

public class FacilityService(QuanLySanBaiContext db, RealtimeUpdates? updates = null)
{
    public async Task<CoSo> Owned(long actor, long id, bool registrationOnly = false)
    {
        var c = await db.CoSos.SingleOrDefaultAsync(c => c.Id == id && c.ChuSoHuuId == actor);
        if(c is null || !registrationOnly && c.TrangThaiDuyet != "DA_DUYET") throw new FacilityAccessException();
        return c;
    }
    public async Task<long> SaveFacility(long actor, FacilityForm form)
    {
        if (!VietnamTime.ValidHours(form.Open, form.Close)) throw new BusinessException("Khoảng giờ mở cửa không hợp lệ.");
        if (string.IsNullOrWhiteSpace(form.Name) || string.IsNullOrWhiteSpace(form.Address)) throw new BusinessException("Nhập tên và địa chỉ cơ sở.");
        await using var tx = await db.Database.BeginTransactionAsync();
        var c = form.Id == 0 ? new CoSo { ChuSoHuuId = actor, TrangThaiDuyet = "CHO_DUYET", NgayTao = VietnamTime.Now } : await Owned(actor, form.Id, registrationOnly: true);
        if (form.Id == 0) db.CoSos.Add(c);
        else if (c.TrangThaiDuyet=="TU_CHOI" || c.TenCoSo != form.Name.Trim() || c.DiaChi != form.Address.Trim()) c.TrangThaiDuyet = "CHO_DUYET";
        c.TenCoSo = form.Name.Trim(); c.DiaChi = form.Address.Trim(); c.SoDienThoaiHotline = form.Phone;
        c.GioMoCua = form.Open; c.GioDongCua = form.Close; c.DangHoatDong = form.Active; c.NgayCapNhat = VietnamTime.Now;
        await db.SaveChangesAsync();
        if (form.Id == 0)
        {
            db.ThanhVienCoSos.Add(new ThanhVienCoSo { CoSoId = c.Id, NguoiDungId = actor, VaiTroNoiBo = "CHU_SAN", TrangThaiHoatDong = true, NgayThamGia = VietnamTime.Now });
            var role = await db.VaiTros.SingleAsync(r => r.MaVaiTro == "CHU_SAN");
            if (!await db.NguoiDungVaiTros.AnyAsync(r => r.NguoiDungId == actor && r.VaiTroId == role.Id)) db.NguoiDungVaiTros.Add(new NguoiDungVaiTro { NguoiDungId = actor, VaiTroId = role.Id, NgayGan = VietnamTime.Now });
            await db.SaveChangesAsync();
        }
        await tx.CommitAsync();
        if(updates is not null) {
            var users=await db.ThanhVienCoSos.Where(m=>m.CoSoId==c.Id).Select(m=>m.NguoiDungId).ToListAsync();
            foreach(var userId in users.Append(actor).Distinct()) await updates.Permissions(userId);
        }
        return c.Id;
    }
    public async Task SaveCourt(long actor, CourtForm form)
    {
        await Owned(actor, form.FacilityId);
        if (!await db.LoaiSans.AnyAsync(t => t.Id == form.TypeId && t.MonTheThao.TrangThai)) throw new BusinessException("Loại sân/môn thể thao không hoạt động.");
        var court = form.Id == 0 ? new San { CoSoId = form.FacilityId, NgayTao = VietnamTime.Now } : await db.Sans.SingleOrDefaultAsync(s => s.Id == form.Id && s.CoSoId == form.FacilityId) ?? throw new FacilityAccessException();
        if (form.Id == 0) db.Sans.Add(court);
        court.TenSan = form.Name.Trim(); court.LoaiSanId = form.TypeId; court.DangHoatDong = form.Active; court.DangBaoTri = form.Maintenance; court.NgayCapNhat = VietnamTime.Now;
        await db.SaveChangesAsync();
    }
    public async Task SaveHours(long actor, HoursForm form)
    {
        await Owned(actor, form.FacilityId);
        if (form.Weekday is < 1 or > 7 || !form.Closed && !VietnamTime.ValidHours(form.Open, form.Close)) throw new BusinessException("Thứ hoặc khoảng giờ không hợp lệ.");
        var hours = await db.LichHoatDongCoSos.SingleOrDefaultAsync(h => h.CoSoId == form.FacilityId && h.ThuTrongTuan == form.Weekday);
        if (hours is null) { hours = new LichHoatDongCoSo { CoSoId = form.FacilityId, ThuTrongTuan = form.Weekday }; db.LichHoatDongCoSos.Add(hours); }
        hours.GioMoCua = form.Open; hours.GioDongCua = form.Close; hours.NghiCaNgay = form.Closed;
        await db.SaveChangesAsync();
    }
    public async Task AddSlot(long actor, SlotForm form)
    {
        await Owned(actor, form.FacilityId);
        if (!VietnamTime.ValidHours(form.Start, form.End)) throw new BusinessException("Khung giờ không hợp lệ; 00:00 ở giờ kết thúc nghĩa là 24:00.");
        db.KhungGioMacDinhs.Add(new KhungGioMacDinh { CoSoId = form.FacilityId, GioBatDau = form.Start, GioKetThuc = form.End });
        await db.SaveChangesAsync();
    }
    public async Task AddPrice(long actor, PriceForm form)
    {
        await Owned(actor, form.FacilityId);
        if (!await db.Sans.AnyAsync(s => s.Id == form.CourtId && s.CoSoId == form.FacilityId) || !await db.KhungGioMacDinhs.AnyAsync(k => k.Id == form.SlotId && k.CoSoId == form.FacilityId)) throw new FacilityAccessException();
        if (form.Weekday is < 1 or > 7 || form.Price < 0 || form.Deposit < 0 || form.Deposit > form.Price || form.Until < form.From || decimal.Round(form.Price,2)!=form.Price || decimal.Round(form.Deposit,2)!=form.Deposit) throw new BusinessException("Giá, cọc, thứ hoặc ngày hiệu lực không hợp lệ. Tiền có tối đa hai chữ số thập phân.");
        db.BangGia.Add(new BangGium { SanId = form.CourtId, KhungGioId = form.SlotId, GiaThue = form.Price, TienCocToiThieu = form.Deposit, ThuTrongTuan = form.Weekday, NgayHieuLucTu = form.From, NgayHieuLucDen = form.Until, DangApDung = true });
        await db.SaveChangesAsync();
    }
    public async Task StopPrice(long actor, long facilityId, long id)
    {
        await Owned(actor, facilityId);
        var price = await db.BangGia.SingleOrDefaultAsync(p => p.Id == id && p.San.CoSoId == facilityId) ?? throw new FacilityAccessException();
        price.DangApDung = false; await db.SaveChangesAsync();
    }
    public static void ValidateFees(List<FeeForm> fees)
    {
        var ordered = fees.OrderBy(f => f.From).ToList();
        if (ordered.Count == 0 || ordered[0].From != 0 || ordered[^1].Until != null) throw new BusinessException("Mức phí phải bao phủ từ 0 giờ đến vô hạn.");
        for (var i=0; i<ordered.Count; i++)
        {
            var f=ordered[i];
            if (f.From < 0 || f.Until <= f.From || f.CancelPercent is < 0 or > 100 || f.ChangePercent is < 0 or > 100 || i < ordered.Count-1 && f.Until != ordered[i+1].From) throw new BusinessException("Các mức phí phải liên tục, không chồng lấn; tỷ lệ từ 0 đến 100%.");
        }
    }
    public async Task AddPolicy(long actor, PolicyForm form)
    {
        await Owned(actor, form.FacilityId); ValidateFees(form.Fees);
        if (form.Until < form.From) throw new BusinessException("Ngày hết hiệu lực phải sau ngày bắt đầu.");
        var policy = new ChinhSachHuyDoi { CoSoId = form.FacilityId, TenChinhSach = form.Name.Trim(), ChoPhepDoiLich = form.AllowChange, NgayHieuLucTu = form.From, NgayHieuLucDen = form.Until, NgayTao = VietnamTime.Now };
        foreach(var fee in form.Fees) policy.MucPhiHuyDois.Add(new MucPhiHuyDoi { SoGioTuTruocTran = fee.From, SoGioDenTruocTran = fee.Until, TiLePhiHuy = fee.CancelPercent, TiLePhiDoi = fee.ChangePercent });
        db.ChinhSachHuyDois.Add(policy); await db.SaveChangesAsync();
    }
    public async Task ClosePolicy(long actor, long facilityId, long id, DateOnly until)
    {
        await Owned(actor, facilityId);
        var policy = await db.ChinhSachHuyDois.SingleOrDefaultAsync(p => p.Id == id && p.CoSoId == facilityId) ?? throw new FacilityAccessException();
        if (until < policy.NgayHieuLucTu || until < VietnamTime.Today) throw new BusinessException("Chỉ đóng hiệu lực từ hôm nay trở đi, không trước ngày bắt đầu.");
        policy.NgayHieuLucDen = until; await db.SaveChangesAsync();
    }
    public async Task AddLock(long actor, LockForm form)
    {
        await Owned(actor, form.FacilityId);
        if (!await db.Sans.AnyAsync(s => s.Id == form.CourtId && s.CoSoId == form.FacilityId)) throw new FacilityAccessException();
        if (form.End <= form.Start) throw new BusinessException("Thời gian khóa không hợp lệ.");
        db.LichKhoaSans.Add(new LichKhoaSan { SanId = form.CourtId, BatDauLuc = form.Start, KetThucLuc = form.End, LyDo = form.Reason, NguoiTaoId = actor, TrangThai = "DANG_AP_DUNG", NgayTao = VietnamTime.Now });
        await db.SaveChangesAsync();
    }
    public async Task CancelLock(long actor, long facilityId, long id)
    {
        await Owned(actor, facilityId);
        var item = await db.LichKhoaSans.SingleOrDefaultAsync(l => l.Id == id && l.San.CoSoId == facilityId) ?? throw new FacilityAccessException();
        item.TrangThai = "DA_HUY"; await db.SaveChangesAsync();
    }
    public async Task<FacilityDashboard> Dashboard(long actor, long id)
    {
        return new FacilityDashboard { Facility = await Owned(actor,id), Courts = await db.Sans.Where(s=>s.CoSoId==id).Include(s=>s.LoaiSan).ToListAsync(), Slots=await db.KhungGioMacDinhs.Where(s=>s.CoSoId==id).OrderBy(s=>s.GioBatDau).ToListAsync(), Hours=await db.LichHoatDongCoSos.Where(h=>h.CoSoId==id).OrderBy(h=>h.ThuTrongTuan).ToListAsync(), Prices=await db.BangGia.Where(p=>p.San.CoSoId==id).Include(p=>p.KhungGio).Include(p=>p.San).ToListAsync(), Policies=await db.ChinhSachHuyDois.Where(p=>p.CoSoId==id).Include(p=>p.MucPhiHuyDois).ToListAsync(), Locks=await db.LichKhoaSans.Where(l=>l.San.CoSoId==id).Include(l=>l.San).ToListAsync() };
    }
}
