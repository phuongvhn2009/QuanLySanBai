using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuanLySanBai.Data;
using QuanLySanBai.Models.Entities;
using QuanLySanBai.ViewModels;
namespace QuanLySanBai.Services;

public class DevelopmentSeeder(QuanLySanBaiContext db, IPasswordHasher<NguoiDung> hasher)
{
    public async Task<string> Seed()
    {
        const string marker="owner@qlsb.example.test";
        if(await db.NguoiDungs.AnyAsync(u=>u.Email==marker)) return "Demo đã tồn tại; không đổi mật khẩu hoặc tạo bản sao. Xem .local/demo-accounts.txt đã tạo ở lần đầu.";
        var credentials=new List<string>();
        // One transaction covers users, ownership, approval and all sellable configuration.
        await using var tx=await db.Database.BeginTransactionAsync();
        async Task<NguoiDung> User(string phone,string email,string role)
        {
            var password=Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(12))+"a!";
            var u=new NguoiDung { SoDienThoai=phone, Email=email, HoVaTen="Demo "+role, TrangThai=true, NgayTao=VietnamTime.Now, NgayCapNhat=VietnamTime.Now };
            u.MatKhauHash=hasher.HashPassword(u,password);
            u.NguoiDungVaiTros.Add(new NguoiDungVaiTro { VaiTroId=(await db.VaiTros.SingleAsync(r=>r.MaVaiTro==role)).Id, NgayGan=VietnamTime.Now });
            db.NguoiDungs.Add(u); await db.SaveChangesAsync(); credentials.Add($"{role}: {phone} | {email} | {password}"); return u;
        }
        await User("0991111101","admin@qlsb.example.test","ADMIN");
        var owner=await User("0991111102",marker,"CHU_SAN");
        await User("0991111103","customer@qlsb.example.test","KHACH_HANG");
        var c=new CoSo { ChuSoHuuId=owner.Id, TenCoSo="Cơ sở Demo", DiaChi="TP. Hồ Chí Minh — dữ liệu phát triển", GioMoCua=new(6,0), GioDongCua=TimeOnly.MinValue, TrangThaiDuyet="DA_DUYET", DangHoatDong=true, NgayTao=VietnamTime.Now, NgayCapNhat=VietnamTime.Now };
        db.CoSos.Add(c); await db.SaveChangesAsync();
        db.ThanhVienCoSos.Add(new ThanhVienCoSo { CoSoId=c.Id,NguoiDungId=owner.Id,VaiTroNoiBo="CHU_SAN",TrangThaiHoatDong=true,NgayThamGia=VietnamTime.Now });
        var sport=await db.MonTheThaos.FirstAsync(m=>m.MaMon=="CAU_LONG");
        var type=new LoaiSan { MonTheThaoId=sport.Id,TenLoai="Cầu lông demo",SucChuaNguoi=4 }; db.LoaiSans.Add(type); await db.SaveChangesAsync();
        var court=new San { CoSoId=c.Id,LoaiSanId=type.Id,TenSan="Sân demo 1",DangHoatDong=true,NgayTao=VietnamTime.Now,NgayCapNhat=VietnamTime.Now }; db.Sans.Add(court); await db.SaveChangesAsync();
        db.KhungGioMacDinhs.AddRange(new KhungGioMacDinh { CoSoId=c.Id,GioBatDau=new(18,0),GioKetThuc=new(19,0) },new KhungGioMacDinh { CoSoId=c.Id,GioBatDau=new(23,0),GioKetThuc=TimeOnly.MinValue }); await db.SaveChangesAsync();
        foreach(var slot in await db.KhungGioMacDinhs.Where(k=>k.CoSoId==c.Id).ToListAsync()) for(int day=1;day<=7;day++) db.BangGia.Add(new BangGium { SanId=court.Id,KhungGioId=slot.Id,ThuTrongTuan=day,GiaThue=150000,TienCocToiThieu=50000,NgayHieuLucTu=VietnamTime.Today,DangApDung=true });
        db.ChinhSachHuyDois.Add(new ChinhSachHuyDoi { CoSoId=c.Id,TenChinhSach="Demo — không thu phí hủy/đổi",ChoPhepDoiLich=true,NgayHieuLucTu=VietnamTime.Today,NgayTao=VietnamTime.Now,MucPhiHuyDois=[new MucPhiHuyDoi { SoGioTuTruocTran=0,SoGioDenTruocTran=null,TiLePhiHuy=0,TiLePhiDoi=0 }] });
        await db.SaveChangesAsync(); await tx.CommitAsync();
        return "Tài khoản chỉ dùng ở môi trường Development. Mật khẩu được tạo ngẫu nhiên.\n"+string.Join("\n",credentials);
    }
}
