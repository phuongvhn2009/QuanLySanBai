using Microsoft.EntityFrameworkCore;
namespace QuanLySanBai.Data;
public partial class QuanLySanBaiContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Models.Entities.DonDatSan>().Property(d=>d.RowVersion).IsRowVersion().HasColumnName("row_version");
        // Keep scaffold-generated files untouched. SQL OUTPUT is incompatible with these triggers.
        foreach(var entity in modelBuilder.Model.GetEntityTypes().Where(e=>e.GetDeclaredTriggers().Any())) entity.UseSqlOutputClause(false);
    }
}
