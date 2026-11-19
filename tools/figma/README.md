# Chuẩn hóa Figma thân thiện cho người dùng (`Quanr ly san bai`)

Công cụ này chạy **trực tiếp bên trong Figma** (không chỉnh sửa bất kỳ mã nguồn nào của hệ thống `QuanLySanBai/`).

## Vì sao dùng Figma Plugin?
MCP / REST API của Figma chỉ hỗ trợ **đọc** dữ liệu từ file Figma, không cho phép ghi đè Variables hay chỉnh layer từ bên ngoài. Plugin local dưới đây giúp tự động chuẩn hóa toàn bộ file Figma chỉ với **1 lần bấm**.

## Plugin làm gì cho file Figma?
1. **Tạo bộ Variables tiếng Việt (`Sân Bãi Tokens`)**: Gom toàn bộ màu nền, màu chữ, màu thương hiệu, màu trạng thái, bo góc (`4px`, `6px`, `8px`) và font chữ (`Be Vietnam Pro`) thành biến dùng chung.
2. **Gắn tự động toàn bộ layer vào Variables**: Khi người dùng đổi 1 màu trong bảng **Variables**, toàn bộ 27 màn hình tự cập nhật theo.
3. **Chuẩn hóa Text Styles (`Chữ/...`)**: Giúp đổi cỡ chữ/font chữ đồng loạt từ bảng Styles bên phải.
4. **Khóa layer trang trí**: Tự động khóa `Đường phân cách` và `Khoảng trống` để người dùng không bấm nhầm làm lệch Auto-Layout.
5. **Tạo 2 khung tham chiếu trên canvas**:
   - `🎨 Design Tokens`: Bảng màu, bo góc, font trực quan.
   - `📖 Hướng dẫn chỉnh sửa nhanh`: Hướng dẫn từng bước bằng tiếng Việt ngay trong file thiết kế.

## Cách chạy (chỉ làm 1 lần, tốn ~5 giây)
1. Mở file **Quanr ly san bai** trên **Figma Desktop** (lưu ý: tính năng Import plugin from manifest chỉ có trên ứng dụng Figma Desktop, không có trên bản web).
2. Vào menu **Plugins** $\rightarrow$ **Development** $\rightarrow$ **Import plugin from manifest...**
3. Chọn file:
   `D:\QuanLysanBai\tools\figma\plugin\manifest.json`
4. Chạy plugin **Sân Bãi – Chuẩn hóa Design Tokens** $\rightarrow$ Chờ vài giây sẽ có thông báo hoàn tất và tự phóng tới khung `🎨 Design Tokens` + `📖 Hướng dẫn chỉnh sửa nhanh`.
5. Lưu hoặc tiếp tục chỉnh sửa trực tiếp trên Figma (bản web hay desktop đều dùng được bình thường sau khi plugin chạy xong).
