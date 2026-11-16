-- Additive update for an existing database. Run before starting the new application.
IF COL_LENGTH(N'dbo.Don_Dat_San', N'row_version') IS NULL
    ALTER TABLE dbo.Don_Dat_San ADD row_version rowversion NOT NULL;
