using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class HoSoTrinhDo
{
    public long Id { get; set; }

    public long NguoiDungId { get; set; }

    public int MonTheThaoId { get; set; }

    public int DiemElo { get; set; }

    public string? HangTrinhDo { get; set; }

    public int SoTranDaDau { get; set; }

    public int SoTranThang { get; set; }

    public int SoTranThua { get; set; }

    public DateTime NgayCapNhat { get; set; }

    public int SoTranHoa { get; set; }

    public virtual ICollection<LichSuElo> LichSuElos { get; set; } = new List<LichSuElo>();

    public virtual MonTheThao MonTheThao { get; set; } = null!;

    public virtual NguoiDung NguoiDung { get; set; } = null!;
}
