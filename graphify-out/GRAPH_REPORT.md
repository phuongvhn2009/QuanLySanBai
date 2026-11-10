# Graph Report - QuanLysanBai  (2026-11-10)

## Corpus Check
- cluster-only mode — file stats not available

## Summary
- 1510 nodes · 2687 edges · 134 communities (94 shown, 40 thin omitted)
- Extraction: 94% EXTRACTED · 6% INFERRED · 0% AMBIGUOUS · INFERRED: 151 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `9e94b0ee`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Community 0
- Community 1
- Community 2
- Community 3
- Community 4
- Community 5
- Community 6
- Community 7
- Community 8
- Community 9
- Community 10
- Community 11
- Community 12
- Community 13
- Community 14
- Community 15
- Community 16
- Community 17
- Community 18
- Community 19
- Community 20
- Community 21
- Community 22
- Community 23
- Community 24
- Community 25
- Community 26
- Community 27
- Community 28
- Community 29
- Community 30
- Community 31
- Community 32
- Community 33
- Community 34
- Community 35
- Community 36
- Community 37
- Community 38
- Community 39
- Community 40
- Community 41
- Community 42
- Community 43
- Community 44
- Community 45
- Community 46
- Community 47
- Community 48
- Community 49
- Community 50
- Community 51
- Community 52
- Community 53
- Community 54
- Community 55
- Community 56
- Community 57
- Community 58
- Community 59
- Community 60
- Community 61
- Community 62
- Community 63
- Community 64
- Community 65
- Community 66
- Community 67
- Community 68
- Community 69
- Community 70
- Community 71
- Community 72
- Community 73
- Community 74
- Community 75
- Community 76
- Community 77
- Community 78
- Community 79
- Community 80
- Community 81
- Community 82
- Community 83
- Community 84
- Community 85
- Community 86
- Community 87
- Community 88
- Community 89
- Community 90
- Community 91
- Community 92
- Community 93
- Community 94
- Community 95
- Community 96
- Community 97
- Community 98
- Community 99
- Community 101
- Community 102
- Community 103
- Community 104
- Community 105
- Community 106
- Community 107
- Community 108
- Community 109
- Community 110
- Community 111
- Community 112
- Community 113
- Community 114
- Community 115

## God Nodes (most connected - your core abstractions)
1. `QuanLySanBaiContext` - 126 edges
2. `NguoiDung` - 80 edges
3. `QuanLySanBai.Models.Entities` - 67 edges
4. `DonDatSan` - 52 edges
5. `CoSo` - 47 edges
6. `ChiTietDatSan` - 45 edges
7. `San` - 37 edges
8. `YeuCauDoiLich` - 35 edges
9. `HoanTien` - 34 edges
10. `ThanhToanDon` - 32 edges

## Surprising Connections (you probably didn't know these)
- `HomeController` --references--> `QuanLySanBaiContext`  [EXTRACTED]
  QuanLySanBai/Areas/CoSo/Controllers/HomeController.cs → QuanLySanBai/Data/QuanLySanBaiContext.Custom.cs
- `MembersController` --references--> `QuanLySanBaiContext`  [EXTRACTED]
  QuanLySanBai/Areas/CoSo/Controllers/MembersController.cs → QuanLySanBai/Data/QuanLySanBaiContext.Custom.cs
- `BookingsController` --references--> `QuanLySanBaiContext`  [EXTRACTED]
  QuanLySanBai/Controllers/BookingsController.cs → QuanLySanBai/Data/QuanLySanBaiContext.Custom.cs
- `BookingsController` --references--> `BookingService`  [EXTRACTED]
  QuanLySanBai/Controllers/BookingsController.cs → QuanLySanBai/Services/BookingService.cs
- `AccessService` --references--> `QuanLySanBaiContext`  [EXTRACTED]
  QuanLySanBai/Services/AccessService.cs → QuanLySanBai/Data/QuanLySanBaiContext.Custom.cs

## Import Cycles
- None detected.

## Communities (134 total, 40 thin omitted)

### Community 0 - "Community 0"
Cohesion: 0.07
Nodes (20): QuanLySanBai.Data, QuanLySanBai.ViewModels, QuanLySanBai.Areas.Admin.Controllers, QuanLySanBai.Controllers, QuanLySanBai.Areas.CoSo.Controllers, QuanLySanBai.Services, List<QuanLySanBai.Models.Entities.NguoiDung>, List<QuanLySanBai.Models.Entities.ThanhVienCoSo> (+12 more)

### Community 2 - "Community 2"
Cohesion: 0.04
Nodes (51): QuanLySanBaiContext, BangGia, BaoCaoHieuSuatSans, CauHinhHoaHongs, ChinhSachHuyDois, ChiTietDatSans, ChiTietDichVuDons, ChiTietDoiSoatDons (+43 more)

### Community 3 - "Community 3"
Cohesion: 0.07
Nodes (47): dbo.Bao_Cao_Hieu_Suat_San, dbo.Cau_Hinh_Hoa_Hong, dbo.Danh_Gia_Co_So, dbo.Dich_Vu, dbo.Dieu_Chinh_Doi_Soat, dbo.Hoan_Tien, dbo.Lich_Su_Elo, dbo.Lich_Su_Huy_San (+39 more)

### Community 4 - "Community 4"
Cohesion: 0.04
Nodes (46): NguoiDung, AnhDaiDien, BiKhoaDatSan, BiKhoaGhepTran, CoSos, DiemUyTin, DieuChinhDoiSoats, DonDatSanNguoiDungs (+38 more)

### Community 5 - "Community 5"
Cohesion: 0.06
Nodes (11): BookingService, PublicCourts, HoldExpiryWorker, BusinessErrorFilter, BookingOptions, ExpiryIntervalSeconds, HoldMinutes, LocalUrls (+3 more)

### Community 6 - "Community 6"
Cohesion: 0.05
Nodes (42): LichSuDungVoucher, DonDatSan, DonDatSanId, GiuChoDen, Id, NgayApDung, NgayGiaiToa, NguoiDung (+34 more)

### Community 7 - "Community 7"
Cohesion: 0.06
Nodes (32): ChiTietDatSans, ChiTietDichVuDons, ChiTietDoiSoatDon, CoSo, CoSoId, DanhGiaCoSo, GhiChu, Id (+24 more)

### Community 8 - "Community 8"
Cohesion: 0.11
Nodes (13): AccountController, LoginModel, Login, Password, RememberMe, ReturnUrl, ProfileModel, Email (+5 more)

### Community 9 - "Community 9"
Cohesion: 0.07
Nodes (29): ChiTietDatSan, BangGia, BangGiaId, ChinhSachHuyDoi, ChinhSachHuyDoiId, ChiTietThayTheCho, ChiTietThayTheChoId, DonDatSan (+21 more)

### Community 10 - "Community 10"
Cohesion: 0.12
Nodes (6): Actor, HomeController, MembersController, Actor, BookingsController, AccessService

### Community 11 - "Community 11"
Cohesion: 0.07
Nodes (27): dbo.Bang_Gia, dbo.Chinh_Sach_Huy_Doi, dbo.Co_So, dbo.Khung_Gio_Mac_Dinh, dbo.Lich_Hoat_Dong_Co_So, dbo.Muc_Phi_Huy_Doi, dbo.Quyen_Han, dbo.Thanh_Toan_Don (+19 more)

### Community 12 - "Community 12"
Cohesion: 0.07
Nodes (27): CoSo, AnhDaiDien, CauHinhHoaHongs, ChinhSachHuyDois, ChuSoHuu, ChuSoHuuId, DangHoatDong, DiaChi (+19 more)

### Community 14 - "Community 14"
Cohesion: 0.08
Nodes (25): DongGopPhong, GhiChu, Id, LoaiGiaoDich, MaThamChieu, NgayGiaoDich, NguoiGhiNhan, NguoiGhiNhanId (+17 more)

### Community 15 - "Community 15"
Cohesion: 0.08
Nodes (24): CollectForm, Amount, Id, Key, Method, Purpose, Reference, Version (+16 more)

### Community 16 - "Community 16"
Cohesion: 0.08
Nodes (24): HoanTien, DieuChinhDoiSoat, GiaoDichCong, GiaoDichCongId, GiaoDichVi, GiaoDichViId, Id, KhoaChongLap (+16 more)

### Community 17 - "Community 17"
Cohesion: 0.08
Nodes (24): YeuCauDoiLich, ChenhLechGia, ChiTietDatSanCu, ChiTietDatSanCuId, ChiTietDatSanMoi, ChiTietDatSanMoiId, GioBatDauMoi, GioKetThucMoi (+16 more)

### Community 18 - "Community 18"
Cohesion: 0.09
Nodes (22): ThanhToanDon, DonDatSan, DonDatSanId, GiaoDichCong, GiaoDichCongId, GiaoDichVi, GiaoDichViId, HoanTiens (+14 more)

### Community 20 - "Community 20"
Cohesion: 0.10
Nodes (21): KyDoiSoat, ChiTietDoiSoatDons, CoSo, CoSoId, DieuChinhDoiSoats, Id, MaChuyenKhoan, MaKyDoiSoat (+13 more)

### Community 21 - "Community 21"
Cohesion: 0.38
Nodes (3): BusinessException, OperationService, Now

### Community 22 - "Community 22"
Cohesion: 0.10
Nodes (20): GiaoDichVi, HoanTien, Id, KhoaChongLap, LoaiGiaoDich, MaGiaoDich, NapTienVi, NgayTao (+12 more)

### Community 23 - "Community 23"
Cohesion: 0.10
Nodes (20): PhongGhepTran, ChiTietDatSan, ChiTietDatSanId, DiemEloToiDa, DiemEloToiThieu, GhiChu, Id, KetQuaTranDau (+12 more)

### Community 24 - "Community 24"
Cohesion: 0.10
Nodes (20): San, BangGia, BaoCaoHieuSuatSans, ChiTietDatSans, CoSo, CoSoId, DangBaoTri, DangHoatDong (+12 more)

### Community 25 - "Community 25"
Cohesion: 0.10
Nodes (20): YeuCauRutTien, GiaoDichCong, GiaoDichCongId, GiaoDichGiuTien, GiaoDichGiuTienId, GiaoDichKetThuc, GiaoDichKetThucId, Id (+12 more)

### Community 26 - "Community 26"
Cohesion: 0.11
Nodes (19): GiaoDichCongThanhToan, CongThanhToan, DuLieuWebhook, HoanTien, Id, LoaiThanhToan, MaDonHangCong, MaGiaoDichNganHang (+11 more)

### Community 27 - "Community 27"
Cohesion: 0.11
Nodes (4): CatalogController, HomeController, CourtsController, HomeController

### Community 28 - "Community 28"
Cohesion: 0.11
Nodes (18): ChiTietDoiSoatDon, CauHinhHoaHong, CauHinhHoaHongId, DieuChinhDoiSoats, DonDatSan, DonDatSanId, Id, KyDoiSoat (+10 more)

### Community 29 - "Community 29"
Cohesion: 0.12
Nodes (17): LichDatCoDinh, ChiTietDatSans, GioBatDau, GioKetThuc, Id, KhungGio, KhungGioId, NgayBatDau (+9 more)

### Community 30 - "Community 30"
Cohesion: 0.24
Nodes (5): VietnamTime, Now, Today, AvailableSlot, CourtCalendar

### Community 31 - "Community 31"
Cohesion: 0.12
Nodes (16): KetQuaTranDau, Id, LichSuElos, NgayCapNhat, NgayGhiNhan, NgayXacNhanDoi1, NgayXacNhanDoi2, NguoiXacNhanDoi1 (+8 more)

### Community 32 - "Community 32"
Cohesion: 0.12
Nodes (16): VLichSan, BatDauLuc, ChiemLich, ChiTietDatSanId, CoSoId, DonDatSanId, GiaThue, GioBatDau (+8 more)

### Community 33 - "Community 33"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 34 - "Community 34"
Cohesion: 0.15
Nodes (12): PhanQuyenThanhVien, Id, QuyenHan, QuyenHanId, ThanhVienCoSo, ThanhVienCoSoId, QuyenHan, Id (+4 more)

### Community 35 - "Community 35"
Cohesion: 0.13
Nodes (15): LichSuHuySan, ChiTietDatSan, ChiTietDatSanId, HoanTiens, Id, LyDoHuy, NgayHuy, NguoiXuLy (+7 more)

### Community 36 - "Community 36"
Cohesion: 0.23
Nodes (6): OrdersController, OrderActionForm, Code, Id, Reason, Version

### Community 37 - "Community 37"
Cohesion: 0.14
Nodes (14): HoSoTrinhDo, DiemElo, HangTrinhDo, Id, LichSuElos, MonTheThao, MonTheThaoId, NgayCapNhat (+6 more)

### Community 38 - "Community 38"
Cohesion: 0.15
Nodes (13): BangGium, ChiTietDatSans, DangApDung, GiaThue, Id, KhungGio, KhungGioId, NgayHieuLucDen (+5 more)

### Community 39 - "Community 39"
Cohesion: 0.15
Nodes (13): DieuChinhDoiSoat, ChiTietDoiSoatGoc, ChiTietDoiSoatGocId, HoanTien, HoanTienId, Id, KyDoiSoat, KyDoiSoatId (+5 more)

### Community 40 - "Community 40"
Cohesion: 0.17
Nodes (12): ChinhSachHuyDoi, ChiTietDatSans, ChoPhepDoiLich, CoSo, CoSoId, GhiChu, Id, MucPhiHuyDois (+4 more)

### Community 41 - "Community 41"
Cohesion: 0.17
Nodes (12): NapTienVi, GiaoDichCong, GiaoDichCongId, GiaoDichVi, GiaoDichViId, Id, NgayHoanTat, NgayTao (+4 more)

### Community 42 - "Community 42"
Cohesion: 0.17
Nodes (12): ThongBao, DaDoc, Id, LoaiThamChieu, LoaiThongBao, MaThamChieuId, NgayDoc, NgayTao (+4 more)

### Community 43 - "Community 43"
Cohesion: 0.17
Nodes (12): ViNguoiDung, GiaoDichVis, Id, NapTienVis, NgayCapNhat, NgayTao, NguoiDung, NguoiDungId (+4 more)

### Community 44 - "Community 44"
Cohesion: 0.18
Nodes (11): dbo.Don_Dat_San, dbo.fn_ThuTrongTuan(), dbo.Lich_Khoa_San, IX_Don_Dat_San_co_so_id, IX_Don_Dat_San_nguoi_dung_id, IX_Don_Dat_San_nhan_vien_tao_id, IX_Don_Dat_San_trang_thai_thoi_gian_het_han_giu_cho, IX_Lich_Khoa_San_nguoi_tao_id (+3 more)

### Community 45 - "Community 45"
Cohesion: 0.18
Nodes (8): NguoiDungVaiTro, Id, NgayGan, NguoiDung, NguoiDungId, VaiTro, VaiTroId, DevelopmentSeeder

### Community 46 - "Community 46"
Cohesion: 0.24
Nodes (8): Microsoft.EntityFrameworkCore.Design (10.0.13), Microsoft.EntityFrameworkCore.SqlServer (10.0.13), Microsoft.EntityFrameworkCore.Tools (10.0.13), System.Security.Cryptography.Pkcs (10.0.13), QuanLySanBai, net10.0, Microsoft.NET.Sdk.Web, Microsoft.NET.Sdk

### Community 47 - "Community 47"
Cohesion: 0.18
Nodes (11): BaoCaoHieuSuatSan, DoanhThuNgay, Id, NgayBaoCao, San, SanId, SoSlotDaDat, SoSlotHuy (+3 more)

### Community 48 - "Community 48"
Cohesion: 0.18
Nodes (11): DichVu, ChiTietDichVuDons, ConHang, CoSo, CoSoId, DangKinhDoanh, DonGia, DonViTinh (+3 more)

### Community 49 - "Community 49"
Cohesion: 0.18
Nodes (11): KhungGioMacDinh, BangGia, ChiTietDatSans, CoSo, CoSoId, GioBatDau, GioKetThuc, Id (+3 more)

### Community 50 - "Community 50"
Cohesion: 0.18
Nodes (11): LichKhoaSan, BatDauLuc, Id, KetThucLuc, LyDo, NgayTao, NguoiTao, NguoiTaoId (+3 more)

### Community 51 - "Community 51"
Cohesion: 0.18
Nodes (11): NhatKyViPham, DiemUyTinBiTru, DonDatSan, DonDatSanId, Id, KhoaTinhNangDen, LoaiViPham, NgayGhiNhan (+3 more)

### Community 52 - "Community 52"
Cohesion: 0.18
Nodes (10): CourtTypeForm, Capacity, Id, Name, SportId, SportForm, Active, Code (+2 more)

### Community 53 - "Community 53"
Cohesion: 0.20
Nodes (8): PolicyForm, AllowChange, FacilityId, Fees, From, Name, Until, OperationList

### Community 54 - "Community 54"
Cohesion: 0.20
Nodes (10): CauHinhHoaHong, ChiTietDoiSoatDons, CoSo, CoSoId, Id, NgayApDung, NgayHetHieuLuc, PhiCoDinhMoiDon (+2 more)

### Community 55 - "Community 55"
Cohesion: 0.20
Nodes (10): DanhGiaCoSo, DonDatSan, DonDatSanId, Id, NgayCapNhat, NgayPhanHoi, NgayTao, NhanXet (+2 more)

### Community 56 - "Community 56"
Cohesion: 0.20
Nodes (10): LichSuElo, DiemSau, DiemThayDoi, DiemTruoc, HoSoTrinhDo, HoSoTrinhDoId, Id, KetQuaTranDau (+2 more)

### Community 57 - "Community 57"
Cohesion: 0.20
Nodes (10): ThanhVienCoSo, CoSo, CoSoId, Id, NgayThamGia, NguoiDung, NguoiDungId, PhanQuyenThanhViens (+2 more)

### Community 58 - "Community 58"
Cohesion: 0.20
Nodes (9): PriceForm, CourtId, Deposit, FacilityId, From, Price, SlotId, Until (+1 more)

### Community 59 - "Community 59"
Cohesion: 0.22
Nodes (9): dbo.Chi_Tiet_Dat_San, dbo.TR_ChiTietDatSan_KiemTra, IX_Chi_Tiet_Dat_San_bang_gia_id, IX_Chi_Tiet_Dat_San_chinh_sach_huy_doi_id, IX_Chi_Tiet_Dat_San_don_dat_san_id, IX_Chi_Tiet_Dat_San_khung_gio_id, IX_Chi_Tiet_Dat_San_san_id_ngay_su_dung_gio_bat_dau, UX_Chi_Tiet_Dat_San_chi_tiet_thay_the_cho_id (+1 more)

### Community 60 - "Community 60"
Cohesion: 0.22
Nodes (9): ChiTietDichVuDon, DichVu, DichVuId, DonDatSan, DonDatSanId, DonGiaLucBan, Id, SoLuong (+1 more)

### Community 61 - "Community 61"
Cohesion: 0.22
Nodes (9): LichSuCheckIn, ChiTietDatSan, ChiTietDatSanId, GhiChu, Id, NhanVienCheckin, NhanVienCheckinId, PhuongThuc (+1 more)

### Community 62 - "Community 62"
Cohesion: 0.22
Nodes (9): LoaiSan, Id, KichThuoc, MonTheThao, MonTheThaoId, MoTa, Sans, SucChuaNguoi (+1 more)

### Community 63 - "Community 63"
Cohesion: 0.22
Nodes (9): MonTheThao, BieuTuongUrl, HoSoTrinhDos, Id, LoaiSans, MaMon, PhongGhepTrans, TenMon (+1 more)

### Community 64 - "Community 64"
Cohesion: 0.22
Nodes (9): VCongNoDon, CoSoId, DonDatSanId, MaDatSan, NguoiDungId, SoTienConPhaiThu, TongDaHoan, TongDaThu (+1 more)

### Community 65 - "Community 65"
Cohesion: 0.25
Nodes (8): LichHoatDongCoSo, CoSo, CoSoId, GioDongCua, GioMoCua, Id, NghiCaNgay, ThuTrongTuan

### Community 66 - "Community 66"
Cohesion: 0.25
Nodes (8): MucPhiHuyDoi, ChinhSachHuyDoi, ChinhSachHuyDoiId, Id, SoGioDenTruocTran, SoGioTuTruocTran, TiLePhiDoi, TiLePhiHuy

### Community 67 - "Community 67"
Cohesion: 0.25
Nodes (8): FacilityDashboard, Courts, Facility, Hours, Locks, Policies, Prices, Slots

### Community 68 - "Community 68"
Cohesion: 0.25
Nodes (8): FacilityForm, Active, Address, Close, Id, Name, Open, Phone

### Community 69 - "Community 69"
Cohesion: 0.29
Nodes (7): dbo.Yeu_Cau_Doi_Lich, IX_Yeu_Cau_Doi_Lich_khung_gio_moi_id, IX_Yeu_Cau_Doi_Lich_nguoi_xu_ly_id, IX_Yeu_Cau_Doi_Lich_nguoi_yeu_cau_id, IX_Yeu_Cau_Doi_Lich_san_moi_id, UX_Yeu_Cau_Doi_Lich_chi_tiet_dat_san_cu_id, UX_Yeu_Cau_Doi_Lich_chi_tiet_dat_san_moi_id

### Community 70 - "Community 70"
Cohesion: 0.29
Nodes (6): VaiTro, Id, MaVaiTro, MoTa, NguoiDungVaiTros, TenVaiTro

### Community 71 - "Community 71"
Cohesion: 0.29
Nodes (7): CourtForm, Active, FacilityId, Id, Maintenance, Name, TypeId

### Community 72 - "Community 72"
Cohesion: 0.33
Nodes (6): dbo.Yeu_Cau_Rut_Tien, IX_Yeu_Cau_Rut_Tien_nhan_vien_duyet_id, IX_Yeu_Cau_Rut_Tien_vi_id, UX_Yeu_Cau_Rut_Tien_giao_dich_cong_id, UX_Yeu_Cau_Rut_Tien_giao_dich_giu_tien_id, UX_Yeu_Cau_Rut_Tien_giao_dich_ket_thuc_id

### Community 74 - "Community 74"
Cohesion: 0.33
Nodes (4): SlotForm, End, FacilityId, Start

### Community 75 - "Community 75"
Cohesion: 0.33
Nodes (6): HoursForm, Close, Closed, FacilityId, Open, Weekday

### Community 76 - "Community 76"
Cohesion: 0.33
Nodes (6): LockForm, CourtId, End, FacilityId, Reason, Start

### Community 77 - "Community 77"
Cohesion: 0.40
Nodes (5): BookingForm, CourtId, Date, Note, SlotId

### Community 78 - "Community 78"
Cohesion: 0.40
Nodes (5): FeeForm, CancelPercent, ChangePercent, From, Until

### Community 79 - "Community 79"
Cohesion: 0.50
Nodes (4): dbo.Chi_Tiet_Doi_Soat_Don, IX_Chi_Tiet_Doi_Soat_Don_cau_hinh_hoa_hong_id, IX_Chi_Tiet_Doi_Soat_Don_ky_doi_soat_id, UX_Chi_Tiet_Doi_Soat_Don_don_dat_san_id

### Community 80 - "Community 80"
Cohesion: 0.50
Nodes (4): dbo.Giao_Dich_Cong_Thanh_Toan, IX_Giao_Dich_Cong_Thanh_Toan_nguoi_dung_id, UX_Giao_Dich_Cong_Thanh_Toan_cong_thanh_toan_ma_giao_dich_ngan_hang, UX_Giao_Dich_Cong_Thanh_Toan_ma_don_hang_cong

### Community 81 - "Community 81"
Cohesion: 0.50
Nodes (4): dbo.Giao_Dich_Vi, IX_Giao_Dich_Vi_vi_id, UX_Giao_Dich_Vi_khoa_chong_lap, UX_Giao_Dich_Vi_ma_giao_dich

### Community 82 - "Community 82"
Cohesion: 0.50
Nodes (4): dbo.Ket_Qua_Tran_Dau, IX_Ket_Qua_Tran_Dau_nguoi_xac_nhan_doi_1_id, IX_Ket_Qua_Tran_Dau_nguoi_xac_nhan_doi_2_id, UX_Ket_Qua_Tran_Dau_phong_ghep_id

### Community 83 - "Community 83"
Cohesion: 0.50
Nodes (4): dbo.Ky_Doi_Soat, IX_Ky_Doi_Soat_co_so_id, IX_Ky_Doi_Soat_nhan_vien_doi_soat_id, UX_Ky_Doi_Soat_ma_ky_doi_soat

### Community 84 - "Community 84"
Cohesion: 0.50
Nodes (4): dbo.Lich_Dat_Co_Dinh, IX_Lich_Dat_Co_Dinh_khung_gio_id, IX_Lich_Dat_Co_Dinh_nguoi_dung_id, IX_Lich_Dat_Co_Dinh_san_id

### Community 85 - "Community 85"
Cohesion: 0.50
Nodes (4): dbo.Lich_Su_Dung_Voucher, IX_Lich_Su_Dung_Voucher_nguoi_dung_id, IX_Lich_Su_Dung_Voucher_voucher_id, UX_Lich_Su_Dung_Voucher_don_dat_san_id

### Community 86 - "Community 86"
Cohesion: 0.50
Nodes (4): dbo.Nap_Tien_Vi, IX_Nap_Tien_Vi_vi_id, UX_Nap_Tien_Vi_giao_dich_cong_id, UX_Nap_Tien_Vi_giao_dich_vi_id

### Community 88 - "Community 88"
Cohesion: 0.67
Nodes (3): dbo.Chi_Tiet_Dich_Vu_Don, IX_Chi_Tiet_Dich_Vu_Don_dich_vu_id, IX_Chi_Tiet_Dich_Vu_Don_don_dat_san_id

### Community 89 - "Community 89"
Cohesion: 0.67
Nodes (3): dbo.Dong_Gop_Phong, IX_Dong_Gop_Phong_nguoi_ghi_nhan_id, IX_Dong_Gop_Phong_thanh_vien_phong_id

### Community 90 - "Community 90"
Cohesion: 0.67
Nodes (3): dbo.Ho_So_Trinh_Do, IX_Ho_So_Trinh_Do_mon_the_thao_id, UX_Ho_So_Trinh_Do_nguoi_dung_id_mon_the_thao_id

### Community 91 - "Community 91"
Cohesion: 0.67
Nodes (3): dbo.Lich_Su_Check_In, IX_Lich_Su_Check_In_chi_tiet_dat_san_id, IX_Lich_Su_Check_In_nhan_vien_checkin_id

### Community 92 - "Community 92"
Cohesion: 0.67
Nodes (3): dbo.Nguoi_Dung, UX_Nguoi_Dung_email, UX_Nguoi_Dung_so_dien_thoai

### Community 93 - "Community 93"
Cohesion: 0.67
Nodes (3): dbo.Nguoi_Dung_Vai_Tro, IX_Nguoi_Dung_Vai_Tro_vai_tro_id, UX_Nguoi_Dung_Vai_Tro_nguoi_dung_id_vai_tro_id

### Community 94 - "Community 94"
Cohesion: 0.67
Nodes (3): dbo.Nhat_Ky_Vi_Pham, IX_Nhat_Ky_Vi_Pham_don_dat_san_id, IX_Nhat_Ky_Vi_Pham_nguoi_dung_id

### Community 95 - "Community 95"
Cohesion: 0.67
Nodes (3): dbo.Phan_Quyen_Thanh_Vien, IX_Phan_Quyen_Thanh_Vien_quyen_han_id, UX_Phan_Quyen_Thanh_Vien_thanh_vien_co_so_id_quyen_han_id

### Community 96 - "Community 96"
Cohesion: 0.67
Nodes (3): dbo.San, IX_San_loai_san_id, UX_San_co_so_id_ten_san

### Community 97 - "Community 97"
Cohesion: 0.67
Nodes (3): dbo.Thanh_Vien_Phong, IX_Thanh_Vien_Phong_nguoi_dung_id, UX_Thanh_Vien_Phong_phong_ghep_id_nguoi_dung_id

### Community 98 - "Community 98"
Cohesion: 0.67
Nodes (3): dbo.Voucher_Nguoi_Dung, IX_Voucher_Nguoi_Dung_voucher_id, UX_Voucher_Nguoi_Dung_nguoi_dung_id_voucher_id

## Knowledge Gaps
- **874 isolated node(s):** `List<QuanLySanBai.Models.Entities.NguoiDung>`, `List<QuanLySanBai.Models.Entities.ThanhVienCoSo>`, `QuanLySanBai`, `List<QuanLySanBai.Models.Entities.CoSo>`, `QuanLySanBai` (+869 more)
  These have ≤1 connection - possible missing edges. (Counts symbols only; 968 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **40 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `QuanLySanBaiContext` connect `Community 2` to `Community 0`, `Community 1`, `Community 4`, `Community 5`, `Community 6`, `Community 7`, `Community 8`, `Community 9`, `Community 10`, `Community 12`, `Community 13`, `Community 14`, `Community 16`, `Community 17`, `Community 18`, `Community 19`, `Community 20`, `Community 21`, `Community 22`, `Community 23`, `Community 24`, `Community 25`, `Community 26`, `Community 27`, `Community 28`, `Community 29`, `Community 31`, `Community 32`, `Community 34`, `Community 35`, `Community 36`, `Community 37`, `Community 38`, `Community 39`, `Community 40`, `Community 41`, `Community 42`, `Community 43`, `Community 45`, `Community 47`, `Community 48`, `Community 49`, `Community 50`, `Community 51`, `Community 54`, `Community 55`, `Community 56`, `Community 57`, `Community 60`, `Community 61`, `Community 62`, `Community 63`, `Community 64`, `Community 65`, `Community 66`, `Community 70`, `Community 87`, `Community 100`?**
  _High betweenness centrality (0.249) - this node is a cross-community bridge._
- **Are the 2 inferred relationships involving `NguoiDung` (e.g. with `AccountController.Register()` and `DevelopmentSeeder.Seed()`) actually correct?**
  _`NguoiDung` has 2 INFERRED edges - model-reasoned connections that need verification._
- **What connects `List<QuanLySanBai.Models.Entities.NguoiDung>`, `List<QuanLySanBai.Models.Entities.ThanhVienCoSo>`, `QuanLySanBai` to the rest of the system?**
  _874 weakly-connected nodes found - possible missing edges._
- **Should `Community 0` be split into smaller, more focused modules?**
  _Cohesion score 0.06990622335890878 - nodes in this community are weakly interconnected._
- **Why does `NguoiDung` connect `Community 4` to `Community 1`, `Community 2`, `Community 6`, `Community 7`, `Community 8`, `Community 10`, `Community 12`, `Community 14`, `Community 16`, `Community 17`, `Community 18`, `Community 20`, `Community 23`, `Community 25`, `Community 26`, `Community 29`, `Community 30`, `Community 31`, `Community 34`, `Community 35`, `Community 37`, `Community 39`, `Community 42`, `Community 43`, `Community 45`, `Community 50`, `Community 51`, `Community 57`, `Community 61`, `Community 70`?**
  _High betweenness centrality (0.070) - this node is a cross-community bridge._
- **Should `Community 1` be split into smaller, more focused modules?**
  _Cohesion score 0.1069182389937107 - nodes in this community are weakly interconnected._
- **Why does `DonDatSan` connect `Community 7` to `Community 1`, `Community 2`, `Community 34`, `Community 4`, `Community 70`, `Community 6`, `Community 9`, `Community 12`, `Community 60`, `Community 15`, `Community 18`, `Community 51`, `Community 21`, `Community 53`, `Community 55`, `Community 28`, `Community 30`?**
  _High betweenness centrality (0.065) - this node is a cross-community bridge._