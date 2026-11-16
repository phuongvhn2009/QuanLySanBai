# Project handoff

Delivery branch: master. Feature branches: MA-TaiKhoanPhanQuyen, MC-CauHinhCoSo, MB-XemLichDatSan. User requested implementation through cluster 4 with simple operational UI, priority on business correctness and Notion updates.

Read docs/cluster-1-4-review.md for completed scope, runtime commands, tests, data rules, demo credentials location and remaining scope. Read docs/A01-A05-review.md for previous authentication work. Clusters A, C and B are committed, pushed and merged into master. Respect existing uncommitted changes.

Notion task data source: collection://d7f0d301-5d78-8267-906b-072ed483f9de. Preserve tasks already marked Hoàn thành by the user. Implemented and verified tasks go to Chờ kiểm tra; user confirms Hoàn thành. Update statuses and evidence during active work.

Database entities/context are scaffolded. Put custom mappings in Data/QuanLySanBaiContext.Custom.cs. Booking procedure owns its transaction; do not wrap it. DevelopmentSeeder is opt-in and Development-only. Credentials are in ignored .local/demo-accounts.txt; never commit them.

Clusters 5–6 (`MO-VanHanhDon` / `MQ-KiemThuCotLoi` `8abc588`) have been merged into `master`, keeping explicit facility permission checks (including `TAO_DON` in `usp_DatSanMotKhung`), `DA_DUYET` approval gating, and SignalR `CalendarChanged` notifications in `OperationService`. See `ai-memory/CURRENT-STATE.md` for latest handoff state. LocalDB additive `row_version` update already applied; new databases get it in base schema. Latest extended SQL/HTTP/WebSocket run on merged `master`: 148 checks passed (and 106 checks passed on `MQ-KiemThuCotLoi` prior to merge). Check-in lead time is provisionally 15 minutes pending user review; complete only after check-in, end time and full payment. TimeProvider injection simulates lifecycle in tests only. Payment/refund extensions remain out of scope.
