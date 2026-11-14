# Bộ nhớ bàn giao cho AI

Đọc file này trước, sau đó đọc [CURRENT-STATE.md](CURRENT-STATE.md) và [PROJECT-RULES.md](PROJECT-RULES.md).

Đây là ghi chú bền vững giữa các phiên AI, không phải trạng thái hệ thống được tự động đồng bộ. Luôn kiểm tra Git và mã nguồn hiện tại trước khi hành động. Không suy ra rằng tính năng có trên mọi nhánh chỉ vì đã được triển khai ở một nhánh khác.

## Cách tiếp nối

1. Chạy `git status --short`, `git branch -vv`, `git log -5 --oneline`.
2. Đọc CURRENT-STATE.md để biết nhánh chứa tính năng cần làm.
3. Đọc tài liệu cụm liên quan trong `docs/`; đối chiếu với code trên nhánh hiện tại.
4. Giữ các thay đổi chưa commit của người dùng; không tự reset, clean hoặc stash chúng.
5. Sau khi làm việc, cập nhật CURRENT-STATE.md: ngày, nhánh/commit, việc thực sự đã làm, kiểm tra thực sự đã chạy, lỗi và bước tiếp theo. Không ghi mật khẩu, token hay connection string chứa bí mật.

Các ghi chú cũ có thể lỗi thời. Đặc biệt `docs/HANDOFF.md` còn nói cụm 5–6 chưa commit; thông tin đó đã được thay thế bởi CURRENT-STATE.md.
