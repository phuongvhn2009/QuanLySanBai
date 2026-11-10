# Project handoff

Delivery branch: master. Feature branches: MA-TaiKhoanPhanQuyen, MC-CauHinhCoSo, MB-XemLichDatSan. User requested implementation through cluster 4 with simple operational UI, priority on business correctness and Notion updates.

Read docs/cluster-1-4-review.md for completed scope, runtime commands, tests, data rules, demo credentials location and remaining scope. Read docs/A01-A05-review.md for previous authentication work. Clusters A, C and B are committed, pushed and merged into master. Respect existing uncommitted changes.

Notion task data source: collection://d7f0d301-5d78-8267-906b-072ed483f9de. Preserve tasks already marked Hoàn thành by the user. Implemented and verified tasks go to Chờ kiểm tra; user confirms Hoàn thành. Update statuses and evidence during active work.

Database entities/context are scaffolded. Put custom mappings in Data/QuanLySanBaiContext.Custom.cs. Booking procedure owns its transaction; do not wrap it. DevelopmentSeeder is opt-in and Development-only. Credentials are in ignored .local/demo-accounts.txt; never commit them.
