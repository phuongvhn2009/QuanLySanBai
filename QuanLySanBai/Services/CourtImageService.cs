using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuanLySanBai.Data;
using QuanLySanBai.Models.Entities;

namespace QuanLySanBai.Services;

public class CourtImageOptions
{
    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024;
    public int MaxImagesPerCourt { get; set; } = 10;
}

public static class CourtImageResolver
{
    public const string PlaceholderUrl = "/assets/images/placeholders/court-placeholder.webp";

    public static string ForCourtType(LoaiSan? courtType, MonTheThao? sport = null)
    {
        var s = sport ?? courtType?.MonTheThao;
        var code = (s?.MaMon ?? "").Trim().ToUpperInvariant();
        var typeName = (courtType?.TenLoai ?? "").Trim().ToLowerInvariant();
        var capacity = courtType?.SucChuaNguoi ?? 0;

        if (code == "BONG_DA" || typeName.Contains("bóng đá") || typeName.Contains("bong da") || typeName.Contains("futsal"))
        {
            if (typeName.Contains("11") || capacity >= 20)
                return "/assets/images/court-types/football-11.webp";
            if (typeName.Contains("7") || capacity >= 13)
                return "/assets/images/court-types/football-7.webp";
            return "/assets/images/court-types/football-5.webp";
        }
        if (code == "CAU_LONG" || typeName.Contains("cầu lông") || typeName.Contains("cau long"))
            return "/assets/images/court-types/badminton.webp";
        if (code == "TENNIS" || typeName.Contains("tennis"))
            return "/assets/images/court-types/tennis.webp";
        if (code == "BONG_RO" || typeName.Contains("bóng rổ") || typeName.Contains("bong ro"))
            return "/assets/images/court-types/basketball.webp";
        if (code == "PICKLEBALL" || typeName.Contains("pickleball"))
            return "/assets/images/court-types/pickleball.webp";

        return PlaceholderUrl;
    }

    public static (string Url, bool IsRealPhoto, int PhotoCount) PrimaryForCourt(San court)
    {
        var ordered = court.HinhAnhSans
            .OrderByDescending(x => x.LaAnhDaiDien)
            .ThenBy(x => x.ThuTuHienThi)
            .ThenBy(x => x.Id)
            .ToList();

        if (ordered.Count > 0)
            return (ordered[0].DuongDan, true, ordered.Count);

        return (ForCourtType(court.LoaiSan, court.LoaiSan?.MonTheThao), false, 0);
    }

    public static List<(string Url, bool IsRealPhoto, bool IsPrimary, string Label)> GalleryForCourt(San court)
    {
        var ordered = court.HinhAnhSans
            .OrderByDescending(x => x.LaAnhDaiDien)
            .ThenBy(x => x.ThuTuHienThi)
            .ThenBy(x => x.Id)
            .ToList();

        if (ordered.Count > 0)
        {
            return ordered.Select((img, idx) => (
                img.DuongDan,
                true,
                img.LaAnhDaiDien,
                img.LaAnhDaiDien ? "Ảnh đại diện thực tế" : $"Ảnh thực tế #{idx + 1}"
            )).ToList();
        }

        return [(ForCourtType(court.LoaiSan, court.LoaiSan?.MonTheThao), false, true, "Ảnh minh họa loại sân")];
    }
}

public class CourtImageService(
    QuanLySanBaiContext db,
    IOptions<CourtImageOptions> options,
    IWebHostEnvironment? env = null)
{
    private static readonly byte[] PngMagic = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public CourtImageOptions Options => options.Value;

    private string WebRootPath => !string.IsNullOrWhiteSpace(env?.WebRootPath)
        ? env!.WebRootPath
        : Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

    private string UploadsRoot => Path.GetFullPath(Path.Combine(WebRootPath, "uploads", "courts"));

    private async Task<San> RequireOwnedCourtAsync(long actor, long facilityId, long courtId)
    {
        var activeUser = await db.NguoiDungs.AsNoTracking().AnyAsync(u => u.Id == actor && u.TrangThai);
        if (!activeUser) throw new FacilityAccessException();

        var court = await db.Sans
            .Include(s => s.CoSo)
            .Include(s => s.LoaiSan).ThenInclude(t => t.MonTheThao)
            .SingleOrDefaultAsync(s => s.Id == courtId && s.CoSoId == facilityId && s.CoSo.ChuSoHuuId == actor && s.CoSo.TrangThaiDuyet == "DA_DUYET");

        return court ?? throw new FacilityAccessException();
    }

    public async Task<List<HinhAnhSan>> ListForOwnerAsync(long actor, long facilityId, long courtId)
    {
        await RequireOwnedCourtAsync(actor, facilityId, courtId);
        return await db.HinhAnhSans.AsNoTracking()
            .Where(h => h.SanId == courtId)
            .OrderByDescending(h => h.LaAnhDaiDien)
            .ThenBy(h => h.ThuTuHienThi)
            .ThenBy(h => h.Id)
            .ToListAsync();
    }

    public string ValidateFile(IFormFile? file)
    {
        if (file is null || file.Length <= 0)
            throw new BusinessException("Tệp ảnh không hợp lệ hoặc bị rỗng.");

        if (file.Length > Options.MaxFileSizeBytes)
        {
            var maxMb = Math.Round(Options.MaxFileSizeBytes / (1024d * 1024d), 1);
            throw new BusinessException($"Ảnh \"{Path.GetFileName(file.FileName)}\" vượt quá dung lượng tối đa {maxMb:0.#} MB.");
        }

        var ext = Path.GetExtension(file.FileName ?? "").ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp"))
            throw new BusinessException("Chỉ chấp nhận tệp ảnh định dạng JPG, JPEG, PNG hoặc WebP.");

        var mime = (file.ContentType ?? "").Split(';')[0].Trim().ToLowerInvariant();
        if (mime is not ("image/jpeg" or "image/jpg" or "image/png" or "image/webp"))
            throw new BusinessException("Loại MIME của tệp không phải ảnh hợp lệ (JPG, PNG, WebP).");

        Span<byte> header = stackalloc byte[12];
        int read;
        using (var stream = file.OpenReadStream())
        {
            read = ReadAtLeastBytes(stream, header);
        }

        if (read < 12)
            throw new BusinessException("Nội dung tệp không phải định dạng ảnh hợp lệ.");

        bool isJpeg = header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        bool isPng = header[..8].SequenceEqual(PngMagic);
        bool isWebp = header[0] == (byte)'R' && header[1] == (byte)'I' && header[2] == (byte)'F' && header[3] == (byte)'F'
                   && header[8] == (byte)'W' && header[9] == (byte)'E' && header[10] == (byte)'B' && header[11] == (byte)'P';

        if (isJpeg && ext is ".jpg" or ".jpeg" && mime is "image/jpeg" or "image/jpg")
            return ".jpg";
        if (isPng && ext == ".png" && mime == "image/png")
            return ".png";
        if (isWebp && ext == ".webp" && mime == "image/webp")
            return ".webp";

        throw new BusinessException("Nội dung thực tế của tệp không khớp định dạng ảnh JPG, PNG hoặc WebP.");
    }

    private static int ReadAtLeastBytes(Stream stream, Span<byte> buffer)
    {
        int total = 0;
        while (total < buffer.Length)
        {
            int n = stream.Read(buffer[total..]);
            if (n <= 0) break;
            total += n;
        }
        return total;
    }

    private (string PhysicalPath, string RelativeUrl) AllocateSafeTarget(long courtId, string canonicalExt)
    {
        var dir = Path.GetFullPath(Path.Combine(UploadsRoot, courtId.ToString()));
        if (!dir.StartsWith(UploadsRoot, StringComparison.OrdinalIgnoreCase))
            throw new BusinessException("Đường dẫn lưu trữ không hợp lệ.");

        Directory.CreateDirectory(dir);
        var fileName = $"{Guid.NewGuid():N}{canonicalExt}";
        var physical = Path.GetFullPath(Path.Combine(dir, fileName));
        if (!physical.StartsWith(dir, StringComparison.OrdinalIgnoreCase))
            throw new BusinessException("Đường dẫn lưu trữ không hợp lệ.");

        return (physical, $"/uploads/courts/{courtId}/{fileName}");
    }

    private void SafeDeletePhysical(string? relativeUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl)) return;
        if (!relativeUrl.StartsWith("/uploads/courts/", StringComparison.OrdinalIgnoreCase)) return;

        var trimmed = relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var physical = Path.GetFullPath(Path.Combine(WebRootPath, trimmed));
        if (!physical.StartsWith(UploadsRoot, StringComparison.OrdinalIgnoreCase)) return;

        try
        {
            if (File.Exists(physical)) File.Delete(physical);
        }
        catch
        {
            // Ignore transient IO lock; DB transaction has already committed safely.
        }
    }

    public async Task<List<HinhAnhSan>> UploadManyAsync(long actor, long facilityId, long courtId, IReadOnlyList<IFormFile> files, CancellationToken ct = default)
    {
        await RequireOwnedCourtAsync(actor, facilityId, courtId);
        if (files is null || files.Count == 0)
            throw new BusinessException("Vui lòng chọn ít nhất một tệp ảnh để tải lên.");

        var validated = new List<(IFormFile File, string Ext)>(files.Count);
        foreach (var file in files)
            validated.Add((file, ValidateFile(file)));

        var existing = await db.HinhAnhSans
            .Where(h => h.SanId == courtId)
            .OrderBy(h => h.ThuTuHienThi)
            .ThenBy(h => h.Id)
            .ToListAsync(ct);

        if (existing.Count + validated.Count > Options.MaxImagesPerCourt)
            throw new BusinessException($"Mỗi sân chỉ được lưu tối đa {Options.MaxImagesPerCourt} ảnh (hiện có {existing.Count} ảnh).");

        var writtenFiles = new List<string>();
        var created = new List<HinhAnhSan>();
        var now = VietnamTime.Now;
        var hasPrimary = existing.Any(e => e.LaAnhDaiDien);
        var nextOrder = existing.Count > 0 ? existing.Max(e => e.ThuTuHienThi) + 1 : 0;

        try
        {
            foreach (var (file, ext) in validated)
            {
                var (physical, url) = AllocateSafeTarget(courtId, ext);
                await using (var fs = new FileStream(physical, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    await file.CopyToAsync(fs, ct);
                }
                writtenFiles.Add(physical);

                var entity = new HinhAnhSan
                {
                    SanId = courtId,
                    DuongDan = url,
                    LaAnhDaiDien = !hasPrimary,
                    ThuTuHienThi = nextOrder++,
                    NgayTao = now,
                    NgayCapNhat = now
                };
                hasPrimary = true;
                created.Add(entity);
            }

            await using var tx = await db.Database.BeginTransactionAsync(ct);
            db.HinhAnhSans.AddRange(created);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return created;
        }
        catch
        {
            foreach (var path in writtenFiles)
            {
                try { if (File.Exists(path)) File.Delete(path); } catch { }
            }
            throw;
        }
    }

    public async Task SetPrimaryAsync(long actor, long facilityId, long courtId, long imageId)
    {
        await RequireOwnedCourtAsync(actor, facilityId, courtId);
        var images = await db.HinhAnhSans.Where(h => h.SanId == courtId).ToListAsync();
        var target = images.SingleOrDefault(h => h.Id == imageId) ?? throw new FacilityAccessException();
        if (target.LaAnhDaiDien) return;

        var now = VietnamTime.Now;
        await using var tx = await db.Database.BeginTransactionAsync();
        foreach (var img in images.Where(i => i.LaAnhDaiDien))
        {
            img.LaAnhDaiDien = false;
            img.NgayCapNhat = now;
        }
        await db.SaveChangesAsync();

        target.LaAnhDaiDien = true;
        target.NgayCapNhat = now;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task MoveOrderAsync(long actor, long facilityId, long courtId, long imageId, int direction)
    {
        await RequireOwnedCourtAsync(actor, facilityId, courtId);
        var images = await db.HinhAnhSans
            .Where(h => h.SanId == courtId)
            .OrderBy(h => h.ThuTuHienThi)
            .ThenBy(h => h.Id)
            .ToListAsync();

        var index = images.FindIndex(h => h.Id == imageId);
        if (index < 0) throw new FacilityAccessException();

        var targetIndex = Math.Clamp(index + (direction < 0 ? -1 : 1), 0, images.Count - 1);
        if (targetIndex == index) return;

        var item = images[index];
        images.RemoveAt(index);
        images.Insert(targetIndex, item);

        var now = VietnamTime.Now;
        await using var tx = await db.Database.BeginTransactionAsync();
        for (int i = 0; i < images.Count; i++)
        {
            images[i].ThuTuHienThi = i;
            images[i].NgayCapNhat = now;
        }
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task ReplaceAsync(long actor, long facilityId, long courtId, long imageId, IFormFile file, CancellationToken ct = default)
    {
        await RequireOwnedCourtAsync(actor, facilityId, courtId);
        var ext = ValidateFile(file);

        var existing = await db.HinhAnhSans.SingleOrDefaultAsync(h => h.Id == imageId && h.SanId == courtId, ct)
            ?? throw new FacilityAccessException();

        var oldRelativeUrl = existing.DuongDan;
        var (newPhysical, newRelativeUrl) = AllocateSafeTarget(courtId, ext);

        try
        {
            await using (var fs = new FileStream(newPhysical, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await file.CopyToAsync(fs, ct);
            }

            await using var tx = await db.Database.BeginTransactionAsync(ct);
            existing.DuongDan = newRelativeUrl;
            existing.NgayCapNhat = VietnamTime.Now;
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            try { if (File.Exists(newPhysical)) File.Delete(newPhysical); } catch { }
            throw;
        }

        // Delete old physical file ONLY after DB transaction commits safely.
        SafeDeletePhysical(oldRelativeUrl);
    }

    public async Task DeleteAsync(long actor, long facilityId, long courtId, long imageId)
    {
        await RequireOwnedCourtAsync(actor, facilityId, courtId);
        var images = await db.HinhAnhSans
            .Where(h => h.SanId == courtId)
            .OrderBy(h => h.ThuTuHienThi)
            .ThenBy(h => h.Id)
            .ToListAsync();

        var target = images.SingleOrDefault(h => h.Id == imageId) ?? throw new FacilityAccessException();
        var oldRelativeUrl = target.DuongDan;
        var wasPrimary = target.LaAnhDaiDien;

        await using var tx = await db.Database.BeginTransactionAsync();
        db.HinhAnhSans.Remove(target);
        images.Remove(target);
        await db.SaveChangesAsync();

        var now = VietnamTime.Now;
        if (wasPrimary && images.Count > 0)
        {
            images[0].LaAnhDaiDien = true;
            images[0].NgayCapNhat = now;
        }
        for (int i = 0; i < images.Count; i++)
        {
            images[i].ThuTuHienThi = i;
        }
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        // Delete physical file ONLY after DB transaction commits safely.
        SafeDeletePhysical(oldRelativeUrl);
    }
}
