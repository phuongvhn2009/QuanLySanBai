-- Additive update for Cluster 7 (Payments, VietQR/SePay Webhook, Cash change).
-- Safe to run multiple times on an existing database.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH(N'dbo.Thanh_Toan_Don', N'so_tien_khach_dua') IS NULL
    ALTER TABLE dbo.Thanh_Toan_Don ADD so_tien_khach_dua decimal(18,2) NULL;

IF COL_LENGTH(N'dbo.Thanh_Toan_Don', N'tien_thua') IS NULL
    ALTER TABLE dbo.Thanh_Toan_Don ADD tien_thua decimal(18,2) NULL;

IF COL_LENGTH(N'dbo.Giao_Dich_Cong_Thanh_Toan', N'don_dat_san_id') IS NULL
    ALTER TABLE dbo.Giao_Dich_Cong_Thanh_Toan ADD don_dat_san_id bigint NULL;

IF COL_LENGTH(N'dbo.Giao_Dich_Cong_Thanh_Toan', N'thoi_gian_het_han_qr') IS NULL
    ALTER TABLE dbo.Giao_Dich_Cong_Thanh_Toan ADD thoi_gian_het_han_qr datetime2(0) NULL;

IF COL_LENGTH(N'dbo.Giao_Dich_Cong_Thanh_Toan', N'noi_dung_chuyen_khoan') IS NULL
    ALTER TABLE dbo.Giao_Dich_Cong_Thanh_Toan ADD noi_dung_chuyen_khoan nvarchar(200) NULL;

IF COL_LENGTH(N'dbo.Giao_Dich_Cong_Thanh_Toan', N'sepay_transaction_id') IS NULL
    ALTER TABLE dbo.Giao_Dich_Cong_Thanh_Toan ADD sepay_transaction_id bigint NULL;
GO

IF OBJECT_ID(N'dbo.FK_Giao_Dich_Cong_Thanh_Toan_don_dat_san_id', N'F') IS NULL
    ALTER TABLE dbo.Giao_Dich_Cong_Thanh_Toan WITH CHECK ADD CONSTRAINT FK_Giao_Dich_Cong_Thanh_Toan_don_dat_san_id
        FOREIGN KEY (don_dat_san_id) REFERENCES dbo.Don_Dat_San(id);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Giao_Dich_Cong_Thanh_Toan_don_dat_san_id' AND object_id = OBJECT_ID(N'dbo.Giao_Dich_Cong_Thanh_Toan'))
    CREATE INDEX IX_Giao_Dich_Cong_Thanh_Toan_don_dat_san_id ON dbo.Giao_Dich_Cong_Thanh_Toan (don_dat_san_id) WHERE don_dat_san_id IS NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Giao_Dich_Cong_Thanh_Toan_sepay_transaction_id' AND object_id = OBJECT_ID(N'dbo.Giao_Dich_Cong_Thanh_Toan'))
    CREATE UNIQUE INDEX UX_Giao_Dich_Cong_Thanh_Toan_sepay_transaction_id ON dbo.Giao_Dich_Cong_Thanh_Toan (sepay_transaction_id) WHERE sepay_transaction_id IS NOT NULL;
GO

IF OBJECT_ID(N'dbo.Nhat_Ky_Webhook', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Nhat_Ky_Webhook (
        id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Nhat_Ky_Webhook PRIMARY KEY,
        cong_thanh_toan nvarchar(30) NOT NULL,
        sepay_transaction_id bigint NULL,
        ma_thanh_toan nvarchar(100) NULL,
        giao_dich_cong_id bigint NULL,
        loai_chuyen_tien nvarchar(20) NULL,
        so_tai_khoan_nhan nvarchar(50) NULL,
        so_tien decimal(18,2) NULL,
        noi_dung nvarchar(max) NULL,
        trang_thai_xu_ly nvarchar(50) NOT NULL,
        ghi_chu nvarchar(500) NULL,
        du_lieu_goc nvarchar(max) NOT NULL,
        ngay_tao datetime2 NOT NULL CONSTRAINT DF_Nhat_Ky_Webhook_ngay_tao DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
        CONSTRAINT FK_Nhat_Ky_Webhook_giao_dich_cong_id FOREIGN KEY (giao_dich_cong_id) REFERENCES dbo.Giao_Dich_Cong_Thanh_Toan(id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Nhat_Ky_Webhook_sepay_transaction_id' AND object_id = OBJECT_ID(N'dbo.Nhat_Ky_Webhook'))
    CREATE INDEX IX_Nhat_Ky_Webhook_sepay_transaction_id ON dbo.Nhat_Ky_Webhook (sepay_transaction_id) WHERE sepay_transaction_id IS NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Nhat_Ky_Webhook_ngay_tao' AND object_id = OBJECT_ID(N'dbo.Nhat_Ky_Webhook'))
    CREATE INDEX IX_Nhat_Ky_Webhook_ngay_tao ON dbo.Nhat_Ky_Webhook (ngay_tao DESC);
GO
