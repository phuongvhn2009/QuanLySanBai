/*
QUẢN LÝ SÂN BÃI - SQL SERVER 2019+ (48 bảng)
Dựa trên Quan_Ly_San_Bai_ERD_Eraser.txt đã chỉnh sửa.

CÁCH CHẠY: mở file trong SSMS, đăng nhập tài khoản có quyền CREATE DATABASE,
chạy toàn bộ bằng Execute (F5). GO là dấu tách batch của SSMS/sqlcmd.
Tên database mặc định: QuanLySanBai. Đổi cả CREATE DATABASE và USE nếu cần.
Chỉ cài trên database mới/chưa có bảng ứng dụng. KHÔNG có DROP DATABASE/TABLE.
Nếu đã cài, script sẽ báo lỗi trước khi tạo bảng để tránh ghi đè.

Quy ước: IDENTITY(1,1); Unicode NVARCHAR; BIT; DATETIME2(0).
DATETIME2 lưu giờ Việt Nam (UTC+7); ứng dụng cũng phải gửi giờ Việt Nam.
Giờ kết thúc 00:00 của một khung được hiểu là 24:00 ngày đó.
Đồng bộ DECIMAL(14,2) cho tiền để tránh tổng đơn lớn hơn giới hạn dòng chi tiết.
Một số cột dẫn xuất dùng computed persisted: không INSERT/UPDATE trực tiếp.
ngay_cap_nhat chỉ có mặc định khi tạo, ứng dụng cập nhật khi sửa.

Phần DDL/seed chạy trong một transaction. Không chứa tài khoản/mật khẩu mẫu.
Có trigger kiểm tra lịch/cùng cơ sở, dữ liệu giá/chính sách và một số quan hệ nghiệp vụ.
Đặt mới qua usp_DatSanMotKhung; job hết hạn gọi usp_GiaiPhongGiuChoHetHan.
Khóa lịch toàn hệ thống là lựa chọn đơn giản cho đồ án; ứng dụng xử lý retry khi deadlock/timeout.

ĐÂY LÀ SCHEMA + NGHIỆP VỤ ĐẶT MỘT KHUNG, KHÔNG PHẢI TOÀN BỘ BACKEND:
- sinh lịch cố định, đổi/hủy, xác thực webhook, cập nhật ví, áp voucher, Elo,
  tính lại tổng đơn và chốt đối soát phải viết service/procedure có transaction.
- Không tự động chuyển tiền khi INSERT Thanh_Toan_Don/Hoan_Tien/Nap_Tien_Vi.
- Tổng tiền sân/dịch vụ/phạt và bộ đếm là cache; service cập nhật cùng transaction.
- Không cấp quyền ghi trực tiếp bảng ví/tài chính cho client.
- Các ràng buộc quyền người dùng, trạng thái chuyển tiếp và liên kết nguồn tiền
  xuyên nhiều bảng chưa được thay thế bằng một hệ thống phân quyền/ledger hoàn chỉnh.
- Khi thay đổi cấu hình phải dùng quy trình quản trị, không sửa khóa cơ sở/sân
  của dữ liệu đã phát sinh lịch. Không coi quyền dbo là quyền ứng dụng.

Tài liệu Microsoft tham khảo:
https://learn.microsoft.com/sql/relational-databases/system-stored-procedures/sp-getapplock-transact-sql
https://learn.microsoft.com/sql/relational-databases/indexes/create-filtered-indexes
https://learn.microsoft.com/sql/relational-databases/triggers/create-dml-triggers-to-handle-multiple-rows-of-data
*/
USE [master];
GO
IF DB_ID(N'QuanLySanBai') IS NULL
    EXEC(N'CREATE DATABASE [QuanLySanBai]');
GO
USE [QuanLySanBai];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
GO
-- Toàn bộ bảng/FK/index/seed trong một batch để rollback nếu có lỗi.
BEGIN TRY
    IF EXISTS (SELECT 1 FROM sys.tables WHERE schema_id = SCHEMA_ID(N'dbo') AND is_ms_shipped = 0)
        THROW 51000, N'Database đã có bảng. Hãy dùng database mới hoặc viết migration; script không xóa dữ liệu.', 1;
    BEGIN TRANSACTION;

-- Nguoi_Dung
CREATE TABLE [dbo].[Nguoi_Dung] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Nguoi_Dung] PRIMARY KEY,
    [so_dien_thoai] nvarchar(20) NOT NULL,
    [email] nvarchar(255) NULL,
    [mat_khau_hash] nvarchar(255) NOT NULL,
    [ho_va_ten] nvarchar(100) NOT NULL,
    [anh_dai_dien] nvarchar(500) NULL,
    [diem_uy_tin] int NOT NULL CONSTRAINT [DF_Nguoi_Dung_diem_uy_tin] DEFAULT (100),
    [bi_khoa_ghep_tran] bit NOT NULL CONSTRAINT [DF_Nguoi_Dung_bi_khoa_ghep_tran] DEFAULT (0),
    [bi_khoa_dat_san] bit NOT NULL CONSTRAINT [DF_Nguoi_Dung_bi_khoa_dat_san] DEFAULT (0),
    [khoa_doi_lich_den] datetime2(0) NULL,
    [trang_thai] bit NOT NULL CONSTRAINT [DF_Nguoi_Dung_trang_thai] DEFAULT (1),
    [ngay_tao] datetime2(0) NOT NULL CONSTRAINT [DF_Nguoi_Dung_ngay_tao] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    [ngay_cap_nhat] datetime2(0) NOT NULL CONSTRAINT [DF_Nguoi_Dung_ngay_cap_nhat] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    [khoa_ghep_tran_den] datetime2(0) NULL,
    [khoa_dat_san_den] datetime2(0) NULL,
    CONSTRAINT [CK_Nguoi_Dung_DiemUyTin] CHECK (diem_uy_tin BETWEEN 0 AND 100),
    CONSTRAINT [CK_Nguoi_Dung_SDT] CHECK (LEN(LTRIM(RTRIM(so_dien_thoai))) BETWEEN 8 AND 20)
);

-- Vai_Tro
CREATE TABLE [dbo].[Vai_Tro] (
    [id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Vai_Tro] PRIMARY KEY,
    [ma_vai_tro] nvarchar(50) NOT NULL,
    [ten_vai_tro] nvarchar(100) NOT NULL,
    [mo_ta] nvarchar(255) NULL
);

-- Nguoi_Dung_Vai_Tro
CREATE TABLE [dbo].[Nguoi_Dung_Vai_Tro] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Nguoi_Dung_Vai_Tro] PRIMARY KEY,
    [nguoi_dung_id] bigint NOT NULL,
    [vai_tro_id] int NOT NULL,
    [ngay_gan] datetime2(0) NOT NULL CONSTRAINT [DF_Nguoi_Dung_Vai_Tro_ngay_gan] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME()))
);

-- Quyen_Han
CREATE TABLE [dbo].[Quyen_Han] (
    [id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Quyen_Han] PRIMARY KEY,
    [ma_quyen] nvarchar(100) NOT NULL,
    [nhom_chuc_nang] nvarchar(50) NULL,
    [mo_ta] nvarchar(255) NULL
);

-- Thanh_Vien_Co_So
CREATE TABLE [dbo].[Thanh_Vien_Co_So] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Thanh_Vien_Co_So] PRIMARY KEY,
    [co_so_id] bigint NOT NULL,
    [nguoi_dung_id] bigint NOT NULL,
    [vai_tro_noi_bo] nvarchar(30) NOT NULL CONSTRAINT [DF_Thanh_Vien_Co_So_vai_tro_noi_bo] DEFAULT (N'NHAN_VIEN'),
    [trang_thai_hoat_dong] bit NOT NULL CONSTRAINT [DF_Thanh_Vien_Co_So_trang_thai_hoat_dong] DEFAULT (1),
    [ngay_tham_gia] datetime2(0) NOT NULL CONSTRAINT [DF_Thanh_Vien_Co_So_ngay_tham_gia] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    CONSTRAINT [CK_Thanh_Vien_Co_So_vai_tro_noi_bo] CHECK ([vai_tro_noi_bo] IN (N'CHU_SAN', N'QUAN_LY', N'NHAN_VIEN'))
);

-- Phan_Quyen_Thanh_Vien
CREATE TABLE [dbo].[Phan_Quyen_Thanh_Vien] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Phan_Quyen_Thanh_Vien] PRIMARY KEY,
    [thanh_vien_co_so_id] bigint NOT NULL,
    [quyen_han_id] int NOT NULL
);

-- Thong_Bao
CREATE TABLE [dbo].[Thong_Bao] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Thong_Bao] PRIMARY KEY,
    [nguoi_dung_id] bigint NOT NULL,
    [tieu_de] nvarchar(150) NOT NULL,
    [noi_dung] nvarchar(max) NOT NULL,
    [loai_thong_bao] nvarchar(50) NOT NULL,
    [ma_tham_chieu_id] bigint NULL,
    [da_doc] bit NOT NULL CONSTRAINT [DF_Thong_Bao_da_doc] DEFAULT (0),
    [ngay_tao] datetime2(0) NOT NULL CONSTRAINT [DF_Thong_Bao_ngay_tao] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    [loai_tham_chieu] nvarchar(50) NULL,
    [ngay_doc] datetime2(0) NULL
);

-- Co_So
CREATE TABLE [dbo].[Co_So] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Co_So] PRIMARY KEY,
    [chu_so_huu_id] bigint NOT NULL,
    [ten_co_so] nvarchar(150) NOT NULL,
    [dia_chi] nvarchar(255) NOT NULL,
    [kinh_do] decimal(10,7) NULL,
    [vi_do] decimal(10,7) NULL,
    [so_dien_thoai_hotline] nvarchar(20) NULL,
    [anh_dai_dien] nvarchar(500) NULL,
    [gio_mo_cua] time(0) NOT NULL,
    [gio_dong_cua] time(0) NOT NULL,
    [tien_ich] nvarchar(max) NULL,
    [trang_thai_duyet] nvarchar(30) NOT NULL CONSTRAINT [DF_Co_So_trang_thai_duyet] DEFAULT (N'CHO_DUYET'),
    [dang_hoat_dong] bit NOT NULL CONSTRAINT [DF_Co_So_dang_hoat_dong] DEFAULT (1),
    [ngay_tao] datetime2(0) NOT NULL CONSTRAINT [DF_Co_So_ngay_tao] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    [ngay_cap_nhat] datetime2(0) NOT NULL CONSTRAINT [DF_Co_So_ngay_cap_nhat] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    CONSTRAINT [CK_Co_So_trang_thai_duyet] CHECK ([trang_thai_duyet] IN (N'CHO_DUYET', N'DA_DUYET', N'TU_CHOI')),
    CONSTRAINT [CK_Co_So_ToaDo] CHECK ((kinh_do IS NULL AND vi_do IS NULL) OR (kinh_do IS NOT NULL AND vi_do IS NOT NULL AND kinh_do BETWEEN -180 AND 180 AND vi_do BETWEEN -90 AND 90)),
    CONSTRAINT [CK_Co_So_GioMoCua] CHECK (gio_mo_cua < gio_dong_cua OR (gio_dong_cua = '00:00:00' AND gio_mo_cua > '00:00:00'))
);

-- Mon_The_Thao
CREATE TABLE [dbo].[Mon_The_Thao] (
    [id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Mon_The_Thao] PRIMARY KEY,
    [ma_mon] nvarchar(50) NOT NULL,
    [ten_mon] nvarchar(100) NOT NULL,
    [bieu_tuong_url] nvarchar(500) NULL,
    [trang_thai] bit NOT NULL CONSTRAINT [DF_Mon_The_Thao_trang_thai] DEFAULT (1)
);

-- Loai_San
CREATE TABLE [dbo].[Loai_San] (
    [id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Loai_San] PRIMARY KEY,
    [mon_the_thao_id] int NOT NULL,
    [ten_loai] nvarchar(100) NOT NULL,
    [kich_thuoc] nvarchar(50) NULL,
    [suc_chua_nguoi] int NOT NULL,
    [mo_ta] nvarchar(max) NULL,
    CONSTRAINT [CK_Loai_San_SucChua] CHECK (suc_chua_nguoi > 0)
);

-- San
CREATE TABLE [dbo].[San] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_San] PRIMARY KEY,
    [co_so_id] bigint NOT NULL,
    [loai_san_id] int NOT NULL,
    [ten_san] nvarchar(100) NOT NULL,
    [la_san_trong_nha] bit NOT NULL CONSTRAINT [DF_San_la_san_trong_nha] DEFAULT (0),
    [loai_mat_san] nvarchar(50) NULL,
    [dang_bao_tri] bit NOT NULL CONSTRAINT [DF_San_dang_bao_tri] DEFAULT (0),
    [ghi_chu] nvarchar(max) NULL,
    [dang_hoat_dong] bit NOT NULL CONSTRAINT [DF_San_dang_hoat_dong] DEFAULT (1),
    [ngay_tao] datetime2(0) NOT NULL CONSTRAINT [DF_San_ngay_tao] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    [ngay_cap_nhat] datetime2(0) NOT NULL CONSTRAINT [DF_San_ngay_cap_nhat] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME()))
);

-- Khung_Gio_Mac_Dinh
CREATE TABLE [dbo].[Khung_Gio_Mac_Dinh] (
    [id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Khung_Gio_Mac_Dinh] PRIMARY KEY,
    [co_so_id] bigint NOT NULL,
    [gio_bat_dau] time(0) NOT NULL,
    [gio_ket_thuc] time(0) NOT NULL,
    [la_gio_vang] bit NOT NULL CONSTRAINT [DF_Khung_Gio_Mac_Dinh_la_gio_vang] DEFAULT (0),
    CONSTRAINT [CK_Khung_Gio_Mac_Dinh_Gio] CHECK (gio_bat_dau < gio_ket_thuc OR (gio_ket_thuc = '00:00:00' AND gio_bat_dau > '00:00:00'))
);

-- Bang_Gia
CREATE TABLE [dbo].[Bang_Gia] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Bang_Gia] PRIMARY KEY,
    [san_id] bigint NOT NULL,
    [khung_gio_id] int NOT NULL,
    [gia_thue] decimal(14,2) NOT NULL,
    [tien_coc_toi_thieu] decimal(14,2) NOT NULL CONSTRAINT [DF_Bang_Gia_tien_coc_toi_thieu] DEFAULT (0),
    [thu_trong_tuan] int NOT NULL,
    [ngay_hieu_luc_tu] date NOT NULL,
    [ngay_hieu_luc_den] date NULL,
    [dang_ap_dung] bit NOT NULL CONSTRAINT [DF_Bang_Gia_dang_ap_dung] DEFAULT (1),
    CONSTRAINT [CK_Bang_Gia_Thu] CHECK (thu_trong_tuan BETWEEN 1 AND 7),
    CONSTRAINT [CK_Bang_Gia_TienCoc] CHECK (tien_coc_toi_thieu <= gia_thue),
    CONSTRAINT [CK_Bang_Gia_KhoangNgay] CHECK (ngay_hieu_luc_den IS NULL OR ngay_hieu_luc_tu <= ngay_hieu_luc_den),
    CONSTRAINT [CK_Bang_Gia_KhongAm_gia_thue] CHECK ([gia_thue] >= 0),
    CONSTRAINT [CK_Bang_Gia_KhongAm_tien_coc_toi_thieu] CHECK ([tien_coc_toi_thieu] >= 0)
);

-- Danh_Gia_Co_So
CREATE TABLE [dbo].[Danh_Gia_Co_So] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Danh_Gia_Co_So] PRIMARY KEY,
    [don_dat_san_id] bigint NOT NULL,
    [so_sao] tinyint NOT NULL,
    [nhan_xet] nvarchar(max) NULL,
    [phan_hoi_chu_san] nvarchar(max) NULL,
    [ngay_tao] datetime2(0) NOT NULL CONSTRAINT [DF_Danh_Gia_Co_So_ngay_tao] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    [ngay_phan_hoi] datetime2(0) NULL,
    [ngay_cap_nhat] datetime2(0) NOT NULL CONSTRAINT [DF_Danh_Gia_Co_So_ngay_cap_nhat] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    CONSTRAINT [CK_Danh_Gia_Co_So_SoSao] CHECK (so_sao BETWEEN 1 AND 5)
);

-- Lich_Hoat_Dong_Co_So
CREATE TABLE [dbo].[Lich_Hoat_Dong_Co_So] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Lich_Hoat_Dong_Co_So] PRIMARY KEY,
    [co_so_id] bigint NOT NULL,
    [thu_trong_tuan] int NOT NULL,
    [gio_mo_cua] time(0) NOT NULL,
    [gio_dong_cua] time(0) NOT NULL,
    [nghi_ca_ngay] bit NOT NULL CONSTRAINT [DF_Lich_Hoat_Dong_Co_So_nghi_ca_ngay] DEFAULT (0),
    CONSTRAINT [CK_Lich_Hoat_Dong_Co_So_Thu] CHECK (thu_trong_tuan BETWEEN 1 AND 7),
    CONSTRAINT [CK_Lich_Hoat_Dong_Co_So_GioMoCua] CHECK (gio_mo_cua < gio_dong_cua OR (gio_dong_cua = '00:00:00' AND gio_mo_cua > '00:00:00') OR nghi_ca_ngay = 1)
);

-- Lich_Khoa_San
CREATE TABLE [dbo].[Lich_Khoa_San] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Lich_Khoa_San] PRIMARY KEY,
    [san_id] bigint NOT NULL,
    [bat_dau_luc] datetime2(0) NOT NULL,
    [ket_thuc_luc] datetime2(0) NOT NULL,
    [ly_do] nvarchar(max) NULL,
    [nguoi_tao_id] bigint NOT NULL,
    [trang_thai] nvarchar(30) NOT NULL CONSTRAINT [DF_Lich_Khoa_San_trang_thai] DEFAULT (N'DANG_AP_DUNG'),
    [ngay_tao] datetime2(0) NOT NULL CONSTRAINT [DF_Lich_Khoa_San_ngay_tao] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    CONSTRAINT [CK_Lich_Khoa_San_trang_thai] CHECK ([trang_thai] IN (N'DANG_AP_DUNG', N'DA_HUY')),
    CONSTRAINT [CK_Lich_Khoa_San_KhoangThoiGian] CHECK (bat_dau_luc < ket_thuc_luc)
);

-- Don_Dat_San
CREATE TABLE [dbo].[Don_Dat_San] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Don_Dat_San] PRIMARY KEY,
    [ma_dat_san] nvarchar(30) NOT NULL CONSTRAINT [DF_Don_Dat_San_ma_dat_san] DEFAULT (LEFT(REPLACE(CONVERT(nvarchar(36), NEWID()), N'-', N''), 30)),
    [nguoi_dung_id] bigint NULL,
    [co_so_id] bigint NOT NULL,
    [nhan_vien_tao_id] bigint NULL,
    [nguon_dat] nvarchar(30) NOT NULL CONSTRAINT [DF_Don_Dat_San_nguon_dat] DEFAULT (N'APP'),
    [tong_tien_phai_tra] AS (CONVERT(decimal(14,2), tong_tien_san + tong_tien_dich_vu + tong_phi_phat - tien_giam_gia)) PERSISTED,
    [tien_giam_gia] decimal(14,2) NOT NULL CONSTRAINT [DF_Don_Dat_San_tien_giam_gia] DEFAULT (0),
    [trang_thai] nvarchar(30) NOT NULL CONSTRAINT [DF_Don_Dat_San_trang_thai] DEFAULT (N'CHO_THANH_TOAN'),
    [ma_qr_checkin] nvarchar(255) NOT NULL CONSTRAINT [DF_Don_Dat_San_ma_qr_checkin] DEFAULT (CONVERT(nvarchar(36), NEWID())),
    [thoi_gian_het_han_giu_cho] datetime2(0) NULL,
    [ghi_chu] nvarchar(max) NULL,
    [ngay_tao] datetime2(0) NOT NULL CONSTRAINT [DF_Don_Dat_San_ngay_tao] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    [ngay_cap_nhat] datetime2(0) NOT NULL CONSTRAINT [DF_Don_Dat_San_ngay_cap_nhat] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    [ten_khach_hang] nvarchar(100) NOT NULL,
    [sdt_khach_hang] nvarchar(20) NOT NULL,
    [tong_tien_san] decimal(14,2) NOT NULL CONSTRAINT [DF_Don_Dat_San_tong_tien_san] DEFAULT (0),
    [tong_tien_dich_vu] decimal(14,2) NOT NULL CONSTRAINT [DF_Don_Dat_San_tong_tien_dich_vu] DEFAULT (0),
    [tong_phi_phat] decimal(14,2) NOT NULL CONSTRAINT [DF_Don_Dat_San_tong_phi_phat] DEFAULT (0),
    [tien_coc_yeu_cau] decimal(14,2) NOT NULL CONSTRAINT [DF_Don_Dat_San_tien_coc_yeu_cau] DEFAULT (0),
    CONSTRAINT [CK_Don_Dat_San_nguon_dat] CHECK ([nguon_dat] IN (N'APP', N'TAI_QUAY')),
    CONSTRAINT [CK_Don_Dat_San_trang_thai] CHECK ([trang_thai] IN (N'CHO_THANH_TOAN', N'DA_XAC_NHAN', N'DANG_SU_DUNG', N'HOAN_THANH', N'DA_HUY', N'HET_HAN')),
    CONSTRAINT [CK_Don_Dat_San_LienHe] CHECK (LEN(LTRIM(RTRIM(ten_khach_hang))) > 0 AND LEN(LTRIM(RTRIM(sdt_khach_hang))) BETWEEN 8 AND 20),
    CONSTRAINT [CK_Don_Dat_San_KhachApp] CHECK (nguon_dat <> N'APP' OR nguoi_dung_id IS NOT NULL),
    CONSTRAINT [CK_Don_Dat_San_TienGiam] CHECK (tien_giam_gia <= tong_tien_san + tong_tien_dich_vu + tong_phi_phat),
    CONSTRAINT [CK_Don_Dat_San_TienCoc] CHECK (tien_coc_yeu_cau <= tong_tien_phai_tra),
    CONSTRAINT [CK_Don_Dat_San_KhongAm_tong_tien_phai_tra] CHECK ([tong_tien_phai_tra] >= 0),
    CONSTRAINT [CK_Don_Dat_San_KhongAm_tien_giam_gia] CHECK ([tien_giam_gia] >= 0),
    CONSTRAINT [CK_Don_Dat_San_KhongAm_tong_tien_san] CHECK ([tong_tien_san] >= 0),
    CONSTRAINT [CK_Don_Dat_San_KhongAm_tong_tien_dich_vu] CHECK ([tong_tien_dich_vu] >= 0),
    CONSTRAINT [CK_Don_Dat_San_KhongAm_tong_phi_phat] CHECK ([tong_phi_phat] >= 0),
    CONSTRAINT [CK_Don_Dat_San_KhongAm_tien_coc_yeu_cau] CHECK ([tien_coc_yeu_cau] >= 0)
);

-- Chi_Tiet_Dat_San
CREATE TABLE [dbo].[Chi_Tiet_Dat_San] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Chi_Tiet_Dat_San] PRIMARY KEY,
    [don_dat_san_id] bigint NOT NULL,
    [san_id] bigint NOT NULL,
    [ngay_su_dung] date NOT NULL,
    [gio_bat_dau] time(0) NOT NULL,
    [gio_ket_thuc] time(0) NOT NULL,
    [gia_thue] decimal(14,2) NOT NULL,
    [trang_thai_slot] nvarchar(30) NOT NULL CONSTRAINT [DF_Chi_Tiet_Dat_San_trang_thai_slot] DEFAULT (N'GIU_CHO'),
    [khung_gio_id] int NOT NULL,
    [bang_gia_id] bigint NOT NULL,
    [lich_dat_co_dinh_id] bigint NULL,
    [chi_tiet_thay_the_cho_id] bigint NULL,
    [chinh_sach_huy_doi_id] bigint NOT NULL,
    [ngay_tao] datetime2(0) NOT NULL CONSTRAINT [DF_Chi_Tiet_Dat_San_ngay_tao] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    [ngay_cap_nhat] datetime2(0) NOT NULL CONSTRAINT [DF_Chi_Tiet_Dat_San_ngay_cap_nhat] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    CONSTRAINT [CK_Chi_Tiet_Dat_San_trang_thai_slot] CHECK ([trang_thai_slot] IN (N'GIU_CHO', N'DA_XAC_NHAN', N'DA_CHECK_IN', N'HOAN_THANH', N'DA_HUY', N'DA_DOI', N'HET_HAN', N'VANG_MAT')),
    CONSTRAINT [CK_Chi_Tiet_Dat_San_Gio] CHECK (gio_bat_dau < gio_ket_thuc OR (gio_ket_thuc = '00:00:00' AND gio_bat_dau > '00:00:00')),
    CONSTRAINT [CK_Chi_Tiet_Dat_San_KhongTuThayThe] CHECK (chi_tiet_thay_the_cho_id IS NULL OR chi_tiet_thay_the_cho_id <> id),
    CONSTRAINT [CK_Chi_Tiet_Dat_San_KhongAm_gia_thue] CHECK ([gia_thue] >= 0)
);

-- Lich_Dat_Co_Dinh
CREATE TABLE [dbo].[Lich_Dat_Co_Dinh] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Lich_Dat_Co_Dinh] PRIMARY KEY,
    [nguoi_dung_id] bigint NOT NULL,
    [san_id] bigint NOT NULL,
    [thu_trong_tuan] int NOT NULL,
    [gio_bat_dau] time(0) NOT NULL,
    [gio_ket_thuc] time(0) NOT NULL,
    [ngay_bat_dau] date NOT NULL,
    [ngay_ket_thuc] date NOT NULL,
    [trang_thai_kich_hoat] bit NOT NULL CONSTRAINT [DF_Lich_Dat_Co_Dinh_trang_thai_kich_hoat] DEFAULT (1),
    [ngay_tao] datetime2(0) NOT NULL CONSTRAINT [DF_Lich_Dat_Co_Dinh_ngay_tao] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    [khung_gio_id] int NOT NULL,
    [ngay_cap_nhat] datetime2(0) NOT NULL CONSTRAINT [DF_Lich_Dat_Co_Dinh_ngay_cap_nhat] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    CONSTRAINT [CK_Lich_Dat_Co_Dinh_Thu] CHECK (thu_trong_tuan BETWEEN 1 AND 7),
    CONSTRAINT [CK_Lich_Dat_Co_Dinh_Gio] CHECK (gio_bat_dau < gio_ket_thuc OR (gio_ket_thuc = '00:00:00' AND gio_bat_dau > '00:00:00')),
    CONSTRAINT [CK_Lich_Dat_Co_Dinh_KhoangNgay] CHECK (ngay_bat_dau <= ngay_ket_thuc)
);

-- Lich_Su_Check_In
CREATE TABLE [dbo].[Lich_Su_Check_In] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Lich_Su_Check_In] PRIMARY KEY,
    [chi_tiet_dat_san_id] bigint NOT NULL,
    [nhan_vien_checkin_id] bigint NOT NULL,
    [thoi_gian_checkin] datetime2(0) NOT NULL CONSTRAINT [DF_Lich_Su_Check_In_thoi_gian_checkin] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    [phuong_thuc] nvarchar(30) NOT NULL CONSTRAINT [DF_Lich_Su_Check_In_phuong_thuc] DEFAULT (N'QR'),
    [ghi_chu] nvarchar(max) NULL,
    CONSTRAINT [CK_Lich_Su_Check_In_phuong_thuc] CHECK ([phuong_thuc] IN (N'QR', N'THU_CONG'))
);

-- Dich_Vu
CREATE TABLE [dbo].[Dich_Vu] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Dich_Vu] PRIMARY KEY,
    [co_so_id] bigint NOT NULL,
    [ten_dich_vu] nvarchar(100) NOT NULL,
    [loai_dich_vu] nvarchar(50) NOT NULL,
    [don_gia] decimal(14,2) NOT NULL CONSTRAINT [DF_Dich_Vu_don_gia] DEFAULT (0),
    [don_vi_tinh] nvarchar(30) NOT NULL,
    [con_hang] bit NOT NULL CONSTRAINT [DF_Dich_Vu_con_hang] DEFAULT (1),
    [dang_kinh_doanh] bit NOT NULL CONSTRAINT [DF_Dich_Vu_dang_kinh_doanh] DEFAULT (1),
    CONSTRAINT [CK_Dich_Vu_KhongAm_don_gia] CHECK ([don_gia] >= 0)
);

-- Chi_Tiet_Dich_Vu_Don
CREATE TABLE [dbo].[Chi_Tiet_Dich_Vu_Don] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Chi_Tiet_Dich_Vu_Don] PRIMARY KEY,
    [don_dat_san_id] bigint NOT NULL,
    [dich_vu_id] bigint NOT NULL,
    [so_luong] int NOT NULL,
    [don_gia_luc_ban] decimal(14,2) NOT NULL CONSTRAINT [DF_Chi_Tiet_Dich_Vu_Don_don_gia_luc_ban] DEFAULT (0),
    [thanh_tien] AS (CONVERT(decimal(14,2), CONVERT(decimal(14,2), so_luong) * don_gia_luc_ban)) PERSISTED,
    CONSTRAINT [CK_Chi_Tiet_Dich_Vu_Don_SoLuong] CHECK (so_luong > 0),
    CONSTRAINT [CK_Chi_Tiet_Dich_Vu_Don_KhongAm_don_gia_luc_ban] CHECK ([don_gia_luc_ban] >= 0),
    CONSTRAINT [CK_Chi_Tiet_Dich_Vu_Don_KhongAm_thanh_tien] CHECK ([thanh_tien] >= 0)
);

-- Ho_So_Trinh_Do
CREATE TABLE [dbo].[Ho_So_Trinh_Do] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Ho_So_Trinh_Do] PRIMARY KEY,
    [nguoi_dung_id] bigint NOT NULL,
    [mon_the_thao_id] int NOT NULL,
    [diem_elo] int NOT NULL CONSTRAINT [DF_Ho_So_Trinh_Do_diem_elo] DEFAULT (1000),
    [hang_trinh_do] nvarchar(50) NULL,
    [so_tran_da_dau] int NOT NULL CONSTRAINT [DF_Ho_So_Trinh_Do_so_tran_da_dau] DEFAULT (0),
    [so_tran_thang] int NOT NULL CONSTRAINT [DF_Ho_So_Trinh_Do_so_tran_thang] DEFAULT (0),
    [so_tran_thua] int NOT NULL CONSTRAINT [DF_Ho_So_Trinh_Do_so_tran_thua] DEFAULT (0),
    [ngay_cap_nhat] datetime2(0) NOT NULL CONSTRAINT [DF_Ho_So_Trinh_Do_ngay_cap_nhat] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    [so_tran_hoa] int NOT NULL CONSTRAINT [DF_Ho_So_Trinh_Do_so_tran_hoa] DEFAULT (0),
    CONSTRAINT [CK_Ho_So_Trinh_Do_ThongKe] CHECK (so_tran_da_dau = so_tran_thang + so_tran_thua + so_tran_hoa),
    CONSTRAINT [CK_Ho_So_Trinh_Do_KhongAm_diem_elo] CHECK ([diem_elo] >= 0),
    CONSTRAINT [CK_Ho_So_Trinh_Do_KhongAm_so_tran_da_dau] CHECK ([so_tran_da_dau] >= 0),
    CONSTRAINT [CK_Ho_So_Trinh_Do_KhongAm_so_tran_thang] CHECK ([so_tran_thang] >= 0),
    CONSTRAINT [CK_Ho_So_Trinh_Do_KhongAm_so_tran_thua] CHECK ([so_tran_thua] >= 0),
    CONSTRAINT [CK_Ho_So_Trinh_Do_KhongAm_so_tran_hoa] CHECK ([so_tran_hoa] >= 0)
);

-- Phong_Ghep_Tran
CREATE TABLE [dbo].[Phong_Ghep_Tran] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Phong_Ghep_Tran] PRIMARY KEY,
    [ma_phong] nvarchar(30) NOT NULL CONSTRAINT [DF_Phong_Ghep_Tran_ma_phong] DEFAULT (LEFT(REPLACE(CONVERT(nvarchar(36), NEWID()), N'-', N''), 30)),
    [nguoi_tao_id] bigint NOT NULL,
    [chi_tiet_dat_san_id] bigint NOT NULL,
    [mon_the_thao_id] int NOT NULL,
    [tieu_de] nvarchar(150) NOT NULL,
    [the_loai] nvarchar(30) NOT NULL CONSTRAINT [DF_Phong_Ghep_Tran_the_loai] DEFAULT (N'GHEP_NGUOI'),
    [diem_elo_toi_thieu] int NOT NULL CONSTRAINT [DF_Phong_Ghep_Tran_diem_elo_toi_thieu] DEFAULT (0),
    [diem_elo_toi_da] int NOT NULL CONSTRAINT [DF_Phong_Ghep_Tran_diem_elo_toi_da] DEFAULT (3000),
    [so_luong_toi_da] int NOT NULL,
    [tien_chia_moi_nguoi] decimal(14,2) NOT NULL CONSTRAINT [DF_Phong_Ghep_Tran_tien_chia_moi_nguoi] DEFAULT (0),
    [trang_thai] nvarchar(30) NOT NULL CONSTRAINT [DF_Phong_Ghep_Tran_trang_thai] DEFAULT (N'DANG_TUYEN'),
    [ghi_chu] nvarchar(max) NULL,
    [ngay_tao] datetime2(0) NOT NULL CONSTRAINT [DF_Phong_Ghep_Tran_ngay_tao] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    CONSTRAINT [CK_Phong_Ghep_Tran_trang_thai] CHECK ([trang_thai] IN (N'DANG_TUYEN', N'DU_NGUOI', N'DANG_DAU', N'HOAN_THANH', N'DA_HUY')),
    CONSTRAINT [CK_Phong_Ghep_Tran_the_loai] CHECK ([the_loai] IN (N'GHEP_NGUOI', N'TIM_DOI')),
    CONSTRAINT [CK_Phong_Ghep_Tran_Elo] CHECK (diem_elo_toi_thieu <= diem_elo_toi_da),
    CONSTRAINT [CK_Phong_Ghep_Tran_SoNguoi] CHECK (so_luong_toi_da > 0),
    CONSTRAINT [CK_Phong_Ghep_Tran_KhongAm_diem_elo_toi_thieu] CHECK ([diem_elo_toi_thieu] >= 0),
    CONSTRAINT [CK_Phong_Ghep_Tran_KhongAm_diem_elo_toi_da] CHECK ([diem_elo_toi_da] >= 0),
    CONSTRAINT [CK_Phong_Ghep_Tran_KhongAm_tien_chia_moi_nguoi] CHECK ([tien_chia_moi_nguoi] >= 0)
);

-- Thanh_Vien_Phong
CREATE TABLE [dbo].[Thanh_Vien_Phong] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Thanh_Vien_Phong] PRIMARY KEY,
    [phong_ghep_id] bigint NOT NULL,
    [nguoi_dung_id] bigint NOT NULL,
    [vai_tro_phong] nvarchar(30) NOT NULL CONSTRAINT [DF_Thanh_Vien_Phong_vai_tro_phong] DEFAULT (N'THANH_VIEN'),
    [trang_thai_tham_gia] nvarchar(30) NOT NULL CONSTRAINT [DF_Thanh_Vien_Phong_trang_thai_tham_gia] DEFAULT (N'CHO_DUYET'),
    [ngay_tham_gia] datetime2(0) NOT NULL CONSTRAINT [DF_Thanh_Vien_Phong_ngay_tham_gia] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    [so_doi] int NULL,
    [ngay_cap_nhat] datetime2(0) NOT NULL CONSTRAINT [DF_Thanh_Vien_Phong_ngay_cap_nhat] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    CONSTRAINT [CK_Thanh_Vien_Phong_vai_tro_phong] CHECK ([vai_tro_phong] IN (N'CHU_PHONG', N'THANH_VIEN')),
    CONSTRAINT [CK_Thanh_Vien_Phong_trang_thai_tham_gia] CHECK ([trang_thai_tham_gia] IN (N'CHO_DUYET', N'DA_THAM_GIA', N'DA_ROI', N'BI_TU_CHOI')),
    CONSTRAINT [CK_Thanh_Vien_Phong_Doi] CHECK (so_doi IS NULL OR so_doi IN (1, 2))
);

-- Ket_Qua_Tran_Dau
CREATE TABLE [dbo].[Ket_Qua_Tran_Dau] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Ket_Qua_Tran_Dau] PRIMARY KEY,
    [phong_ghep_id] bigint NOT NULL,
    [ti_so] nvarchar(50) NOT NULL,
    [so_doi_thang] int NULL,
    [nguoi_xac_nhan_doi_1_id] bigint NULL,
    [nguoi_xac_nhan_doi_2_id] bigint NULL,
    [trang_thai_xac_nhan] nvarchar(30) NOT NULL CONSTRAINT [DF_Ket_Qua_Tran_Dau_trang_thai_xac_nhan] DEFAULT (N'CHO_XAC_NHAN'),
    [ngay_ghi_nhan] datetime2(0) NOT NULL CONSTRAINT [DF_Ket_Qua_Tran_Dau_ngay_ghi_nhan] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    [ngay_xac_nhan_doi_1] datetime2(0) NULL,
    [ngay_xac_nhan_doi_2] datetime2(0) NULL,
    [ngay_cap_nhat] datetime2(0) NOT NULL CONSTRAINT [DF_Ket_Qua_Tran_Dau_ngay_cap_nhat] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    CONSTRAINT [CK_Ket_Qua_Tran_Dau_trang_thai_xac_nhan] CHECK ([trang_thai_xac_nhan] IN (N'CHO_XAC_NHAN', N'DA_XAC_NHAN', N'TRANH_CHAP')),
    CONSTRAINT [CK_Ket_Qua_Tran_Dau_DoiThang] CHECK (so_doi_thang IS NULL OR so_doi_thang IN (0, 1, 2)),
    CONSTRAINT [CK_Ket_Qua_Tran_Dau_XacNhan] CHECK (trang_thai_xac_nhan <> N'DA_XAC_NHAN' OR (so_doi_thang IS NOT NULL AND nguoi_xac_nhan_doi_1_id IS NOT NULL AND nguoi_xac_nhan_doi_2_id IS NOT NULL AND ngay_xac_nhan_doi_1 IS NOT NULL AND ngay_xac_nhan_doi_2 IS NOT NULL))
);

-- Lich_Su_Elo
CREATE TABLE [dbo].[Lich_Su_Elo] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Lich_Su_Elo] PRIMARY KEY,
    [ho_so_trinh_do_id] bigint NOT NULL,
    [ket_qua_tran_dau_id] bigint NOT NULL,
    [diem_truoc] int NOT NULL,
    [diem_thay_doi] int NOT NULL,
    [diem_sau] int NOT NULL,
    [ngay_tao] datetime2(0) NOT NULL CONSTRAINT [DF_Lich_Su_Elo_ngay_tao] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    CONSTRAINT [CK_Lich_Su_Elo_CongDiem] CHECK (diem_sau = diem_truoc + diem_thay_doi)
);

-- Dong_Gop_Phong
CREATE TABLE [dbo].[Dong_Gop_Phong] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Dong_Gop_Phong] PRIMARY KEY,
    [thanh_vien_phong_id] bigint NOT NULL,
    [nguoi_ghi_nhan_id] bigint NOT NULL,
    [so_tien] decimal(14,2) NOT NULL,
    [loai_giao_dich] nvarchar(20) NOT NULL,
    [phuong_thuc] nvarchar(30) NOT NULL,
    [trang_thai] nvarchar(30) NOT NULL CONSTRAINT [DF_Dong_Gop_Phong_trang_thai] DEFAULT (N'DANG_XU_LY'),
    [ma_tham_chieu] nvarchar(100) NULL,
    [ngay_giao_dich] datetime2(0) NOT NULL CONSTRAINT [DF_Dong_Gop_Phong_ngay_giao_dich] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    [ghi_chu] nvarchar(max) NULL,
    CONSTRAINT [CK_Dong_Gop_Phong_loai_giao_dich] CHECK ([loai_giao_dich] IN (N'THU', N'HOAN')),
    CONSTRAINT [CK_Dong_Gop_Phong_phuong_thuc] CHECK ([phuong_thuc] IN (N'TIEN_MAT', N'CHUYEN_KHOAN_NGOAI')),
    CONSTRAINT [CK_Dong_Gop_Phong_trang_thai] CHECK ([trang_thai] IN (N'DANG_XU_LY', N'THANH_CONG', N'THAT_BAI', N'DA_HUY')),
    CONSTRAINT [CK_Dong_Gop_Phong_Duong_so_tien] CHECK ([so_tien] > 0)
);

-- Chinh_Sach_Huy_Doi
CREATE TABLE [dbo].[Chinh_Sach_Huy_Doi] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Chinh_Sach_Huy_Doi] PRIMARY KEY,
    [co_so_id] bigint NOT NULL,
    [cho_phep_doi_lich] bit NOT NULL CONSTRAINT [DF_Chinh_Sach_Huy_Doi_cho_phep_doi_lich] DEFAULT (1),
    [ghi_chu] nvarchar(255) NULL,
    [ngay_hieu_luc_tu] date NOT NULL,
    [ngay_hieu_luc_den] date NULL,
    [ngay_tao] datetime2(0) NOT NULL CONSTRAINT [DF_Chinh_Sach_Huy_Doi_ngay_tao] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    [ten_chinh_sach] nvarchar(150) NOT NULL,
    CONSTRAINT [CK_Chinh_Sach_Huy_Doi_KhoangNgay] CHECK (ngay_hieu_luc_den IS NULL OR ngay_hieu_luc_tu <= ngay_hieu_luc_den)
);

-- Yeu_Cau_Doi_Lich
CREATE TABLE [dbo].[Yeu_Cau_Doi_Lich] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Yeu_Cau_Doi_Lich] PRIMARY KEY,
    [chi_tiet_dat_san_cu_id] bigint NOT NULL,
    [san_moi_id] bigint NOT NULL,
    [ngay_su_dung_moi] date NOT NULL,
    [gio_bat_dau_moi] time(0) NOT NULL,
    [gio_ket_thuc_moi] time(0) NOT NULL,
    [phi_doi_lich] decimal(14,2) NOT NULL CONSTRAINT [DF_Yeu_Cau_Doi_Lich_phi_doi_lich] DEFAULT (0),
    [chenh_lech_gia] decimal(14,2) NOT NULL,
    [trang_thai] nvarchar(30) NOT NULL CONSTRAINT [DF_Yeu_Cau_Doi_Lich_trang_thai] DEFAULT (N'CHO_DUYET'),
    [ly_do] nvarchar(max) NULL,
    [ngay_tao] datetime2(0) NOT NULL CONSTRAINT [DF_Yeu_Cau_Doi_Lich_ngay_tao] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    [nguoi_yeu_cau_id] bigint NULL,
    [nguoi_xu_ly_id] bigint NULL,
    [khung_gio_moi_id] int NOT NULL,
    [chi_tiet_dat_san_moi_id] bigint NULL,
    [ngay_xu_ly] datetime2(0) NULL,
    CONSTRAINT [CK_Yeu_Cau_Doi_Lich_trang_thai] CHECK ([trang_thai] IN (N'CHO_DUYET', N'DA_DUYET', N'TU_CHOI', N'DA_HUY')),
    CONSTRAINT [CK_Yeu_Cau_Doi_Lich_GioMoi] CHECK (gio_bat_dau_moi < gio_ket_thuc_moi OR (gio_ket_thuc_moi = '00:00:00' AND gio_bat_dau_moi > '00:00:00')),
    CONSTRAINT [CK_Yeu_Cau_Doi_Lich_BuoiMoi] CHECK (trang_thai <> N'DA_DUYET' OR (chi_tiet_dat_san_moi_id IS NOT NULL AND ngay_xu_ly IS NOT NULL)),
    CONSTRAINT [CK_Yeu_Cau_Doi_Lich_KhongAm_phi_doi_lich] CHECK ([phi_doi_lich] >= 0)
);

-- Lich_Su_Huy_San
CREATE TABLE [dbo].[Lich_Su_Huy_San] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Lich_Su_Huy_San] PRIMARY KEY,
    [chi_tiet_dat_san_id] bigint NOT NULL,
    [nguoi_yeu_cau_id] bigint NULL,
    [so_gio_truoc_tran] decimal(10,2) NOT NULL,
    [so_tien_da_phan_bo] decimal(14,2) NOT NULL CONSTRAINT [DF_Lich_Su_Huy_San_so_tien_da_phan_bo] DEFAULT (0),
    [phi_phat_huy] decimal(14,2) NOT NULL CONSTRAINT [DF_Lich_Su_Huy_San_phi_phat_huy] DEFAULT (0),
    [so_tien_du_kien_hoan] decimal(14,2) NOT NULL CONSTRAINT [DF_Lich_Su_Huy_San_so_tien_du_kien_hoan] DEFAULT (0),
    [ly_do_huy] nvarchar(max) NULL,
    [ngay_huy] datetime2(0) NOT NULL CONSTRAINT [DF_Lich_Su_Huy_San_ngay_huy] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    [nguoi_xu_ly_id] bigint NULL,
    CONSTRAINT [CK_Lich_Su_Huy_San_KhongAm_so_tien_da_phan_bo] CHECK ([so_tien_da_phan_bo] >= 0),
    CONSTRAINT [CK_Lich_Su_Huy_San_KhongAm_phi_phat_huy] CHECK ([phi_phat_huy] >= 0),
    CONSTRAINT [CK_Lich_Su_Huy_San_KhongAm_so_tien_du_kien_hoan] CHECK ([so_tien_du_kien_hoan] >= 0)
);

-- Nhat_Ky_Vi_Pham
CREATE TABLE [dbo].[Nhat_Ky_Vi_Pham] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Nhat_Ky_Vi_Pham] PRIMARY KEY,
    [nguoi_dung_id] bigint NOT NULL,
    [don_dat_san_id] bigint NULL,
    [loai_vi_pham] nvarchar(50) NOT NULL,
    [diem_uy_tin_bi_tru] int NOT NULL CONSTRAINT [DF_Nhat_Ky_Vi_Pham_diem_uy_tin_bi_tru] DEFAULT (0),
    [khoa_tinh_nang_den] datetime2(0) NULL,
    [noi_dung_vi_pham] nvarchar(max) NOT NULL,
    [ngay_ghi_nhan] datetime2(0) NOT NULL CONSTRAINT [DF_Nhat_Ky_Vi_Pham_ngay_ghi_nhan] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    CONSTRAINT [CK_Nhat_Ky_Vi_Pham_KhongAm_diem_uy_tin_bi_tru] CHECK ([diem_uy_tin_bi_tru] >= 0)
);

-- Muc_Phi_Huy_Doi
CREATE TABLE [dbo].[Muc_Phi_Huy_Doi] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Muc_Phi_Huy_Doi] PRIMARY KEY,
    [chinh_sach_huy_doi_id] bigint NOT NULL,
    [so_gio_tu_truoc_tran] decimal(10,2) NOT NULL CONSTRAINT [DF_Muc_Phi_Huy_Doi_so_gio_tu_truoc_tran] DEFAULT (0),
    [so_gio_den_truoc_tran] decimal(10,2) NULL,
    [ti_le_phi_huy] decimal(5,2) NOT NULL CONSTRAINT [DF_Muc_Phi_Huy_Doi_ti_le_phi_huy] DEFAULT (0),
    [ti_le_phi_doi] decimal(5,2) NOT NULL CONSTRAINT [DF_Muc_Phi_Huy_Doi_ti_le_phi_doi] DEFAULT (0),
    CONSTRAINT [CK_Muc_Phi_Huy_Doi_KhoangGio] CHECK (so_gio_tu_truoc_tran >= 0 AND (so_gio_den_truoc_tran IS NULL OR so_gio_den_truoc_tran > so_gio_tu_truoc_tran)),
    CONSTRAINT [CK_Muc_Phi_Huy_Doi_KhongAm_so_gio_tu_truoc_tran] CHECK ([so_gio_tu_truoc_tran] >= 0),
    CONSTRAINT [CK_Muc_Phi_Huy_Doi_KhongAm_so_gio_den_truoc_tran] CHECK ([so_gio_den_truoc_tran] >= 0),
    CONSTRAINT [CK_Muc_Phi_Huy_Doi_KhongAm_ti_le_phi_huy] CHECK ([ti_le_phi_huy] >= 0),
    CONSTRAINT [CK_Muc_Phi_Huy_Doi_PhanTram_ti_le_phi_huy] CHECK ([ti_le_phi_huy] BETWEEN 0 AND 100),
    CONSTRAINT [CK_Muc_Phi_Huy_Doi_KhongAm_ti_le_phi_doi] CHECK ([ti_le_phi_doi] >= 0),
    CONSTRAINT [CK_Muc_Phi_Huy_Doi_PhanTram_ti_le_phi_doi] CHECK ([ti_le_phi_doi] BETWEEN 0 AND 100)
);

-- Vi_Nguoi_Dung
CREATE TABLE [dbo].[Vi_Nguoi_Dung] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Vi_Nguoi_Dung] PRIMARY KEY,
    [nguoi_dung_id] bigint NOT NULL,
    [so_du_kha_dung] decimal(14,2) NOT NULL CONSTRAINT [DF_Vi_Nguoi_Dung_so_du_kha_dung] DEFAULT (0),
    [so_du_dong_bang] decimal(14,2) NOT NULL CONSTRAINT [DF_Vi_Nguoi_Dung_so_du_dong_bang] DEFAULT (0),
    [trang_thai] nvarchar(30) NOT NULL CONSTRAINT [DF_Vi_Nguoi_Dung_trang_thai] DEFAULT (N'HOAT_DONG'),
    [ngay_tao] datetime2(0) NOT NULL CONSTRAINT [DF_Vi_Nguoi_Dung_ngay_tao] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    [ngay_cap_nhat] datetime2(0) NOT NULL CONSTRAINT [DF_Vi_Nguoi_Dung_ngay_cap_nhat] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    CONSTRAINT [CK_Vi_Nguoi_Dung_trang_thai] CHECK ([trang_thai] IN (N'HOAT_DONG', N'BI_KHOA')),
    CONSTRAINT [CK_Vi_Nguoi_Dung_KhongAm_so_du_kha_dung] CHECK ([so_du_kha_dung] >= 0),
    CONSTRAINT [CK_Vi_Nguoi_Dung_KhongAm_so_du_dong_bang] CHECK ([so_du_dong_bang] >= 0)
);

-- Giao_Dich_Vi
CREATE TABLE [dbo].[Giao_Dich_Vi] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Giao_Dich_Vi] PRIMARY KEY,
    [vi_id] bigint NOT NULL,
    [ma_giao_dich] nvarchar(50) NOT NULL,
    [loai_giao_dich] nvarchar(50) NOT NULL,
    [so_tien] decimal(14,2) NOT NULL,
    [so_du_truoc] decimal(14,2) NOT NULL CONSTRAINT [DF_Giao_Dich_Vi_so_du_truoc] DEFAULT (0),
    [so_du_sau] decimal(14,2) NOT NULL CONSTRAINT [DF_Giao_Dich_Vi_so_du_sau] DEFAULT (0),
    [noi_dung] nvarchar(max) NULL,
    [trang_thai] nvarchar(30) NOT NULL CONSTRAINT [DF_Giao_Dich_Vi_trang_thai] DEFAULT (N'DA_GHI_SO'),
    [ngay_tao] datetime2(0) NOT NULL CONSTRAINT [DF_Giao_Dich_Vi_ngay_tao] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    [so_du_dong_bang_truoc] decimal(14,2) NOT NULL CONSTRAINT [DF_Giao_Dich_Vi_so_du_dong_bang_truoc] DEFAULT (0),
    [so_du_dong_bang_sau] decimal(14,2) NOT NULL CONSTRAINT [DF_Giao_Dich_Vi_so_du_dong_bang_sau] DEFAULT (0),
    [khoa_chong_lap] nvarchar(100) NOT NULL,
    CONSTRAINT [CK_Giao_Dich_Vi_loai_giao_dich] CHECK ([loai_giao_dich] IN (N'NAP', N'CHI_TRA', N'HOAN', N'GIU_TIEN', N'GIAI_TOA', N'RUT')),
    CONSTRAINT [CK_Giao_Dich_Vi_trang_thai] CHECK ([trang_thai] IN (N'DA_GHI_SO')),
    CONSTRAINT [CK_Giao_Dich_Vi_BienDong] CHECK ((loai_giao_dich IN (N'NAP', N'HOAN') AND so_du_sau = so_du_truoc + so_tien AND so_du_dong_bang_sau = so_du_dong_bang_truoc)
 OR (loai_giao_dich = N'CHI_TRA' AND so_du_sau = so_du_truoc - so_tien AND so_du_dong_bang_sau = so_du_dong_bang_truoc)
 OR (loai_giao_dich = N'GIU_TIEN' AND so_du_sau = so_du_truoc - so_tien AND so_du_dong_bang_sau = so_du_dong_bang_truoc + so_tien)
 OR (loai_giao_dich = N'GIAI_TOA' AND so_du_sau = so_du_truoc + so_tien AND so_du_dong_bang_sau = so_du_dong_bang_truoc - so_tien)
 OR (loai_giao_dich = N'RUT' AND so_du_sau = so_du_truoc AND so_du_dong_bang_sau = so_du_dong_bang_truoc - so_tien)),
    CONSTRAINT [CK_Giao_Dich_Vi_Duong_so_tien] CHECK ([so_tien] > 0),
    CONSTRAINT [CK_Giao_Dich_Vi_KhongAm_so_du_truoc] CHECK ([so_du_truoc] >= 0),
    CONSTRAINT [CK_Giao_Dich_Vi_KhongAm_so_du_sau] CHECK ([so_du_sau] >= 0),
    CONSTRAINT [CK_Giao_Dich_Vi_KhongAm_so_du_dong_bang_truoc] CHECK ([so_du_dong_bang_truoc] >= 0),
    CONSTRAINT [CK_Giao_Dich_Vi_KhongAm_so_du_dong_bang_sau] CHECK ([so_du_dong_bang_sau] >= 0)
);

-- Giao_Dich_Cong_Thanh_Toan
CREATE TABLE [dbo].[Giao_Dich_Cong_Thanh_Toan] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Giao_Dich_Cong_Thanh_Toan] PRIMARY KEY,
    [ma_don_hang_cong] nvarchar(100) NOT NULL,
    [ma_giao_dich_ngan_hang] nvarchar(100) NULL,
    [nguoi_dung_id] bigint NULL,
    [cong_thanh_toan] nvarchar(30) NOT NULL,
    [so_tien] decimal(14,2) NOT NULL,
    [loai_thanh_toan] nvarchar(50) NOT NULL,
    [du_lieu_webhook] nvarchar(max) NULL,
    [trang_thai] nvarchar(30) NOT NULL CONSTRAINT [DF_Giao_Dich_Cong_Thanh_Toan_trang_thai] DEFAULT (N'KHOI_TAO'),
    [ngay_tao] datetime2(0) NOT NULL CONSTRAINT [DF_Giao_Dich_Cong_Thanh_Toan_ngay_tao] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    [ngay_thanh_toan] datetime2(0) NULL,
    [ma_su_kien_cong] nvarchar(100) NULL,
    [ngay_cap_nhat] datetime2(0) NOT NULL CONSTRAINT [DF_Giao_Dich_Cong_Thanh_Toan_ngay_cap_nhat] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    CONSTRAINT [CK_Giao_Dich_Cong_Thanh_Toan_loai_thanh_toan] CHECK ([loai_thanh_toan] IN (N'THU_TIEN', N'HOAN_TIEN', N'CHUYEN_TIEN')),
    CONSTRAINT [CK_Giao_Dich_Cong_Thanh_Toan_trang_thai] CHECK ([trang_thai] IN (N'KHOI_TAO', N'DANG_XU_LY', N'THANH_CONG', N'THAT_BAI', N'DA_HUY')),
    CONSTRAINT [CK_Giao_Dich_Cong_Thanh_Toan_Duong_so_tien] CHECK ([so_tien] > 0)
);

-- Yeu_Cau_Rut_Tien
CREATE TABLE [dbo].[Yeu_Cau_Rut_Tien] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Yeu_Cau_Rut_Tien] PRIMARY KEY,
    [vi_id] bigint NOT NULL,
    [so_tien] decimal(14,2) NOT NULL,
    [ten_ngan_hang] nvarchar(100) NOT NULL,
    [so_tai_khoan] nvarchar(50) NOT NULL,
    [ten_chu_tai_khoan] nvarchar(100) NOT NULL,
    [nhan_vien_duyet_id] bigint NULL,
    [trang_thai] nvarchar(30) NOT NULL CONSTRAINT [DF_Yeu_Cau_Rut_Tien_trang_thai] DEFAULT (N'CHO_DUYET'),
    [ly_do_tu_choi] nvarchar(max) NULL,
    [ngay_tao] datetime2(0) NOT NULL CONSTRAINT [DF_Yeu_Cau_Rut_Tien_ngay_tao] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    [ngay_xu_ly] datetime2(0) NULL,
    [giao_dich_giu_tien_id] bigint NOT NULL,
    [giao_dich_ket_thuc_id] bigint NULL,
    [giao_dich_cong_id] bigint NULL,
    CONSTRAINT [CK_Yeu_Cau_Rut_Tien_trang_thai] CHECK ([trang_thai] IN (N'CHO_DUYET', N'DANG_XU_LY', N'THANH_CONG', N'TU_CHOI', N'THAT_BAI', N'DA_HUY')),
    CONSTRAINT [CK_Yeu_Cau_Rut_Tien_Duong_so_tien] CHECK ([so_tien] > 0)
);

-- Thanh_Toan_Don
CREATE TABLE [dbo].[Thanh_Toan_Don] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Thanh_Toan_Don] PRIMARY KEY,
    [ma_thanh_toan] nvarchar(50) NOT NULL,
    [don_dat_san_id] bigint NOT NULL,
    [nguoi_thanh_toan_id] bigint NULL,
    [nhan_vien_thu_id] bigint NULL,
    [giao_dich_vi_id] bigint NULL,
    [giao_dich_cong_id] bigint NULL,
    [phuong_thuc] nvarchar(30) NOT NULL,
    [muc_dich] nvarchar(30) NOT NULL CONSTRAINT [DF_Thanh_Toan_Don_muc_dich] DEFAULT (N'THANH_TOAN'),
    [so_tien] decimal(14,2) NOT NULL,
    [ma_tham_chieu_ngoai] nvarchar(100) NULL,
    [khoa_chong_lap] nvarchar(100) NOT NULL,
    [trang_thai] nvarchar(30) NOT NULL CONSTRAINT [DF_Thanh_Toan_Don_trang_thai] DEFAULT (N'KHOI_TAO'),
    [ngay_tao] datetime2(0) NOT NULL CONSTRAINT [DF_Thanh_Toan_Don_ngay_tao] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    [ngay_thanh_toan] datetime2(0) NULL,
    CONSTRAINT [CK_Thanh_Toan_Don_phuong_thuc] CHECK ([phuong_thuc] IN (N'VI', N'CONG_THANH_TOAN', N'TIEN_MAT', N'CHUYEN_KHOAN_TAI_QUAY')),
    CONSTRAINT [CK_Thanh_Toan_Don_trang_thai] CHECK ([trang_thai] IN (N'KHOI_TAO', N'DANG_XU_LY', N'THANH_CONG', N'THAT_BAI', N'DA_HUY')),
    CONSTRAINT [CK_Thanh_Toan_Don_muc_dich] CHECK ([muc_dich] IN (N'COC', N'THANH_TOAN', N'BO_SUNG')),
    CONSTRAINT [CK_Thanh_Toan_Don_NguonThanhToan] CHECK ((phuong_thuc = N'VI' AND giao_dich_cong_id IS NULL AND (trang_thai <> N'THANH_CONG' OR giao_dich_vi_id IS NOT NULL))
 OR (phuong_thuc = N'CONG_THANH_TOAN' AND giao_dich_vi_id IS NULL AND (trang_thai <> N'THANH_CONG' OR giao_dich_cong_id IS NOT NULL))
 OR (phuong_thuc IN (N'TIEN_MAT', N'CHUYEN_KHOAN_TAI_QUAY') AND giao_dich_vi_id IS NULL AND giao_dich_cong_id IS NULL)),
    CONSTRAINT [CK_Thanh_Toan_Don_NgayThanhCong] CHECK (trang_thai <> N'THANH_CONG' OR ngay_thanh_toan IS NOT NULL),
    CONSTRAINT [CK_Thanh_Toan_Don_NguoiThu] CHECK (phuong_thuc NOT IN (N'TIEN_MAT', N'CHUYEN_KHOAN_TAI_QUAY') OR nhan_vien_thu_id IS NOT NULL),
    CONSTRAINT [CK_Thanh_Toan_Don_ChuVi] CHECK (phuong_thuc <> N'VI' OR nguoi_thanh_toan_id IS NOT NULL),
    CONSTRAINT [CK_Thanh_Toan_Don_Duong_so_tien] CHECK ([so_tien] > 0)
);

-- Hoan_Tien
CREATE TABLE [dbo].[Hoan_Tien] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Hoan_Tien] PRIMARY KEY,
    [ma_hoan_tien] nvarchar(50) NOT NULL,
    [thanh_toan_don_id] bigint NOT NULL,
    [lich_su_huy_san_id] bigint NULL,
    [yeu_cau_doi_lich_id] bigint NULL,
    [giao_dich_vi_id] bigint NULL,
    [giao_dich_cong_id] bigint NULL,
    [nguoi_duyet_id] bigint NULL,
    [so_tien] decimal(14,2) NOT NULL,
    [phuong_thuc] nvarchar(30) NOT NULL,
    [ma_tham_chieu_ngoai] nvarchar(100) NULL,
    [khoa_chong_lap] nvarchar(100) NOT NULL,
    [trang_thai] nvarchar(30) NOT NULL CONSTRAINT [DF_Hoan_Tien_trang_thai] DEFAULT (N'KHOI_TAO'),
    [ly_do] nvarchar(max) NOT NULL,
    [ngay_tao] datetime2(0) NOT NULL CONSTRAINT [DF_Hoan_Tien_ngay_tao] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    [ngay_hoan_tien] datetime2(0) NULL,
    CONSTRAINT [CK_Hoan_Tien_phuong_thuc] CHECK ([phuong_thuc] IN (N'VI', N'CONG_THANH_TOAN', N'TIEN_MAT', N'CHUYEN_KHOAN_TAI_QUAY')),
    CONSTRAINT [CK_Hoan_Tien_trang_thai] CHECK ([trang_thai] IN (N'KHOI_TAO', N'DANG_XU_LY', N'THANH_CONG', N'THAT_BAI', N'DA_HUY')),
    CONSTRAINT [CK_Hoan_Tien_Nguon] CHECK (NOT (lich_su_huy_san_id IS NOT NULL AND yeu_cau_doi_lich_id IS NOT NULL)),
    CONSTRAINT [CK_Hoan_Tien_NguonThanhToan] CHECK ((phuong_thuc = N'VI' AND giao_dich_cong_id IS NULL AND (trang_thai <> N'THANH_CONG' OR giao_dich_vi_id IS NOT NULL))
 OR (phuong_thuc = N'CONG_THANH_TOAN' AND giao_dich_vi_id IS NULL AND (trang_thai <> N'THANH_CONG' OR giao_dich_cong_id IS NOT NULL))
 OR (phuong_thuc IN (N'TIEN_MAT', N'CHUYEN_KHOAN_TAI_QUAY') AND giao_dich_vi_id IS NULL AND giao_dich_cong_id IS NULL)),
    CONSTRAINT [CK_Hoan_Tien_NgayThanhCong] CHECK (trang_thai <> N'THANH_CONG' OR ngay_hoan_tien IS NOT NULL),
    CONSTRAINT [CK_Hoan_Tien_Duong_so_tien] CHECK ([so_tien] > 0)
);

-- Nap_Tien_Vi
CREATE TABLE [dbo].[Nap_Tien_Vi] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Nap_Tien_Vi] PRIMARY KEY,
    [vi_id] bigint NOT NULL,
    [giao_dich_cong_id] bigint NOT NULL,
    [giao_dich_vi_id] bigint NULL,
    [so_tien] decimal(14,2) NOT NULL,
    [trang_thai] nvarchar(30) NOT NULL CONSTRAINT [DF_Nap_Tien_Vi_trang_thai] DEFAULT (N'KHOI_TAO'),
    [ngay_tao] datetime2(0) NOT NULL CONSTRAINT [DF_Nap_Tien_Vi_ngay_tao] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    [ngay_hoan_tat] datetime2(0) NULL,
    CONSTRAINT [CK_Nap_Tien_Vi_trang_thai] CHECK ([trang_thai] IN (N'KHOI_TAO', N'DANG_XU_LY', N'THANH_CONG', N'THAT_BAI', N'DA_HUY')),
    CONSTRAINT [CK_Nap_Tien_Vi_HoanTat] CHECK (trang_thai <> N'THANH_CONG' OR (giao_dich_vi_id IS NOT NULL AND ngay_hoan_tat IS NOT NULL)),
    CONSTRAINT [CK_Nap_Tien_Vi_Duong_so_tien] CHECK ([so_tien] > 0)
);

-- Voucher
CREATE TABLE [dbo].[Voucher] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Voucher] PRIMARY KEY,
    [ma_voucher] nvarchar(30) NOT NULL,
    [tieu_de] nvarchar(150) NOT NULL,
    [co_so_id] bigint NULL,
    [loai_phat_hanh] nvarchar(30) NOT NULL,
    [loai_giam] nvarchar(20) NOT NULL,
    [gia_tri_giam] decimal(14,2) NOT NULL,
    [giam_toi_da] decimal(14,2) NULL,
    [gia_tri_don_toi_thieu] decimal(14,2) NOT NULL CONSTRAINT [DF_Voucher_gia_tri_don_toi_thieu] DEFAULT (0),
    [so_luong_tong] int NOT NULL,
    [so_luong_da_dung] int NOT NULL CONSTRAINT [DF_Voucher_so_luong_da_dung] DEFAULT (0),
    [gioi_han_moi_user] int NOT NULL,
    [ngay_bat_dau] datetime2(0) NOT NULL CONSTRAINT [DF_Voucher_ngay_bat_dau] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    [ngay_ket_thuc] datetime2(0) NOT NULL CONSTRAINT [DF_Voucher_ngay_ket_thuc] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    [chi_ap_dung_gio_thap_diem] bit NOT NULL CONSTRAINT [DF_Voucher_chi_ap_dung_gio_thap_diem] DEFAULT (0),
    [trang_thai] bit NOT NULL CONSTRAINT [DF_Voucher_trang_thai] DEFAULT (1),
    [ngay_tao] datetime2(0) NOT NULL CONSTRAINT [DF_Voucher_ngay_tao] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
    [ben_chiu_giam] nvarchar(20) NOT NULL,
    CONSTRAINT [CK_Voucher_loai_phat_hanh] CHECK ([loai_phat_hanh] IN (N'TOAN_SAN', N'CO_SO')),
    CONSTRAINT [CK_Voucher_loai_giam] CHECK ([loai_giam] IN (N'SO_TIEN', N'PHAN_TRAM')),
    CONSTRAINT [CK_Voucher_ben_chiu_giam] CHECK ([ben_chiu_giam] IN (N'SAN', N'CO_SO')),
    CONSTRAINT [CK_Voucher_PhatHanh] CHECK ((loai_phat_hanh = N'TOAN_SAN' AND co_so_id IS NULL) OR (loai_phat_hanh = N'CO_SO' AND co_so_id IS NOT NULL)),
    CONSTRAINT [CK_Voucher_GiaTri] CHECK (gia_tri_giam > 0 AND (loai_giam <> N'PHAN_TRAM' OR gia_tri_giam <= 100)),
    CONSTRAINT [CK_Voucher_HanMuc] CHECK (so_luong_tong > 0 AND gioi_han_moi_user > 0 AND so_luong_da_dung BETWEEN 0 AND so_luong_tong),
    CONSTRAINT [CK_Voucher_KhoangNgay] CHECK (ngay_bat_dau <= ngay_ket_thuc),
    CONSTRAINT [CK_Voucher_KhongAm_gia_tri_giam] CHECK ([gia_tri_giam] >= 0),
    CONSTRAINT [CK_Voucher_KhongAm_giam_toi_da] CHECK ([giam_toi_da] >= 0),
    CONSTRAINT [CK_Voucher_KhongAm_gia_tri_don_toi_thieu] CHECK ([gia_tri_don_toi_thieu] >= 0)
);

-- Voucher_Nguoi_Dung
CREATE TABLE [dbo].[Voucher_Nguoi_Dung] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Voucher_Nguoi_Dung] PRIMARY KEY,
    [nguoi_dung_id] bigint NOT NULL,
    [voucher_id] bigint NOT NULL,
    [ngay_thu_thap] datetime2(0) NOT NULL CONSTRAINT [DF_Voucher_Nguoi_Dung_ngay_thu_thap] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME()))
);

-- Lich_Su_Dung_Voucher
CREATE TABLE [dbo].[Lich_Su_Dung_Voucher] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Lich_Su_Dung_Voucher] PRIMARY KEY,
    [voucher_id] bigint NOT NULL,
    [don_dat_san_id] bigint NOT NULL,
    [nguoi_dung_id] bigint NOT NULL,
    [so_tien_giam_thuc_te] decimal(14,2) NOT NULL CONSTRAINT [DF_Lich_Su_Dung_Voucher_so_tien_giam_thuc_te] DEFAULT (0),
    [ngay_ap_dung] datetime2(0) NULL,
    [trang_thai] nvarchar(30) NOT NULL CONSTRAINT [DF_Lich_Su_Dung_Voucher_trang_thai] DEFAULT (N'GIU_CHO'),
    [giu_cho_den] datetime2(0) NULL,
    [ngay_giai_toa] datetime2(0) NULL,
    CONSTRAINT [CK_Lich_Su_Dung_Voucher_trang_thai] CHECK ([trang_thai] IN (N'GIU_CHO', N'DA_AP_DUNG', N'DA_GIAI_TOA')),
    CONSTRAINT [CK_Lich_Su_Dung_Voucher_GiuCho] CHECK (trang_thai <> N'GIU_CHO' OR giu_cho_den IS NOT NULL),
    CONSTRAINT [CK_Lich_Su_Dung_Voucher_NgayDung] CHECK (trang_thai <> N'DA_AP_DUNG' OR ngay_ap_dung IS NOT NULL),
    CONSTRAINT [CK_Lich_Su_Dung_Voucher_KhongAm_so_tien_giam_thuc_te] CHECK ([so_tien_giam_thuc_te] >= 0)
);

-- Cau_Hinh_Hoa_Hong
CREATE TABLE [dbo].[Cau_Hinh_Hoa_Hong] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Cau_Hinh_Hoa_Hong] PRIMARY KEY,
    [co_so_id] bigint NOT NULL,
    [ti_le_hoa_hong] decimal(5,2) NOT NULL CONSTRAINT [DF_Cau_Hinh_Hoa_Hong_ti_le_hoa_hong] DEFAULT (0),
    [phi_co_dinh_moi_don] decimal(14,2) NOT NULL CONSTRAINT [DF_Cau_Hinh_Hoa_Hong_phi_co_dinh_moi_don] DEFAULT (0),
    [ngay_ap_dung] date NOT NULL,
    [trang_thai] bit NOT NULL CONSTRAINT [DF_Cau_Hinh_Hoa_Hong_trang_thai] DEFAULT (1),
    [ngay_het_hieu_luc] date NULL,
    CONSTRAINT [CK_Cau_Hinh_Hoa_Hong_KhoangNgay] CHECK (ngay_het_hieu_luc IS NULL OR ngay_ap_dung <= ngay_het_hieu_luc),
    CONSTRAINT [CK_Cau_Hinh_Hoa_Hong_KhongAm_ti_le_hoa_hong] CHECK ([ti_le_hoa_hong] >= 0),
    CONSTRAINT [CK_Cau_Hinh_Hoa_Hong_PhanTram_ti_le_hoa_hong] CHECK ([ti_le_hoa_hong] BETWEEN 0 AND 100),
    CONSTRAINT [CK_Cau_Hinh_Hoa_Hong_KhongAm_phi_co_dinh_moi_don] CHECK ([phi_co_dinh_moi_don] >= 0)
);

-- Ky_Doi_Soat
CREATE TABLE [dbo].[Ky_Doi_Soat] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Ky_Doi_Soat] PRIMARY KEY,
    [ma_ky_doi_soat] nvarchar(50) NOT NULL,
    [co_so_id] bigint NOT NULL,
    [ngay_bat_dau] date NOT NULL,
    [ngay_ket_thuc] date NOT NULL,
    [tong_doanh_thu_app] decimal(14,2) NOT NULL CONSTRAINT [DF_Ky_Doi_Soat_tong_doanh_thu_app] DEFAULT (0),
    [tong_doanh_thu_tai_quay] decimal(14,2) NOT NULL CONSTRAINT [DF_Ky_Doi_Soat_tong_doanh_thu_tai_quay] DEFAULT (0),
    [tong_voucher_san_tro_gia] decimal(14,2) NOT NULL CONSTRAINT [DF_Ky_Doi_Soat_tong_voucher_san_tro_gia] DEFAULT (0),
    [tong_phi_hoa_hong] decimal(14,2) NOT NULL CONSTRAINT [DF_Ky_Doi_Soat_tong_phi_hoa_hong] DEFAULT (0),
    [so_tien_thuc_chuyen] decimal(14,2) NOT NULL,
    [trang_thai] nvarchar(30) NOT NULL CONSTRAINT [DF_Ky_Doi_Soat_trang_thai] DEFAULT (N'DANG_LAP'),
    [nhan_vien_doi_soat_id] bigint NULL,
    [ngay_chot] datetime2(0) NULL,
    [ngay_thanh_toan] datetime2(0) NULL,
    [tong_tien_hoan_app] decimal(14,2) NOT NULL CONSTRAINT [DF_Ky_Doi_Soat_tong_tien_hoan_app] DEFAULT (0),
    [ma_chuyen_khoan] nvarchar(100) NULL,
    CONSTRAINT [CK_Ky_Doi_Soat_trang_thai] CHECK ([trang_thai] IN (N'DANG_LAP', N'DA_CHOT', N'DA_THANH_TOAN')),
    CONSTRAINT [CK_Ky_Doi_Soat_KhoangNgay] CHECK (ngay_bat_dau <= ngay_ket_thuc),
    CONSTRAINT [CK_Ky_Doi_Soat_KhongAm_tong_doanh_thu_app] CHECK ([tong_doanh_thu_app] >= 0),
    CONSTRAINT [CK_Ky_Doi_Soat_KhongAm_tong_doanh_thu_tai_quay] CHECK ([tong_doanh_thu_tai_quay] >= 0),
    CONSTRAINT [CK_Ky_Doi_Soat_KhongAm_tong_voucher_san_tro_gia] CHECK ([tong_voucher_san_tro_gia] >= 0),
    CONSTRAINT [CK_Ky_Doi_Soat_KhongAm_tong_phi_hoa_hong] CHECK ([tong_phi_hoa_hong] >= 0),
    CONSTRAINT [CK_Ky_Doi_Soat_KhongAm_tong_tien_hoan_app] CHECK ([tong_tien_hoan_app] >= 0)
);

-- Chi_Tiet_Doi_Soat_Don
CREATE TABLE [dbo].[Chi_Tiet_Doi_Soat_Don] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Chi_Tiet_Doi_Soat_Don] PRIMARY KEY,
    [ky_doi_soat_id] bigint NOT NULL,
    [don_dat_san_id] bigint NOT NULL,
    [tong_tien_don] decimal(14,2) NOT NULL CONSTRAINT [DF_Chi_Tiet_Doi_Soat_Don_tong_tien_don] DEFAULT (0),
    [tien_khach_tra_app] decimal(14,2) NOT NULL CONSTRAINT [DF_Chi_Tiet_Doi_Soat_Don_tien_khach_tra_app] DEFAULT (0),
    [tien_thu_tai_quay] decimal(14,2) NOT NULL CONSTRAINT [DF_Chi_Tiet_Doi_Soat_Don_tien_thu_tai_quay] DEFAULT (0),
    [tro_gia_san_phai_tra_co_so] decimal(14,2) NOT NULL CONSTRAINT [DF_Chi_Tiet_Doi_Soat_Don_tro_gia_san_phai_tra_co_so] DEFAULT (0),
    [phi_hoa_hong_don] decimal(14,2) NOT NULL CONSTRAINT [DF_Chi_Tiet_Doi_Soat_Don_phi_hoa_hong_don] DEFAULT (0),
    [so_tien_quyet_toan] decimal(14,2) NOT NULL,
    [cau_hinh_hoa_hong_id] bigint NOT NULL,
    [ti_le_hoa_hong_ap_dung] decimal(5,2) NOT NULL CONSTRAINT [DF_Chi_Tiet_Doi_Soat_Don_ti_le_hoa_hong_ap_dung] DEFAULT (0),
    [phi_co_dinh_ap_dung] decimal(14,2) NOT NULL CONSTRAINT [DF_Chi_Tiet_Doi_Soat_Don_phi_co_dinh_ap_dung] DEFAULT (0),
    [tien_hoan_app] decimal(14,2) NOT NULL CONSTRAINT [DF_Chi_Tiet_Doi_Soat_Don_tien_hoan_app] DEFAULT (0),
    CONSTRAINT [CK_Chi_Tiet_Doi_Soat_Don_QuyetToan] CHECK (so_tien_quyet_toan = tien_khach_tra_app - tien_hoan_app + tro_gia_san_phai_tra_co_so - phi_hoa_hong_don),
    CONSTRAINT [CK_Chi_Tiet_Doi_Soat_Don_Hoan] CHECK (tien_hoan_app <= tien_khach_tra_app),
    CONSTRAINT [CK_Chi_Tiet_Doi_Soat_Don_KhongAm_tong_tien_don] CHECK ([tong_tien_don] >= 0),
    CONSTRAINT [CK_Chi_Tiet_Doi_Soat_Don_KhongAm_tien_khach_tra_app] CHECK ([tien_khach_tra_app] >= 0),
    CONSTRAINT [CK_Chi_Tiet_Doi_Soat_Don_KhongAm_tien_thu_tai_quay] CHECK ([tien_thu_tai_quay] >= 0),
    CONSTRAINT [CK_Chi_Tiet_Doi_Soat_Don_KhongAm_tro_gia_san_phai_tra_co_so] CHECK ([tro_gia_san_phai_tra_co_so] >= 0),
    CONSTRAINT [CK_Chi_Tiet_Doi_Soat_Don_KhongAm_phi_hoa_hong_don] CHECK ([phi_hoa_hong_don] >= 0),
    CONSTRAINT [CK_Chi_Tiet_Doi_Soat_Don_KhongAm_ti_le_hoa_hong_ap_dung] CHECK ([ti_le_hoa_hong_ap_dung] >= 0),
    CONSTRAINT [CK_Chi_Tiet_Doi_Soat_Don_PhanTram_ti_le_hoa_hong_ap_dung] CHECK ([ti_le_hoa_hong_ap_dung] BETWEEN 0 AND 100),
    CONSTRAINT [CK_Chi_Tiet_Doi_Soat_Don_KhongAm_phi_co_dinh_ap_dung] CHECK ([phi_co_dinh_ap_dung] >= 0),
    CONSTRAINT [CK_Chi_Tiet_Doi_Soat_Don_KhongAm_tien_hoan_app] CHECK ([tien_hoan_app] >= 0)
);

-- Bao_Cao_Hieu_Suat_San
CREATE TABLE [dbo].[Bao_Cao_Hieu_Suat_San] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Bao_Cao_Hieu_Suat_San] PRIMARY KEY,
    [san_id] bigint NOT NULL,
    [ngay_bao_cao] date NOT NULL,
    [tong_so_slot_trong_ngay] int NOT NULL CONSTRAINT [DF_Bao_Cao_Hieu_Suat_San_tong_so_slot_trong_ngay] DEFAULT (0),
    [so_slot_da_dat] int NOT NULL CONSTRAINT [DF_Bao_Cao_Hieu_Suat_San_so_slot_da_dat] DEFAULT (0),
    [so_slot_trong] AS ((tong_so_slot_trong_ngay - so_slot_da_dat)) PERSISTED,
    [so_slot_huy] int NOT NULL CONSTRAINT [DF_Bao_Cao_Hieu_Suat_San_so_slot_huy] DEFAULT (0),
    [ty_le_lap_day] AS (CONVERT(decimal(5,2), CASE WHEN tong_so_slot_trong_ngay = 0 THEN 0 ELSE so_slot_da_dat * 100.0 / tong_so_slot_trong_ngay END)) PERSISTED,
    [doanh_thu_ngay] decimal(14,2) NOT NULL CONSTRAINT [DF_Bao_Cao_Hieu_Suat_San_doanh_thu_ngay] DEFAULT (0),
    CONSTRAINT [CK_Bao_Cao_Hieu_Suat_San_Slot] CHECK (so_slot_da_dat <= tong_so_slot_trong_ngay AND so_slot_trong = tong_so_slot_trong_ngay - so_slot_da_dat),
    CONSTRAINT [CK_Bao_Cao_Hieu_Suat_San_KhongAm_tong_so_slot_trong_ngay] CHECK ([tong_so_slot_trong_ngay] >= 0),
    CONSTRAINT [CK_Bao_Cao_Hieu_Suat_San_KhongAm_so_slot_da_dat] CHECK ([so_slot_da_dat] >= 0),
    CONSTRAINT [CK_Bao_Cao_Hieu_Suat_San_KhongAm_so_slot_trong] CHECK ([so_slot_trong] >= 0),
    CONSTRAINT [CK_Bao_Cao_Hieu_Suat_San_KhongAm_so_slot_huy] CHECK ([so_slot_huy] >= 0),
    CONSTRAINT [CK_Bao_Cao_Hieu_Suat_San_KhongAm_ty_le_lap_day] CHECK ([ty_le_lap_day] >= 0),
    CONSTRAINT [CK_Bao_Cao_Hieu_Suat_San_PhanTram_ty_le_lap_day] CHECK ([ty_le_lap_day] BETWEEN 0 AND 100),
    CONSTRAINT [CK_Bao_Cao_Hieu_Suat_San_KhongAm_doanh_thu_ngay] CHECK ([doanh_thu_ngay] >= 0)
);

-- Dieu_Chinh_Doi_Soat
CREATE TABLE [dbo].[Dieu_Chinh_Doi_Soat] (
    [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Dieu_Chinh_Doi_Soat] PRIMARY KEY,
    [ky_doi_soat_id] bigint NOT NULL,
    [chi_tiet_doi_soat_goc_id] bigint NOT NULL,
    [hoan_tien_id] bigint NULL,
    [so_tien_dieu_chinh] decimal(14,2) NOT NULL,
    [ly_do] nvarchar(max) NOT NULL,
    [nguoi_tao_id] bigint NOT NULL,
    [ngay_tao] datetime2(0) NOT NULL CONSTRAINT [DF_Dieu_Chinh_Doi_Soat_ngay_tao] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME()))
);

ALTER TABLE [dbo].[Nguoi_Dung_Vai_Tro] WITH CHECK ADD CONSTRAINT [FK_Nguoi_Dung_Vai_Tro_nguoi_dung_id] FOREIGN KEY ([nguoi_dung_id]) REFERENCES [dbo].[Nguoi_Dung] ([id]);
ALTER TABLE [dbo].[Nguoi_Dung_Vai_Tro] WITH CHECK ADD CONSTRAINT [FK_Nguoi_Dung_Vai_Tro_vai_tro_id] FOREIGN KEY ([vai_tro_id]) REFERENCES [dbo].[Vai_Tro] ([id]);
ALTER TABLE [dbo].[Thanh_Vien_Co_So] WITH CHECK ADD CONSTRAINT [FK_Thanh_Vien_Co_So_nguoi_dung_id] FOREIGN KEY ([nguoi_dung_id]) REFERENCES [dbo].[Nguoi_Dung] ([id]);
ALTER TABLE [dbo].[Phan_Quyen_Thanh_Vien] WITH CHECK ADD CONSTRAINT [FK_Phan_Quyen_Thanh_Vien_thanh_vien_co_so_id] FOREIGN KEY ([thanh_vien_co_so_id]) REFERENCES [dbo].[Thanh_Vien_Co_So] ([id]);
ALTER TABLE [dbo].[Phan_Quyen_Thanh_Vien] WITH CHECK ADD CONSTRAINT [FK_Phan_Quyen_Thanh_Vien_quyen_han_id] FOREIGN KEY ([quyen_han_id]) REFERENCES [dbo].[Quyen_Han] ([id]);
ALTER TABLE [dbo].[Thong_Bao] WITH CHECK ADD CONSTRAINT [FK_Thong_Bao_nguoi_dung_id] FOREIGN KEY ([nguoi_dung_id]) REFERENCES [dbo].[Nguoi_Dung] ([id]);
ALTER TABLE [dbo].[Co_So] WITH CHECK ADD CONSTRAINT [FK_Co_So_chu_so_huu_id] FOREIGN KEY ([chu_so_huu_id]) REFERENCES [dbo].[Nguoi_Dung] ([id]);
ALTER TABLE [dbo].[Thanh_Vien_Co_So] WITH CHECK ADD CONSTRAINT [FK_Thanh_Vien_Co_So_co_so_id] FOREIGN KEY ([co_so_id]) REFERENCES [dbo].[Co_So] ([id]);
ALTER TABLE [dbo].[San] WITH CHECK ADD CONSTRAINT [FK_San_co_so_id] FOREIGN KEY ([co_so_id]) REFERENCES [dbo].[Co_So] ([id]);
ALTER TABLE [dbo].[Loai_San] WITH CHECK ADD CONSTRAINT [FK_Loai_San_mon_the_thao_id] FOREIGN KEY ([mon_the_thao_id]) REFERENCES [dbo].[Mon_The_Thao] ([id]);
ALTER TABLE [dbo].[San] WITH CHECK ADD CONSTRAINT [FK_San_loai_san_id] FOREIGN KEY ([loai_san_id]) REFERENCES [dbo].[Loai_San] ([id]);
ALTER TABLE [dbo].[Khung_Gio_Mac_Dinh] WITH CHECK ADD CONSTRAINT [FK_Khung_Gio_Mac_Dinh_co_so_id] FOREIGN KEY ([co_so_id]) REFERENCES [dbo].[Co_So] ([id]);
ALTER TABLE [dbo].[Bang_Gia] WITH CHECK ADD CONSTRAINT [FK_Bang_Gia_san_id] FOREIGN KEY ([san_id]) REFERENCES [dbo].[San] ([id]);
ALTER TABLE [dbo].[Bang_Gia] WITH CHECK ADD CONSTRAINT [FK_Bang_Gia_khung_gio_id] FOREIGN KEY ([khung_gio_id]) REFERENCES [dbo].[Khung_Gio_Mac_Dinh] ([id]);
ALTER TABLE [dbo].[Don_Dat_San] WITH CHECK ADD CONSTRAINT [FK_Don_Dat_San_nguoi_dung_id] FOREIGN KEY ([nguoi_dung_id]) REFERENCES [dbo].[Nguoi_Dung] ([id]);
ALTER TABLE [dbo].[Don_Dat_San] WITH CHECK ADD CONSTRAINT [FK_Don_Dat_San_co_so_id] FOREIGN KEY ([co_so_id]) REFERENCES [dbo].[Co_So] ([id]);
ALTER TABLE [dbo].[Don_Dat_San] WITH CHECK ADD CONSTRAINT [FK_Don_Dat_San_nhan_vien_tao_id] FOREIGN KEY ([nhan_vien_tao_id]) REFERENCES [dbo].[Nguoi_Dung] ([id]);
ALTER TABLE [dbo].[Chi_Tiet_Dat_San] WITH CHECK ADD CONSTRAINT [FK_Chi_Tiet_Dat_San_don_dat_san_id] FOREIGN KEY ([don_dat_san_id]) REFERENCES [dbo].[Don_Dat_San] ([id]);
ALTER TABLE [dbo].[Chi_Tiet_Dat_San] WITH CHECK ADD CONSTRAINT [FK_Chi_Tiet_Dat_San_san_id] FOREIGN KEY ([san_id]) REFERENCES [dbo].[San] ([id]);
ALTER TABLE [dbo].[Lich_Dat_Co_Dinh] WITH CHECK ADD CONSTRAINT [FK_Lich_Dat_Co_Dinh_nguoi_dung_id] FOREIGN KEY ([nguoi_dung_id]) REFERENCES [dbo].[Nguoi_Dung] ([id]);
ALTER TABLE [dbo].[Lich_Dat_Co_Dinh] WITH CHECK ADD CONSTRAINT [FK_Lich_Dat_Co_Dinh_san_id] FOREIGN KEY ([san_id]) REFERENCES [dbo].[San] ([id]);
ALTER TABLE [dbo].[Lich_Su_Check_In] WITH CHECK ADD CONSTRAINT [FK_Lich_Su_Check_In_nhan_vien_checkin_id] FOREIGN KEY ([nhan_vien_checkin_id]) REFERENCES [dbo].[Nguoi_Dung] ([id]);
ALTER TABLE [dbo].[Danh_Gia_Co_So] WITH CHECK ADD CONSTRAINT [FK_Danh_Gia_Co_So_don_dat_san_id] FOREIGN KEY ([don_dat_san_id]) REFERENCES [dbo].[Don_Dat_San] ([id]);
ALTER TABLE [dbo].[Dich_Vu] WITH CHECK ADD CONSTRAINT [FK_Dich_Vu_co_so_id] FOREIGN KEY ([co_so_id]) REFERENCES [dbo].[Co_So] ([id]);
ALTER TABLE [dbo].[Chi_Tiet_Dich_Vu_Don] WITH CHECK ADD CONSTRAINT [FK_Chi_Tiet_Dich_Vu_Don_don_dat_san_id] FOREIGN KEY ([don_dat_san_id]) REFERENCES [dbo].[Don_Dat_San] ([id]);
ALTER TABLE [dbo].[Chi_Tiet_Dich_Vu_Don] WITH CHECK ADD CONSTRAINT [FK_Chi_Tiet_Dich_Vu_Don_dich_vu_id] FOREIGN KEY ([dich_vu_id]) REFERENCES [dbo].[Dich_Vu] ([id]);
ALTER TABLE [dbo].[Ho_So_Trinh_Do] WITH CHECK ADD CONSTRAINT [FK_Ho_So_Trinh_Do_nguoi_dung_id] FOREIGN KEY ([nguoi_dung_id]) REFERENCES [dbo].[Nguoi_Dung] ([id]);
ALTER TABLE [dbo].[Ho_So_Trinh_Do] WITH CHECK ADD CONSTRAINT [FK_Ho_So_Trinh_Do_mon_the_thao_id] FOREIGN KEY ([mon_the_thao_id]) REFERENCES [dbo].[Mon_The_Thao] ([id]);
ALTER TABLE [dbo].[Phong_Ghep_Tran] WITH CHECK ADD CONSTRAINT [FK_Phong_Ghep_Tran_nguoi_tao_id] FOREIGN KEY ([nguoi_tao_id]) REFERENCES [dbo].[Nguoi_Dung] ([id]);
ALTER TABLE [dbo].[Phong_Ghep_Tran] WITH CHECK ADD CONSTRAINT [FK_Phong_Ghep_Tran_mon_the_thao_id] FOREIGN KEY ([mon_the_thao_id]) REFERENCES [dbo].[Mon_The_Thao] ([id]);
ALTER TABLE [dbo].[Thanh_Vien_Phong] WITH CHECK ADD CONSTRAINT [FK_Thanh_Vien_Phong_phong_ghep_id] FOREIGN KEY ([phong_ghep_id]) REFERENCES [dbo].[Phong_Ghep_Tran] ([id]);
ALTER TABLE [dbo].[Thanh_Vien_Phong] WITH CHECK ADD CONSTRAINT [FK_Thanh_Vien_Phong_nguoi_dung_id] FOREIGN KEY ([nguoi_dung_id]) REFERENCES [dbo].[Nguoi_Dung] ([id]);
ALTER TABLE [dbo].[Ket_Qua_Tran_Dau] WITH CHECK ADD CONSTRAINT [FK_Ket_Qua_Tran_Dau_phong_ghep_id] FOREIGN KEY ([phong_ghep_id]) REFERENCES [dbo].[Phong_Ghep_Tran] ([id]);
ALTER TABLE [dbo].[Ket_Qua_Tran_Dau] WITH CHECK ADD CONSTRAINT [FK_Ket_Qua_Tran_Dau_nguoi_xac_nhan_doi_1_id] FOREIGN KEY ([nguoi_xac_nhan_doi_1_id]) REFERENCES [dbo].[Nguoi_Dung] ([id]);
ALTER TABLE [dbo].[Ket_Qua_Tran_Dau] WITH CHECK ADD CONSTRAINT [FK_Ket_Qua_Tran_Dau_nguoi_xac_nhan_doi_2_id] FOREIGN KEY ([nguoi_xac_nhan_doi_2_id]) REFERENCES [dbo].[Nguoi_Dung] ([id]);
ALTER TABLE [dbo].[Chinh_Sach_Huy_Doi] WITH CHECK ADD CONSTRAINT [FK_Chinh_Sach_Huy_Doi_co_so_id] FOREIGN KEY ([co_so_id]) REFERENCES [dbo].[Co_So] ([id]);
ALTER TABLE [dbo].[Yeu_Cau_Doi_Lich] WITH CHECK ADD CONSTRAINT [FK_Yeu_Cau_Doi_Lich_chi_tiet_dat_san_cu_id] FOREIGN KEY ([chi_tiet_dat_san_cu_id]) REFERENCES [dbo].[Chi_Tiet_Dat_San] ([id]);
ALTER TABLE [dbo].[Yeu_Cau_Doi_Lich] WITH CHECK ADD CONSTRAINT [FK_Yeu_Cau_Doi_Lich_san_moi_id] FOREIGN KEY ([san_moi_id]) REFERENCES [dbo].[San] ([id]);
ALTER TABLE [dbo].[Lich_Su_Huy_San] WITH CHECK ADD CONSTRAINT [FK_Lich_Su_Huy_San_nguoi_yeu_cau_id] FOREIGN KEY ([nguoi_yeu_cau_id]) REFERENCES [dbo].[Nguoi_Dung] ([id]);
ALTER TABLE [dbo].[Nhat_Ky_Vi_Pham] WITH CHECK ADD CONSTRAINT [FK_Nhat_Ky_Vi_Pham_nguoi_dung_id] FOREIGN KEY ([nguoi_dung_id]) REFERENCES [dbo].[Nguoi_Dung] ([id]);
ALTER TABLE [dbo].[Nhat_Ky_Vi_Pham] WITH CHECK ADD CONSTRAINT [FK_Nhat_Ky_Vi_Pham_don_dat_san_id] FOREIGN KEY ([don_dat_san_id]) REFERENCES [dbo].[Don_Dat_San] ([id]);
ALTER TABLE [dbo].[Vi_Nguoi_Dung] WITH CHECK ADD CONSTRAINT [FK_Vi_Nguoi_Dung_nguoi_dung_id] FOREIGN KEY ([nguoi_dung_id]) REFERENCES [dbo].[Nguoi_Dung] ([id]);
ALTER TABLE [dbo].[Giao_Dich_Vi] WITH CHECK ADD CONSTRAINT [FK_Giao_Dich_Vi_vi_id] FOREIGN KEY ([vi_id]) REFERENCES [dbo].[Vi_Nguoi_Dung] ([id]);
ALTER TABLE [dbo].[Giao_Dich_Cong_Thanh_Toan] WITH CHECK ADD CONSTRAINT [FK_Giao_Dich_Cong_Thanh_Toan_nguoi_dung_id] FOREIGN KEY ([nguoi_dung_id]) REFERENCES [dbo].[Nguoi_Dung] ([id]);
ALTER TABLE [dbo].[Yeu_Cau_Rut_Tien] WITH CHECK ADD CONSTRAINT [FK_Yeu_Cau_Rut_Tien_vi_id] FOREIGN KEY ([vi_id]) REFERENCES [dbo].[Vi_Nguoi_Dung] ([id]);
ALTER TABLE [dbo].[Yeu_Cau_Rut_Tien] WITH CHECK ADD CONSTRAINT [FK_Yeu_Cau_Rut_Tien_nhan_vien_duyet_id] FOREIGN KEY ([nhan_vien_duyet_id]) REFERENCES [dbo].[Nguoi_Dung] ([id]);
ALTER TABLE [dbo].[Voucher] WITH CHECK ADD CONSTRAINT [FK_Voucher_co_so_id] FOREIGN KEY ([co_so_id]) REFERENCES [dbo].[Co_So] ([id]);
ALTER TABLE [dbo].[Voucher_Nguoi_Dung] WITH CHECK ADD CONSTRAINT [FK_Voucher_Nguoi_Dung_nguoi_dung_id] FOREIGN KEY ([nguoi_dung_id]) REFERENCES [dbo].[Nguoi_Dung] ([id]);
ALTER TABLE [dbo].[Voucher_Nguoi_Dung] WITH CHECK ADD CONSTRAINT [FK_Voucher_Nguoi_Dung_voucher_id] FOREIGN KEY ([voucher_id]) REFERENCES [dbo].[Voucher] ([id]);
ALTER TABLE [dbo].[Lich_Su_Dung_Voucher] WITH CHECK ADD CONSTRAINT [FK_Lich_Su_Dung_Voucher_voucher_id] FOREIGN KEY ([voucher_id]) REFERENCES [dbo].[Voucher] ([id]);
ALTER TABLE [dbo].[Lich_Su_Dung_Voucher] WITH CHECK ADD CONSTRAINT [FK_Lich_Su_Dung_Voucher_don_dat_san_id] FOREIGN KEY ([don_dat_san_id]) REFERENCES [dbo].[Don_Dat_San] ([id]);
ALTER TABLE [dbo].[Lich_Su_Dung_Voucher] WITH CHECK ADD CONSTRAINT [FK_Lich_Su_Dung_Voucher_nguoi_dung_id] FOREIGN KEY ([nguoi_dung_id]) REFERENCES [dbo].[Nguoi_Dung] ([id]);
ALTER TABLE [dbo].[Cau_Hinh_Hoa_Hong] WITH CHECK ADD CONSTRAINT [FK_Cau_Hinh_Hoa_Hong_co_so_id] FOREIGN KEY ([co_so_id]) REFERENCES [dbo].[Co_So] ([id]);
ALTER TABLE [dbo].[Ky_Doi_Soat] WITH CHECK ADD CONSTRAINT [FK_Ky_Doi_Soat_co_so_id] FOREIGN KEY ([co_so_id]) REFERENCES [dbo].[Co_So] ([id]);
ALTER TABLE [dbo].[Ky_Doi_Soat] WITH CHECK ADD CONSTRAINT [FK_Ky_Doi_Soat_nhan_vien_doi_soat_id] FOREIGN KEY ([nhan_vien_doi_soat_id]) REFERENCES [dbo].[Nguoi_Dung] ([id]);
ALTER TABLE [dbo].[Chi_Tiet_Doi_Soat_Don] WITH CHECK ADD CONSTRAINT [FK_Chi_Tiet_Doi_Soat_Don_ky_doi_soat_id] FOREIGN KEY ([ky_doi_soat_id]) REFERENCES [dbo].[Ky_Doi_Soat] ([id]);
ALTER TABLE [dbo].[Chi_Tiet_Doi_Soat_Don] WITH CHECK ADD CONSTRAINT [FK_Chi_Tiet_Doi_Soat_Don_don_dat_san_id] FOREIGN KEY ([don_dat_san_id]) REFERENCES [dbo].[Don_Dat_San] ([id]);
ALTER TABLE [dbo].[Bao_Cao_Hieu_Suat_San] WITH CHECK ADD CONSTRAINT [FK_Bao_Cao_Hieu_Suat_San_san_id] FOREIGN KEY ([san_id]) REFERENCES [dbo].[San] ([id]);
ALTER TABLE [dbo].[Muc_Phi_Huy_Doi] WITH CHECK ADD CONSTRAINT [FK_Muc_Phi_Huy_Doi_chinh_sach_huy_doi_id] FOREIGN KEY ([chinh_sach_huy_doi_id]) REFERENCES [dbo].[Chinh_Sach_Huy_Doi] ([id]);
ALTER TABLE [dbo].[Chi_Tiet_Dat_San] WITH CHECK ADD CONSTRAINT [FK_Chi_Tiet_Dat_San_khung_gio_id] FOREIGN KEY ([khung_gio_id]) REFERENCES [dbo].[Khung_Gio_Mac_Dinh] ([id]);
ALTER TABLE [dbo].[Chi_Tiet_Dat_San] WITH CHECK ADD CONSTRAINT [FK_Chi_Tiet_Dat_San_bang_gia_id] FOREIGN KEY ([bang_gia_id]) REFERENCES [dbo].[Bang_Gia] ([id]);
ALTER TABLE [dbo].[Chi_Tiet_Dat_San] WITH CHECK ADD CONSTRAINT [FK_Chi_Tiet_Dat_San_lich_dat_co_dinh_id] FOREIGN KEY ([lich_dat_co_dinh_id]) REFERENCES [dbo].[Lich_Dat_Co_Dinh] ([id]);
ALTER TABLE [dbo].[Chi_Tiet_Dat_San] WITH CHECK ADD CONSTRAINT [FK_Chi_Tiet_Dat_San_chi_tiet_thay_the_cho_id] FOREIGN KEY ([chi_tiet_thay_the_cho_id]) REFERENCES [dbo].[Chi_Tiet_Dat_San] ([id]);
ALTER TABLE [dbo].[Chi_Tiet_Dat_San] WITH CHECK ADD CONSTRAINT [FK_Chi_Tiet_Dat_San_chinh_sach_huy_doi_id] FOREIGN KEY ([chinh_sach_huy_doi_id]) REFERENCES [dbo].[Chinh_Sach_Huy_Doi] ([id]);
ALTER TABLE [dbo].[Lich_Dat_Co_Dinh] WITH CHECK ADD CONSTRAINT [FK_Lich_Dat_Co_Dinh_khung_gio_id] FOREIGN KEY ([khung_gio_id]) REFERENCES [dbo].[Khung_Gio_Mac_Dinh] ([id]);
ALTER TABLE [dbo].[Lich_Su_Check_In] WITH CHECK ADD CONSTRAINT [FK_Lich_Su_Check_In_chi_tiet_dat_san_id] FOREIGN KEY ([chi_tiet_dat_san_id]) REFERENCES [dbo].[Chi_Tiet_Dat_San] ([id]);
ALTER TABLE [dbo].[Phong_Ghep_Tran] WITH CHECK ADD CONSTRAINT [FK_Phong_Ghep_Tran_chi_tiet_dat_san_id] FOREIGN KEY ([chi_tiet_dat_san_id]) REFERENCES [dbo].[Chi_Tiet_Dat_San] ([id]);
ALTER TABLE [dbo].[Yeu_Cau_Doi_Lich] WITH CHECK ADD CONSTRAINT [FK_Yeu_Cau_Doi_Lich_nguoi_yeu_cau_id] FOREIGN KEY ([nguoi_yeu_cau_id]) REFERENCES [dbo].[Nguoi_Dung] ([id]);
ALTER TABLE [dbo].[Yeu_Cau_Doi_Lich] WITH CHECK ADD CONSTRAINT [FK_Yeu_Cau_Doi_Lich_nguoi_xu_ly_id] FOREIGN KEY ([nguoi_xu_ly_id]) REFERENCES [dbo].[Nguoi_Dung] ([id]);
ALTER TABLE [dbo].[Yeu_Cau_Doi_Lich] WITH CHECK ADD CONSTRAINT [FK_Yeu_Cau_Doi_Lich_khung_gio_moi_id] FOREIGN KEY ([khung_gio_moi_id]) REFERENCES [dbo].[Khung_Gio_Mac_Dinh] ([id]);
ALTER TABLE [dbo].[Yeu_Cau_Doi_Lich] WITH CHECK ADD CONSTRAINT [FK_Yeu_Cau_Doi_Lich_chi_tiet_dat_san_moi_id] FOREIGN KEY ([chi_tiet_dat_san_moi_id]) REFERENCES [dbo].[Chi_Tiet_Dat_San] ([id]);
ALTER TABLE [dbo].[Lich_Su_Huy_San] WITH CHECK ADD CONSTRAINT [FK_Lich_Su_Huy_San_chi_tiet_dat_san_id] FOREIGN KEY ([chi_tiet_dat_san_id]) REFERENCES [dbo].[Chi_Tiet_Dat_San] ([id]);
ALTER TABLE [dbo].[Lich_Su_Huy_San] WITH CHECK ADD CONSTRAINT [FK_Lich_Su_Huy_San_nguoi_xu_ly_id] FOREIGN KEY ([nguoi_xu_ly_id]) REFERENCES [dbo].[Nguoi_Dung] ([id]);
ALTER TABLE [dbo].[Yeu_Cau_Rut_Tien] WITH CHECK ADD CONSTRAINT [FK_Yeu_Cau_Rut_Tien_giao_dich_giu_tien_id] FOREIGN KEY ([giao_dich_giu_tien_id]) REFERENCES [dbo].[Giao_Dich_Vi] ([id]);
ALTER TABLE [dbo].[Yeu_Cau_Rut_Tien] WITH CHECK ADD CONSTRAINT [FK_Yeu_Cau_Rut_Tien_giao_dich_ket_thuc_id] FOREIGN KEY ([giao_dich_ket_thuc_id]) REFERENCES [dbo].[Giao_Dich_Vi] ([id]);
ALTER TABLE [dbo].[Yeu_Cau_Rut_Tien] WITH CHECK ADD CONSTRAINT [FK_Yeu_Cau_Rut_Tien_giao_dich_cong_id] FOREIGN KEY ([giao_dich_cong_id]) REFERENCES [dbo].[Giao_Dich_Cong_Thanh_Toan] ([id]);
ALTER TABLE [dbo].[Chi_Tiet_Doi_Soat_Don] WITH CHECK ADD CONSTRAINT [FK_Chi_Tiet_Doi_Soat_Don_cau_hinh_hoa_hong_id] FOREIGN KEY ([cau_hinh_hoa_hong_id]) REFERENCES [dbo].[Cau_Hinh_Hoa_Hong] ([id]);
ALTER TABLE [dbo].[Lich_Hoat_Dong_Co_So] WITH CHECK ADD CONSTRAINT [FK_Lich_Hoat_Dong_Co_So_co_so_id] FOREIGN KEY ([co_so_id]) REFERENCES [dbo].[Co_So] ([id]);
ALTER TABLE [dbo].[Lich_Khoa_San] WITH CHECK ADD CONSTRAINT [FK_Lich_Khoa_San_san_id] FOREIGN KEY ([san_id]) REFERENCES [dbo].[San] ([id]);
ALTER TABLE [dbo].[Lich_Khoa_San] WITH CHECK ADD CONSTRAINT [FK_Lich_Khoa_San_nguoi_tao_id] FOREIGN KEY ([nguoi_tao_id]) REFERENCES [dbo].[Nguoi_Dung] ([id]);
ALTER TABLE [dbo].[Lich_Su_Elo] WITH CHECK ADD CONSTRAINT [FK_Lich_Su_Elo_ho_so_trinh_do_id] FOREIGN KEY ([ho_so_trinh_do_id]) REFERENCES [dbo].[Ho_So_Trinh_Do] ([id]);
ALTER TABLE [dbo].[Lich_Su_Elo] WITH CHECK ADD CONSTRAINT [FK_Lich_Su_Elo_ket_qua_tran_dau_id] FOREIGN KEY ([ket_qua_tran_dau_id]) REFERENCES [dbo].[Ket_Qua_Tran_Dau] ([id]);
ALTER TABLE [dbo].[Dong_Gop_Phong] WITH CHECK ADD CONSTRAINT [FK_Dong_Gop_Phong_thanh_vien_phong_id] FOREIGN KEY ([thanh_vien_phong_id]) REFERENCES [dbo].[Thanh_Vien_Phong] ([id]);
ALTER TABLE [dbo].[Dong_Gop_Phong] WITH CHECK ADD CONSTRAINT [FK_Dong_Gop_Phong_nguoi_ghi_nhan_id] FOREIGN KEY ([nguoi_ghi_nhan_id]) REFERENCES [dbo].[Nguoi_Dung] ([id]);
ALTER TABLE [dbo].[Thanh_Toan_Don] WITH CHECK ADD CONSTRAINT [FK_Thanh_Toan_Don_don_dat_san_id] FOREIGN KEY ([don_dat_san_id]) REFERENCES [dbo].[Don_Dat_San] ([id]);
ALTER TABLE [dbo].[Thanh_Toan_Don] WITH CHECK ADD CONSTRAINT [FK_Thanh_Toan_Don_nguoi_thanh_toan_id] FOREIGN KEY ([nguoi_thanh_toan_id]) REFERENCES [dbo].[Nguoi_Dung] ([id]);
ALTER TABLE [dbo].[Thanh_Toan_Don] WITH CHECK ADD CONSTRAINT [FK_Thanh_Toan_Don_nhan_vien_thu_id] FOREIGN KEY ([nhan_vien_thu_id]) REFERENCES [dbo].[Nguoi_Dung] ([id]);
ALTER TABLE [dbo].[Thanh_Toan_Don] WITH CHECK ADD CONSTRAINT [FK_Thanh_Toan_Don_giao_dich_vi_id] FOREIGN KEY ([giao_dich_vi_id]) REFERENCES [dbo].[Giao_Dich_Vi] ([id]);
ALTER TABLE [dbo].[Thanh_Toan_Don] WITH CHECK ADD CONSTRAINT [FK_Thanh_Toan_Don_giao_dich_cong_id] FOREIGN KEY ([giao_dich_cong_id]) REFERENCES [dbo].[Giao_Dich_Cong_Thanh_Toan] ([id]);
ALTER TABLE [dbo].[Hoan_Tien] WITH CHECK ADD CONSTRAINT [FK_Hoan_Tien_thanh_toan_don_id] FOREIGN KEY ([thanh_toan_don_id]) REFERENCES [dbo].[Thanh_Toan_Don] ([id]);
ALTER TABLE [dbo].[Hoan_Tien] WITH CHECK ADD CONSTRAINT [FK_Hoan_Tien_lich_su_huy_san_id] FOREIGN KEY ([lich_su_huy_san_id]) REFERENCES [dbo].[Lich_Su_Huy_San] ([id]);
ALTER TABLE [dbo].[Hoan_Tien] WITH CHECK ADD CONSTRAINT [FK_Hoan_Tien_yeu_cau_doi_lich_id] FOREIGN KEY ([yeu_cau_doi_lich_id]) REFERENCES [dbo].[Yeu_Cau_Doi_Lich] ([id]);
ALTER TABLE [dbo].[Hoan_Tien] WITH CHECK ADD CONSTRAINT [FK_Hoan_Tien_giao_dich_vi_id] FOREIGN KEY ([giao_dich_vi_id]) REFERENCES [dbo].[Giao_Dich_Vi] ([id]);
ALTER TABLE [dbo].[Hoan_Tien] WITH CHECK ADD CONSTRAINT [FK_Hoan_Tien_giao_dich_cong_id] FOREIGN KEY ([giao_dich_cong_id]) REFERENCES [dbo].[Giao_Dich_Cong_Thanh_Toan] ([id]);
ALTER TABLE [dbo].[Hoan_Tien] WITH CHECK ADD CONSTRAINT [FK_Hoan_Tien_nguoi_duyet_id] FOREIGN KEY ([nguoi_duyet_id]) REFERENCES [dbo].[Nguoi_Dung] ([id]);
ALTER TABLE [dbo].[Nap_Tien_Vi] WITH CHECK ADD CONSTRAINT [FK_Nap_Tien_Vi_vi_id] FOREIGN KEY ([vi_id]) REFERENCES [dbo].[Vi_Nguoi_Dung] ([id]);
ALTER TABLE [dbo].[Nap_Tien_Vi] WITH CHECK ADD CONSTRAINT [FK_Nap_Tien_Vi_giao_dich_cong_id] FOREIGN KEY ([giao_dich_cong_id]) REFERENCES [dbo].[Giao_Dich_Cong_Thanh_Toan] ([id]);
ALTER TABLE [dbo].[Nap_Tien_Vi] WITH CHECK ADD CONSTRAINT [FK_Nap_Tien_Vi_giao_dich_vi_id] FOREIGN KEY ([giao_dich_vi_id]) REFERENCES [dbo].[Giao_Dich_Vi] ([id]);
ALTER TABLE [dbo].[Dieu_Chinh_Doi_Soat] WITH CHECK ADD CONSTRAINT [FK_Dieu_Chinh_Doi_Soat_ky_doi_soat_id] FOREIGN KEY ([ky_doi_soat_id]) REFERENCES [dbo].[Ky_Doi_Soat] ([id]);
ALTER TABLE [dbo].[Dieu_Chinh_Doi_Soat] WITH CHECK ADD CONSTRAINT [FK_Dieu_Chinh_Doi_Soat_chi_tiet_doi_soat_goc_id] FOREIGN KEY ([chi_tiet_doi_soat_goc_id]) REFERENCES [dbo].[Chi_Tiet_Doi_Soat_Don] ([id]);
ALTER TABLE [dbo].[Dieu_Chinh_Doi_Soat] WITH CHECK ADD CONSTRAINT [FK_Dieu_Chinh_Doi_Soat_hoan_tien_id] FOREIGN KEY ([hoan_tien_id]) REFERENCES [dbo].[Hoan_Tien] ([id]);
ALTER TABLE [dbo].[Dieu_Chinh_Doi_Soat] WITH CHECK ADD CONSTRAINT [FK_Dieu_Chinh_Doi_Soat_nguoi_tao_id] FOREIGN KEY ([nguoi_tao_id]) REFERENCES [dbo].[Nguoi_Dung] ([id]);
CREATE UNIQUE INDEX [UX_Nguoi_Dung_so_dien_thoai] ON [dbo].[Nguoi_Dung] ([so_dien_thoai]);
CREATE UNIQUE INDEX [UX_Vai_Tro_ma_vai_tro] ON [dbo].[Vai_Tro] ([ma_vai_tro]);
CREATE UNIQUE INDEX [UX_Nguoi_Dung_Vai_Tro_nguoi_dung_id_vai_tro_id] ON [dbo].[Nguoi_Dung_Vai_Tro] ([nguoi_dung_id], [vai_tro_id]);
CREATE UNIQUE INDEX [UX_Quyen_Han_ma_quyen] ON [dbo].[Quyen_Han] ([ma_quyen]);
CREATE UNIQUE INDEX [UX_Thanh_Vien_Co_So_co_so_id_nguoi_dung_id] ON [dbo].[Thanh_Vien_Co_So] ([co_so_id], [nguoi_dung_id]);
CREATE UNIQUE INDEX [UX_Phan_Quyen_Thanh_Vien_thanh_vien_co_so_id_quyen_han_id] ON [dbo].[Phan_Quyen_Thanh_Vien] ([thanh_vien_co_so_id], [quyen_han_id]);
CREATE UNIQUE INDEX [UX_Mon_The_Thao_ma_mon] ON [dbo].[Mon_The_Thao] ([ma_mon]);
CREATE UNIQUE INDEX [UX_Loai_San_mon_the_thao_id_ten_loai] ON [dbo].[Loai_San] ([mon_the_thao_id], [ten_loai]);
CREATE UNIQUE INDEX [UX_San_co_so_id_ten_san] ON [dbo].[San] ([co_so_id], [ten_san]);
CREATE UNIQUE INDEX [UX_Khung_Gio_Mac_Dinh_co_so_id_gio_bat_dau_gio_ket_thuc] ON [dbo].[Khung_Gio_Mac_Dinh] ([co_so_id], [gio_bat_dau], [gio_ket_thuc]);
CREATE UNIQUE INDEX [UX_Danh_Gia_Co_So_don_dat_san_id] ON [dbo].[Danh_Gia_Co_So] ([don_dat_san_id]);
CREATE UNIQUE INDEX [UX_Lich_Hoat_Dong_Co_So_co_so_id_thu_trong_tuan] ON [dbo].[Lich_Hoat_Dong_Co_So] ([co_so_id], [thu_trong_tuan]);
CREATE UNIQUE INDEX [UX_Don_Dat_San_ma_dat_san] ON [dbo].[Don_Dat_San] ([ma_dat_san]);
CREATE UNIQUE INDEX [UX_Don_Dat_San_ma_qr_checkin] ON [dbo].[Don_Dat_San] ([ma_qr_checkin]);
CREATE UNIQUE INDEX [UX_Chi_Tiet_Dat_San_chi_tiet_thay_the_cho_id] ON [dbo].[Chi_Tiet_Dat_San] ([chi_tiet_thay_the_cho_id]) WHERE [chi_tiet_thay_the_cho_id] IS NOT NULL;
CREATE UNIQUE INDEX [UX_Ho_So_Trinh_Do_nguoi_dung_id_mon_the_thao_id] ON [dbo].[Ho_So_Trinh_Do] ([nguoi_dung_id], [mon_the_thao_id]);
CREATE UNIQUE INDEX [UX_Phong_Ghep_Tran_ma_phong] ON [dbo].[Phong_Ghep_Tran] ([ma_phong]);
CREATE UNIQUE INDEX [UX_Thanh_Vien_Phong_phong_ghep_id_nguoi_dung_id] ON [dbo].[Thanh_Vien_Phong] ([phong_ghep_id], [nguoi_dung_id]);
CREATE UNIQUE INDEX [UX_Ket_Qua_Tran_Dau_phong_ghep_id] ON [dbo].[Ket_Qua_Tran_Dau] ([phong_ghep_id]);
CREATE UNIQUE INDEX [UX_Lich_Su_Elo_ho_so_trinh_do_id_ket_qua_tran_dau_id] ON [dbo].[Lich_Su_Elo] ([ho_so_trinh_do_id], [ket_qua_tran_dau_id]);
CREATE UNIQUE INDEX [UX_Yeu_Cau_Doi_Lich_chi_tiet_dat_san_moi_id] ON [dbo].[Yeu_Cau_Doi_Lich] ([chi_tiet_dat_san_moi_id]) WHERE [chi_tiet_dat_san_moi_id] IS NOT NULL;
CREATE UNIQUE INDEX [UX_Lich_Su_Huy_San_chi_tiet_dat_san_id] ON [dbo].[Lich_Su_Huy_San] ([chi_tiet_dat_san_id]);
CREATE UNIQUE INDEX [UX_Vi_Nguoi_Dung_nguoi_dung_id] ON [dbo].[Vi_Nguoi_Dung] ([nguoi_dung_id]);
CREATE UNIQUE INDEX [UX_Giao_Dich_Vi_ma_giao_dich] ON [dbo].[Giao_Dich_Vi] ([ma_giao_dich]);
CREATE UNIQUE INDEX [UX_Giao_Dich_Vi_khoa_chong_lap] ON [dbo].[Giao_Dich_Vi] ([khoa_chong_lap]);
CREATE UNIQUE INDEX [UX_Giao_Dich_Cong_Thanh_Toan_ma_don_hang_cong] ON [dbo].[Giao_Dich_Cong_Thanh_Toan] ([ma_don_hang_cong]);
CREATE UNIQUE INDEX [UX_Yeu_Cau_Rut_Tien_giao_dich_giu_tien_id] ON [dbo].[Yeu_Cau_Rut_Tien] ([giao_dich_giu_tien_id]);
CREATE UNIQUE INDEX [UX_Yeu_Cau_Rut_Tien_giao_dich_ket_thuc_id] ON [dbo].[Yeu_Cau_Rut_Tien] ([giao_dich_ket_thuc_id]) WHERE [giao_dich_ket_thuc_id] IS NOT NULL;
CREATE UNIQUE INDEX [UX_Yeu_Cau_Rut_Tien_giao_dich_cong_id] ON [dbo].[Yeu_Cau_Rut_Tien] ([giao_dich_cong_id]) WHERE [giao_dich_cong_id] IS NOT NULL;
CREATE UNIQUE INDEX [UX_Thanh_Toan_Don_ma_thanh_toan] ON [dbo].[Thanh_Toan_Don] ([ma_thanh_toan]);
CREATE UNIQUE INDEX [UX_Thanh_Toan_Don_giao_dich_vi_id] ON [dbo].[Thanh_Toan_Don] ([giao_dich_vi_id]) WHERE [giao_dich_vi_id] IS NOT NULL;
CREATE UNIQUE INDEX [UX_Thanh_Toan_Don_giao_dich_cong_id] ON [dbo].[Thanh_Toan_Don] ([giao_dich_cong_id]) WHERE [giao_dich_cong_id] IS NOT NULL;
CREATE UNIQUE INDEX [UX_Thanh_Toan_Don_khoa_chong_lap] ON [dbo].[Thanh_Toan_Don] ([khoa_chong_lap]);
CREATE UNIQUE INDEX [UX_Hoan_Tien_ma_hoan_tien] ON [dbo].[Hoan_Tien] ([ma_hoan_tien]);
CREATE UNIQUE INDEX [UX_Hoan_Tien_giao_dich_vi_id] ON [dbo].[Hoan_Tien] ([giao_dich_vi_id]) WHERE [giao_dich_vi_id] IS NOT NULL;
CREATE UNIQUE INDEX [UX_Hoan_Tien_giao_dich_cong_id] ON [dbo].[Hoan_Tien] ([giao_dich_cong_id]) WHERE [giao_dich_cong_id] IS NOT NULL;
CREATE UNIQUE INDEX [UX_Hoan_Tien_khoa_chong_lap] ON [dbo].[Hoan_Tien] ([khoa_chong_lap]);
CREATE UNIQUE INDEX [UX_Nap_Tien_Vi_giao_dich_cong_id] ON [dbo].[Nap_Tien_Vi] ([giao_dich_cong_id]);
CREATE UNIQUE INDEX [UX_Nap_Tien_Vi_giao_dich_vi_id] ON [dbo].[Nap_Tien_Vi] ([giao_dich_vi_id]) WHERE [giao_dich_vi_id] IS NOT NULL;
CREATE UNIQUE INDEX [UX_Voucher_ma_voucher] ON [dbo].[Voucher] ([ma_voucher]);
CREATE UNIQUE INDEX [UX_Voucher_Nguoi_Dung_nguoi_dung_id_voucher_id] ON [dbo].[Voucher_Nguoi_Dung] ([nguoi_dung_id], [voucher_id]);
CREATE UNIQUE INDEX [UX_Ky_Doi_Soat_ma_ky_doi_soat] ON [dbo].[Ky_Doi_Soat] ([ma_ky_doi_soat]);
CREATE UNIQUE INDEX [UX_Chi_Tiet_Doi_Soat_Don_don_dat_san_id] ON [dbo].[Chi_Tiet_Doi_Soat_Don] ([don_dat_san_id]);
CREATE UNIQUE INDEX [UX_Bao_Cao_Hieu_Suat_San_san_id_ngay_bao_cao] ON [dbo].[Bao_Cao_Hieu_Suat_San] ([san_id], [ngay_bao_cao]);
CREATE UNIQUE INDEX [UX_Dieu_Chinh_Doi_Soat_hoan_tien_id] ON [dbo].[Dieu_Chinh_Doi_Soat] ([hoan_tien_id]) WHERE [hoan_tien_id] IS NOT NULL;
CREATE UNIQUE INDEX [UX_Nguoi_Dung_email] ON [dbo].[Nguoi_Dung] ([email]) WHERE [email] IS NOT NULL;
CREATE UNIQUE INDEX [UX_Giao_Dich_Cong_Thanh_Toan_cong_thanh_toan_ma_giao_dich_ngan_hang] ON [dbo].[Giao_Dich_Cong_Thanh_Toan] ([cong_thanh_toan], [ma_giao_dich_ngan_hang]) WHERE [ma_giao_dich_ngan_hang] IS NOT NULL;
CREATE UNIQUE INDEX [UX_Yeu_Cau_Doi_Lich_chi_tiet_dat_san_cu_id] ON [dbo].[Yeu_Cau_Doi_Lich] ([chi_tiet_dat_san_cu_id]) WHERE [trang_thai] = N'CHO_DUYET';
CREATE UNIQUE INDEX [UX_Phong_Ghep_Tran_chi_tiet_dat_san_id] ON [dbo].[Phong_Ghep_Tran] ([chi_tiet_dat_san_id]) WHERE [trang_thai] IN (N'DANG_TUYEN', N'DU_NGUOI', N'DANG_DAU');
CREATE UNIQUE INDEX [UX_Lich_Su_Dung_Voucher_don_dat_san_id] ON [dbo].[Lich_Su_Dung_Voucher] ([don_dat_san_id]) WHERE [trang_thai] IN (N'GIU_CHO', N'DA_AP_DUNG');
CREATE INDEX [IX_Chi_Tiet_Dat_San_san_id_ngay_su_dung_gio_bat_dau] ON [dbo].[Chi_Tiet_Dat_San] ([san_id], [ngay_su_dung], [gio_bat_dau]) INCLUDE ([gio_ket_thuc], [trang_thai_slot], [don_dat_san_id]);
CREATE UNIQUE INDEX [UX_Chi_Tiet_Dat_San_lich_dat_co_dinh_id_ngay_su_dung] ON [dbo].[Chi_Tiet_Dat_San] ([lich_dat_co_dinh_id], [ngay_su_dung]) WHERE [lich_dat_co_dinh_id] IS NOT NULL AND [trang_thai_slot] IN (N'GIU_CHO', N'DA_XAC_NHAN', N'DA_CHECK_IN', N'HOAN_THANH', N'VANG_MAT');
CREATE INDEX [IX_Don_Dat_San_trang_thai_thoi_gian_het_han_giu_cho] ON [dbo].[Don_Dat_San] ([trang_thai], [thoi_gian_het_han_giu_cho]);
CREATE INDEX [IX_Bang_Gia_san_id_khung_gio_id_thu_trong_tuan_ngay_hieu_luc_tu] ON [dbo].[Bang_Gia] ([san_id], [khung_gio_id], [thu_trong_tuan], [ngay_hieu_luc_tu]);
CREATE INDEX [IX_Lich_Khoa_San_san_id_bat_dau_luc_ket_thuc_luc] ON [dbo].[Lich_Khoa_San] ([san_id], [bat_dau_luc], [ket_thuc_luc]) INCLUDE ([trang_thai]);
CREATE INDEX [IX_Thong_Bao_nguoi_dung_id_da_doc_ngay_tao] ON [dbo].[Thong_Bao] ([nguoi_dung_id], [da_doc], [ngay_tao]);
CREATE INDEX [IX_Thanh_Toan_Don_don_dat_san_id_trang_thai] ON [dbo].[Thanh_Toan_Don] ([don_dat_san_id], [trang_thai]) INCLUDE ([so_tien], [phuong_thuc]);
CREATE INDEX [IX_Hoan_Tien_thanh_toan_don_id_trang_thai] ON [dbo].[Hoan_Tien] ([thanh_toan_don_id], [trang_thai]) INCLUDE ([so_tien]);
CREATE INDEX [IX_Nguoi_Dung_Vai_Tro_vai_tro_id] ON [dbo].[Nguoi_Dung_Vai_Tro] ([vai_tro_id]);
CREATE INDEX [IX_Thanh_Vien_Co_So_nguoi_dung_id] ON [dbo].[Thanh_Vien_Co_So] ([nguoi_dung_id]);
CREATE INDEX [IX_Phan_Quyen_Thanh_Vien_quyen_han_id] ON [dbo].[Phan_Quyen_Thanh_Vien] ([quyen_han_id]);
CREATE INDEX [IX_Co_So_chu_so_huu_id] ON [dbo].[Co_So] ([chu_so_huu_id]);
CREATE INDEX [IX_San_loai_san_id] ON [dbo].[San] ([loai_san_id]);
CREATE INDEX [IX_Bang_Gia_khung_gio_id] ON [dbo].[Bang_Gia] ([khung_gio_id]);
CREATE INDEX [IX_Don_Dat_San_nguoi_dung_id] ON [dbo].[Don_Dat_San] ([nguoi_dung_id]);
CREATE INDEX [IX_Don_Dat_San_co_so_id] ON [dbo].[Don_Dat_San] ([co_so_id]);
CREATE INDEX [IX_Don_Dat_San_nhan_vien_tao_id] ON [dbo].[Don_Dat_San] ([nhan_vien_tao_id]);
CREATE INDEX [IX_Chi_Tiet_Dat_San_don_dat_san_id] ON [dbo].[Chi_Tiet_Dat_San] ([don_dat_san_id]);
CREATE INDEX [IX_Lich_Dat_Co_Dinh_nguoi_dung_id] ON [dbo].[Lich_Dat_Co_Dinh] ([nguoi_dung_id]);
CREATE INDEX [IX_Lich_Dat_Co_Dinh_san_id] ON [dbo].[Lich_Dat_Co_Dinh] ([san_id]);
CREATE INDEX [IX_Lich_Su_Check_In_nhan_vien_checkin_id] ON [dbo].[Lich_Su_Check_In] ([nhan_vien_checkin_id]);
CREATE INDEX [IX_Dich_Vu_co_so_id] ON [dbo].[Dich_Vu] ([co_so_id]);
CREATE INDEX [IX_Chi_Tiet_Dich_Vu_Don_don_dat_san_id] ON [dbo].[Chi_Tiet_Dich_Vu_Don] ([don_dat_san_id]);
CREATE INDEX [IX_Chi_Tiet_Dich_Vu_Don_dich_vu_id] ON [dbo].[Chi_Tiet_Dich_Vu_Don] ([dich_vu_id]);
CREATE INDEX [IX_Ho_So_Trinh_Do_mon_the_thao_id] ON [dbo].[Ho_So_Trinh_Do] ([mon_the_thao_id]);
CREATE INDEX [IX_Phong_Ghep_Tran_nguoi_tao_id] ON [dbo].[Phong_Ghep_Tran] ([nguoi_tao_id]);
CREATE INDEX [IX_Phong_Ghep_Tran_mon_the_thao_id] ON [dbo].[Phong_Ghep_Tran] ([mon_the_thao_id]);
CREATE INDEX [IX_Thanh_Vien_Phong_nguoi_dung_id] ON [dbo].[Thanh_Vien_Phong] ([nguoi_dung_id]);
CREATE INDEX [IX_Ket_Qua_Tran_Dau_nguoi_xac_nhan_doi_1_id] ON [dbo].[Ket_Qua_Tran_Dau] ([nguoi_xac_nhan_doi_1_id]);
CREATE INDEX [IX_Ket_Qua_Tran_Dau_nguoi_xac_nhan_doi_2_id] ON [dbo].[Ket_Qua_Tran_Dau] ([nguoi_xac_nhan_doi_2_id]);
CREATE INDEX [IX_Chinh_Sach_Huy_Doi_co_so_id] ON [dbo].[Chinh_Sach_Huy_Doi] ([co_so_id]);
CREATE INDEX [IX_Yeu_Cau_Doi_Lich_san_moi_id] ON [dbo].[Yeu_Cau_Doi_Lich] ([san_moi_id]);
CREATE INDEX [IX_Lich_Su_Huy_San_nguoi_yeu_cau_id] ON [dbo].[Lich_Su_Huy_San] ([nguoi_yeu_cau_id]);
CREATE INDEX [IX_Nhat_Ky_Vi_Pham_nguoi_dung_id] ON [dbo].[Nhat_Ky_Vi_Pham] ([nguoi_dung_id]);
CREATE INDEX [IX_Nhat_Ky_Vi_Pham_don_dat_san_id] ON [dbo].[Nhat_Ky_Vi_Pham] ([don_dat_san_id]);
CREATE INDEX [IX_Giao_Dich_Vi_vi_id] ON [dbo].[Giao_Dich_Vi] ([vi_id]);
CREATE INDEX [IX_Giao_Dich_Cong_Thanh_Toan_nguoi_dung_id] ON [dbo].[Giao_Dich_Cong_Thanh_Toan] ([nguoi_dung_id]);
CREATE INDEX [IX_Yeu_Cau_Rut_Tien_vi_id] ON [dbo].[Yeu_Cau_Rut_Tien] ([vi_id]);
CREATE INDEX [IX_Yeu_Cau_Rut_Tien_nhan_vien_duyet_id] ON [dbo].[Yeu_Cau_Rut_Tien] ([nhan_vien_duyet_id]);
CREATE INDEX [IX_Voucher_co_so_id] ON [dbo].[Voucher] ([co_so_id]);
CREATE INDEX [IX_Voucher_Nguoi_Dung_voucher_id] ON [dbo].[Voucher_Nguoi_Dung] ([voucher_id]);
CREATE INDEX [IX_Lich_Su_Dung_Voucher_voucher_id] ON [dbo].[Lich_Su_Dung_Voucher] ([voucher_id]);
CREATE INDEX [IX_Lich_Su_Dung_Voucher_nguoi_dung_id] ON [dbo].[Lich_Su_Dung_Voucher] ([nguoi_dung_id]);
CREATE INDEX [IX_Cau_Hinh_Hoa_Hong_co_so_id] ON [dbo].[Cau_Hinh_Hoa_Hong] ([co_so_id]);
CREATE INDEX [IX_Ky_Doi_Soat_co_so_id] ON [dbo].[Ky_Doi_Soat] ([co_so_id]);
CREATE INDEX [IX_Ky_Doi_Soat_nhan_vien_doi_soat_id] ON [dbo].[Ky_Doi_Soat] ([nhan_vien_doi_soat_id]);
CREATE INDEX [IX_Chi_Tiet_Doi_Soat_Don_ky_doi_soat_id] ON [dbo].[Chi_Tiet_Doi_Soat_Don] ([ky_doi_soat_id]);
CREATE INDEX [IX_Muc_Phi_Huy_Doi_chinh_sach_huy_doi_id] ON [dbo].[Muc_Phi_Huy_Doi] ([chinh_sach_huy_doi_id]);
CREATE INDEX [IX_Chi_Tiet_Dat_San_khung_gio_id] ON [dbo].[Chi_Tiet_Dat_San] ([khung_gio_id]);
CREATE INDEX [IX_Chi_Tiet_Dat_San_bang_gia_id] ON [dbo].[Chi_Tiet_Dat_San] ([bang_gia_id]);
CREATE INDEX [IX_Chi_Tiet_Dat_San_chinh_sach_huy_doi_id] ON [dbo].[Chi_Tiet_Dat_San] ([chinh_sach_huy_doi_id]);
CREATE INDEX [IX_Lich_Dat_Co_Dinh_khung_gio_id] ON [dbo].[Lich_Dat_Co_Dinh] ([khung_gio_id]);
CREATE INDEX [IX_Lich_Su_Check_In_chi_tiet_dat_san_id] ON [dbo].[Lich_Su_Check_In] ([chi_tiet_dat_san_id]);
CREATE INDEX [IX_Yeu_Cau_Doi_Lich_nguoi_yeu_cau_id] ON [dbo].[Yeu_Cau_Doi_Lich] ([nguoi_yeu_cau_id]);
CREATE INDEX [IX_Yeu_Cau_Doi_Lich_nguoi_xu_ly_id] ON [dbo].[Yeu_Cau_Doi_Lich] ([nguoi_xu_ly_id]);
CREATE INDEX [IX_Yeu_Cau_Doi_Lich_khung_gio_moi_id] ON [dbo].[Yeu_Cau_Doi_Lich] ([khung_gio_moi_id]);
CREATE INDEX [IX_Lich_Su_Huy_San_nguoi_xu_ly_id] ON [dbo].[Lich_Su_Huy_San] ([nguoi_xu_ly_id]);
CREATE INDEX [IX_Chi_Tiet_Doi_Soat_Don_cau_hinh_hoa_hong_id] ON [dbo].[Chi_Tiet_Doi_Soat_Don] ([cau_hinh_hoa_hong_id]);
CREATE INDEX [IX_Lich_Khoa_San_nguoi_tao_id] ON [dbo].[Lich_Khoa_San] ([nguoi_tao_id]);
CREATE INDEX [IX_Lich_Su_Elo_ket_qua_tran_dau_id] ON [dbo].[Lich_Su_Elo] ([ket_qua_tran_dau_id]);
CREATE INDEX [IX_Dong_Gop_Phong_thanh_vien_phong_id] ON [dbo].[Dong_Gop_Phong] ([thanh_vien_phong_id]);
CREATE INDEX [IX_Dong_Gop_Phong_nguoi_ghi_nhan_id] ON [dbo].[Dong_Gop_Phong] ([nguoi_ghi_nhan_id]);
CREATE INDEX [IX_Thanh_Toan_Don_nguoi_thanh_toan_id] ON [dbo].[Thanh_Toan_Don] ([nguoi_thanh_toan_id]);
CREATE INDEX [IX_Thanh_Toan_Don_nhan_vien_thu_id] ON [dbo].[Thanh_Toan_Don] ([nhan_vien_thu_id]);
CREATE INDEX [IX_Hoan_Tien_lich_su_huy_san_id] ON [dbo].[Hoan_Tien] ([lich_su_huy_san_id]);
CREATE INDEX [IX_Hoan_Tien_yeu_cau_doi_lich_id] ON [dbo].[Hoan_Tien] ([yeu_cau_doi_lich_id]);
CREATE INDEX [IX_Hoan_Tien_nguoi_duyet_id] ON [dbo].[Hoan_Tien] ([nguoi_duyet_id]);
CREATE INDEX [IX_Nap_Tien_Vi_vi_id] ON [dbo].[Nap_Tien_Vi] ([vi_id]);
CREATE INDEX [IX_Dieu_Chinh_Doi_Soat_ky_doi_soat_id] ON [dbo].[Dieu_Chinh_Doi_Soat] ([ky_doi_soat_id]);
CREATE INDEX [IX_Dieu_Chinh_Doi_Soat_chi_tiet_doi_soat_goc_id] ON [dbo].[Dieu_Chinh_Doi_Soat] ([chi_tiet_doi_soat_goc_id]);
CREATE INDEX [IX_Dieu_Chinh_Doi_Soat_nguoi_tao_id] ON [dbo].[Dieu_Chinh_Doi_Soat] ([nguoi_tao_id]);

-- Dữ liệu danh mục tối thiểu; không tạo tài khoản, cơ sở hay booking giả.
INSERT dbo.Vai_Tro (ma_vai_tro, ten_vai_tro, mo_ta) VALUES
(N'ADMIN', N'Quản trị hệ thống', N'Quản lý toàn hệ thống'),
(N'CHU_SAN', N'Chủ cơ sở', N'Quyền chi tiết theo thành viên cơ sở'),
(N'NHAN_VIEN', N'Nhân viên', N'Quyền chi tiết theo thành viên cơ sở'),
(N'KHACH_HANG', N'Khách hàng', N'Đặt sân và tham gia ghép trận');
INSERT dbo.Mon_The_Thao (ma_mon, ten_mon) VALUES
(N'BONG_DA', N'Bóng đá'), (N'CAU_LONG', N'Cầu lông'),
(N'TENNIS', N'Tennis'), (N'PICKLEBALL', N'Pickleball'), (N'BONG_RO', N'Bóng rổ');
INSERT dbo.Quyen_Han (ma_quyen, nhom_chuc_nang, mo_ta) VALUES
(N'XEM_LICH', N'DAT_SAN', N'Xem lịch cơ sở'),
(N'TAO_DON', N'DAT_SAN', N'Tạo đơn tại quầy'),
(N'DOI_HUY_DON', N'DAT_SAN', N'Xử lý đổi/hủy'),
(N'CHECK_IN', N'DAT_SAN', N'Check-in khách'),
(N'THU_TIEN', N'TAI_CHINH', N'Ghi nhận tiền tại quầy'),
(N'QUAN_LY_GIA', N'CAU_HINH', N'Quản lý bảng giá'),
(N'QUAN_LY_THANH_VIEN', N'CAU_HINH', N'Quản lý nhân viên'),
(N'XEM_DOI_SOAT', N'TAI_CHINH', N'Xem đối soát cơ sở');

-- Tạo routines trong cùng transaction cài đặt.
EXEC sys.sp_executesql N'/* =============================================================
   HÀM NGÀY/GIỜ VÀ KHÓA LỊCH
   ============================================================= */
CREATE FUNCTION dbo.fn_BatDauBuoi (@ngay date, @gio time(0))
RETURNS datetime2(0)
WITH SCHEMABINDING
AS
BEGIN
    RETURN DATEADD(SECOND, DATEDIFF(SECOND, CONVERT(time(0), ''00:00:00''), @gio), CONVERT(datetime2(0), @ngay));
END;';
EXEC sys.sp_executesql N'CREATE FUNCTION dbo.fn_KetThucBuoi (@ngay date, @gio time(0))
RETURNS datetime2(0)
WITH SCHEMABINDING
AS
BEGIN
    RETURN CASE WHEN @gio = CONVERT(time(0), ''00:00:00'')
        THEN DATEADD(DAY, 1, CONVERT(datetime2(0), @ngay))
        ELSE dbo.fn_BatDauBuoi(@ngay, @gio) END;
END;';
EXEC sys.sp_executesql N'-- Hàm kiểm tra không phụ thuộc SET DATEFIRST/ngôn ngữ phiên kết nối.
CREATE FUNCTION dbo.fn_ThuTrongTuan (@ngay date)
RETURNS int
WITH SCHEMABINDING
AS
BEGIN
    RETURN ((DATEDIFF(DAY, CONVERT(date, ''19000101'', 112), @ngay) % 7 + 7) % 7) + 1;
END;';
EXEC sys.sp_executesql N'CREATE PROCEDURE dbo.usp_KhoaVaKiemTraLich
AS
BEGIN
    SET NOCOUNT ON;
    IF @@TRANCOUNT = 0
        THROW 51100, N''Kiểm tra lịch phải nằm trong transaction.'', 1;
    DECLARE @kq int, @baygio datetime2(0) = DATEADD(HOUR, 7, SYSUTCDATETIME());
    EXEC @kq = sys.sp_getapplock @Resource = N''QLSB:LICH_SAN'',
        @LockMode = N''Exclusive'', @LockOwner = N''Transaction'', @LockTimeout = 10000;
    IF @kq < 0
        THROW 51101, N''Không lấy được khóa lịch; hãy thử lại toàn bộ giao dịch.'', 1;

    -- UPDLOCK/HOLDLOCK buộc đọc dữ liệu đang khóa, không đọc snapshot cũ.
    -- Khi client ghi trực tiếp trước khi lấy applock có thể phát sinh deadlock;
    -- SQL Server hủy một transaction, client phải retry. Procedure đặt sân lấy khóa trước khi ghi.
    DECLARE @lich TABLE (id bigint PRIMARY KEY, san_id bigint, bat_dau datetime2(0), ket_thuc datetime2(0));
    INSERT @lich (id, san_id, bat_dau, ket_thuc)
    SELECT c.id, c.san_id, dbo.fn_BatDauBuoi(c.ngay_su_dung, c.gio_bat_dau),
           dbo.fn_KetThucBuoi(c.ngay_su_dung, c.gio_ket_thuc)
    FROM dbo.Chi_Tiet_Dat_San c WITH (UPDLOCK, HOLDLOCK)
    JOIN dbo.Don_Dat_San d WITH (UPDLOCK, HOLDLOCK) ON d.id = c.don_dat_san_id
    WHERE c.trang_thai_slot IN (N''DA_XAC_NHAN'', N''DA_CHECK_IN'', N''HOAN_THANH'', N''VANG_MAT'')
       OR (c.trang_thai_slot = N''GIU_CHO'' AND d.thoi_gian_het_han_giu_cho > @baygio);

    IF EXISTS (
        SELECT 1 FROM @lich a JOIN @lich b ON a.id < b.id AND a.san_id = b.san_id
        AND a.bat_dau < b.ket_thuc AND b.bat_dau < a.ket_thuc
    ) THROW 51102, N''Sân đã có lịch giao nhau trong khoảng thời gian này.'', 1;

    IF EXISTS (
        SELECT 1 FROM @lich a JOIN dbo.Lich_Khoa_San k WITH (UPDLOCK, HOLDLOCK)
          ON k.san_id = a.san_id AND k.trang_thai = N''DANG_AP_DUNG''
         AND a.bat_dau < k.ket_thuc_luc AND k.bat_dau_luc < a.ket_thuc
    ) THROW 51103, N''Lịch đặt giao với lịch khóa/bảo trì sân.'', 1;

    IF EXISTS (
        SELECT 1 FROM dbo.Chi_Tiet_Dat_San c WITH (UPDLOCK, HOLDLOCK)
        JOIN dbo.Don_Dat_San d WITH (UPDLOCK, HOLDLOCK) ON d.id = c.don_dat_san_id
        WHERE c.trang_thai_slot = N''GIU_CHO'' AND d.thoi_gian_het_han_giu_cho IS NULL
    ) THROW 51104, N''Chi tiết giữ chỗ phải có hạn giữ chỗ ở đơn.'', 1;
END;';
EXEC sys.sp_executesql N'/* =============================================================
   TRIGGER KIỂM TRA ĐẶT SÂN VÀ CẤU HÌNH
   ============================================================= */
CREATE TRIGGER dbo.TR_ChiTietDatSan_KiemTra
ON dbo.Chi_Tiet_Dat_San AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM inserted) RETURN;
    IF EXISTS (
        SELECT 1 FROM inserted i
        JOIN dbo.Don_Dat_San d ON d.id = i.don_dat_san_id
        JOIN dbo.San s ON s.id = i.san_id
        JOIN dbo.Khung_Gio_Mac_Dinh k ON k.id = i.khung_gio_id
        JOIN dbo.Bang_Gia g ON g.id = i.bang_gia_id
        JOIN dbo.Chinh_Sach_Huy_Doi p ON p.id = i.chinh_sach_huy_doi_id
        WHERE d.co_so_id <> s.co_so_id OR k.co_so_id <> s.co_so_id OR p.co_so_id <> s.co_so_id
           OR g.san_id <> i.san_id OR g.khung_gio_id <> i.khung_gio_id
           OR k.gio_bat_dau <> i.gio_bat_dau OR k.gio_ket_thuc <> i.gio_ket_thuc
           OR g.thu_trong_tuan <> dbo.fn_ThuTrongTuan(i.ngay_su_dung)
           OR i.ngay_su_dung < g.ngay_hieu_luc_tu
           OR (g.ngay_hieu_luc_den IS NOT NULL AND i.ngay_su_dung > g.ngay_hieu_luc_den)
    ) THROW 51200, N''Đơn/sân/khung/giá/chính sách không khớp cơ sở, ngày hoặc giờ.'', 1;

    IF EXISTS (
        SELECT 1 FROM inserted i JOIN dbo.Lich_Dat_Co_Dinh l ON l.id = i.lich_dat_co_dinh_id
        JOIN dbo.Don_Dat_San d ON d.id = i.don_dat_san_id
        WHERE l.san_id <> i.san_id OR l.khung_gio_id <> i.khung_gio_id
           OR d.nguoi_dung_id IS NULL OR l.nguoi_dung_id <> d.nguoi_dung_id
           OR dbo.fn_ThuTrongTuan(i.ngay_su_dung) <> l.thu_trong_tuan
           OR i.ngay_su_dung NOT BETWEEN l.ngay_bat_dau AND l.ngay_ket_thuc
    ) THROW 51201, N''Buổi đặt không thuộc lịch cố định đã chọn.'', 1;

    IF EXISTS (
        SELECT 1 FROM inserted i JOIN dbo.Chi_Tiet_Dat_San c ON c.id = i.chi_tiet_thay_the_cho_id
        WHERE c.don_dat_san_id <> i.don_dat_san_id
    ) THROW 51202, N''Buổi thay thế phải thuộc cùng đơn.'', 1;

    -- Giá và thời gian của chi tiết cũ được giữ nguyên; đổi lịch bằng dòng mới.
    IF EXISTS (
        SELECT 1 FROM inserted i JOIN deleted o ON o.id = i.id
        WHERE i.don_dat_san_id <> o.don_dat_san_id OR i.san_id <> o.san_id
           OR i.khung_gio_id <> o.khung_gio_id OR i.bang_gia_id <> o.bang_gia_id
           OR i.ngay_su_dung <> o.ngay_su_dung OR i.gio_bat_dau <> o.gio_bat_dau
           OR i.gio_ket_thuc <> o.gio_ket_thuc OR i.gia_thue <> o.gia_thue
           OR i.chinh_sach_huy_doi_id <> o.chinh_sach_huy_doi_id
    ) THROW 51203, N''Không sửa giá/thời gian/nguồn của buổi cũ; hãy tạo buổi thay thế.'', 1;
    EXEC dbo.usp_KhoaVaKiemTraLich;
END;';
EXEC sys.sp_executesql N'CREATE TRIGGER dbo.TR_DonDatSan_KiemTra
ON dbo.Don_Dat_San AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM inserted) RETURN;
    IF EXISTS (
        SELECT 1 FROM inserted i JOIN deleted o ON o.id = i.id
        WHERE i.co_so_id <> o.co_so_id
    ) THROW 51204, N''Không đổi cơ sở của đơn; hãy tạo đơn mới.'', 1;
    IF EXISTS (
        SELECT 1 FROM inserted i
        WHERE i.nhan_vien_tao_id IS NOT NULL AND NOT EXISTS (
            SELECT 1 FROM dbo.Thanh_Vien_Co_So m
            WHERE m.co_so_id = i.co_so_id AND m.nguoi_dung_id = i.nhan_vien_tao_id
        )
    ) THROW 51205, N''Nhân viên tạo đơn không thuộc cơ sở.'', 1;
    EXEC dbo.usp_KhoaVaKiemTraLich;
END;';
EXEC sys.sp_executesql N'CREATE TRIGGER dbo.TR_LichKhoaSan_KiemTra
ON dbo.Lich_Khoa_San AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM inserted) RETURN;
    EXEC dbo.usp_KhoaVaKiemTraLich;
END;';
EXEC sys.sp_executesql N'CREATE TRIGGER dbo.TR_BangGia_KiemTra
ON dbo.Bang_Gia AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM inserted) RETURN;
    DECLARE @kq int;
    EXEC @kq = sys.sp_getapplock @Resource=N''QLSB:CAU_HINH'', @LockMode=N''Exclusive'', @LockOwner=N''Transaction'', @LockTimeout=10000;
    IF @kq < 0 THROW 51206, N''Không lấy được khóa cấu hình; hãy thử lại.'', 1;
    IF EXISTS (
        SELECT 1 FROM inserted i JOIN dbo.San s ON s.id = i.san_id
        JOIN dbo.Khung_Gio_Mac_Dinh k ON k.id = i.khung_gio_id
        WHERE s.co_so_id <> k.co_so_id
    ) THROW 51207, N''Sân và khung giờ của bảng giá khác cơ sở.'', 1;
    IF EXISTS (
        SELECT 1 FROM inserted i JOIN dbo.Bang_Gia g WITH (UPDLOCK, HOLDLOCK)
          ON g.id <> i.id AND g.san_id = i.san_id AND g.khung_gio_id = i.khung_gio_id
         AND g.thu_trong_tuan = i.thu_trong_tuan
        WHERE i.dang_ap_dung = 1 AND g.dang_ap_dung = 1
          AND i.ngay_hieu_luc_tu <= ISNULL(g.ngay_hieu_luc_den, CONVERT(date,''99991231'',112))
          AND g.ngay_hieu_luc_tu <= ISNULL(i.ngay_hieu_luc_den, CONVERT(date,''99991231'',112))
    ) THROW 51208, N''Có hai bảng giá cùng sân/khung/thứ giao nhau về hiệu lực.'', 1;
    IF EXISTS (
        SELECT 1 FROM inserted i JOIN deleted o ON o.id = i.id
        WHERE EXISTS (SELECT 1 FROM dbo.Chi_Tiet_Dat_San c WHERE c.bang_gia_id = i.id)
          AND (i.san_id <> o.san_id OR i.khung_gio_id <> o.khung_gio_id
            OR i.thu_trong_tuan <> o.thu_trong_tuan OR i.gia_thue <> o.gia_thue
            OR i.tien_coc_toi_thieu <> o.tien_coc_toi_thieu
            OR i.ngay_hieu_luc_tu <> o.ngay_hieu_luc_tu
            OR ISNULL(i.ngay_hieu_luc_den, CONVERT(date,''99991231'',112)) <> ISNULL(o.ngay_hieu_luc_den, CONVERT(date,''99991231'',112)))
    ) THROW 51209, N''Bảng giá đã dùng: chỉ được tắt áp dụng và tạo giá mới.'', 1;
END;';
EXEC sys.sp_executesql N'CREATE TRIGGER dbo.TR_KhungGio_KiemTra
ON dbo.Khung_Gio_Mac_Dinh AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM inserted) RETURN;
    DECLARE @kq int;
    EXEC @kq = sys.sp_getapplock @Resource=N''QLSB:CAU_HINH'', @LockMode=N''Exclusive'', @LockOwner=N''Transaction'', @LockTimeout=10000;
    IF @kq < 0 THROW 51210, N''Không lấy được khóa cấu hình.'', 1;
    IF EXISTS (
        SELECT 1 FROM inserted i JOIN dbo.Khung_Gio_Mac_Dinh k WITH (UPDLOCK, HOLDLOCK)
          ON k.id <> i.id AND k.co_so_id = i.co_so_id
        WHERE DATEDIFF(SECOND, CONVERT(time(0),''00:00:00''), i.gio_bat_dau)
                < CASE WHEN k.gio_ket_thuc = ''00:00:00'' THEN 86400 ELSE DATEDIFF(SECOND, CONVERT(time(0),''00:00:00''), k.gio_ket_thuc) END
          AND DATEDIFF(SECOND, CONVERT(time(0),''00:00:00''), k.gio_bat_dau)
                < CASE WHEN i.gio_ket_thuc = ''00:00:00'' THEN 86400 ELSE DATEDIFF(SECOND, CONVERT(time(0),''00:00:00''), i.gio_ket_thuc) END
    ) THROW 51211, N''Các khung giờ của cơ sở không được chồng nhau.'', 1;
    IF EXISTS (
        SELECT 1 FROM inserted i JOIN deleted o ON o.id = i.id
        WHERE (i.co_so_id <> o.co_so_id OR i.gio_bat_dau <> o.gio_bat_dau OR i.gio_ket_thuc <> o.gio_ket_thuc)
          AND (EXISTS (SELECT 1 FROM dbo.Bang_Gia g WHERE g.khung_gio_id=i.id)
            OR EXISTS (SELECT 1 FROM dbo.Lich_Dat_Co_Dinh l WHERE l.khung_gio_id=i.id))
    ) THROW 51212, N''Khung giờ đã dùng: không sửa cơ sở hoặc thời gian.'', 1;
END;';
EXEC sys.sp_executesql N'CREATE TRIGGER dbo.TR_ChinhSach_KiemTra
ON dbo.Chinh_Sach_Huy_Doi AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM inserted) RETURN;
    DECLARE @kq int;
    EXEC @kq = sys.sp_getapplock @Resource=N''QLSB:CAU_HINH'', @LockMode=N''Exclusive'', @LockOwner=N''Transaction'', @LockTimeout=10000;
    IF @kq < 0 THROW 51213, N''Không lấy được khóa cấu hình.'', 1;
    IF EXISTS (
        SELECT 1 FROM inserted i JOIN dbo.Chinh_Sach_Huy_Doi p WITH (UPDLOCK, HOLDLOCK)
          ON p.id <> i.id AND p.co_so_id=i.co_so_id
        WHERE i.ngay_hieu_luc_tu <= ISNULL(p.ngay_hieu_luc_den,CONVERT(date,''99991231'',112))
          AND p.ngay_hieu_luc_tu <= ISNULL(i.ngay_hieu_luc_den,CONVERT(date,''99991231'',112))
    ) THROW 51214, N''Hai phiên bản chính sách giao nhau về ngày hiệu lực.'', 1;
    IF EXISTS (
        SELECT 1 FROM inserted i JOIN deleted o ON o.id=i.id
        WHERE EXISTS (SELECT 1 FROM dbo.Chi_Tiet_Dat_San c WHERE c.chinh_sach_huy_doi_id=i.id)
          AND (i.co_so_id<>o.co_so_id OR i.cho_phep_doi_lich<>o.cho_phep_doi_lich
            OR i.ngay_hieu_luc_tu<>o.ngay_hieu_luc_tu)
    ) THROW 51215, N''Không sửa điều khoản/ngày bắt đầu của chính sách đã được dùng.'', 1;
    -- Được đóng ngày hết hiệu lực cho việc bán mới; không làm thay đổi chính sách của đơn cũ.
END;';
EXEC sys.sp_executesql N'CREATE TRIGGER dbo.TR_MucPhi_KiemTra
ON dbo.Muc_Phi_Huy_Doi AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM inserted) AND NOT EXISTS (SELECT 1 FROM deleted) RETURN;
    DECLARE @kq int;
    EXEC @kq = sys.sp_getapplock @Resource=N''QLSB:CAU_HINH'', @LockMode=N''Exclusive'', @LockOwner=N''Transaction'', @LockTimeout=10000;
    IF @kq < 0 THROW 51216, N''Không lấy được khóa cấu hình.'', 1;
    IF EXISTS (
        SELECT 1 FROM dbo.Chi_Tiet_Dat_San c
        WHERE c.chinh_sach_huy_doi_id IN (
            SELECT chinh_sach_huy_doi_id FROM inserted UNION SELECT chinh_sach_huy_doi_id FROM deleted)
    ) THROW 51217, N''Không thay đổi mức phí của phiên bản chính sách đã được dùng.'', 1;
    IF EXISTS (
        SELECT 1 FROM inserted i JOIN dbo.Muc_Phi_Huy_Doi p WITH (UPDLOCK, HOLDLOCK)
          ON i.id <> p.id AND i.chinh_sach_huy_doi_id=p.chinh_sach_huy_doi_id
        WHERE (p.so_gio_den_truoc_tran IS NULL OR i.so_gio_tu_truoc_tran < p.so_gio_den_truoc_tran)
          AND (i.so_gio_den_truoc_tran IS NULL OR p.so_gio_tu_truoc_tran < i.so_gio_den_truoc_tran)
    ) THROW 51218, N''Các khoảng giờ tính phí không được giao nhau.'', 1;
END;';
EXEC sys.sp_executesql N'CREATE TRIGGER dbo.TR_DichVuDon_CungCoSo
ON dbo.Chi_Tiet_Dich_Vu_Don AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1 FROM inserted i JOIN dbo.Don_Dat_San d ON d.id=i.don_dat_san_id
        JOIN dbo.Dich_Vu v ON v.id=i.dich_vu_id WHERE d.co_so_id<>v.co_so_id
    ) THROW 51219, N''Dịch vụ không thuộc cơ sở của đơn.'', 1;
END;';
EXEC sys.sp_executesql N'CREATE TRIGGER dbo.TR_PhongGhep_DungMon
ON dbo.Phong_Ghep_Tran AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1 FROM inserted i JOIN dbo.Chi_Tiet_Dat_San c ON c.id=i.chi_tiet_dat_san_id
        JOIN dbo.San s ON s.id=c.san_id JOIN dbo.Loai_San l ON l.id=s.loai_san_id
        WHERE i.mon_the_thao_id<>l.mon_the_thao_id
    ) THROW 51220, N''Môn của phòng không khớp môn của sân.'', 1;
    IF EXISTS (
        SELECT 1 FROM inserted i JOIN dbo.Chi_Tiet_Dat_San c ON c.id=i.chi_tiet_dat_san_id
        WHERE i.trang_thai IN (N''DANG_TUYEN'',N''DU_NGUOI'',N''DANG_DAU'')
          AND c.trang_thai_slot NOT IN (N''DA_XAC_NHAN'',N''DA_CHECK_IN'')
    ) THROW 51221, N''Chỉ mở phòng khi buổi đặt đã xác nhận.'', 1;
END;';
EXEC sys.sp_executesql N'CREATE TRIGGER dbo.TR_KetQua_DungDoi
ON dbo.Ket_Qua_Tran_Dau AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1 FROM inserted i WHERE
        (i.nguoi_xac_nhan_doi_1_id IS NOT NULL AND NOT EXISTS (
            SELECT 1 FROM dbo.Thanh_Vien_Phong m WHERE m.phong_ghep_id=i.phong_ghep_id
              AND m.nguoi_dung_id=i.nguoi_xac_nhan_doi_1_id AND m.so_doi=1 AND m.trang_thai_tham_gia=N''DA_THAM_GIA''))
        OR (i.nguoi_xac_nhan_doi_2_id IS NOT NULL AND NOT EXISTS (
            SELECT 1 FROM dbo.Thanh_Vien_Phong m WHERE m.phong_ghep_id=i.phong_ghep_id
              AND m.nguoi_dung_id=i.nguoi_xac_nhan_doi_2_id AND m.so_doi=2 AND m.trang_thai_tham_gia=N''DA_THAM_GIA''))
    ) THROW 51222, N''Người xác nhận không thuộc đội tương ứng.'', 1;
END;';
EXEC sys.sp_executesql N'CREATE TRIGGER dbo.TR_GiaoDichVi_BatBien
ON dbo.Giao_Dich_Vi AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted)
        THROW 51223, N''Nhật ký ví đã ghi sổ không được sửa/xóa; hãy ghi bút toán bù.'', 1;
END;';
EXEC sys.sp_executesql N'CREATE TRIGGER dbo.TR_LichCoDinh_KiemTra
ON dbo.Lich_Dat_Co_Dinh AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1 FROM inserted i JOIN dbo.San s ON s.id=i.san_id
        JOIN dbo.Khung_Gio_Mac_Dinh k ON k.id=i.khung_gio_id
        WHERE s.co_so_id<>k.co_so_id OR i.gio_bat_dau<>k.gio_bat_dau OR i.gio_ket_thuc<>k.gio_ket_thuc
    ) THROW 51224,N''Lịch cố định không khớp cơ sở/khung giờ của sân.'',1;
    IF EXISTS (
        SELECT 1 FROM inserted i JOIN deleted o ON o.id=i.id
        WHERE (i.nguoi_dung_id<>o.nguoi_dung_id OR i.san_id<>o.san_id
            OR i.khung_gio_id<>o.khung_gio_id OR i.thu_trong_tuan<>o.thu_trong_tuan
            OR i.ngay_bat_dau<>o.ngay_bat_dau OR i.ngay_ket_thuc<>o.ngay_ket_thuc)
          AND EXISTS (SELECT 1 FROM dbo.Chi_Tiet_Dat_San c WHERE c.lich_dat_co_dinh_id=i.id)
    ) THROW 51225,N''Lịch cố định đã sinh buổi: hãy tắt và tạo lịch mới để đổi cấu hình.'',1;
END;';
EXEC sys.sp_executesql N'CREATE TRIGGER dbo.TR_San_KhongDoiTaiNguyen
ON dbo.San AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1 FROM inserted i JOIN deleted o ON o.id=i.id
        WHERE (i.co_so_id<>o.co_so_id OR i.loai_san_id<>o.loai_san_id)
          AND (EXISTS (SELECT 1 FROM dbo.Chi_Tiet_Dat_San c WHERE c.san_id=i.id)
            OR EXISTS (SELECT 1 FROM dbo.Bang_Gia g WHERE g.san_id=i.id)
            OR EXISTS (SELECT 1 FROM dbo.Lich_Dat_Co_Dinh l WHERE l.san_id=i.id))
    ) THROW 51226,N''Sân đã có cấu hình/lịch: không đổi cơ sở hoặc loại sân.'',1;
END;';
EXEC sys.sp_executesql N'/* =============================================================
   VIEW: LỊCH VÀ SỐ TIỀN CÒN PHẢI THU
   ============================================================= */
CREATE VIEW dbo.v_LichSan
AS
SELECT c.id AS chi_tiet_dat_san_id, c.don_dat_san_id, d.ma_dat_san,
       s.co_so_id, c.san_id, s.ten_san, c.ngay_su_dung, c.gio_bat_dau, c.gio_ket_thuc,
       dbo.fn_BatDauBuoi(c.ngay_su_dung,c.gio_bat_dau) AS bat_dau_luc,
       dbo.fn_KetThucBuoi(c.ngay_su_dung,c.gio_ket_thuc) AS ket_thuc_luc,
       c.trang_thai_slot, c.gia_thue, d.thoi_gian_het_han_giu_cho,
       CONVERT(bit, CASE WHEN c.trang_thai_slot IN (N''DA_XAC_NHAN'',N''DA_CHECK_IN'',N''HOAN_THANH'',N''VANG_MAT'')
         OR (c.trang_thai_slot=N''GIU_CHO'' AND d.thoi_gian_het_han_giu_cho>DATEADD(HOUR,7,SYSUTCDATETIME()))
         THEN 1 ELSE 0 END) AS chiem_lich
FROM dbo.Chi_Tiet_Dat_San c JOIN dbo.Don_Dat_San d ON d.id=c.don_dat_san_id
JOIN dbo.San s ON s.id=c.san_id;';
EXEC sys.sp_executesql N'CREATE VIEW dbo.v_CongNoDon
AS
SELECT d.id AS don_dat_san_id, d.ma_dat_san, d.co_so_id, d.nguoi_dung_id,
       d.tong_tien_phai_tra, ISNULL(t.da_thu,CONVERT(decimal(38,2),0)) AS tong_da_thu,
       ISNULL(h.da_hoan,CONVERT(decimal(38,2),0)) AS tong_da_hoan,
       d.tong_tien_phai_tra - ISNULL(t.da_thu,0) + ISNULL(h.da_hoan,0) AS so_tien_con_phai_thu
FROM dbo.Don_Dat_San d
OUTER APPLY (SELECT SUM(p.so_tien) AS da_thu FROM dbo.Thanh_Toan_Don p
             WHERE p.don_dat_san_id=d.id AND p.trang_thai=N''THANH_CONG'') t
OUTER APPLY (SELECT SUM(r.so_tien) AS da_hoan FROM dbo.Hoan_Tien r
             JOIN dbo.Thanh_Toan_Don p ON p.id=r.thanh_toan_don_id
             WHERE p.don_dat_san_id=d.id AND p.trang_thai=N''THANH_CONG'' AND r.trang_thai=N''THANH_CONG'') h;';
EXEC sys.sp_executesql N'/* =============================================================
   PROCEDURE ĐẶT MỘT KHUNG - MỘT ĐƠN
   ============================================================= */
CREATE PROCEDURE dbo.usp_DatSanMotKhung
    @san_id bigint,
    @khung_gio_id int,
    @ngay_su_dung date,
    @ten_khach_hang nvarchar(100),
    @sdt_khach_hang nvarchar(20),
    @nguoi_dung_id bigint = NULL,
    @nhan_vien_tao_id bigint = NULL,
    @nguon_dat nvarchar(30) = N''APP'',
    @so_phut_giu_cho int = 10,
    @ghi_chu nvarchar(max) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    -- Procedure này sở hữu transaction; không gọi lồng trong transaction của service.
    IF @@TRANCOUNT <> 0 THROW 51300, N''Gọi procedure ngoài transaction có sẵn.'', 1;
    IF @so_phut_giu_cho IS NULL OR @so_phut_giu_cho NOT BETWEEN 1 AND 60
        THROW 51301, N''Thời gian giữ chỗ phải từ 1 đến 60 phút.'', 1;
    IF @nguon_dat IS NULL OR @nguon_dat NOT IN (N''APP'',N''TAI_QUAY'')
        THROW 51302, N''Nguồn đặt không hợp lệ.'', 1;
    IF @nguon_dat=N''APP'' AND @nguoi_dung_id IS NULL
        THROW 51303, N''Đặt qua app phải có tài khoản.'', 1;
    IF @nguon_dat=N''TAI_QUAY'' AND @nhan_vien_tao_id IS NULL
        THROW 51304, N''Đặt tại quầy phải có nhân viên tạo.'', 1;
    DECLARE @baygio datetime2(0)=DATEADD(HOUR,7,SYSUTCDATETIME()), @kq int,
            @co_so_id bigint, @gio_bat_dau time(0), @gio_ket_thuc time(0),
            @gia decimal(14,2), @coc decimal(14,2), @bang_gia_id bigint,
            @chinh_sach_id bigint, @don_id bigint, @ct_id bigint,
            @thu int=dbo.fn_ThuTrongTuan(@ngay_su_dung),
            @gio_mo time(0), @gio_dong time(0), @nghi bit;
    BEGIN TRY
        BEGIN TRANSACTION;
        EXEC @kq=sys.sp_getapplock @Resource=N''QLSB:LICH_SAN'', @LockMode=N''Exclusive'',
             @LockOwner=N''Transaction'', @LockTimeout=10000;
        IF @kq<0 THROW 51305, N''Đang có người thao tác lịch, hãy thử lại.'', 1;

        SELECT @co_so_id=s.co_so_id, @gio_bat_dau=k.gio_bat_dau, @gio_ket_thuc=k.gio_ket_thuc,
               @gio_mo=ISNULL(h.gio_mo_cua,c.gio_mo_cua), @gio_dong=ISNULL(h.gio_dong_cua,c.gio_dong_cua),
               @nghi=ISNULL(h.nghi_ca_ngay,0)
        FROM dbo.San s WITH (UPDLOCK,HOLDLOCK)
        JOIN dbo.Co_So c WITH (UPDLOCK,HOLDLOCK) ON c.id=s.co_so_id
        JOIN dbo.Khung_Gio_Mac_Dinh k WITH (UPDLOCK,HOLDLOCK) ON k.co_so_id=c.id AND k.id=@khung_gio_id
        LEFT JOIN dbo.Lich_Hoat_Dong_Co_So h WITH (UPDLOCK,HOLDLOCK) ON h.co_so_id=c.id AND h.thu_trong_tuan=@thu
        WHERE s.id=@san_id AND s.dang_hoat_dong=1 AND s.dang_bao_tri=0
          AND c.dang_hoat_dong=1 AND c.trang_thai_duyet=N''DA_DUYET'';
        IF @co_so_id IS NULL THROW 51306, N''Sân/khung không hợp lệ, cơ sở chưa duyệt hoặc sân ngừng hoạt động.'', 1;
        IF @ngay_su_dung IS NULL OR dbo.fn_BatDauBuoi(@ngay_su_dung,@gio_bat_dau)<=@baygio
            THROW 51307, N''Buổi đặt phải bắt đầu trong tương lai.'', 1;
        IF @nghi=1 OR @gio_bat_dau<@gio_mo
           OR dbo.fn_KetThucBuoi(@ngay_su_dung,@gio_ket_thuc)>dbo.fn_KetThucBuoi(@ngay_su_dung,@gio_dong)
            THROW 51308, N''Khung giờ nằm ngoài lịch mở cửa.'', 1;
        IF @nguoi_dung_id IS NOT NULL AND NOT EXISTS (
            SELECT 1 FROM dbo.Nguoi_Dung u WITH (UPDLOCK,HOLDLOCK)
            WHERE u.id=@nguoi_dung_id AND u.trang_thai=1 AND u.bi_khoa_dat_san=0
              AND (u.khoa_dat_san_den IS NULL OR u.khoa_dat_san_den<=@baygio)
        ) THROW 51309, N''Người dùng không tồn tại hoặc đang bị khóa đặt sân.'', 1;
        IF @nhan_vien_tao_id IS NOT NULL AND NOT EXISTS (
            SELECT 1 FROM dbo.Thanh_Vien_Co_So m WITH (UPDLOCK,HOLDLOCK)
            JOIN dbo.Nguoi_Dung u ON u.id=m.nguoi_dung_id
            WHERE m.co_so_id=@co_so_id AND m.nguoi_dung_id=@nhan_vien_tao_id
              AND m.trang_thai_hoat_dong=1 AND u.trang_thai=1
              AND (m.vai_tro_noi_bo IN (N''CHU_SAN'',N''QUAN_LY'') OR EXISTS (
                  SELECT 1 FROM dbo.Phan_Quyen_Thanh_Vien p JOIN dbo.Quyen_Han q ON q.id=p.quyen_han_id
                  WHERE p.thanh_vien_co_so_id=m.id AND q.ma_quyen=N''TAO_DON''))
        ) THROW 51310, N''Nhân viên chưa có quyền tạo đơn tại cơ sở.'', 1;

        SELECT @bang_gia_id=g.id,@gia=g.gia_thue,@coc=g.tien_coc_toi_thieu
        FROM dbo.Bang_Gia g WITH (UPDLOCK,HOLDLOCK)
        WHERE g.san_id=@san_id AND g.khung_gio_id=@khung_gio_id AND g.thu_trong_tuan=@thu
          AND g.dang_ap_dung=1 AND g.ngay_hieu_luc_tu<=@ngay_su_dung
          AND (g.ngay_hieu_luc_den IS NULL OR @ngay_su_dung<=g.ngay_hieu_luc_den);
        IF @bang_gia_id IS NULL THROW 51311, N''Chưa cấu hình giá cho sân/khung/thứ/ngày này.'', 1;
        SELECT @chinh_sach_id=p.id FROM dbo.Chinh_Sach_Huy_Doi p WITH (UPDLOCK,HOLDLOCK)
        WHERE p.co_so_id=@co_so_id AND p.ngay_hieu_luc_tu<=CONVERT(date,@baygio)
          AND (p.ngay_hieu_luc_den IS NULL OR CONVERT(date,@baygio)<=p.ngay_hieu_luc_den);
        IF @chinh_sach_id IS NULL THROW 51312, N''Chưa có chính sách đổi/hủy hiện hành.'', 1;
        -- Các mức phí phải liên tục từ 0 đến vô hạn trước khi bán.
        IF NOT EXISTS (SELECT 1 FROM dbo.Muc_Phi_Huy_Doi WITH (UPDLOCK,HOLDLOCK)
                       WHERE chinh_sach_huy_doi_id=@chinh_sach_id AND so_gio_tu_truoc_tran=0)
           OR NOT EXISTS (SELECT 1 FROM dbo.Muc_Phi_Huy_Doi WITH (UPDLOCK,HOLDLOCK)
                          WHERE chinh_sach_huy_doi_id=@chinh_sach_id AND so_gio_den_truoc_tran IS NULL)
           OR EXISTS (
               SELECT 1 FROM dbo.Muc_Phi_Huy_Doi a WITH (UPDLOCK,HOLDLOCK)
               WHERE a.chinh_sach_huy_doi_id=@chinh_sach_id AND a.so_gio_den_truoc_tran IS NOT NULL
                 AND NOT EXISTS (SELECT 1 FROM dbo.Muc_Phi_Huy_Doi b
                     WHERE b.chinh_sach_huy_doi_id=a.chinh_sach_huy_doi_id
                       AND b.so_gio_tu_truoc_tran=a.so_gio_den_truoc_tran)
           ) THROW 51313, N''Các mức phí chưa bao phủ liên tục từ 0 giờ đến vô hạn.'', 1;

        INSERT dbo.Don_Dat_San (nguoi_dung_id,co_so_id,nhan_vien_tao_id,nguon_dat,
            ten_khach_hang,sdt_khach_hang,tong_tien_san,tien_coc_yeu_cau,thoi_gian_het_han_giu_cho,ghi_chu)
        VALUES (@nguoi_dung_id,@co_so_id,@nhan_vien_tao_id,@nguon_dat,
            @ten_khach_hang,@sdt_khach_hang,@gia,@coc,DATEADD(MINUTE,@so_phut_giu_cho,@baygio),@ghi_chu);
        SET @don_id=CONVERT(bigint,SCOPE_IDENTITY());
        INSERT dbo.Chi_Tiet_Dat_San (don_dat_san_id,san_id,ngay_su_dung,gio_bat_dau,gio_ket_thuc,
            gia_thue,khung_gio_id,bang_gia_id,chinh_sach_huy_doi_id)
        VALUES (@don_id,@san_id,@ngay_su_dung,@gio_bat_dau,@gio_ket_thuc,@gia,@khung_gio_id,@bang_gia_id,@chinh_sach_id);
        SET @ct_id=CONVERT(bigint,SCOPE_IDENTITY());
        COMMIT TRANSACTION;
        SELECT d.id AS don_dat_san_id,@ct_id AS chi_tiet_dat_san_id,d.ma_dat_san,d.tong_tien_phai_tra,
               d.tien_coc_yeu_cau,d.thoi_gian_het_han_giu_cho,d.trang_thai
        FROM dbo.Don_Dat_San d WHERE d.id=@don_id;
    END TRY
    BEGIN CATCH
        IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;';
EXEC sys.sp_executesql N'CREATE PROCEDURE dbo.usp_GiaiPhongGiuChoHetHan
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @@TRANCOUNT<>0 THROW 51320,N''Gọi procedure ngoài transaction có sẵn.'',1;
    DECLARE @kq int,@baygio datetime2(0)=DATEADD(HOUR,7,SYSUTCDATETIME()),@so_buoi int;
    BEGIN TRY
        BEGIN TRANSACTION;
        EXEC @kq=sys.sp_getapplock @Resource=N''QLSB:LICH_SAN'',@LockMode=N''Exclusive'',@LockOwner=N''Transaction'',@LockTimeout=10000;
        IF @kq<0 THROW 51321,N''Không lấy được khóa lịch.'',1;
        UPDATE c SET trang_thai_slot=N''HET_HAN'',ngay_cap_nhat=@baygio
        FROM dbo.Chi_Tiet_Dat_San c JOIN dbo.Don_Dat_San d ON d.id=c.don_dat_san_id
        WHERE c.trang_thai_slot=N''GIU_CHO'' AND d.thoi_gian_het_han_giu_cho<=@baygio;
        SET @so_buoi=@@ROWCOUNT;
        -- Chỉ kết thúc đơn chưa thu thành công; đơn có tiền phải qua quy trình hoàn/đối chiếu.
        UPDATE d SET trang_thai=N''HET_HAN'',ngay_cap_nhat=@baygio,
            tong_tien_san=0,tong_tien_dich_vu=0,tong_phi_phat=0,tien_giam_gia=0,tien_coc_yeu_cau=0
        FROM dbo.Don_Dat_San d
        WHERE d.trang_thai=N''CHO_THANH_TOAN'' AND d.thoi_gian_het_han_giu_cho<=@baygio
          AND NOT EXISTS (SELECT 1 FROM dbo.Chi_Tiet_Dat_San c WHERE c.don_dat_san_id=d.id
              AND c.trang_thai_slot IN (N''GIU_CHO'',N''DA_XAC_NHAN'',N''DA_CHECK_IN'',N''HOAN_THANH'',N''VANG_MAT''))
          AND NOT EXISTS (SELECT 1 FROM dbo.Thanh_Toan_Don p WHERE p.don_dat_san_id=d.id AND p.trang_thai=N''THANH_CONG'');
        COMMIT TRANSACTION;
        SELECT @so_buoi AS so_buoi_da_giai_phong;
    END TRY
    BEGIN CATCH
        IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;';

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

/* =============================================================
   KIỂM TRA CÀI ĐẶT VÀ VÍ DỤ GỌI (CHỈ LÀ CHÚ THÍCH)
   ============================================================= */
IF (SELECT COUNT(*) FROM sys.tables WHERE schema_id=SCHEMA_ID(N'dbo') AND is_ms_shipped=0)<>48
    THROW 51400,N'Số bảng không đúng 48. Xem lỗi ở các batch phía trên.',1;
IF OBJECT_ID(N'dbo.usp_DatSanMotKhung',N'P') IS NULL
   OR OBJECT_ID(N'dbo.TR_ChiTietDatSan_KiemTra',N'TR') IS NULL
    THROW 51401,N'Chưa tạo đủ procedure/trigger. Xem lỗi ở các batch phía trên.',1;
IF (SELECT COUNT(*) FROM sys.foreign_keys WHERE schema_id=SCHEMA_ID(N'dbo'))<>102
    OR (SELECT COUNT(*) FROM sys.triggers WHERE parent_class=1 AND is_ms_shipped=0)<>13
    OR (SELECT COUNT(*) FROM sys.procedures WHERE schema_id=SCHEMA_ID(N'dbo') AND is_ms_shipped=0)<>3
    OR (SELECT COUNT(*) FROM sys.views WHERE schema_id=SCHEMA_ID(N'dbo') AND is_ms_shipped=0)<>2
    OR (SELECT COUNT(*) FROM sys.objects WHERE schema_id=SCHEMA_ID(N'dbo') AND type=N'FN')<>3
    THROW 51402,N'Chưa tạo đủ FK/trigger/procedure/view/function. Xem lỗi ở trên.',1;
SELECT N'Đã tạo schema Quản Lý Sân Bãi' AS thong_bao,
    (SELECT COUNT(*) FROM sys.tables WHERE schema_id=SCHEMA_ID(N'dbo') AND is_ms_shipped=0) AS so_bang,
    (SELECT COUNT(*) FROM sys.foreign_keys WHERE schema_id=SCHEMA_ID(N'dbo')) AS so_khoa_ngoai;
GO
/*
THỨ TỰ CẤU HÌNH TRƯỚC KHI ĐẶT:
1. Tạo Nguoi_Dung (mật khẩu đã hash từ ứng dụng), Co_So, Thanh_Vien_Co_So của chủ.
2. Đặt trạng thái cơ sở DA_DUYET, tạo Loai_San, San, Khung_Gio_Mac_Dinh.
3. Tạo Bang_Gia cho từng sân/khung/thứ, và chính sách + các mức phí.
4. Cấp vai trò/quyền cho nhân viên; ứng dụng xác thực người gọi trước khi gọi procedure.

VÍ DỤ (thay các ID/ngày bằng dữ liệu thật, bỏ chú thích để chạy):
EXEC dbo.usp_DatSanMotKhung
    @san_id=1, @khung_gio_id=1, @ngay_su_dung='20261101',
    @ten_khach_hang=N'Nguyễn Văn A', @sdt_khach_hang=N'0901234567',
    @nguoi_dung_id=1, @nguon_dat=N'APP', @so_phut_giu_cho=10;

SELECT * FROM dbo.v_LichSan WHERE san_id=1;
SELECT * FROM dbo.v_CongNoDon;
EXEC dbo.usp_GiaiPhongGiuChoHetHan;

Job nền gọi giải phóng giữ chỗ định kỳ (ví dụ 1 phút).
SQL Server Express có thể dùng BackgroundService của ASP.NET Core thay SQL Agent.
Thanh toán đến sau hạn giữ: kiểm tra lại lịch và xử lý hoàn, không tự xác nhận lại slot.
Nếu dùng EF Core: khai báo trigger/HasTrigger hoặc tắt SQL OUTPUT trực tiếp
cho bảng có trigger theo phiên bản EF Core; computed column cấu hình ValueGeneratedOnAddOrUpdate.
*/
