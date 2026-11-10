using Microsoft.EntityFrameworkCore;
namespace QuanLySanBai.Data;
public partial class QuanLySanBaiContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // Keep scaffold-generated files untouched. SQL OUTPUT is incompatible with these triggers.
        foreach(var entity in modelBuilder.Model.GetEntityTypes().Where(e=>e.GetDeclaredTriggers().Any())) entity.UseSqlOutputClause(false);
    }
}
