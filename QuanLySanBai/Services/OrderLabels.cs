namespace QuanLySanBai.Services;
public static class OrderLabels { public static string Status(string s)=>s switch { "CHO_THANH_TOAN"=>"Chờ thanh toán", "DA_XAC_NHAN"=>"Đã xác nhận", "DANG_SU_DUNG"=>"Đang sử dụng", "HOAN_THANH"=>"Hoàn thành", "DA_HUY"=>"Đã hủy", "HET_HAN"=>"Hết hạn", _=>s }; }
