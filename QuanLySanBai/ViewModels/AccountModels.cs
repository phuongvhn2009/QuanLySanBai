using System.ComponentModel.DataAnnotations;
namespace QuanLySanBai.ViewModels;

public class ProfileModel
{
    [Required, StringLength(100), Display(Name = "Họ và tên")] public string HoVaTen { get; set; } = "";
    [Required, RegularExpression(@"^0[0-9]{9}$", ErrorMessage = "Số điện thoại gồm 10 chữ số, bắt đầu bằng 0."), Display(Name = "Số điện thoại")] public string SoDienThoai { get; set; } = "";
    [EmailAddress, StringLength(255), Display(Name = "Email")] public string? Email { get; set; }
}
public class RegisterModel : ProfileModel
{
    [Required, StringLength(128, MinimumLength = 8), DataType(DataType.Password), Display(Name = "Mật khẩu")] public string Password { get; set; } = "";
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp."), DataType(DataType.Password), Display(Name = "Xác nhận mật khẩu")] public string ConfirmPassword { get; set; } = "";
}
public class LoginModel
{
    [Required, Display(Name = "Số điện thoại hoặc email")] public string Login { get; set; } = "";
    [Required, DataType(DataType.Password), Display(Name = "Mật khẩu")] public string Password { get; set; } = "";
    [Display(Name = "Ghi nhớ đăng nhập")] public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}
