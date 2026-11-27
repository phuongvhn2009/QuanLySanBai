-- ============================================================================
-- Migration 003: Quản lý hình ảnh thực tế của sân (Hinh_Anh_San)
-- An toàn, idempotent — không sửa hay xóa bảng/dữ liệu hiện có.
-- ============================================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.Hinh_Anh_San', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Hinh_Anh_San] (
        [id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Hinh_Anh_San] PRIMARY KEY,
        [san_id] bigint NOT NULL,
        [duong_dan] nvarchar(500) NOT NULL,
        [la_anh_dai_dien] bit NOT NULL CONSTRAINT [DF_Hinh_Anh_San_la_anh_dai_dien] DEFAULT (0),
        [thu_tu_hien_thi] int NOT NULL CONSTRAINT [DF_Hinh_Anh_San_thu_tu_hien_thi] DEFAULT (0),
        [ngay_tao] datetime2(0) NOT NULL CONSTRAINT [DF_Hinh_Anh_San_ngay_tao] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
        [ngay_cap_nhat] datetime2(0) NOT NULL CONSTRAINT [DF_Hinh_Anh_San_ngay_cap_nhat] DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
        CONSTRAINT [FK_Hinh_Anh_San_san_id] FOREIGN KEY ([san_id]) REFERENCES [dbo].[San]([id]) ON DELETE CASCADE,
        CONSTRAINT [CK_Hinh_Anh_San_thu_tu] CHECK ([thu_tu_hien_thi] >= 0)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Hinh_Anh_San_san_id_thu_tu' AND object_id = OBJECT_ID(N'dbo.Hinh_Anh_San'))
BEGIN
    CREATE INDEX [IX_Hinh_Anh_San_san_id_thu_tu]
        ON [dbo].[Hinh_Anh_San] ([san_id], [la_anh_dai_dien] DESC, [thu_tu_hien_thi] ASC, [id] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Hinh_Anh_San_san_id_dai_dien' AND object_id = OBJECT_ID(N'dbo.Hinh_Anh_San'))
BEGIN
    CREATE UNIQUE INDEX [UX_Hinh_Anh_San_san_id_dai_dien]
        ON [dbo].[Hinh_Anh_San] ([san_id])
        WHERE ([la_anh_dai_dien] = 1);
END
GO
