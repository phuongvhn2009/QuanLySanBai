using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using QuanLySanBai.Models.Entities;

namespace QuanLySanBai.Data;

public partial class QuanLySanBaiContext : DbContext
{
    public QuanLySanBaiContext(DbContextOptions<QuanLySanBaiContext> options)
        : base(options)
    {
    }

    public virtual DbSet<BangGium> BangGia { get; set; }

    public virtual DbSet<BaoCaoHieuSuatSan> BaoCaoHieuSuatSans { get; set; }

    public virtual DbSet<CauHinhHoaHong> CauHinhHoaHongs { get; set; }

    public virtual DbSet<ChiTietDatSan> ChiTietDatSans { get; set; }

    public virtual DbSet<ChiTietDichVuDon> ChiTietDichVuDons { get; set; }

    public virtual DbSet<ChiTietDoiSoatDon> ChiTietDoiSoatDons { get; set; }

    public virtual DbSet<ChinhSachHuyDoi> ChinhSachHuyDois { get; set; }

    public virtual DbSet<CoSo> CoSos { get; set; }

    public virtual DbSet<DanhGiaCoSo> DanhGiaCoSos { get; set; }

    public virtual DbSet<DichVu> DichVus { get; set; }

    public virtual DbSet<DieuChinhDoiSoat> DieuChinhDoiSoats { get; set; }

    public virtual DbSet<DonDatSan> DonDatSans { get; set; }

    public virtual DbSet<DongGopPhong> DongGopPhongs { get; set; }

    public virtual DbSet<GiaoDichCongThanhToan> GiaoDichCongThanhToans { get; set; }

    public virtual DbSet<GiaoDichVi> GiaoDichVis { get; set; }

    public virtual DbSet<HoSoTrinhDo> HoSoTrinhDos { get; set; }

    public virtual DbSet<HoanTien> HoanTiens { get; set; }

    public virtual DbSet<KetQuaTranDau> KetQuaTranDaus { get; set; }

    public virtual DbSet<KhungGioMacDinh> KhungGioMacDinhs { get; set; }

    public virtual DbSet<KyDoiSoat> KyDoiSoats { get; set; }

    public virtual DbSet<LichDatCoDinh> LichDatCoDinhs { get; set; }

    public virtual DbSet<LichHoatDongCoSo> LichHoatDongCoSos { get; set; }

    public virtual DbSet<LichKhoaSan> LichKhoaSans { get; set; }

    public virtual DbSet<LichSuCheckIn> LichSuCheckIns { get; set; }

    public virtual DbSet<LichSuDungVoucher> LichSuDungVouchers { get; set; }

    public virtual DbSet<LichSuElo> LichSuElos { get; set; }

    public virtual DbSet<LichSuHuySan> LichSuHuySans { get; set; }

    public virtual DbSet<LoaiSan> LoaiSans { get; set; }

    public virtual DbSet<MonTheThao> MonTheThaos { get; set; }

    public virtual DbSet<MucPhiHuyDoi> MucPhiHuyDois { get; set; }

    public virtual DbSet<NapTienVi> NapTienVis { get; set; }

    public virtual DbSet<NguoiDung> NguoiDungs { get; set; }

    public virtual DbSet<NguoiDungVaiTro> NguoiDungVaiTros { get; set; }

    public virtual DbSet<NhatKyViPham> NhatKyViPhams { get; set; }

    public virtual DbSet<PhanQuyenThanhVien> PhanQuyenThanhViens { get; set; }

    public virtual DbSet<PhongGhepTran> PhongGhepTrans { get; set; }

    public virtual DbSet<QuyenHan> QuyenHans { get; set; }

    public virtual DbSet<San> Sans { get; set; }

    public virtual DbSet<ThanhToanDon> ThanhToanDons { get; set; }

    public virtual DbSet<ThanhVienCoSo> ThanhVienCoSos { get; set; }

    public virtual DbSet<ThanhVienPhong> ThanhVienPhongs { get; set; }

    public virtual DbSet<ThongBao> ThongBaos { get; set; }

    public virtual DbSet<VCongNoDon> VCongNoDons { get; set; }

    public virtual DbSet<VLichSan> VLichSans { get; set; }

    public virtual DbSet<VaiTro> VaiTros { get; set; }

    public virtual DbSet<ViNguoiDung> ViNguoiDungs { get; set; }

    public virtual DbSet<Voucher> Vouchers { get; set; }

    public virtual DbSet<VoucherNguoiDung> VoucherNguoiDungs { get; set; }

    public virtual DbSet<YeuCauDoiLich> YeuCauDoiLiches { get; set; }

    public virtual DbSet<YeuCauRutTien> YeuCauRutTiens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BangGium>(entity =>
        {
            entity.ToTable("Bang_Gia", tb => tb.HasTrigger("TR_BangGia_KiemTra"));

            entity.HasIndex(e => e.KhungGioId, "IX_Bang_Gia_khung_gio_id");

            entity.HasIndex(e => new { e.SanId, e.KhungGioId, e.ThuTrongTuan, e.NgayHieuLucTu }, "IX_Bang_Gia_san_id_khung_gio_id_thu_trong_tuan_ngay_hieu_luc_tu");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DangApDung)
                .HasDefaultValue(true, "DF_Bang_Gia_dang_ap_dung")
                .HasColumnName("dang_ap_dung");
            entity.Property(e => e.GiaThue)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("gia_thue");
            entity.Property(e => e.KhungGioId).HasColumnName("khung_gio_id");
            entity.Property(e => e.NgayHieuLucDen).HasColumnName("ngay_hieu_luc_den");
            entity.Property(e => e.NgayHieuLucTu).HasColumnName("ngay_hieu_luc_tu");
            entity.Property(e => e.SanId).HasColumnName("san_id");
            entity.Property(e => e.ThuTrongTuan).HasColumnName("thu_trong_tuan");
            entity.Property(e => e.TienCocToiThieu)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("tien_coc_toi_thieu");

            entity.HasOne(d => d.KhungGio).WithMany(p => p.BangGia)
                .HasForeignKey(d => d.KhungGioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bang_Gia_khung_gio_id");

            entity.HasOne(d => d.San).WithMany(p => p.BangGia)
                .HasForeignKey(d => d.SanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bang_Gia_san_id");
        });

        modelBuilder.Entity<BaoCaoHieuSuatSan>(entity =>
        {
            entity.ToTable("Bao_Cao_Hieu_Suat_San");

            entity.HasIndex(e => new { e.SanId, e.NgayBaoCao }, "UX_Bao_Cao_Hieu_Suat_San_san_id_ngay_bao_cao").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DoanhThuNgay)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("doanh_thu_ngay");
            entity.Property(e => e.NgayBaoCao).HasColumnName("ngay_bao_cao");
            entity.Property(e => e.SanId).HasColumnName("san_id");
            entity.Property(e => e.SoSlotDaDat).HasColumnName("so_slot_da_dat");
            entity.Property(e => e.SoSlotHuy).HasColumnName("so_slot_huy");
            entity.Property(e => e.SoSlotTrong)
                .HasComputedColumnSql("([tong_so_slot_trong_ngay]-[so_slot_da_dat])", true)
                .HasColumnName("so_slot_trong");
            entity.Property(e => e.TongSoSlotTrongNgay).HasColumnName("tong_so_slot_trong_ngay");
            entity.Property(e => e.TyLeLapDay)
                .HasComputedColumnSql("(CONVERT([decimal](5,2),case when [tong_so_slot_trong_ngay]=(0) then (0) else ([so_slot_da_dat]*(100.0))/[tong_so_slot_trong_ngay] end))", true)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("ty_le_lap_day");

            entity.HasOne(d => d.San).WithMany(p => p.BaoCaoHieuSuatSans)
                .HasForeignKey(d => d.SanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bao_Cao_Hieu_Suat_San_san_id");
        });

        modelBuilder.Entity<CauHinhHoaHong>(entity =>
        {
            entity.ToTable("Cau_Hinh_Hoa_Hong");

            entity.HasIndex(e => e.CoSoId, "IX_Cau_Hinh_Hoa_Hong_co_so_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CoSoId).HasColumnName("co_so_id");
            entity.Property(e => e.NgayApDung).HasColumnName("ngay_ap_dung");
            entity.Property(e => e.NgayHetHieuLuc).HasColumnName("ngay_het_hieu_luc");
            entity.Property(e => e.PhiCoDinhMoiDon)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("phi_co_dinh_moi_don");
            entity.Property(e => e.TiLeHoaHong)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("ti_le_hoa_hong");
            entity.Property(e => e.TrangThai)
                .HasDefaultValue(true, "DF_Cau_Hinh_Hoa_Hong_trang_thai")
                .HasColumnName("trang_thai");

            entity.HasOne(d => d.CoSo).WithMany(p => p.CauHinhHoaHongs)
                .HasForeignKey(d => d.CoSoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Cau_Hinh_Hoa_Hong_co_so_id");
        });

        modelBuilder.Entity<ChiTietDatSan>(entity =>
        {
            entity.ToTable("Chi_Tiet_Dat_San", tb => tb.HasTrigger("TR_ChiTietDatSan_KiemTra"));

            entity.HasIndex(e => e.BangGiaId, "IX_Chi_Tiet_Dat_San_bang_gia_id");

            entity.HasIndex(e => e.ChinhSachHuyDoiId, "IX_Chi_Tiet_Dat_San_chinh_sach_huy_doi_id");

            entity.HasIndex(e => e.DonDatSanId, "IX_Chi_Tiet_Dat_San_don_dat_san_id");

            entity.HasIndex(e => e.KhungGioId, "IX_Chi_Tiet_Dat_San_khung_gio_id");

            entity.HasIndex(e => new { e.SanId, e.NgaySuDung, e.GioBatDau }, "IX_Chi_Tiet_Dat_San_san_id_ngay_su_dung_gio_bat_dau");

            entity.HasIndex(e => e.ChiTietThayTheChoId, "UX_Chi_Tiet_Dat_San_chi_tiet_thay_the_cho_id")
                .IsUnique()
                .HasFilter("([chi_tiet_thay_the_cho_id] IS NOT NULL)");

            entity.HasIndex(e => new { e.LichDatCoDinhId, e.NgaySuDung }, "UX_Chi_Tiet_Dat_San_lich_dat_co_dinh_id_ngay_su_dung")
                .IsUnique()
                .HasFilter("([lich_dat_co_dinh_id] IS NOT NULL AND ([trang_thai_slot] IN (N'GIU_CHO', N'DA_XAC_NHAN', N'DA_CHECK_IN', N'HOAN_THANH', N'VANG_MAT')))");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BangGiaId).HasColumnName("bang_gia_id");
            entity.Property(e => e.ChiTietThayTheChoId).HasColumnName("chi_tiet_thay_the_cho_id");
            entity.Property(e => e.ChinhSachHuyDoiId).HasColumnName("chinh_sach_huy_doi_id");
            entity.Property(e => e.DonDatSanId).HasColumnName("don_dat_san_id");
            entity.Property(e => e.GiaThue)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("gia_thue");
            entity.Property(e => e.GioBatDau)
                .HasPrecision(0)
                .HasColumnName("gio_bat_dau");
            entity.Property(e => e.GioKetThuc)
                .HasPrecision(0)
                .HasColumnName("gio_ket_thuc");
            entity.Property(e => e.KhungGioId).HasColumnName("khung_gio_id");
            entity.Property(e => e.LichDatCoDinhId).HasColumnName("lich_dat_co_dinh_id");
            entity.Property(e => e.NgayCapNhat)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Chi_Tiet_Dat_San_ngay_cap_nhat")
                .HasColumnName("ngay_cap_nhat");
            entity.Property(e => e.NgaySuDung).HasColumnName("ngay_su_dung");
            entity.Property(e => e.NgayTao)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Chi_Tiet_Dat_San_ngay_tao")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.SanId).HasColumnName("san_id");
            entity.Property(e => e.TrangThaiSlot)
                .HasMaxLength(30)
                .HasDefaultValue("GIU_CHO", "DF_Chi_Tiet_Dat_San_trang_thai_slot")
                .HasColumnName("trang_thai_slot");

            entity.HasOne(d => d.BangGia).WithMany(p => p.ChiTietDatSans)
                .HasForeignKey(d => d.BangGiaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Chi_Tiet_Dat_San_bang_gia_id");

            entity.HasOne(d => d.ChiTietThayTheCho).WithOne(p => p.InverseChiTietThayTheCho)
                .HasForeignKey<ChiTietDatSan>(d => d.ChiTietThayTheChoId)
                .HasConstraintName("FK_Chi_Tiet_Dat_San_chi_tiet_thay_the_cho_id");

            entity.HasOne(d => d.ChinhSachHuyDoi).WithMany(p => p.ChiTietDatSans)
                .HasForeignKey(d => d.ChinhSachHuyDoiId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Chi_Tiet_Dat_San_chinh_sach_huy_doi_id");

            entity.HasOne(d => d.DonDatSan).WithMany(p => p.ChiTietDatSans)
                .HasForeignKey(d => d.DonDatSanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Chi_Tiet_Dat_San_don_dat_san_id");

            entity.HasOne(d => d.KhungGio).WithMany(p => p.ChiTietDatSans)
                .HasForeignKey(d => d.KhungGioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Chi_Tiet_Dat_San_khung_gio_id");

            entity.HasOne(d => d.LichDatCoDinh).WithMany(p => p.ChiTietDatSans)
                .HasForeignKey(d => d.LichDatCoDinhId)
                .HasConstraintName("FK_Chi_Tiet_Dat_San_lich_dat_co_dinh_id");

            entity.HasOne(d => d.San).WithMany(p => p.ChiTietDatSans)
                .HasForeignKey(d => d.SanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Chi_Tiet_Dat_San_san_id");
        });

        modelBuilder.Entity<ChiTietDichVuDon>(entity =>
        {
            entity.ToTable("Chi_Tiet_Dich_Vu_Don", tb => tb.HasTrigger("TR_DichVuDon_CungCoSo"));

            entity.HasIndex(e => e.DichVuId, "IX_Chi_Tiet_Dich_Vu_Don_dich_vu_id");

            entity.HasIndex(e => e.DonDatSanId, "IX_Chi_Tiet_Dich_Vu_Don_don_dat_san_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DichVuId).HasColumnName("dich_vu_id");
            entity.Property(e => e.DonDatSanId).HasColumnName("don_dat_san_id");
            entity.Property(e => e.DonGiaLucBan)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("don_gia_luc_ban");
            entity.Property(e => e.SoLuong).HasColumnName("so_luong");
            entity.Property(e => e.ThanhTien)
                .HasComputedColumnSql("(CONVERT([decimal](14,2),CONVERT([decimal](14,2),[so_luong])*[don_gia_luc_ban]))", true)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("thanh_tien");

            entity.HasOne(d => d.DichVu).WithMany(p => p.ChiTietDichVuDons)
                .HasForeignKey(d => d.DichVuId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Chi_Tiet_Dich_Vu_Don_dich_vu_id");

            entity.HasOne(d => d.DonDatSan).WithMany(p => p.ChiTietDichVuDons)
                .HasForeignKey(d => d.DonDatSanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Chi_Tiet_Dich_Vu_Don_don_dat_san_id");
        });

        modelBuilder.Entity<ChiTietDoiSoatDon>(entity =>
        {
            entity.ToTable("Chi_Tiet_Doi_Soat_Don");

            entity.HasIndex(e => e.CauHinhHoaHongId, "IX_Chi_Tiet_Doi_Soat_Don_cau_hinh_hoa_hong_id");

            entity.HasIndex(e => e.KyDoiSoatId, "IX_Chi_Tiet_Doi_Soat_Don_ky_doi_soat_id");

            entity.HasIndex(e => e.DonDatSanId, "UX_Chi_Tiet_Doi_Soat_Don_don_dat_san_id").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CauHinhHoaHongId).HasColumnName("cau_hinh_hoa_hong_id");
            entity.Property(e => e.DonDatSanId).HasColumnName("don_dat_san_id");
            entity.Property(e => e.KyDoiSoatId).HasColumnName("ky_doi_soat_id");
            entity.Property(e => e.PhiCoDinhApDung)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("phi_co_dinh_ap_dung");
            entity.Property(e => e.PhiHoaHongDon)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("phi_hoa_hong_don");
            entity.Property(e => e.SoTienQuyetToan)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("so_tien_quyet_toan");
            entity.Property(e => e.TiLeHoaHongApDung)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("ti_le_hoa_hong_ap_dung");
            entity.Property(e => e.TienHoanApp)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("tien_hoan_app");
            entity.Property(e => e.TienKhachTraApp)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("tien_khach_tra_app");
            entity.Property(e => e.TienThuTaiQuay)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("tien_thu_tai_quay");
            entity.Property(e => e.TongTienDon)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("tong_tien_don");
            entity.Property(e => e.TroGiaSanPhaiTraCoSo)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("tro_gia_san_phai_tra_co_so");

            entity.HasOne(d => d.CauHinhHoaHong).WithMany(p => p.ChiTietDoiSoatDons)
                .HasForeignKey(d => d.CauHinhHoaHongId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Chi_Tiet_Doi_Soat_Don_cau_hinh_hoa_hong_id");

            entity.HasOne(d => d.DonDatSan).WithOne(p => p.ChiTietDoiSoatDon)
                .HasForeignKey<ChiTietDoiSoatDon>(d => d.DonDatSanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Chi_Tiet_Doi_Soat_Don_don_dat_san_id");

            entity.HasOne(d => d.KyDoiSoat).WithMany(p => p.ChiTietDoiSoatDons)
                .HasForeignKey(d => d.KyDoiSoatId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Chi_Tiet_Doi_Soat_Don_ky_doi_soat_id");
        });

        modelBuilder.Entity<ChinhSachHuyDoi>(entity =>
        {
            entity.ToTable("Chinh_Sach_Huy_Doi", tb => tb.HasTrigger("TR_ChinhSach_KiemTra"));

            entity.HasIndex(e => e.CoSoId, "IX_Chinh_Sach_Huy_Doi_co_so_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChoPhepDoiLich)
                .HasDefaultValue(true, "DF_Chinh_Sach_Huy_Doi_cho_phep_doi_lich")
                .HasColumnName("cho_phep_doi_lich");
            entity.Property(e => e.CoSoId).HasColumnName("co_so_id");
            entity.Property(e => e.GhiChu)
                .HasMaxLength(255)
                .HasColumnName("ghi_chu");
            entity.Property(e => e.NgayHieuLucDen).HasColumnName("ngay_hieu_luc_den");
            entity.Property(e => e.NgayHieuLucTu).HasColumnName("ngay_hieu_luc_tu");
            entity.Property(e => e.NgayTao)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Chinh_Sach_Huy_Doi_ngay_tao")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.TenChinhSach)
                .HasMaxLength(150)
                .HasColumnName("ten_chinh_sach");

            entity.HasOne(d => d.CoSo).WithMany(p => p.ChinhSachHuyDois)
                .HasForeignKey(d => d.CoSoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Chinh_Sach_Huy_Doi_co_so_id");
        });

        modelBuilder.Entity<CoSo>(entity =>
        {
            entity.ToTable("Co_So");

            entity.HasIndex(e => e.ChuSoHuuId, "IX_Co_So_chu_so_huu_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AnhDaiDien)
                .HasMaxLength(500)
                .HasColumnName("anh_dai_dien");
            entity.Property(e => e.ChuSoHuuId).HasColumnName("chu_so_huu_id");
            entity.Property(e => e.DangHoatDong)
                .HasDefaultValue(true, "DF_Co_So_dang_hoat_dong")
                .HasColumnName("dang_hoat_dong");
            entity.Property(e => e.DiaChi)
                .HasMaxLength(255)
                .HasColumnName("dia_chi");
            entity.Property(e => e.GioDongCua)
                .HasPrecision(0)
                .HasColumnName("gio_dong_cua");
            entity.Property(e => e.GioMoCua)
                .HasPrecision(0)
                .HasColumnName("gio_mo_cua");
            entity.Property(e => e.KinhDo)
                .HasColumnType("decimal(10, 7)")
                .HasColumnName("kinh_do");
            entity.Property(e => e.NgayCapNhat)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Co_So_ngay_cap_nhat")
                .HasColumnName("ngay_cap_nhat");
            entity.Property(e => e.NgayTao)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Co_So_ngay_tao")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.SoDienThoaiHotline)
                .HasMaxLength(20)
                .HasColumnName("so_dien_thoai_hotline");
            entity.Property(e => e.TenCoSo)
                .HasMaxLength(150)
                .HasColumnName("ten_co_so");
            entity.Property(e => e.TienIch).HasColumnName("tien_ich");
            entity.Property(e => e.TrangThaiDuyet)
                .HasMaxLength(30)
                .HasDefaultValue("CHO_DUYET", "DF_Co_So_trang_thai_duyet")
                .HasColumnName("trang_thai_duyet");
            entity.Property(e => e.ViDo)
                .HasColumnType("decimal(10, 7)")
                .HasColumnName("vi_do");

            entity.HasOne(d => d.ChuSoHuu).WithMany(p => p.CoSos)
                .HasForeignKey(d => d.ChuSoHuuId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Co_So_chu_so_huu_id");
        });

        modelBuilder.Entity<DanhGiaCoSo>(entity =>
        {
            entity.ToTable("Danh_Gia_Co_So");

            entity.HasIndex(e => e.DonDatSanId, "UX_Danh_Gia_Co_So_don_dat_san_id").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DonDatSanId).HasColumnName("don_dat_san_id");
            entity.Property(e => e.NgayCapNhat)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Danh_Gia_Co_So_ngay_cap_nhat")
                .HasColumnName("ngay_cap_nhat");
            entity.Property(e => e.NgayPhanHoi)
                .HasPrecision(0)
                .HasColumnName("ngay_phan_hoi");
            entity.Property(e => e.NgayTao)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Danh_Gia_Co_So_ngay_tao")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.NhanXet).HasColumnName("nhan_xet");
            entity.Property(e => e.PhanHoiChuSan).HasColumnName("phan_hoi_chu_san");
            entity.Property(e => e.SoSao).HasColumnName("so_sao");

            entity.HasOne(d => d.DonDatSan).WithOne(p => p.DanhGiaCoSo)
                .HasForeignKey<DanhGiaCoSo>(d => d.DonDatSanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Danh_Gia_Co_So_don_dat_san_id");
        });

        modelBuilder.Entity<DichVu>(entity =>
        {
            entity.ToTable("Dich_Vu");

            entity.HasIndex(e => e.CoSoId, "IX_Dich_Vu_co_so_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CoSoId).HasColumnName("co_so_id");
            entity.Property(e => e.ConHang)
                .HasDefaultValue(true, "DF_Dich_Vu_con_hang")
                .HasColumnName("con_hang");
            entity.Property(e => e.DangKinhDoanh)
                .HasDefaultValue(true, "DF_Dich_Vu_dang_kinh_doanh")
                .HasColumnName("dang_kinh_doanh");
            entity.Property(e => e.DonGia)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("don_gia");
            entity.Property(e => e.DonViTinh)
                .HasMaxLength(30)
                .HasColumnName("don_vi_tinh");
            entity.Property(e => e.LoaiDichVu)
                .HasMaxLength(50)
                .HasColumnName("loai_dich_vu");
            entity.Property(e => e.TenDichVu)
                .HasMaxLength(100)
                .HasColumnName("ten_dich_vu");

            entity.HasOne(d => d.CoSo).WithMany(p => p.DichVus)
                .HasForeignKey(d => d.CoSoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Dich_Vu_co_so_id");
        });

        modelBuilder.Entity<DieuChinhDoiSoat>(entity =>
        {
            entity.ToTable("Dieu_Chinh_Doi_Soat");

            entity.HasIndex(e => e.ChiTietDoiSoatGocId, "IX_Dieu_Chinh_Doi_Soat_chi_tiet_doi_soat_goc_id");

            entity.HasIndex(e => e.KyDoiSoatId, "IX_Dieu_Chinh_Doi_Soat_ky_doi_soat_id");

            entity.HasIndex(e => e.NguoiTaoId, "IX_Dieu_Chinh_Doi_Soat_nguoi_tao_id");

            entity.HasIndex(e => e.HoanTienId, "UX_Dieu_Chinh_Doi_Soat_hoan_tien_id")
                .IsUnique()
                .HasFilter("([hoan_tien_id] IS NOT NULL)");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChiTietDoiSoatGocId).HasColumnName("chi_tiet_doi_soat_goc_id");
            entity.Property(e => e.HoanTienId).HasColumnName("hoan_tien_id");
            entity.Property(e => e.KyDoiSoatId).HasColumnName("ky_doi_soat_id");
            entity.Property(e => e.LyDo).HasColumnName("ly_do");
            entity.Property(e => e.NgayTao)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Dieu_Chinh_Doi_Soat_ngay_tao")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.NguoiTaoId).HasColumnName("nguoi_tao_id");
            entity.Property(e => e.SoTienDieuChinh)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("so_tien_dieu_chinh");

            entity.HasOne(d => d.ChiTietDoiSoatGoc).WithMany(p => p.DieuChinhDoiSoats)
                .HasForeignKey(d => d.ChiTietDoiSoatGocId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Dieu_Chinh_Doi_Soat_chi_tiet_doi_soat_goc_id");

            entity.HasOne(d => d.HoanTien).WithOne(p => p.DieuChinhDoiSoat)
                .HasForeignKey<DieuChinhDoiSoat>(d => d.HoanTienId)
                .HasConstraintName("FK_Dieu_Chinh_Doi_Soat_hoan_tien_id");

            entity.HasOne(d => d.KyDoiSoat).WithMany(p => p.DieuChinhDoiSoats)
                .HasForeignKey(d => d.KyDoiSoatId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Dieu_Chinh_Doi_Soat_ky_doi_soat_id");

            entity.HasOne(d => d.NguoiTao).WithMany(p => p.DieuChinhDoiSoats)
                .HasForeignKey(d => d.NguoiTaoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Dieu_Chinh_Doi_Soat_nguoi_tao_id");
        });

        modelBuilder.Entity<DonDatSan>(entity =>
        {
            entity.ToTable("Don_Dat_San", tb => tb.HasTrigger("TR_DonDatSan_KiemTra"));

            entity.HasIndex(e => e.CoSoId, "IX_Don_Dat_San_co_so_id");

            entity.HasIndex(e => e.NguoiDungId, "IX_Don_Dat_San_nguoi_dung_id");

            entity.HasIndex(e => e.NhanVienTaoId, "IX_Don_Dat_San_nhan_vien_tao_id");

            entity.HasIndex(e => new { e.TrangThai, e.ThoiGianHetHanGiuCho }, "IX_Don_Dat_San_trang_thai_thoi_gian_het_han_giu_cho");

            entity.HasIndex(e => e.MaDatSan, "UX_Don_Dat_San_ma_dat_san").IsUnique();

            entity.HasIndex(e => e.MaQrCheckin, "UX_Don_Dat_San_ma_qr_checkin").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CoSoId).HasColumnName("co_so_id");
            entity.Property(e => e.GhiChu).HasColumnName("ghi_chu");
            entity.Property(e => e.MaDatSan)
                .HasMaxLength(30)
                .HasDefaultValueSql("(left(replace(CONVERT([nvarchar](36),newid()),N'-',N''),(30)))", "DF_Don_Dat_San_ma_dat_san")
                .HasColumnName("ma_dat_san");
            entity.Property(e => e.MaQrCheckin)
                .HasMaxLength(255)
                .HasDefaultValueSql("(CONVERT([nvarchar](36),newid()))", "DF_Don_Dat_San_ma_qr_checkin")
                .HasColumnName("ma_qr_checkin");
            entity.Property(e => e.NgayCapNhat)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Don_Dat_San_ngay_cap_nhat")
                .HasColumnName("ngay_cap_nhat");
            entity.Property(e => e.NgayTao)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Don_Dat_San_ngay_tao")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.NguoiDungId).HasColumnName("nguoi_dung_id");
            entity.Property(e => e.NguonDat)
                .HasMaxLength(30)
                .HasDefaultValue("APP", "DF_Don_Dat_San_nguon_dat")
                .HasColumnName("nguon_dat");
            entity.Property(e => e.NhanVienTaoId).HasColumnName("nhan_vien_tao_id");
            entity.Property(e => e.SdtKhachHang)
                .HasMaxLength(20)
                .HasColumnName("sdt_khach_hang");
            entity.Property(e => e.TenKhachHang)
                .HasMaxLength(100)
                .HasColumnName("ten_khach_hang");
            entity.Property(e => e.ThoiGianHetHanGiuCho)
                .HasPrecision(0)
                .HasColumnName("thoi_gian_het_han_giu_cho");
            entity.Property(e => e.TienCocYeuCau)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("tien_coc_yeu_cau");
            entity.Property(e => e.TienGiamGia)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("tien_giam_gia");
            entity.Property(e => e.TongPhiPhat)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("tong_phi_phat");
            entity.Property(e => e.TongTienDichVu)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("tong_tien_dich_vu");
            entity.Property(e => e.TongTienPhaiTra)
                .HasComputedColumnSql("(CONVERT([decimal](14,2),(([tong_tien_san]+[tong_tien_dich_vu])+[tong_phi_phat])-[tien_giam_gia]))", true)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("tong_tien_phai_tra");
            entity.Property(e => e.TongTienSan)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("tong_tien_san");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(30)
                .HasDefaultValue("CHO_THANH_TOAN", "DF_Don_Dat_San_trang_thai")
                .HasColumnName("trang_thai");

            entity.HasOne(d => d.CoSo).WithMany(p => p.DonDatSans)
                .HasForeignKey(d => d.CoSoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Don_Dat_San_co_so_id");

            entity.HasOne(d => d.NguoiDung).WithMany(p => p.DonDatSanNguoiDungs)
                .HasForeignKey(d => d.NguoiDungId)
                .HasConstraintName("FK_Don_Dat_San_nguoi_dung_id");

            entity.HasOne(d => d.NhanVienTao).WithMany(p => p.DonDatSanNhanVienTaos)
                .HasForeignKey(d => d.NhanVienTaoId)
                .HasConstraintName("FK_Don_Dat_San_nhan_vien_tao_id");
        });

        modelBuilder.Entity<DongGopPhong>(entity =>
        {
            entity.ToTable("Dong_Gop_Phong");

            entity.HasIndex(e => e.NguoiGhiNhanId, "IX_Dong_Gop_Phong_nguoi_ghi_nhan_id");

            entity.HasIndex(e => e.ThanhVienPhongId, "IX_Dong_Gop_Phong_thanh_vien_phong_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GhiChu).HasColumnName("ghi_chu");
            entity.Property(e => e.LoaiGiaoDich)
                .HasMaxLength(20)
                .HasColumnName("loai_giao_dich");
            entity.Property(e => e.MaThamChieu)
                .HasMaxLength(100)
                .HasColumnName("ma_tham_chieu");
            entity.Property(e => e.NgayGiaoDich)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Dong_Gop_Phong_ngay_giao_dich")
                .HasColumnName("ngay_giao_dich");
            entity.Property(e => e.NguoiGhiNhanId).HasColumnName("nguoi_ghi_nhan_id");
            entity.Property(e => e.PhuongThuc)
                .HasMaxLength(30)
                .HasColumnName("phuong_thuc");
            entity.Property(e => e.SoTien)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("so_tien");
            entity.Property(e => e.ThanhVienPhongId).HasColumnName("thanh_vien_phong_id");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(30)
                .HasDefaultValue("DANG_XU_LY", "DF_Dong_Gop_Phong_trang_thai")
                .HasColumnName("trang_thai");

            entity.HasOne(d => d.NguoiGhiNhan).WithMany(p => p.DongGopPhongs)
                .HasForeignKey(d => d.NguoiGhiNhanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Dong_Gop_Phong_nguoi_ghi_nhan_id");

            entity.HasOne(d => d.ThanhVienPhong).WithMany(p => p.DongGopPhongs)
                .HasForeignKey(d => d.ThanhVienPhongId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Dong_Gop_Phong_thanh_vien_phong_id");
        });

        modelBuilder.Entity<GiaoDichCongThanhToan>(entity =>
        {
            entity.ToTable("Giao_Dich_Cong_Thanh_Toan");

            entity.HasIndex(e => e.NguoiDungId, "IX_Giao_Dich_Cong_Thanh_Toan_nguoi_dung_id");

            entity.HasIndex(e => new { e.CongThanhToan, e.MaGiaoDichNganHang }, "UX_Giao_Dich_Cong_Thanh_Toan_cong_thanh_toan_ma_giao_dich_ngan_hang")
                .IsUnique()
                .HasFilter("([ma_giao_dich_ngan_hang] IS NOT NULL)");

            entity.HasIndex(e => e.MaDonHangCong, "UX_Giao_Dich_Cong_Thanh_Toan_ma_don_hang_cong").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CongThanhToan)
                .HasMaxLength(30)
                .HasColumnName("cong_thanh_toan");
            entity.Property(e => e.DuLieuWebhook).HasColumnName("du_lieu_webhook");
            entity.Property(e => e.LoaiThanhToan)
                .HasMaxLength(50)
                .HasColumnName("loai_thanh_toan");
            entity.Property(e => e.MaDonHangCong)
                .HasMaxLength(100)
                .HasColumnName("ma_don_hang_cong");
            entity.Property(e => e.MaGiaoDichNganHang)
                .HasMaxLength(100)
                .HasColumnName("ma_giao_dich_ngan_hang");
            entity.Property(e => e.MaSuKienCong)
                .HasMaxLength(100)
                .HasColumnName("ma_su_kien_cong");
            entity.Property(e => e.NgayCapNhat)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Giao_Dich_Cong_Thanh_Toan_ngay_cap_nhat")
                .HasColumnName("ngay_cap_nhat");
            entity.Property(e => e.NgayTao)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Giao_Dich_Cong_Thanh_Toan_ngay_tao")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.NgayThanhToan)
                .HasPrecision(0)
                .HasColumnName("ngay_thanh_toan");
            entity.Property(e => e.NguoiDungId).HasColumnName("nguoi_dung_id");
            entity.Property(e => e.SoTien)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("so_tien");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(30)
                .HasDefaultValue("KHOI_TAO", "DF_Giao_Dich_Cong_Thanh_Toan_trang_thai")
                .HasColumnName("trang_thai");

            entity.HasOne(d => d.NguoiDung).WithMany(p => p.GiaoDichCongThanhToans)
                .HasForeignKey(d => d.NguoiDungId)
                .HasConstraintName("FK_Giao_Dich_Cong_Thanh_Toan_nguoi_dung_id");
        });

        modelBuilder.Entity<GiaoDichVi>(entity =>
        {
            entity.ToTable("Giao_Dich_Vi", tb => tb.HasTrigger("TR_GiaoDichVi_BatBien"));

            entity.HasIndex(e => e.ViId, "IX_Giao_Dich_Vi_vi_id");

            entity.HasIndex(e => e.KhoaChongLap, "UX_Giao_Dich_Vi_khoa_chong_lap").IsUnique();

            entity.HasIndex(e => e.MaGiaoDich, "UX_Giao_Dich_Vi_ma_giao_dich").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.KhoaChongLap)
                .HasMaxLength(100)
                .HasColumnName("khoa_chong_lap");
            entity.Property(e => e.LoaiGiaoDich)
                .HasMaxLength(50)
                .HasColumnName("loai_giao_dich");
            entity.Property(e => e.MaGiaoDich)
                .HasMaxLength(50)
                .HasColumnName("ma_giao_dich");
            entity.Property(e => e.NgayTao)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Giao_Dich_Vi_ngay_tao")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.NoiDung).HasColumnName("noi_dung");
            entity.Property(e => e.SoDuDongBangSau)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("so_du_dong_bang_sau");
            entity.Property(e => e.SoDuDongBangTruoc)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("so_du_dong_bang_truoc");
            entity.Property(e => e.SoDuSau)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("so_du_sau");
            entity.Property(e => e.SoDuTruoc)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("so_du_truoc");
            entity.Property(e => e.SoTien)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("so_tien");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(30)
                .HasDefaultValue("DA_GHI_SO", "DF_Giao_Dich_Vi_trang_thai")
                .HasColumnName("trang_thai");
            entity.Property(e => e.ViId).HasColumnName("vi_id");

            entity.HasOne(d => d.Vi).WithMany(p => p.GiaoDichVis)
                .HasForeignKey(d => d.ViId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Giao_Dich_Vi_vi_id");
        });

        modelBuilder.Entity<HoSoTrinhDo>(entity =>
        {
            entity.ToTable("Ho_So_Trinh_Do");

            entity.HasIndex(e => e.MonTheThaoId, "IX_Ho_So_Trinh_Do_mon_the_thao_id");

            entity.HasIndex(e => new { e.NguoiDungId, e.MonTheThaoId }, "UX_Ho_So_Trinh_Do_nguoi_dung_id_mon_the_thao_id").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DiemElo)
                .HasDefaultValue(1000, "DF_Ho_So_Trinh_Do_diem_elo")
                .HasColumnName("diem_elo");
            entity.Property(e => e.HangTrinhDo)
                .HasMaxLength(50)
                .HasColumnName("hang_trinh_do");
            entity.Property(e => e.MonTheThaoId).HasColumnName("mon_the_thao_id");
            entity.Property(e => e.NgayCapNhat)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Ho_So_Trinh_Do_ngay_cap_nhat")
                .HasColumnName("ngay_cap_nhat");
            entity.Property(e => e.NguoiDungId).HasColumnName("nguoi_dung_id");
            entity.Property(e => e.SoTranDaDau).HasColumnName("so_tran_da_dau");
            entity.Property(e => e.SoTranHoa).HasColumnName("so_tran_hoa");
            entity.Property(e => e.SoTranThang).HasColumnName("so_tran_thang");
            entity.Property(e => e.SoTranThua).HasColumnName("so_tran_thua");

            entity.HasOne(d => d.MonTheThao).WithMany(p => p.HoSoTrinhDos)
                .HasForeignKey(d => d.MonTheThaoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ho_So_Trinh_Do_mon_the_thao_id");

            entity.HasOne(d => d.NguoiDung).WithMany(p => p.HoSoTrinhDos)
                .HasForeignKey(d => d.NguoiDungId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ho_So_Trinh_Do_nguoi_dung_id");
        });

        modelBuilder.Entity<HoanTien>(entity =>
        {
            entity.ToTable("Hoan_Tien");

            entity.HasIndex(e => e.LichSuHuySanId, "IX_Hoan_Tien_lich_su_huy_san_id");

            entity.HasIndex(e => e.NguoiDuyetId, "IX_Hoan_Tien_nguoi_duyet_id");

            entity.HasIndex(e => new { e.ThanhToanDonId, e.TrangThai }, "IX_Hoan_Tien_thanh_toan_don_id_trang_thai");

            entity.HasIndex(e => e.YeuCauDoiLichId, "IX_Hoan_Tien_yeu_cau_doi_lich_id");

            entity.HasIndex(e => e.GiaoDichCongId, "UX_Hoan_Tien_giao_dich_cong_id")
                .IsUnique()
                .HasFilter("([giao_dich_cong_id] IS NOT NULL)");

            entity.HasIndex(e => e.GiaoDichViId, "UX_Hoan_Tien_giao_dich_vi_id")
                .IsUnique()
                .HasFilter("([giao_dich_vi_id] IS NOT NULL)");

            entity.HasIndex(e => e.KhoaChongLap, "UX_Hoan_Tien_khoa_chong_lap").IsUnique();

            entity.HasIndex(e => e.MaHoanTien, "UX_Hoan_Tien_ma_hoan_tien").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GiaoDichCongId).HasColumnName("giao_dich_cong_id");
            entity.Property(e => e.GiaoDichViId).HasColumnName("giao_dich_vi_id");
            entity.Property(e => e.KhoaChongLap)
                .HasMaxLength(100)
                .HasColumnName("khoa_chong_lap");
            entity.Property(e => e.LichSuHuySanId).HasColumnName("lich_su_huy_san_id");
            entity.Property(e => e.LyDo).HasColumnName("ly_do");
            entity.Property(e => e.MaHoanTien)
                .HasMaxLength(50)
                .HasColumnName("ma_hoan_tien");
            entity.Property(e => e.MaThamChieuNgoai)
                .HasMaxLength(100)
                .HasColumnName("ma_tham_chieu_ngoai");
            entity.Property(e => e.NgayHoanTien)
                .HasPrecision(0)
                .HasColumnName("ngay_hoan_tien");
            entity.Property(e => e.NgayTao)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Hoan_Tien_ngay_tao")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.NguoiDuyetId).HasColumnName("nguoi_duyet_id");
            entity.Property(e => e.PhuongThuc)
                .HasMaxLength(30)
                .HasColumnName("phuong_thuc");
            entity.Property(e => e.SoTien)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("so_tien");
            entity.Property(e => e.ThanhToanDonId).HasColumnName("thanh_toan_don_id");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(30)
                .HasDefaultValue("KHOI_TAO", "DF_Hoan_Tien_trang_thai")
                .HasColumnName("trang_thai");
            entity.Property(e => e.YeuCauDoiLichId).HasColumnName("yeu_cau_doi_lich_id");

            entity.HasOne(d => d.GiaoDichCong).WithOne(p => p.HoanTien)
                .HasForeignKey<HoanTien>(d => d.GiaoDichCongId)
                .HasConstraintName("FK_Hoan_Tien_giao_dich_cong_id");

            entity.HasOne(d => d.GiaoDichVi).WithOne(p => p.HoanTien)
                .HasForeignKey<HoanTien>(d => d.GiaoDichViId)
                .HasConstraintName("FK_Hoan_Tien_giao_dich_vi_id");

            entity.HasOne(d => d.LichSuHuySan).WithMany(p => p.HoanTiens)
                .HasForeignKey(d => d.LichSuHuySanId)
                .HasConstraintName("FK_Hoan_Tien_lich_su_huy_san_id");

            entity.HasOne(d => d.NguoiDuyet).WithMany(p => p.HoanTiens)
                .HasForeignKey(d => d.NguoiDuyetId)
                .HasConstraintName("FK_Hoan_Tien_nguoi_duyet_id");

            entity.HasOne(d => d.ThanhToanDon).WithMany(p => p.HoanTiens)
                .HasForeignKey(d => d.ThanhToanDonId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Hoan_Tien_thanh_toan_don_id");

            entity.HasOne(d => d.YeuCauDoiLich).WithMany(p => p.HoanTiens)
                .HasForeignKey(d => d.YeuCauDoiLichId)
                .HasConstraintName("FK_Hoan_Tien_yeu_cau_doi_lich_id");
        });

        modelBuilder.Entity<KetQuaTranDau>(entity =>
        {
            entity.ToTable("Ket_Qua_Tran_Dau", tb => tb.HasTrigger("TR_KetQua_DungDoi"));

            entity.HasIndex(e => e.NguoiXacNhanDoi1Id, "IX_Ket_Qua_Tran_Dau_nguoi_xac_nhan_doi_1_id");

            entity.HasIndex(e => e.NguoiXacNhanDoi2Id, "IX_Ket_Qua_Tran_Dau_nguoi_xac_nhan_doi_2_id");

            entity.HasIndex(e => e.PhongGhepId, "UX_Ket_Qua_Tran_Dau_phong_ghep_id").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.NgayCapNhat)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Ket_Qua_Tran_Dau_ngay_cap_nhat")
                .HasColumnName("ngay_cap_nhat");
            entity.Property(e => e.NgayGhiNhan)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Ket_Qua_Tran_Dau_ngay_ghi_nhan")
                .HasColumnName("ngay_ghi_nhan");
            entity.Property(e => e.NgayXacNhanDoi1)
                .HasPrecision(0)
                .HasColumnName("ngay_xac_nhan_doi_1");
            entity.Property(e => e.NgayXacNhanDoi2)
                .HasPrecision(0)
                .HasColumnName("ngay_xac_nhan_doi_2");
            entity.Property(e => e.NguoiXacNhanDoi1Id).HasColumnName("nguoi_xac_nhan_doi_1_id");
            entity.Property(e => e.NguoiXacNhanDoi2Id).HasColumnName("nguoi_xac_nhan_doi_2_id");
            entity.Property(e => e.PhongGhepId).HasColumnName("phong_ghep_id");
            entity.Property(e => e.SoDoiThang).HasColumnName("so_doi_thang");
            entity.Property(e => e.TiSo)
                .HasMaxLength(50)
                .HasColumnName("ti_so");
            entity.Property(e => e.TrangThaiXacNhan)
                .HasMaxLength(30)
                .HasDefaultValue("CHO_XAC_NHAN", "DF_Ket_Qua_Tran_Dau_trang_thai_xac_nhan")
                .HasColumnName("trang_thai_xac_nhan");

            entity.HasOne(d => d.NguoiXacNhanDoi1).WithMany(p => p.KetQuaTranDauNguoiXacNhanDoi1s)
                .HasForeignKey(d => d.NguoiXacNhanDoi1Id)
                .HasConstraintName("FK_Ket_Qua_Tran_Dau_nguoi_xac_nhan_doi_1_id");

            entity.HasOne(d => d.NguoiXacNhanDoi2).WithMany(p => p.KetQuaTranDauNguoiXacNhanDoi2s)
                .HasForeignKey(d => d.NguoiXacNhanDoi2Id)
                .HasConstraintName("FK_Ket_Qua_Tran_Dau_nguoi_xac_nhan_doi_2_id");

            entity.HasOne(d => d.PhongGhep).WithOne(p => p.KetQuaTranDau)
                .HasForeignKey<KetQuaTranDau>(d => d.PhongGhepId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ket_Qua_Tran_Dau_phong_ghep_id");
        });

        modelBuilder.Entity<KhungGioMacDinh>(entity =>
        {
            entity.ToTable("Khung_Gio_Mac_Dinh", tb => tb.HasTrigger("TR_KhungGio_KiemTra"));

            entity.HasIndex(e => new { e.CoSoId, e.GioBatDau, e.GioKetThuc }, "UX_Khung_Gio_Mac_Dinh_co_so_id_gio_bat_dau_gio_ket_thuc").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CoSoId).HasColumnName("co_so_id");
            entity.Property(e => e.GioBatDau)
                .HasPrecision(0)
                .HasColumnName("gio_bat_dau");
            entity.Property(e => e.GioKetThuc)
                .HasPrecision(0)
                .HasColumnName("gio_ket_thuc");
            entity.Property(e => e.LaGioVang).HasColumnName("la_gio_vang");

            entity.HasOne(d => d.CoSo).WithMany(p => p.KhungGioMacDinhs)
                .HasForeignKey(d => d.CoSoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Khung_Gio_Mac_Dinh_co_so_id");
        });

        modelBuilder.Entity<KyDoiSoat>(entity =>
        {
            entity.ToTable("Ky_Doi_Soat");

            entity.HasIndex(e => e.CoSoId, "IX_Ky_Doi_Soat_co_so_id");

            entity.HasIndex(e => e.NhanVienDoiSoatId, "IX_Ky_Doi_Soat_nhan_vien_doi_soat_id");

            entity.HasIndex(e => e.MaKyDoiSoat, "UX_Ky_Doi_Soat_ma_ky_doi_soat").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CoSoId).HasColumnName("co_so_id");
            entity.Property(e => e.MaChuyenKhoan)
                .HasMaxLength(100)
                .HasColumnName("ma_chuyen_khoan");
            entity.Property(e => e.MaKyDoiSoat)
                .HasMaxLength(50)
                .HasColumnName("ma_ky_doi_soat");
            entity.Property(e => e.NgayBatDau).HasColumnName("ngay_bat_dau");
            entity.Property(e => e.NgayChot)
                .HasPrecision(0)
                .HasColumnName("ngay_chot");
            entity.Property(e => e.NgayKetThuc).HasColumnName("ngay_ket_thuc");
            entity.Property(e => e.NgayThanhToan)
                .HasPrecision(0)
                .HasColumnName("ngay_thanh_toan");
            entity.Property(e => e.NhanVienDoiSoatId).HasColumnName("nhan_vien_doi_soat_id");
            entity.Property(e => e.SoTienThucChuyen)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("so_tien_thuc_chuyen");
            entity.Property(e => e.TongDoanhThuApp)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("tong_doanh_thu_app");
            entity.Property(e => e.TongDoanhThuTaiQuay)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("tong_doanh_thu_tai_quay");
            entity.Property(e => e.TongPhiHoaHong)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("tong_phi_hoa_hong");
            entity.Property(e => e.TongTienHoanApp)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("tong_tien_hoan_app");
            entity.Property(e => e.TongVoucherSanTroGia)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("tong_voucher_san_tro_gia");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(30)
                .HasDefaultValue("DANG_LAP", "DF_Ky_Doi_Soat_trang_thai")
                .HasColumnName("trang_thai");

            entity.HasOne(d => d.CoSo).WithMany(p => p.KyDoiSoats)
                .HasForeignKey(d => d.CoSoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ky_Doi_Soat_co_so_id");

            entity.HasOne(d => d.NhanVienDoiSoat).WithMany(p => p.KyDoiSoats)
                .HasForeignKey(d => d.NhanVienDoiSoatId)
                .HasConstraintName("FK_Ky_Doi_Soat_nhan_vien_doi_soat_id");
        });

        modelBuilder.Entity<LichDatCoDinh>(entity =>
        {
            entity.ToTable("Lich_Dat_Co_Dinh", tb => tb.HasTrigger("TR_LichCoDinh_KiemTra"));

            entity.HasIndex(e => e.KhungGioId, "IX_Lich_Dat_Co_Dinh_khung_gio_id");

            entity.HasIndex(e => e.NguoiDungId, "IX_Lich_Dat_Co_Dinh_nguoi_dung_id");

            entity.HasIndex(e => e.SanId, "IX_Lich_Dat_Co_Dinh_san_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GioBatDau)
                .HasPrecision(0)
                .HasColumnName("gio_bat_dau");
            entity.Property(e => e.GioKetThuc)
                .HasPrecision(0)
                .HasColumnName("gio_ket_thuc");
            entity.Property(e => e.KhungGioId).HasColumnName("khung_gio_id");
            entity.Property(e => e.NgayBatDau).HasColumnName("ngay_bat_dau");
            entity.Property(e => e.NgayCapNhat)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Lich_Dat_Co_Dinh_ngay_cap_nhat")
                .HasColumnName("ngay_cap_nhat");
            entity.Property(e => e.NgayKetThuc).HasColumnName("ngay_ket_thuc");
            entity.Property(e => e.NgayTao)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Lich_Dat_Co_Dinh_ngay_tao")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.NguoiDungId).HasColumnName("nguoi_dung_id");
            entity.Property(e => e.SanId).HasColumnName("san_id");
            entity.Property(e => e.ThuTrongTuan).HasColumnName("thu_trong_tuan");
            entity.Property(e => e.TrangThaiKichHoat)
                .HasDefaultValue(true, "DF_Lich_Dat_Co_Dinh_trang_thai_kich_hoat")
                .HasColumnName("trang_thai_kich_hoat");

            entity.HasOne(d => d.KhungGio).WithMany(p => p.LichDatCoDinhs)
                .HasForeignKey(d => d.KhungGioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Lich_Dat_Co_Dinh_khung_gio_id");

            entity.HasOne(d => d.NguoiDung).WithMany(p => p.LichDatCoDinhs)
                .HasForeignKey(d => d.NguoiDungId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Lich_Dat_Co_Dinh_nguoi_dung_id");

            entity.HasOne(d => d.San).WithMany(p => p.LichDatCoDinhs)
                .HasForeignKey(d => d.SanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Lich_Dat_Co_Dinh_san_id");
        });

        modelBuilder.Entity<LichHoatDongCoSo>(entity =>
        {
            entity.ToTable("Lich_Hoat_Dong_Co_So");

            entity.HasIndex(e => new { e.CoSoId, e.ThuTrongTuan }, "UX_Lich_Hoat_Dong_Co_So_co_so_id_thu_trong_tuan").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CoSoId).HasColumnName("co_so_id");
            entity.Property(e => e.GioDongCua)
                .HasPrecision(0)
                .HasColumnName("gio_dong_cua");
            entity.Property(e => e.GioMoCua)
                .HasPrecision(0)
                .HasColumnName("gio_mo_cua");
            entity.Property(e => e.NghiCaNgay).HasColumnName("nghi_ca_ngay");
            entity.Property(e => e.ThuTrongTuan).HasColumnName("thu_trong_tuan");

            entity.HasOne(d => d.CoSo).WithMany(p => p.LichHoatDongCoSos)
                .HasForeignKey(d => d.CoSoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Lich_Hoat_Dong_Co_So_co_so_id");
        });

        modelBuilder.Entity<LichKhoaSan>(entity =>
        {
            entity.ToTable("Lich_Khoa_San", tb => tb.HasTrigger("TR_LichKhoaSan_KiemTra"));

            entity.HasIndex(e => e.NguoiTaoId, "IX_Lich_Khoa_San_nguoi_tao_id");

            entity.HasIndex(e => new { e.SanId, e.BatDauLuc, e.KetThucLuc }, "IX_Lich_Khoa_San_san_id_bat_dau_luc_ket_thuc_luc");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BatDauLuc)
                .HasPrecision(0)
                .HasColumnName("bat_dau_luc");
            entity.Property(e => e.KetThucLuc)
                .HasPrecision(0)
                .HasColumnName("ket_thuc_luc");
            entity.Property(e => e.LyDo).HasColumnName("ly_do");
            entity.Property(e => e.NgayTao)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Lich_Khoa_San_ngay_tao")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.NguoiTaoId).HasColumnName("nguoi_tao_id");
            entity.Property(e => e.SanId).HasColumnName("san_id");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(30)
                .HasDefaultValue("DANG_AP_DUNG", "DF_Lich_Khoa_San_trang_thai")
                .HasColumnName("trang_thai");

            entity.HasOne(d => d.NguoiTao).WithMany(p => p.LichKhoaSans)
                .HasForeignKey(d => d.NguoiTaoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Lich_Khoa_San_nguoi_tao_id");

            entity.HasOne(d => d.San).WithMany(p => p.LichKhoaSans)
                .HasForeignKey(d => d.SanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Lich_Khoa_San_san_id");
        });

        modelBuilder.Entity<LichSuCheckIn>(entity =>
        {
            entity.ToTable("Lich_Su_Check_In");

            entity.HasIndex(e => e.ChiTietDatSanId, "IX_Lich_Su_Check_In_chi_tiet_dat_san_id");

            entity.HasIndex(e => e.NhanVienCheckinId, "IX_Lich_Su_Check_In_nhan_vien_checkin_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChiTietDatSanId).HasColumnName("chi_tiet_dat_san_id");
            entity.Property(e => e.GhiChu).HasColumnName("ghi_chu");
            entity.Property(e => e.NhanVienCheckinId).HasColumnName("nhan_vien_checkin_id");
            entity.Property(e => e.PhuongThuc)
                .HasMaxLength(30)
                .HasDefaultValue("QR", "DF_Lich_Su_Check_In_phuong_thuc")
                .HasColumnName("phuong_thuc");
            entity.Property(e => e.ThoiGianCheckin)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Lich_Su_Check_In_thoi_gian_checkin")
                .HasColumnName("thoi_gian_checkin");

            entity.HasOne(d => d.ChiTietDatSan).WithMany(p => p.LichSuCheckIns)
                .HasForeignKey(d => d.ChiTietDatSanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Lich_Su_Check_In_chi_tiet_dat_san_id");

            entity.HasOne(d => d.NhanVienCheckin).WithMany(p => p.LichSuCheckIns)
                .HasForeignKey(d => d.NhanVienCheckinId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Lich_Su_Check_In_nhan_vien_checkin_id");
        });

        modelBuilder.Entity<LichSuDungVoucher>(entity =>
        {
            entity.ToTable("Lich_Su_Dung_Voucher");

            entity.HasIndex(e => e.NguoiDungId, "IX_Lich_Su_Dung_Voucher_nguoi_dung_id");

            entity.HasIndex(e => e.VoucherId, "IX_Lich_Su_Dung_Voucher_voucher_id");

            entity.HasIndex(e => e.DonDatSanId, "UX_Lich_Su_Dung_Voucher_don_dat_san_id")
                .IsUnique()
                .HasFilter("([trang_thai] IN (N'GIU_CHO', N'DA_AP_DUNG'))");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DonDatSanId).HasColumnName("don_dat_san_id");
            entity.Property(e => e.GiuChoDen)
                .HasPrecision(0)
                .HasColumnName("giu_cho_den");
            entity.Property(e => e.NgayApDung)
                .HasPrecision(0)
                .HasColumnName("ngay_ap_dung");
            entity.Property(e => e.NgayGiaiToa)
                .HasPrecision(0)
                .HasColumnName("ngay_giai_toa");
            entity.Property(e => e.NguoiDungId).HasColumnName("nguoi_dung_id");
            entity.Property(e => e.SoTienGiamThucTe)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("so_tien_giam_thuc_te");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(30)
                .HasDefaultValue("GIU_CHO", "DF_Lich_Su_Dung_Voucher_trang_thai")
                .HasColumnName("trang_thai");
            entity.Property(e => e.VoucherId).HasColumnName("voucher_id");

            entity.HasOne(d => d.DonDatSan).WithOne(p => p.LichSuDungVoucher)
                .HasForeignKey<LichSuDungVoucher>(d => d.DonDatSanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Lich_Su_Dung_Voucher_don_dat_san_id");

            entity.HasOne(d => d.NguoiDung).WithMany(p => p.LichSuDungVouchers)
                .HasForeignKey(d => d.NguoiDungId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Lich_Su_Dung_Voucher_nguoi_dung_id");

            entity.HasOne(d => d.Voucher).WithMany(p => p.LichSuDungVouchers)
                .HasForeignKey(d => d.VoucherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Lich_Su_Dung_Voucher_voucher_id");
        });

        modelBuilder.Entity<LichSuElo>(entity =>
        {
            entity.ToTable("Lich_Su_Elo");

            entity.HasIndex(e => e.KetQuaTranDauId, "IX_Lich_Su_Elo_ket_qua_tran_dau_id");

            entity.HasIndex(e => new { e.HoSoTrinhDoId, e.KetQuaTranDauId }, "UX_Lich_Su_Elo_ho_so_trinh_do_id_ket_qua_tran_dau_id").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DiemSau).HasColumnName("diem_sau");
            entity.Property(e => e.DiemThayDoi).HasColumnName("diem_thay_doi");
            entity.Property(e => e.DiemTruoc).HasColumnName("diem_truoc");
            entity.Property(e => e.HoSoTrinhDoId).HasColumnName("ho_so_trinh_do_id");
            entity.Property(e => e.KetQuaTranDauId).HasColumnName("ket_qua_tran_dau_id");
            entity.Property(e => e.NgayTao)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Lich_Su_Elo_ngay_tao")
                .HasColumnName("ngay_tao");

            entity.HasOne(d => d.HoSoTrinhDo).WithMany(p => p.LichSuElos)
                .HasForeignKey(d => d.HoSoTrinhDoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Lich_Su_Elo_ho_so_trinh_do_id");

            entity.HasOne(d => d.KetQuaTranDau).WithMany(p => p.LichSuElos)
                .HasForeignKey(d => d.KetQuaTranDauId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Lich_Su_Elo_ket_qua_tran_dau_id");
        });

        modelBuilder.Entity<LichSuHuySan>(entity =>
        {
            entity.ToTable("Lich_Su_Huy_San");

            entity.HasIndex(e => e.NguoiXuLyId, "IX_Lich_Su_Huy_San_nguoi_xu_ly_id");

            entity.HasIndex(e => e.NguoiYeuCauId, "IX_Lich_Su_Huy_San_nguoi_yeu_cau_id");

            entity.HasIndex(e => e.ChiTietDatSanId, "UX_Lich_Su_Huy_San_chi_tiet_dat_san_id").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChiTietDatSanId).HasColumnName("chi_tiet_dat_san_id");
            entity.Property(e => e.LyDoHuy).HasColumnName("ly_do_huy");
            entity.Property(e => e.NgayHuy)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Lich_Su_Huy_San_ngay_huy")
                .HasColumnName("ngay_huy");
            entity.Property(e => e.NguoiXuLyId).HasColumnName("nguoi_xu_ly_id");
            entity.Property(e => e.NguoiYeuCauId).HasColumnName("nguoi_yeu_cau_id");
            entity.Property(e => e.PhiPhatHuy)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("phi_phat_huy");
            entity.Property(e => e.SoGioTruocTran)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("so_gio_truoc_tran");
            entity.Property(e => e.SoTienDaPhanBo)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("so_tien_da_phan_bo");
            entity.Property(e => e.SoTienDuKienHoan)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("so_tien_du_kien_hoan");

            entity.HasOne(d => d.ChiTietDatSan).WithOne(p => p.LichSuHuySan)
                .HasForeignKey<LichSuHuySan>(d => d.ChiTietDatSanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Lich_Su_Huy_San_chi_tiet_dat_san_id");

            entity.HasOne(d => d.NguoiXuLy).WithMany(p => p.LichSuHuySanNguoiXuLies)
                .HasForeignKey(d => d.NguoiXuLyId)
                .HasConstraintName("FK_Lich_Su_Huy_San_nguoi_xu_ly_id");

            entity.HasOne(d => d.NguoiYeuCau).WithMany(p => p.LichSuHuySanNguoiYeuCaus)
                .HasForeignKey(d => d.NguoiYeuCauId)
                .HasConstraintName("FK_Lich_Su_Huy_San_nguoi_yeu_cau_id");
        });

        modelBuilder.Entity<LoaiSan>(entity =>
        {
            entity.ToTable("Loai_San");

            entity.HasIndex(e => new { e.MonTheThaoId, e.TenLoai }, "UX_Loai_San_mon_the_thao_id_ten_loai").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.KichThuoc)
                .HasMaxLength(50)
                .HasColumnName("kich_thuoc");
            entity.Property(e => e.MoTa).HasColumnName("mo_ta");
            entity.Property(e => e.MonTheThaoId).HasColumnName("mon_the_thao_id");
            entity.Property(e => e.SucChuaNguoi).HasColumnName("suc_chua_nguoi");
            entity.Property(e => e.TenLoai)
                .HasMaxLength(100)
                .HasColumnName("ten_loai");

            entity.HasOne(d => d.MonTheThao).WithMany(p => p.LoaiSans)
                .HasForeignKey(d => d.MonTheThaoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Loai_San_mon_the_thao_id");
        });

        modelBuilder.Entity<MonTheThao>(entity =>
        {
            entity.ToTable("Mon_The_Thao");

            entity.HasIndex(e => e.MaMon, "UX_Mon_The_Thao_ma_mon").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BieuTuongUrl)
                .HasMaxLength(500)
                .HasColumnName("bieu_tuong_url");
            entity.Property(e => e.MaMon)
                .HasMaxLength(50)
                .HasColumnName("ma_mon");
            entity.Property(e => e.TenMon)
                .HasMaxLength(100)
                .HasColumnName("ten_mon");
            entity.Property(e => e.TrangThai)
                .HasDefaultValue(true, "DF_Mon_The_Thao_trang_thai")
                .HasColumnName("trang_thai");
        });

        modelBuilder.Entity<MucPhiHuyDoi>(entity =>
        {
            entity.ToTable("Muc_Phi_Huy_Doi", tb => tb.HasTrigger("TR_MucPhi_KiemTra"));

            entity.HasIndex(e => e.ChinhSachHuyDoiId, "IX_Muc_Phi_Huy_Doi_chinh_sach_huy_doi_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChinhSachHuyDoiId).HasColumnName("chinh_sach_huy_doi_id");
            entity.Property(e => e.SoGioDenTruocTran)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("so_gio_den_truoc_tran");
            entity.Property(e => e.SoGioTuTruocTran)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("so_gio_tu_truoc_tran");
            entity.Property(e => e.TiLePhiDoi)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("ti_le_phi_doi");
            entity.Property(e => e.TiLePhiHuy)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("ti_le_phi_huy");

            entity.HasOne(d => d.ChinhSachHuyDoi).WithMany(p => p.MucPhiHuyDois)
                .HasForeignKey(d => d.ChinhSachHuyDoiId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Muc_Phi_Huy_Doi_chinh_sach_huy_doi_id");
        });

        modelBuilder.Entity<NapTienVi>(entity =>
        {
            entity.ToTable("Nap_Tien_Vi");

            entity.HasIndex(e => e.ViId, "IX_Nap_Tien_Vi_vi_id");

            entity.HasIndex(e => e.GiaoDichCongId, "UX_Nap_Tien_Vi_giao_dich_cong_id").IsUnique();

            entity.HasIndex(e => e.GiaoDichViId, "UX_Nap_Tien_Vi_giao_dich_vi_id")
                .IsUnique()
                .HasFilter("([giao_dich_vi_id] IS NOT NULL)");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GiaoDichCongId).HasColumnName("giao_dich_cong_id");
            entity.Property(e => e.GiaoDichViId).HasColumnName("giao_dich_vi_id");
            entity.Property(e => e.NgayHoanTat)
                .HasPrecision(0)
                .HasColumnName("ngay_hoan_tat");
            entity.Property(e => e.NgayTao)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Nap_Tien_Vi_ngay_tao")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.SoTien)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("so_tien");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(30)
                .HasDefaultValue("KHOI_TAO", "DF_Nap_Tien_Vi_trang_thai")
                .HasColumnName("trang_thai");
            entity.Property(e => e.ViId).HasColumnName("vi_id");

            entity.HasOne(d => d.GiaoDichCong).WithOne(p => p.NapTienVi)
                .HasForeignKey<NapTienVi>(d => d.GiaoDichCongId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Nap_Tien_Vi_giao_dich_cong_id");

            entity.HasOne(d => d.GiaoDichVi).WithOne(p => p.NapTienVi)
                .HasForeignKey<NapTienVi>(d => d.GiaoDichViId)
                .HasConstraintName("FK_Nap_Tien_Vi_giao_dich_vi_id");

            entity.HasOne(d => d.Vi).WithMany(p => p.NapTienVis)
                .HasForeignKey(d => d.ViId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Nap_Tien_Vi_vi_id");
        });

        modelBuilder.Entity<NguoiDung>(entity =>
        {
            entity.ToTable("Nguoi_Dung");

            entity.HasIndex(e => e.Email, "UX_Nguoi_Dung_email")
                .IsUnique()
                .HasFilter("([email] IS NOT NULL)");

            entity.HasIndex(e => e.SoDienThoai, "UX_Nguoi_Dung_so_dien_thoai").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AnhDaiDien)
                .HasMaxLength(500)
                .HasColumnName("anh_dai_dien");
            entity.Property(e => e.BiKhoaDatSan).HasColumnName("bi_khoa_dat_san");
            entity.Property(e => e.BiKhoaGhepTran).HasColumnName("bi_khoa_ghep_tran");
            entity.Property(e => e.DiemUyTin)
                .HasDefaultValue(100, "DF_Nguoi_Dung_diem_uy_tin")
                .HasColumnName("diem_uy_tin");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.HoVaTen)
                .HasMaxLength(100)
                .HasColumnName("ho_va_ten");
            entity.Property(e => e.KhoaDatSanDen)
                .HasPrecision(0)
                .HasColumnName("khoa_dat_san_den");
            entity.Property(e => e.KhoaDoiLichDen)
                .HasPrecision(0)
                .HasColumnName("khoa_doi_lich_den");
            entity.Property(e => e.KhoaGhepTranDen)
                .HasPrecision(0)
                .HasColumnName("khoa_ghep_tran_den");
            entity.Property(e => e.MatKhauHash)
                .HasMaxLength(255)
                .HasColumnName("mat_khau_hash");
            entity.Property(e => e.NgayCapNhat)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Nguoi_Dung_ngay_cap_nhat")
                .HasColumnName("ngay_cap_nhat");
            entity.Property(e => e.NgayTao)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Nguoi_Dung_ngay_tao")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.SoDienThoai)
                .HasMaxLength(20)
                .HasColumnName("so_dien_thoai");
            entity.Property(e => e.TrangThai)
                .HasDefaultValue(true, "DF_Nguoi_Dung_trang_thai")
                .HasColumnName("trang_thai");
        });

        modelBuilder.Entity<NguoiDungVaiTro>(entity =>
        {
            entity.ToTable("Nguoi_Dung_Vai_Tro");

            entity.HasIndex(e => e.VaiTroId, "IX_Nguoi_Dung_Vai_Tro_vai_tro_id");

            entity.HasIndex(e => new { e.NguoiDungId, e.VaiTroId }, "UX_Nguoi_Dung_Vai_Tro_nguoi_dung_id_vai_tro_id").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.NgayGan)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Nguoi_Dung_Vai_Tro_ngay_gan")
                .HasColumnName("ngay_gan");
            entity.Property(e => e.NguoiDungId).HasColumnName("nguoi_dung_id");
            entity.Property(e => e.VaiTroId).HasColumnName("vai_tro_id");

            entity.HasOne(d => d.NguoiDung).WithMany(p => p.NguoiDungVaiTros)
                .HasForeignKey(d => d.NguoiDungId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Nguoi_Dung_Vai_Tro_nguoi_dung_id");

            entity.HasOne(d => d.VaiTro).WithMany(p => p.NguoiDungVaiTros)
                .HasForeignKey(d => d.VaiTroId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Nguoi_Dung_Vai_Tro_vai_tro_id");
        });

        modelBuilder.Entity<NhatKyViPham>(entity =>
        {
            entity.ToTable("Nhat_Ky_Vi_Pham");

            entity.HasIndex(e => e.DonDatSanId, "IX_Nhat_Ky_Vi_Pham_don_dat_san_id");

            entity.HasIndex(e => e.NguoiDungId, "IX_Nhat_Ky_Vi_Pham_nguoi_dung_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DiemUyTinBiTru).HasColumnName("diem_uy_tin_bi_tru");
            entity.Property(e => e.DonDatSanId).HasColumnName("don_dat_san_id");
            entity.Property(e => e.KhoaTinhNangDen)
                .HasPrecision(0)
                .HasColumnName("khoa_tinh_nang_den");
            entity.Property(e => e.LoaiViPham)
                .HasMaxLength(50)
                .HasColumnName("loai_vi_pham");
            entity.Property(e => e.NgayGhiNhan)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Nhat_Ky_Vi_Pham_ngay_ghi_nhan")
                .HasColumnName("ngay_ghi_nhan");
            entity.Property(e => e.NguoiDungId).HasColumnName("nguoi_dung_id");
            entity.Property(e => e.NoiDungViPham).HasColumnName("noi_dung_vi_pham");

            entity.HasOne(d => d.DonDatSan).WithMany(p => p.NhatKyViPhams)
                .HasForeignKey(d => d.DonDatSanId)
                .HasConstraintName("FK_Nhat_Ky_Vi_Pham_don_dat_san_id");

            entity.HasOne(d => d.NguoiDung).WithMany(p => p.NhatKyViPhams)
                .HasForeignKey(d => d.NguoiDungId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Nhat_Ky_Vi_Pham_nguoi_dung_id");
        });

        modelBuilder.Entity<PhanQuyenThanhVien>(entity =>
        {
            entity.ToTable("Phan_Quyen_Thanh_Vien");

            entity.HasIndex(e => e.QuyenHanId, "IX_Phan_Quyen_Thanh_Vien_quyen_han_id");

            entity.HasIndex(e => new { e.ThanhVienCoSoId, e.QuyenHanId }, "UX_Phan_Quyen_Thanh_Vien_thanh_vien_co_so_id_quyen_han_id").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.QuyenHanId).HasColumnName("quyen_han_id");
            entity.Property(e => e.ThanhVienCoSoId).HasColumnName("thanh_vien_co_so_id");

            entity.HasOne(d => d.QuyenHan).WithMany(p => p.PhanQuyenThanhViens)
                .HasForeignKey(d => d.QuyenHanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Phan_Quyen_Thanh_Vien_quyen_han_id");

            entity.HasOne(d => d.ThanhVienCoSo).WithMany(p => p.PhanQuyenThanhViens)
                .HasForeignKey(d => d.ThanhVienCoSoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Phan_Quyen_Thanh_Vien_thanh_vien_co_so_id");
        });

        modelBuilder.Entity<PhongGhepTran>(entity =>
        {
            entity.ToTable("Phong_Ghep_Tran", tb => tb.HasTrigger("TR_PhongGhep_DungMon"));

            entity.HasIndex(e => e.MonTheThaoId, "IX_Phong_Ghep_Tran_mon_the_thao_id");

            entity.HasIndex(e => e.NguoiTaoId, "IX_Phong_Ghep_Tran_nguoi_tao_id");

            entity.HasIndex(e => e.ChiTietDatSanId, "UX_Phong_Ghep_Tran_chi_tiet_dat_san_id")
                .IsUnique()
                .HasFilter("([trang_thai] IN (N'DANG_TUYEN', N'DU_NGUOI', N'DANG_DAU'))");

            entity.HasIndex(e => e.MaPhong, "UX_Phong_Ghep_Tran_ma_phong").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChiTietDatSanId).HasColumnName("chi_tiet_dat_san_id");
            entity.Property(e => e.DiemEloToiDa)
                .HasDefaultValue(3000, "DF_Phong_Ghep_Tran_diem_elo_toi_da")
                .HasColumnName("diem_elo_toi_da");
            entity.Property(e => e.DiemEloToiThieu).HasColumnName("diem_elo_toi_thieu");
            entity.Property(e => e.GhiChu).HasColumnName("ghi_chu");
            entity.Property(e => e.MaPhong)
                .HasMaxLength(30)
                .HasDefaultValueSql("(left(replace(CONVERT([nvarchar](36),newid()),N'-',N''),(30)))", "DF_Phong_Ghep_Tran_ma_phong")
                .HasColumnName("ma_phong");
            entity.Property(e => e.MonTheThaoId).HasColumnName("mon_the_thao_id");
            entity.Property(e => e.NgayTao)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Phong_Ghep_Tran_ngay_tao")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.NguoiTaoId).HasColumnName("nguoi_tao_id");
            entity.Property(e => e.SoLuongToiDa).HasColumnName("so_luong_toi_da");
            entity.Property(e => e.TheLoai)
                .HasMaxLength(30)
                .HasDefaultValue("GHEP_NGUOI", "DF_Phong_Ghep_Tran_the_loai")
                .HasColumnName("the_loai");
            entity.Property(e => e.TienChiaMoiNguoi)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("tien_chia_moi_nguoi");
            entity.Property(e => e.TieuDe)
                .HasMaxLength(150)
                .HasColumnName("tieu_de");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(30)
                .HasDefaultValue("DANG_TUYEN", "DF_Phong_Ghep_Tran_trang_thai")
                .HasColumnName("trang_thai");

            entity.HasOne(d => d.ChiTietDatSan).WithOne(p => p.PhongGhepTran)
                .HasForeignKey<PhongGhepTran>(d => d.ChiTietDatSanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Phong_Ghep_Tran_chi_tiet_dat_san_id");

            entity.HasOne(d => d.MonTheThao).WithMany(p => p.PhongGhepTrans)
                .HasForeignKey(d => d.MonTheThaoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Phong_Ghep_Tran_mon_the_thao_id");

            entity.HasOne(d => d.NguoiTao).WithMany(p => p.PhongGhepTrans)
                .HasForeignKey(d => d.NguoiTaoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Phong_Ghep_Tran_nguoi_tao_id");
        });

        modelBuilder.Entity<QuyenHan>(entity =>
        {
            entity.ToTable("Quyen_Han");

            entity.HasIndex(e => e.MaQuyen, "UX_Quyen_Han_ma_quyen").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.MaQuyen)
                .HasMaxLength(100)
                .HasColumnName("ma_quyen");
            entity.Property(e => e.MoTa)
                .HasMaxLength(255)
                .HasColumnName("mo_ta");
            entity.Property(e => e.NhomChucNang)
                .HasMaxLength(50)
                .HasColumnName("nhom_chuc_nang");
        });

        modelBuilder.Entity<San>(entity =>
        {
            entity.ToTable("San", tb => tb.HasTrigger("TR_San_KhongDoiTaiNguyen"));

            entity.HasIndex(e => e.LoaiSanId, "IX_San_loai_san_id");

            entity.HasIndex(e => new { e.CoSoId, e.TenSan }, "UX_San_co_so_id_ten_san").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CoSoId).HasColumnName("co_so_id");
            entity.Property(e => e.DangBaoTri).HasColumnName("dang_bao_tri");
            entity.Property(e => e.DangHoatDong)
                .HasDefaultValue(true, "DF_San_dang_hoat_dong")
                .HasColumnName("dang_hoat_dong");
            entity.Property(e => e.GhiChu).HasColumnName("ghi_chu");
            entity.Property(e => e.LaSanTrongNha).HasColumnName("la_san_trong_nha");
            entity.Property(e => e.LoaiMatSan)
                .HasMaxLength(50)
                .HasColumnName("loai_mat_san");
            entity.Property(e => e.LoaiSanId).HasColumnName("loai_san_id");
            entity.Property(e => e.NgayCapNhat)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_San_ngay_cap_nhat")
                .HasColumnName("ngay_cap_nhat");
            entity.Property(e => e.NgayTao)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_San_ngay_tao")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.TenSan)
                .HasMaxLength(100)
                .HasColumnName("ten_san");

            entity.HasOne(d => d.CoSo).WithMany(p => p.Sans)
                .HasForeignKey(d => d.CoSoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_San_co_so_id");

            entity.HasOne(d => d.LoaiSan).WithMany(p => p.Sans)
                .HasForeignKey(d => d.LoaiSanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_San_loai_san_id");
        });

        modelBuilder.Entity<ThanhToanDon>(entity =>
        {
            entity.ToTable("Thanh_Toan_Don");

            entity.HasIndex(e => new { e.DonDatSanId, e.TrangThai }, "IX_Thanh_Toan_Don_don_dat_san_id_trang_thai");

            entity.HasIndex(e => e.NguoiThanhToanId, "IX_Thanh_Toan_Don_nguoi_thanh_toan_id");

            entity.HasIndex(e => e.NhanVienThuId, "IX_Thanh_Toan_Don_nhan_vien_thu_id");

            entity.HasIndex(e => e.GiaoDichCongId, "UX_Thanh_Toan_Don_giao_dich_cong_id")
                .IsUnique()
                .HasFilter("([giao_dich_cong_id] IS NOT NULL)");

            entity.HasIndex(e => e.GiaoDichViId, "UX_Thanh_Toan_Don_giao_dich_vi_id")
                .IsUnique()
                .HasFilter("([giao_dich_vi_id] IS NOT NULL)");

            entity.HasIndex(e => e.KhoaChongLap, "UX_Thanh_Toan_Don_khoa_chong_lap").IsUnique();

            entity.HasIndex(e => e.MaThanhToan, "UX_Thanh_Toan_Don_ma_thanh_toan").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DonDatSanId).HasColumnName("don_dat_san_id");
            entity.Property(e => e.GiaoDichCongId).HasColumnName("giao_dich_cong_id");
            entity.Property(e => e.GiaoDichViId).HasColumnName("giao_dich_vi_id");
            entity.Property(e => e.KhoaChongLap)
                .HasMaxLength(100)
                .HasColumnName("khoa_chong_lap");
            entity.Property(e => e.MaThamChieuNgoai)
                .HasMaxLength(100)
                .HasColumnName("ma_tham_chieu_ngoai");
            entity.Property(e => e.MaThanhToan)
                .HasMaxLength(50)
                .HasColumnName("ma_thanh_toan");
            entity.Property(e => e.MucDich)
                .HasMaxLength(30)
                .HasDefaultValue("THANH_TOAN", "DF_Thanh_Toan_Don_muc_dich")
                .HasColumnName("muc_dich");
            entity.Property(e => e.NgayTao)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Thanh_Toan_Don_ngay_tao")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.NgayThanhToan)
                .HasPrecision(0)
                .HasColumnName("ngay_thanh_toan");
            entity.Property(e => e.NguoiThanhToanId).HasColumnName("nguoi_thanh_toan_id");
            entity.Property(e => e.NhanVienThuId).HasColumnName("nhan_vien_thu_id");
            entity.Property(e => e.PhuongThuc)
                .HasMaxLength(30)
                .HasColumnName("phuong_thuc");
            entity.Property(e => e.SoTien)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("so_tien");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(30)
                .HasDefaultValue("KHOI_TAO", "DF_Thanh_Toan_Don_trang_thai")
                .HasColumnName("trang_thai");

            entity.HasOne(d => d.DonDatSan).WithMany(p => p.ThanhToanDons)
                .HasForeignKey(d => d.DonDatSanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Thanh_Toan_Don_don_dat_san_id");

            entity.HasOne(d => d.GiaoDichCong).WithOne(p => p.ThanhToanDon)
                .HasForeignKey<ThanhToanDon>(d => d.GiaoDichCongId)
                .HasConstraintName("FK_Thanh_Toan_Don_giao_dich_cong_id");

            entity.HasOne(d => d.GiaoDichVi).WithOne(p => p.ThanhToanDon)
                .HasForeignKey<ThanhToanDon>(d => d.GiaoDichViId)
                .HasConstraintName("FK_Thanh_Toan_Don_giao_dich_vi_id");

            entity.HasOne(d => d.NguoiThanhToan).WithMany(p => p.ThanhToanDonNguoiThanhToans)
                .HasForeignKey(d => d.NguoiThanhToanId)
                .HasConstraintName("FK_Thanh_Toan_Don_nguoi_thanh_toan_id");

            entity.HasOne(d => d.NhanVienThu).WithMany(p => p.ThanhToanDonNhanVienThus)
                .HasForeignKey(d => d.NhanVienThuId)
                .HasConstraintName("FK_Thanh_Toan_Don_nhan_vien_thu_id");
        });

        modelBuilder.Entity<ThanhVienCoSo>(entity =>
        {
            entity.ToTable("Thanh_Vien_Co_So");

            entity.HasIndex(e => e.NguoiDungId, "IX_Thanh_Vien_Co_So_nguoi_dung_id");

            entity.HasIndex(e => new { e.CoSoId, e.NguoiDungId }, "UX_Thanh_Vien_Co_So_co_so_id_nguoi_dung_id").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CoSoId).HasColumnName("co_so_id");
            entity.Property(e => e.NgayThamGia)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Thanh_Vien_Co_So_ngay_tham_gia")
                .HasColumnName("ngay_tham_gia");
            entity.Property(e => e.NguoiDungId).HasColumnName("nguoi_dung_id");
            entity.Property(e => e.TrangThaiHoatDong)
                .HasDefaultValue(true, "DF_Thanh_Vien_Co_So_trang_thai_hoat_dong")
                .HasColumnName("trang_thai_hoat_dong");
            entity.Property(e => e.VaiTroNoiBo)
                .HasMaxLength(30)
                .HasDefaultValue("NHAN_VIEN", "DF_Thanh_Vien_Co_So_vai_tro_noi_bo")
                .HasColumnName("vai_tro_noi_bo");

            entity.HasOne(d => d.CoSo).WithMany(p => p.ThanhVienCoSos)
                .HasForeignKey(d => d.CoSoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Thanh_Vien_Co_So_co_so_id");

            entity.HasOne(d => d.NguoiDung).WithMany(p => p.ThanhVienCoSos)
                .HasForeignKey(d => d.NguoiDungId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Thanh_Vien_Co_So_nguoi_dung_id");
        });

        modelBuilder.Entity<ThanhVienPhong>(entity =>
        {
            entity.ToTable("Thanh_Vien_Phong");

            entity.HasIndex(e => e.NguoiDungId, "IX_Thanh_Vien_Phong_nguoi_dung_id");

            entity.HasIndex(e => new { e.PhongGhepId, e.NguoiDungId }, "UX_Thanh_Vien_Phong_phong_ghep_id_nguoi_dung_id").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.NgayCapNhat)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Thanh_Vien_Phong_ngay_cap_nhat")
                .HasColumnName("ngay_cap_nhat");
            entity.Property(e => e.NgayThamGia)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Thanh_Vien_Phong_ngay_tham_gia")
                .HasColumnName("ngay_tham_gia");
            entity.Property(e => e.NguoiDungId).HasColumnName("nguoi_dung_id");
            entity.Property(e => e.PhongGhepId).HasColumnName("phong_ghep_id");
            entity.Property(e => e.SoDoi).HasColumnName("so_doi");
            entity.Property(e => e.TrangThaiThamGia)
                .HasMaxLength(30)
                .HasDefaultValue("CHO_DUYET", "DF_Thanh_Vien_Phong_trang_thai_tham_gia")
                .HasColumnName("trang_thai_tham_gia");
            entity.Property(e => e.VaiTroPhong)
                .HasMaxLength(30)
                .HasDefaultValue("THANH_VIEN", "DF_Thanh_Vien_Phong_vai_tro_phong")
                .HasColumnName("vai_tro_phong");

            entity.HasOne(d => d.NguoiDung).WithMany(p => p.ThanhVienPhongs)
                .HasForeignKey(d => d.NguoiDungId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Thanh_Vien_Phong_nguoi_dung_id");

            entity.HasOne(d => d.PhongGhep).WithMany(p => p.ThanhVienPhongs)
                .HasForeignKey(d => d.PhongGhepId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Thanh_Vien_Phong_phong_ghep_id");
        });

        modelBuilder.Entity<ThongBao>(entity =>
        {
            entity.ToTable("Thong_Bao");

            entity.HasIndex(e => new { e.NguoiDungId, e.DaDoc, e.NgayTao }, "IX_Thong_Bao_nguoi_dung_id_da_doc_ngay_tao");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DaDoc).HasColumnName("da_doc");
            entity.Property(e => e.LoaiThamChieu)
                .HasMaxLength(50)
                .HasColumnName("loai_tham_chieu");
            entity.Property(e => e.LoaiThongBao)
                .HasMaxLength(50)
                .HasColumnName("loai_thong_bao");
            entity.Property(e => e.MaThamChieuId).HasColumnName("ma_tham_chieu_id");
            entity.Property(e => e.NgayDoc)
                .HasPrecision(0)
                .HasColumnName("ngay_doc");
            entity.Property(e => e.NgayTao)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Thong_Bao_ngay_tao")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.NguoiDungId).HasColumnName("nguoi_dung_id");
            entity.Property(e => e.NoiDung).HasColumnName("noi_dung");
            entity.Property(e => e.TieuDe)
                .HasMaxLength(150)
                .HasColumnName("tieu_de");

            entity.HasOne(d => d.NguoiDung).WithMany(p => p.ThongBaos)
                .HasForeignKey(d => d.NguoiDungId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Thong_Bao_nguoi_dung_id");
        });

        modelBuilder.Entity<VCongNoDon>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_CongNoDon");

            entity.Property(e => e.CoSoId).HasColumnName("co_so_id");
            entity.Property(e => e.DonDatSanId).HasColumnName("don_dat_san_id");
            entity.Property(e => e.MaDatSan)
                .HasMaxLength(30)
                .HasColumnName("ma_dat_san");
            entity.Property(e => e.NguoiDungId).HasColumnName("nguoi_dung_id");
            entity.Property(e => e.SoTienConPhaiThu)
                .HasColumnType("decimal(38, 2)")
                .HasColumnName("so_tien_con_phai_thu");
            entity.Property(e => e.TongDaHoan)
                .HasColumnType("decimal(38, 2)")
                .HasColumnName("tong_da_hoan");
            entity.Property(e => e.TongDaThu)
                .HasColumnType("decimal(38, 2)")
                .HasColumnName("tong_da_thu");
            entity.Property(e => e.TongTienPhaiTra)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("tong_tien_phai_tra");
        });

        modelBuilder.Entity<VLichSan>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_LichSan");

            entity.Property(e => e.BatDauLuc)
                .HasPrecision(0)
                .HasColumnName("bat_dau_luc");
            entity.Property(e => e.ChiTietDatSanId).HasColumnName("chi_tiet_dat_san_id");
            entity.Property(e => e.ChiemLich).HasColumnName("chiem_lich");
            entity.Property(e => e.CoSoId).HasColumnName("co_so_id");
            entity.Property(e => e.DonDatSanId).HasColumnName("don_dat_san_id");
            entity.Property(e => e.GiaThue)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("gia_thue");
            entity.Property(e => e.GioBatDau)
                .HasPrecision(0)
                .HasColumnName("gio_bat_dau");
            entity.Property(e => e.GioKetThuc)
                .HasPrecision(0)
                .HasColumnName("gio_ket_thuc");
            entity.Property(e => e.KetThucLuc)
                .HasPrecision(0)
                .HasColumnName("ket_thuc_luc");
            entity.Property(e => e.MaDatSan)
                .HasMaxLength(30)
                .HasColumnName("ma_dat_san");
            entity.Property(e => e.NgaySuDung).HasColumnName("ngay_su_dung");
            entity.Property(e => e.SanId).HasColumnName("san_id");
            entity.Property(e => e.TenSan)
                .HasMaxLength(100)
                .HasColumnName("ten_san");
            entity.Property(e => e.ThoiGianHetHanGiuCho)
                .HasPrecision(0)
                .HasColumnName("thoi_gian_het_han_giu_cho");
            entity.Property(e => e.TrangThaiSlot)
                .HasMaxLength(30)
                .HasColumnName("trang_thai_slot");
        });

        modelBuilder.Entity<VaiTro>(entity =>
        {
            entity.ToTable("Vai_Tro");

            entity.HasIndex(e => e.MaVaiTro, "UX_Vai_Tro_ma_vai_tro").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.MaVaiTro)
                .HasMaxLength(50)
                .HasColumnName("ma_vai_tro");
            entity.Property(e => e.MoTa)
                .HasMaxLength(255)
                .HasColumnName("mo_ta");
            entity.Property(e => e.TenVaiTro)
                .HasMaxLength(100)
                .HasColumnName("ten_vai_tro");
        });

        modelBuilder.Entity<ViNguoiDung>(entity =>
        {
            entity.ToTable("Vi_Nguoi_Dung");

            entity.HasIndex(e => e.NguoiDungId, "UX_Vi_Nguoi_Dung_nguoi_dung_id").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.NgayCapNhat)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Vi_Nguoi_Dung_ngay_cap_nhat")
                .HasColumnName("ngay_cap_nhat");
            entity.Property(e => e.NgayTao)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Vi_Nguoi_Dung_ngay_tao")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.NguoiDungId).HasColumnName("nguoi_dung_id");
            entity.Property(e => e.SoDuDongBang)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("so_du_dong_bang");
            entity.Property(e => e.SoDuKhaDung)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("so_du_kha_dung");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(30)
                .HasDefaultValue("HOAT_DONG", "DF_Vi_Nguoi_Dung_trang_thai")
                .HasColumnName("trang_thai");

            entity.HasOne(d => d.NguoiDung).WithOne(p => p.ViNguoiDung)
                .HasForeignKey<ViNguoiDung>(d => d.NguoiDungId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Vi_Nguoi_Dung_nguoi_dung_id");
        });

        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.ToTable("Voucher");

            entity.HasIndex(e => e.CoSoId, "IX_Voucher_co_so_id");

            entity.HasIndex(e => e.MaVoucher, "UX_Voucher_ma_voucher").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BenChiuGiam)
                .HasMaxLength(20)
                .HasColumnName("ben_chiu_giam");
            entity.Property(e => e.ChiApDungGioThapDiem).HasColumnName("chi_ap_dung_gio_thap_diem");
            entity.Property(e => e.CoSoId).HasColumnName("co_so_id");
            entity.Property(e => e.GiaTriDonToiThieu)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("gia_tri_don_toi_thieu");
            entity.Property(e => e.GiaTriGiam)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("gia_tri_giam");
            entity.Property(e => e.GiamToiDa)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("giam_toi_da");
            entity.Property(e => e.GioiHanMoiUser).HasColumnName("gioi_han_moi_user");
            entity.Property(e => e.LoaiGiam)
                .HasMaxLength(20)
                .HasColumnName("loai_giam");
            entity.Property(e => e.LoaiPhatHanh)
                .HasMaxLength(30)
                .HasColumnName("loai_phat_hanh");
            entity.Property(e => e.MaVoucher)
                .HasMaxLength(30)
                .HasColumnName("ma_voucher");
            entity.Property(e => e.NgayBatDau)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Voucher_ngay_bat_dau")
                .HasColumnName("ngay_bat_dau");
            entity.Property(e => e.NgayKetThuc)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Voucher_ngay_ket_thuc")
                .HasColumnName("ngay_ket_thuc");
            entity.Property(e => e.NgayTao)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Voucher_ngay_tao")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.SoLuongDaDung).HasColumnName("so_luong_da_dung");
            entity.Property(e => e.SoLuongTong).HasColumnName("so_luong_tong");
            entity.Property(e => e.TieuDe)
                .HasMaxLength(150)
                .HasColumnName("tieu_de");
            entity.Property(e => e.TrangThai)
                .HasDefaultValue(true, "DF_Voucher_trang_thai")
                .HasColumnName("trang_thai");

            entity.HasOne(d => d.CoSo).WithMany(p => p.Vouchers)
                .HasForeignKey(d => d.CoSoId)
                .HasConstraintName("FK_Voucher_co_so_id");
        });

        modelBuilder.Entity<VoucherNguoiDung>(entity =>
        {
            entity.ToTable("Voucher_Nguoi_Dung");

            entity.HasIndex(e => e.VoucherId, "IX_Voucher_Nguoi_Dung_voucher_id");

            entity.HasIndex(e => new { e.NguoiDungId, e.VoucherId }, "UX_Voucher_Nguoi_Dung_nguoi_dung_id_voucher_id").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.NgayThuThap)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Voucher_Nguoi_Dung_ngay_thu_thap")
                .HasColumnName("ngay_thu_thap");
            entity.Property(e => e.NguoiDungId).HasColumnName("nguoi_dung_id");
            entity.Property(e => e.VoucherId).HasColumnName("voucher_id");

            entity.HasOne(d => d.NguoiDung).WithMany(p => p.VoucherNguoiDungs)
                .HasForeignKey(d => d.NguoiDungId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Voucher_Nguoi_Dung_nguoi_dung_id");

            entity.HasOne(d => d.Voucher).WithMany(p => p.VoucherNguoiDungs)
                .HasForeignKey(d => d.VoucherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Voucher_Nguoi_Dung_voucher_id");
        });

        modelBuilder.Entity<YeuCauDoiLich>(entity =>
        {
            entity.ToTable("Yeu_Cau_Doi_Lich");

            entity.HasIndex(e => e.KhungGioMoiId, "IX_Yeu_Cau_Doi_Lich_khung_gio_moi_id");

            entity.HasIndex(e => e.NguoiXuLyId, "IX_Yeu_Cau_Doi_Lich_nguoi_xu_ly_id");

            entity.HasIndex(e => e.NguoiYeuCauId, "IX_Yeu_Cau_Doi_Lich_nguoi_yeu_cau_id");

            entity.HasIndex(e => e.SanMoiId, "IX_Yeu_Cau_Doi_Lich_san_moi_id");

            entity.HasIndex(e => e.ChiTietDatSanCuId, "UX_Yeu_Cau_Doi_Lich_chi_tiet_dat_san_cu_id")
                .IsUnique()
                .HasFilter("([trang_thai]=N'CHO_DUYET')");

            entity.HasIndex(e => e.ChiTietDatSanMoiId, "UX_Yeu_Cau_Doi_Lich_chi_tiet_dat_san_moi_id")
                .IsUnique()
                .HasFilter("([chi_tiet_dat_san_moi_id] IS NOT NULL)");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChenhLechGia)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("chenh_lech_gia");
            entity.Property(e => e.ChiTietDatSanCuId).HasColumnName("chi_tiet_dat_san_cu_id");
            entity.Property(e => e.ChiTietDatSanMoiId).HasColumnName("chi_tiet_dat_san_moi_id");
            entity.Property(e => e.GioBatDauMoi)
                .HasPrecision(0)
                .HasColumnName("gio_bat_dau_moi");
            entity.Property(e => e.GioKetThucMoi)
                .HasPrecision(0)
                .HasColumnName("gio_ket_thuc_moi");
            entity.Property(e => e.KhungGioMoiId).HasColumnName("khung_gio_moi_id");
            entity.Property(e => e.LyDo).HasColumnName("ly_do");
            entity.Property(e => e.NgaySuDungMoi).HasColumnName("ngay_su_dung_moi");
            entity.Property(e => e.NgayTao)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Yeu_Cau_Doi_Lich_ngay_tao")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.NgayXuLy)
                .HasPrecision(0)
                .HasColumnName("ngay_xu_ly");
            entity.Property(e => e.NguoiXuLyId).HasColumnName("nguoi_xu_ly_id");
            entity.Property(e => e.NguoiYeuCauId).HasColumnName("nguoi_yeu_cau_id");
            entity.Property(e => e.PhiDoiLich)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("phi_doi_lich");
            entity.Property(e => e.SanMoiId).HasColumnName("san_moi_id");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(30)
                .HasDefaultValue("CHO_DUYET", "DF_Yeu_Cau_Doi_Lich_trang_thai")
                .HasColumnName("trang_thai");

            entity.HasOne(d => d.ChiTietDatSanCu).WithOne(p => p.YeuCauDoiLichChiTietDatSanCu)
                .HasForeignKey<YeuCauDoiLich>(d => d.ChiTietDatSanCuId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Yeu_Cau_Doi_Lich_chi_tiet_dat_san_cu_id");

            entity.HasOne(d => d.ChiTietDatSanMoi).WithOne(p => p.YeuCauDoiLichChiTietDatSanMoi)
                .HasForeignKey<YeuCauDoiLich>(d => d.ChiTietDatSanMoiId)
                .HasConstraintName("FK_Yeu_Cau_Doi_Lich_chi_tiet_dat_san_moi_id");

            entity.HasOne(d => d.KhungGioMoi).WithMany(p => p.YeuCauDoiLiches)
                .HasForeignKey(d => d.KhungGioMoiId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Yeu_Cau_Doi_Lich_khung_gio_moi_id");

            entity.HasOne(d => d.NguoiXuLy).WithMany(p => p.YeuCauDoiLichNguoiXuLies)
                .HasForeignKey(d => d.NguoiXuLyId)
                .HasConstraintName("FK_Yeu_Cau_Doi_Lich_nguoi_xu_ly_id");

            entity.HasOne(d => d.NguoiYeuCau).WithMany(p => p.YeuCauDoiLichNguoiYeuCaus)
                .HasForeignKey(d => d.NguoiYeuCauId)
                .HasConstraintName("FK_Yeu_Cau_Doi_Lich_nguoi_yeu_cau_id");

            entity.HasOne(d => d.SanMoi).WithMany(p => p.YeuCauDoiLiches)
                .HasForeignKey(d => d.SanMoiId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Yeu_Cau_Doi_Lich_san_moi_id");
        });

        modelBuilder.Entity<YeuCauRutTien>(entity =>
        {
            entity.ToTable("Yeu_Cau_Rut_Tien");

            entity.HasIndex(e => e.NhanVienDuyetId, "IX_Yeu_Cau_Rut_Tien_nhan_vien_duyet_id");

            entity.HasIndex(e => e.ViId, "IX_Yeu_Cau_Rut_Tien_vi_id");

            entity.HasIndex(e => e.GiaoDichCongId, "UX_Yeu_Cau_Rut_Tien_giao_dich_cong_id")
                .IsUnique()
                .HasFilter("([giao_dich_cong_id] IS NOT NULL)");

            entity.HasIndex(e => e.GiaoDichGiuTienId, "UX_Yeu_Cau_Rut_Tien_giao_dich_giu_tien_id").IsUnique();

            entity.HasIndex(e => e.GiaoDichKetThucId, "UX_Yeu_Cau_Rut_Tien_giao_dich_ket_thuc_id")
                .IsUnique()
                .HasFilter("([giao_dich_ket_thuc_id] IS NOT NULL)");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GiaoDichCongId).HasColumnName("giao_dich_cong_id");
            entity.Property(e => e.GiaoDichGiuTienId).HasColumnName("giao_dich_giu_tien_id");
            entity.Property(e => e.GiaoDichKetThucId).HasColumnName("giao_dich_ket_thuc_id");
            entity.Property(e => e.LyDoTuChoi).HasColumnName("ly_do_tu_choi");
            entity.Property(e => e.NgayTao)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Yeu_Cau_Rut_Tien_ngay_tao")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.NgayXuLy)
                .HasPrecision(0)
                .HasColumnName("ngay_xu_ly");
            entity.Property(e => e.NhanVienDuyetId).HasColumnName("nhan_vien_duyet_id");
            entity.Property(e => e.SoTaiKhoan)
                .HasMaxLength(50)
                .HasColumnName("so_tai_khoan");
            entity.Property(e => e.SoTien)
                .HasColumnType("decimal(14, 2)")
                .HasColumnName("so_tien");
            entity.Property(e => e.TenChuTaiKhoan)
                .HasMaxLength(100)
                .HasColumnName("ten_chu_tai_khoan");
            entity.Property(e => e.TenNganHang)
                .HasMaxLength(100)
                .HasColumnName("ten_ngan_hang");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(30)
                .HasDefaultValue("CHO_DUYET", "DF_Yeu_Cau_Rut_Tien_trang_thai")
                .HasColumnName("trang_thai");
            entity.Property(e => e.ViId).HasColumnName("vi_id");

            entity.HasOne(d => d.GiaoDichCong).WithOne(p => p.YeuCauRutTien)
                .HasForeignKey<YeuCauRutTien>(d => d.GiaoDichCongId)
                .HasConstraintName("FK_Yeu_Cau_Rut_Tien_giao_dich_cong_id");

            entity.HasOne(d => d.GiaoDichGiuTien).WithOne(p => p.YeuCauRutTienGiaoDichGiuTien)
                .HasForeignKey<YeuCauRutTien>(d => d.GiaoDichGiuTienId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Yeu_Cau_Rut_Tien_giao_dich_giu_tien_id");

            entity.HasOne(d => d.GiaoDichKetThuc).WithOne(p => p.YeuCauRutTienGiaoDichKetThuc)
                .HasForeignKey<YeuCauRutTien>(d => d.GiaoDichKetThucId)
                .HasConstraintName("FK_Yeu_Cau_Rut_Tien_giao_dich_ket_thuc_id");

            entity.HasOne(d => d.NhanVienDuyet).WithMany(p => p.YeuCauRutTiens)
                .HasForeignKey(d => d.NhanVienDuyetId)
                .HasConstraintName("FK_Yeu_Cau_Rut_Tien_nhan_vien_duyet_id");

            entity.HasOne(d => d.Vi).WithMany(p => p.YeuCauRutTiens)
                .HasForeignKey(d => d.ViId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Yeu_Cau_Rut_Tien_vi_id");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
