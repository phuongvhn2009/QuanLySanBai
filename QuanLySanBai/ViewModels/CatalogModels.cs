using System.ComponentModel.DataAnnotations;
namespace QuanLySanBai.ViewModels;
public class SportForm
{
    public int Id { get; set; }
    [Required,StringLength(50)] public string Code { get; set; }="";
    [Required,StringLength(100)] public string Name { get; set; }="";
    public bool Active { get; set; }=true;
}
public class CourtTypeForm
{
    public int Id { get; set; }
    [Range(1,int.MaxValue)] public int SportId { get; set; }
    [Required,StringLength(100)] public string Name { get; set; }="";
    [Range(1,1000)] public int Capacity { get; set; }=10;
}
