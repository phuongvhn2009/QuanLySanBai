using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuanLySanBai.Data;
using QuanLySanBai.Models.Entities;
using QuanLySanBai.Services;

public static class CourtImageChecks
{
    private static readonly byte[] ValidPng =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52
    ];

    private static readonly byte[] ValidJpeg =
    [
        0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46,
        0x49, 0x46, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01
    ];

    private static readonly byte[] ValidWebp =
    [
        (byte)'R', (byte)'I', (byte)'F', (byte)'F',
        0x24, 0x00, 0x00, 0x00,
        (byte)'W', (byte)'E', (byte)'B', (byte)'P',
        (byte)'V', (byte)'P', (byte)'8', (byte)' '
    ];

    private static IFormFile MakeFile(string name, string contentType, byte[] bytes) =>
        new FormFile(new MemoryStream(bytes), 0, bytes.Length, "files", name)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };

    public static async Task<int> Run(DbContextOptions<QuanLySanBaiContext> options, string repoRoot, long owner, long other, long facility, long court)
    {
        int count = 0;
        void Check(bool ok, string name) { if (!ok) throw new Exception("FAIL: " + name); count++; Console.WriteLine("PASS: " + name); }
        async Task Reject(Func<Task> work, string name)
        {
            try { await work(); }
            catch (Exception e) when (e is BusinessException or FacilityAccessException) { Check(true, name); return; }
            throw new Exception("FAIL: " + name);
        }

        // 1. Verify all 8 generated default WebP illustrations exist and have real WEBP magic headers
        var expectedAssets = new[]
        {
            "assets/images/court-types/football-5.webp",
            "assets/images/court-types/football-7.webp",
            "assets/images/court-types/football-11.webp",
            "assets/images/court-types/badminton.webp",
            "assets/images/court-types/tennis.webp",
            "assets/images/court-types/basketball.webp",
            "assets/images/court-types/pickleball.webp",
            "assets/images/placeholders/court-placeholder.webp"
        };
        var wwwroot = Path.Combine(repoRoot, "QuanLySanBai", "wwwroot");
        foreach (var rel in expectedAssets)
        {
            var full = Path.Combine(wwwroot, rel.Replace('/', Path.DirectorySeparatorChar));
            var bytes = File.Exists(full) ? await File.ReadAllBytesAsync(full) : [];
            bool isWebp = bytes.Length > 1000 && bytes[0] == 'R' && bytes[1] == 'I' && bytes[2] == 'F' && bytes[3] == 'F'
                       && bytes[8] == 'W' && bytes[9] == 'E' && bytes[10] == 'B' && bytes[11] == 'P';
            Check(isWebp, "IMG Asset valid WebP: " + rel);
        }

        // 2. Verify CourtImageResolver maps sport/court types properly
        Check(CourtImageResolver.ForCourtType(new LoaiSan { TenLoai = "Sân 5 người", SucChuaNguoi = 10 }, new MonTheThao { MaMon = "BONG_DA" }) == "/assets/images/court-types/football-5.webp", "IMG Resolver: football-5");
        Check(CourtImageResolver.ForCourtType(new LoaiSan { TenLoai = "Sân 7 người", SucChuaNguoi = 14 }, new MonTheThao { MaMon = "BONG_DA" }) == "/assets/images/court-types/football-7.webp", "IMG Resolver: football-7");
        Check(CourtImageResolver.ForCourtType(new LoaiSan { TenLoai = "Sân 11 người", SucChuaNguoi = 22 }, new MonTheThao { MaMon = "BONG_DA" }) == "/assets/images/court-types/football-11.webp", "IMG Resolver: football-11");
        Check(CourtImageResolver.ForCourtType(new LoaiSan { TenLoai = "Cầu lông đôi", SucChuaNguoi = 4 }, new MonTheThao { MaMon = "CAU_LONG" }) == "/assets/images/court-types/badminton.webp", "IMG Resolver: badminton");
        Check(CourtImageResolver.ForCourtType(new LoaiSan { TenLoai = "Tennis", SucChuaNguoi = 4 }, new MonTheThao { MaMon = "TENNIS" }) == "/assets/images/court-types/tennis.webp", "IMG Resolver: tennis");
        Check(CourtImageResolver.ForCourtType(new LoaiSan { TenLoai = "Bóng rổ", SucChuaNguoi = 10 }, new MonTheThao { MaMon = "BONG_RO" }) == "/assets/images/court-types/basketball.webp", "IMG Resolver: basketball");
        Check(CourtImageResolver.ForCourtType(new LoaiSan { TenLoai = "Pickleball", SucChuaNguoi = 4 }, new MonTheThao { MaMon = "PICKLEBALL" }) == "/assets/images/court-types/pickleball.webp", "IMG Resolver: pickleball");
        Check(CourtImageResolver.ForCourtType(new LoaiSan { TenLoai = "Khác", SucChuaNguoi = 2 }, new MonTheThao { MaMon = "DEMO" }) == CourtImageResolver.PlaceholderUrl, "IMG Resolver: fallback placeholder");

        // 3. Test CourtImageService in an isolated temp webroot so no test files pollute wwwroot/uploads
        var tempRoot = Path.Combine(Path.GetTempPath(), "qlsb_img_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        var fakeEnv = new TempWebEnv(tempRoot);
        var imgOpts = Options.Create(new CourtImageOptions { MaxFileSizeBytes = 1024, MaxImagesPerCourt = 3 });

        CourtImageService Service(QuanLySanBaiContext db) => new(db, imgOpts, fakeEnv);

        try
        {
            // Unauthorized user cannot upload
            await Reject(async () =>
            {
                await using var db = new QuanLySanBaiContext(options);
                await Service(db).UploadManyAsync(other, facility, court, [MakeFile("a.png", "image/png", ValidPng)]);
            }, "IMG Security: stranger cannot upload court image");

            // Invalid extension rejected
            await Reject(async () =>
            {
                await using var db = new QuanLySanBaiContext(options);
                await Service(db).UploadManyAsync(owner, facility, court, [MakeFile("shell.php", "application/x-php", System.Text.Encoding.UTF8.GetBytes("<?php echo 1; ?>"))]);
            }, "IMG Security: executable/php file rejected");

            // Spoofed extension + MIME with non-image content rejected by magic byte inspection
            await Reject(async () =>
            {
                await using var db = new QuanLySanBaiContext(options);
                await Service(db).UploadManyAsync(owner, facility, court, [MakeFile("fake.jpg", "image/jpeg", System.Text.Encoding.UTF8.GetBytes("<script>alert(1)</script>"))]);
            }, "IMG Security: spoofed JPG with script bytes rejected by magic bytes");

            // Oversized file rejected
            await Reject(async () =>
            {
                await using var db = new QuanLySanBaiContext(options);
                var big = new byte[2048];
                Array.Copy(ValidPng, big, ValidPng.Length);
                await Service(db).UploadManyAsync(owner, facility, court, [MakeFile("big.png", "image/png", big)]);
            }, "IMG Security: oversized file rejected");

            // Upload 2 valid files at once (PNG + WebP)
            List<HinhAnhSan> uploaded;
            await using (var db = new QuanLySanBaiContext(options))
            {
                uploaded = await Service(db).UploadManyAsync(owner, facility, court, [
                    MakeFile("../../evil name.png", "image/png", ValidPng),
                    MakeFile("photo2.webp", "image/webp", ValidWebp)
                ]);
            }
            Check(uploaded.Count == 2 && uploaded[0].LaAnhDaiDien && !uploaded[1].LaAnhDaiDien, "IMG Upload: multi-upload saves metadata and auto-sets first as primary");
            Check(!uploaded[0].DuongDan.Contains("evil") && uploaded[0].DuongDan.StartsWith($"/uploads/courts/{court}/") && uploaded[0].DuongDan.EndsWith(".png"), "IMG Security: filename sanitized to safe GUID");

            var p1 = Path.Combine(tempRoot, uploaded[0].DuongDan.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            var p2 = Path.Combine(tempRoot, uploaded[1].DuongDan.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            Check(File.Exists(p1) && File.Exists(p2), "IMG Upload: physical files written inside uploads/courts");

            // Exceeding MaxImagesPerCourt (3) rejected cleanly
            await Reject(async () =>
            {
                await using var db = new QuanLySanBaiContext(options);
                await Service(db).UploadManyAsync(owner, facility, court, [
                    MakeFile("c.jpg", "image/jpeg", ValidJpeg),
                    MakeFile("d.jpg", "image/jpeg", ValidJpeg)
                ]);
            }, "IMG Quota: exceeding MaxImagesPerCourt rejected");

            // Cross-user cannot set primary, reorder, replace, or delete
            await Reject(async () =>
            {
                await using var db = new QuanLySanBaiContext(options);
                await Service(db).SetPrimaryAsync(other, facility, court, uploaded[1].Id);
            }, "IMG Security: stranger cannot set primary image");

            await Reject(async () =>
            {
                await using var db = new QuanLySanBaiContext(options);
                await Service(db).DeleteAsync(other, facility, court, uploaded[0].Id);
            }, "IMG Security: stranger cannot delete image");

            await Reject(async () =>
            {
                await using var db = new QuanLySanBaiContext(options);
                await Service(db).DeleteAsync(owner, facility + 9999, court, uploaded[0].Id);
            }, "IMG Security: forged facilityId rejected");

            // Set second image as primary
            await using (var db = new QuanLySanBaiContext(options))
                await Service(db).SetPrimaryAsync(owner, facility, court, uploaded[1].Id);

            await using (var db = new QuanLySanBaiContext(options))
            {
                var list = await db.HinhAnhSans.AsNoTracking().Where(h => h.SanId == court).ToListAsync();
                Check(list.Count(h => h.LaAnhDaiDien) == 1 && list.Single(h => h.LaAnhDaiDien).Id == uploaded[1].Id, "IMG Primary: switches primary image atomically");
            }

            // Reorder images
            await using (var db = new QuanLySanBaiContext(options))
                await Service(db).MoveOrderAsync(owner, facility, court, uploaded[1].Id, -1);

            await using (var db = new QuanLySanBaiContext(options))
            {
                var ordered = await db.HinhAnhSans.AsNoTracking().Where(h => h.SanId == court).OrderBy(h => h.ThuTuHienThi).ToListAsync();
                Check(ordered[0].Id == uploaded[1].Id && ordered[0].ThuTuHienThi == 0 && ordered[1].ThuTuHienThi == 1, "IMG Order: MoveOrder updates display order");
            }

            // Replace image — new file created, old file removed after commit
            await using (var db = new QuanLySanBaiContext(options))
                await Service(db).ReplaceAsync(owner, facility, court, uploaded[0].Id, MakeFile("new.jpg", "image/jpeg", ValidJpeg));

            string replacedUrl;
            await using (var db = new QuanLySanBaiContext(options))
                replacedUrl = (await db.HinhAnhSans.AsNoTracking().SingleAsync(h => h.Id == uploaded[0].Id)).DuongDan;

            var replacedPath = Path.Combine(tempRoot, replacedUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            Check(replacedUrl.EndsWith(".jpg") && File.Exists(replacedPath) && !File.Exists(p1), "IMG Replace: swaps file and removes old file after commit");

            // Delete primary image — promotes remaining image to primary and removes old file
            await using (var db = new QuanLySanBaiContext(options))
                await Service(db).DeleteAsync(owner, facility, court, uploaded[1].Id);

            await using (var db = new QuanLySanBaiContext(options))
            {
                var remaining = await db.HinhAnhSans.AsNoTracking().Where(h => h.SanId == court).ToListAsync();
                Check(remaining.Count == 1 && remaining[0].Id == uploaded[0].Id && remaining[0].LaAnhDaiDien && !File.Exists(p2), "IMG Delete: promotes next image to primary and deletes file");
            }

            // Delete last image — court returns to fallback illustration cleanly
            await using (var db = new QuanLySanBaiContext(options))
                await Service(db).DeleteAsync(owner, facility, court, uploaded[0].Id);

            await using (var db = new QuanLySanBaiContext(options))
            {
                Check(await db.HinhAnhSans.CountAsync(h => h.SanId == court) == 0 && !File.Exists(replacedPath), "IMG Delete: removing last image leaves zero orphan files");
            }
        }
        finally
        {
            try { if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true); } catch { }
        }

        return count;
    }

    private sealed class TempWebEnv(string webRoot) : Microsoft.AspNetCore.Hosting.IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = webRoot;
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ApplicationName { get; set; } = "QuanLySanBai.Tests";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = webRoot;
        public string EnvironmentName { get; set; } = "Development";
    }
}
