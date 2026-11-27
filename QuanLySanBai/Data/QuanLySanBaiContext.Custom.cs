using Microsoft.EntityFrameworkCore;
using QuanLySanBai.Models.Entities;

namespace QuanLySanBai.Data;

public partial class QuanLySanBaiContext
{
    public virtual DbSet<NhatKyWebhook> NhatKyWebhooks { get; set; } = null!;
    public virtual DbSet<HinhAnhSan> HinhAnhSans { get; set; } = null!;

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DonDatSan>().Property(d => d.RowVersion).IsRowVersion().HasColumnName("row_version");

        modelBuilder.Entity<ThanhToanDon>(entity =>
        {
            entity.Property(e => e.SoTienKhachDua).HasColumnType("decimal(18, 2)").HasColumnName("so_tien_khach_dua");
            entity.Property(e => e.TienThua).HasColumnType("decimal(18, 2)").HasColumnName("tien_thua");
        });

        modelBuilder.Entity<GiaoDichCongThanhToan>(entity =>
        {
            entity.Property(e => e.DonDatSanId).HasColumnName("don_dat_san_id");
            entity.Property(e => e.ThoiGianHetHanQr).HasPrecision(0).HasColumnName("thoi_gian_het_han_qr");
            entity.Property(e => e.NoiDungChuyenKhoan).HasMaxLength(200).HasColumnName("noi_dung_chuyen_khoan");
            entity.Property(e => e.SepayTransactionId).HasColumnName("sepay_transaction_id");

            entity.HasIndex(e => e.DonDatSanId, "IX_Giao_Dich_Cong_Thanh_Toan_don_dat_san_id")
                .HasFilter("([don_dat_san_id] IS NOT NULL)");

            entity.HasIndex(e => e.SepayTransactionId, "UX_Giao_Dich_Cong_Thanh_Toan_sepay_transaction_id")
                .IsUnique()
                .HasFilter("([sepay_transaction_id] IS NOT NULL)");

            entity.HasOne(e => e.DonDatSan)
                .WithMany(d => d.GiaoDichCongThanhToans)
                .HasForeignKey(e => e.DonDatSanId)
                .HasConstraintName("FK_Giao_Dich_Cong_Thanh_Toan_don_dat_san_id");
        });

        modelBuilder.Entity<NhatKyWebhook>(entity =>
        {
            entity.ToTable("Nhat_Ky_Webhook");
            entity.HasKey(e => e.Id).HasName("PK_Nhat_Ky_Webhook");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CongThanhToan).HasMaxLength(30).HasColumnName("cong_thanh_toan");
            entity.Property(e => e.SepayTransactionId).HasColumnName("sepay_transaction_id");
            entity.Property(e => e.MaThanhToan).HasMaxLength(100).HasColumnName("ma_thanh_toan");
            entity.Property(e => e.GiaoDichCongId).HasColumnName("giao_dich_cong_id");
            entity.Property(e => e.LoaiChuyenTien).HasMaxLength(20).HasColumnName("loai_chuyen_tien");
            entity.Property(e => e.SoTaiKhoanNhan).HasMaxLength(50).HasColumnName("so_tai_khoan_nhan");
            entity.Property(e => e.SoTien).HasColumnType("decimal(18, 2)").HasColumnName("so_tien");
            entity.Property(e => e.NoiDung).HasColumnName("noi_dung");
            entity.Property(e => e.TrangThaiXuLy).HasMaxLength(50).HasColumnName("trang_thai_xu_ly");
            entity.Property(e => e.GhiChu).HasMaxLength(500).HasColumnName("ghi_chu");
            entity.Property(e => e.DuLieuGoc).HasColumnName("du_lieu_goc");
            entity.Property(e => e.NgayTao).HasColumnName("ngay_tao")
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Nhat_Ky_Webhook_ngay_tao");

            entity.HasIndex(e => e.SepayTransactionId, "IX_Nhat_Ky_Webhook_sepay_transaction_id")
                .HasFilter("([sepay_transaction_id] IS NOT NULL)");
            entity.HasIndex(e => e.NgayTao, "IX_Nhat_Ky_Webhook_ngay_tao");

            entity.HasOne(e => e.GiaoDichCong)
                .WithMany(g => g.NhatKyWebhooks)
                .HasForeignKey(e => e.GiaoDichCongId)
                .HasConstraintName("FK_Nhat_Ky_Webhook_giao_dich_cong_id");
        });

        modelBuilder.Entity<HinhAnhSan>(entity =>
        {
            entity.ToTable("Hinh_Anh_San");
            entity.HasKey(e => e.Id).HasName("PK_Hinh_Anh_San");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SanId).HasColumnName("san_id");
            entity.Property(e => e.DuongDan).HasMaxLength(500).HasColumnName("duong_dan");
            entity.Property(e => e.LaAnhDaiDien).HasColumnName("la_anh_dai_dien");
            entity.Property(e => e.ThuTuHienThi).HasColumnName("thu_tu_hien_thi");
            entity.Property(e => e.NgayTao).HasPrecision(0).HasColumnName("ngay_tao")
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Hinh_Anh_San_ngay_tao");
            entity.Property(e => e.NgayCapNhat).HasPrecision(0).HasColumnName("ngay_cap_nhat")
                .HasDefaultValueSql("(dateadd(hour,(7),sysutcdatetime()))", "DF_Hinh_Anh_San_ngay_cap_nhat");

            entity.HasIndex(e => new { e.SanId, e.LaAnhDaiDien, e.ThuTuHienThi, e.Id }, "IX_Hinh_Anh_San_san_id_thu_tu");
            entity.HasIndex(e => e.SanId, "UX_Hinh_Anh_San_san_id_dai_dien")
                .IsUnique()
                .HasFilter("([la_anh_dai_dien]=(1))");

            entity.HasOne(e => e.San)
                .WithMany(s => s.HinhAnhSans)
                .HasForeignKey(e => e.SanId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Hinh_Anh_San_san_id");
        });

        // Keep scaffold-generated files untouched. SQL OUTPUT is incompatible with these triggers.
        foreach (var entity in modelBuilder.Model.GetEntityTypes().Where(e => e.GetDeclaredTriggers().Any()))
            entity.UseSqlOutputClause(false);
    }
}
