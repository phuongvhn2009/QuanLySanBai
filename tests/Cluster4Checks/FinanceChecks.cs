using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuanLySanBai.Data;
using QuanLySanBai.Models.Entities;
using QuanLySanBai.Services;
using QuanLySanBai.ViewModels;

public static class FinanceChecks
{
    public static async Task<int> Run(
        DbContextOptions<QuanLySanBaiContext> options,
        long admin,
        long owner,
        long customer,
        long other,
        long facility,
        long court,
        int slot,
        int midnightSlot,
        Func<string, Task> sql,
        Func<string, Task<int>> scalar)
    {
        int count = 0;
        void Check(bool ok, string name)
        {
            if (!ok) throw new Exception("FAIL: " + name);
            count++;
            Console.WriteLine("PASS: " + name);
        }

        async Task Reject(Func<Task> work, string name)
        {
            try
            {
                await work();
            }
            catch (Exception e) when (e is BusinessException or FacilityAccessException or DbUpdateException or SqlException)
            {
                Check(true, name);
                return;
            }
            throw new Exception("FAIL: " + name);
        }

        var sepayOpt = Options.Create(new SePayOptions
        {
            BankCode = "MBBank",
            BankName = "MB Bank",
            AccountNumber = "099888777666",
            AccountName = "CHU SAN TEST",
            WebhookApiKey = "SEPAY_TEST_API_KEY_999",
            QrBaseUrl = "https://qr.sepay.vn/img",
            QrExpireMinutes = 15,
            Template = "compact"
        });
        var bookingOpt = Options.Create(new BookingOptions());
        var opOpt = Options.Create(new OperationOptions());

        async Task<long> Book(long user, int slotId, int daysAhead)
        {
            await using var db = new QuanLySanBaiContext(options);
            return await new BookingService(db, bookingOpt).Book(user, new BookingForm
            {
                CourtId = court,
                SlotId = slotId,
                Date = VietnamTime.Today.AddDays(daysAhead)
            });
        }

        PaymentService MakePaymentSvc(QuanLySanBaiContext db, TimeProvider? clock = null)
        {
            var c = clock ?? TimeProvider.System;
            var vqr = new VietQrService(sepayOpt);
            var ws = new WalletService(db, vqr, c, sepayOpt);
            return new PaymentService(db, new AccessService(db), vqr, ws, c, sepayOpt);
        }

        WebhookService MakeWebhookSvc(QuanLySanBaiContext db, TimeProvider? clock = null)
        {
            var c = clock ?? TimeProvider.System;
            var vqr = new VietQrService(sepayOpt);
            var ws = new WalletService(db, vqr, c, sepayOpt);
            return new WebhookService(db, new SePayService(sepayOpt), ws);
        }

        WalletService MakeWalletSvc(QuanLySanBaiContext db, TimeProvider? clock = null)
        {
            var c = clock ?? TimeProvider.System;
            var vqr = new VietQrService(sepayOpt);
            return new WalletService(db, vqr, c, sepayOpt);
        }

        string MakeWebhookJson(long sepayId, decimal amount, string code, string account = "099888777666", string transferType = "in", string? content = null) =>
            JsonSerializer.Serialize(new
            {
                id = sepayId,
                gateway = "MBBank",
                transactionDate = "2026-11-17 15:00:00",
                accountNumber = account,
                subAccount = "",
                code,
                content = content ?? $"Thanh toan {code}",
                transferType,
                description = "CHUYEN KHOAN",
                transferAmount = amount,
                accumulated = 5000000m,
                referenceCode = $"FT{sepayId}"
            });

        // 1, 2, 3. Cash payment sufficient, insufficient, and exact change + receipt
        var cashOrderId = await Book(customer, slot, 20);
        await using (var db = new QuanLySanBaiContext(options))
        {
            var order = await db.DonDatSans.AsNoTracking().SingleAsync(d => d.Id == cashOrderId);
            var psvc = MakePaymentSvc(db);
            await Reject(() => psvc.CollectCash(owner, new ThanhToanTienMatVM
            {
                OrderId = cashOrderId,
                Version = Convert.ToBase64String(order.RowVersion),
                Amount = 123456.78m,
                CashReceived = 100000m,
                Purpose = "THANH_TOAN",
                Key = Guid.NewGuid().ToString("N")
            }), "Finance 2: Cash insufficient rejected");
        }
        long cashPaymentId;
        await using (var db = new QuanLySanBaiContext(options))
        {
            var order = await db.DonDatSans.AsNoTracking().SingleAsync(d => d.Id == cashOrderId);
            var psvc = MakePaymentSvc(db);
            cashPaymentId = await psvc.CollectCash(owner, new ThanhToanTienMatVM
            {
                OrderId = cashOrderId,
                Version = Convert.ToBase64String(order.RowVersion),
                Amount = 123456.78m,
                CashReceived = 200000m,
                Purpose = "THANH_TOAN",
                Key = Guid.NewGuid().ToString("N")
            });
            var receipt = await psvc.GetReceipt(owner, cashPaymentId);
            Check(receipt.SoTienThanhToan == 123456.78m && receipt.SoTienKhachDua == 200000m && receipt.TienThua == 76543.22m,
                "Finance 1 & 3: Cash sufficient confirmed and exact change computed on receipt");
        }

        // 4. Create VietQR dynamic URL accurately from backend config
        var qrOrderId = await Book(customer, slot, 21);
        long gwId1;
        string code1;
        await using (var db = new QuanLySanBaiContext(options))
        {
            var psvc = MakePaymentSvc(db);
            gwId1 = await psvc.CreateBookingQr(customer, qrOrderId, "COC");
            var qr = await psvc.GetQrDetails(customer, gwId1);
            code1 = qr.PaymentCode;
            Check(qr.QrImageUrl.Contains("acc=099888777666") && qr.QrImageUrl.Contains("bank=MBBank") &&
                  qr.QrImageUrl.Contains("amount=50000.25") && qr.QrImageUrl.Contains($"des={code1}"),
                "Finance 4: VietQR URL built from backend SePay config and DB deposit amount");
        }

        // 6. Webhook bad auth rejected
        await using (var db = new QuanLySanBaiContext(options))
        {
            var wsvc = MakeWebhookSvc(db);
            var res = await wsvc.ProcessSePayWebhook(MakeWebhookJson(100001, 50000.25m, code1), "Apikey WRONG_KEY");
            Check(res.StatusCode == 401 && !res.Success && res.ProcessingStatus == "SAI_CHU_KY",
                "Finance 6: Webhook invalid Apikey rejected with 401 and logged");
        }

        // 8. Wrong destination account
        await using (var db = new QuanLySanBaiContext(options))
        {
            var wsvc = MakeWebhookSvc(db);
            var res = await wsvc.ProcessSePayWebhook(MakeWebhookJson(100002, 50000.25m, code1, account: "999999999"), "Apikey SEPAY_TEST_API_KEY_999");
            Check(res.StatusCode == 200 && res.ProcessingStatus == "SAI_TAI_KHOAN" &&
                  await scalar($"SELECT COUNT(*) FROM Thanh_Toan_Don WHERE don_dat_san_id={qrOrderId}") == 0,
                "Finance 8: Wrong receiving account rejected without confirming order");
        }

        // 9. Wrong payment code
        await using (var db = new QuanLySanBaiContext(options))
        {
            var wsvc = MakeWebhookSvc(db);
            var res = await wsvc.ProcessSePayWebhook(MakeWebhookJson(100003, 50000.25m, "QSBNOTEXIST999"), "Apikey SEPAY_TEST_API_KEY_999");
            Check(res.StatusCode == 200 && res.ProcessingStatus == "KHONG_TIM_THAY_MA",
                "Finance 9: Unknown payment code safely logged without confirming");
        }

        // 10. Underpaid transfer
        await using (var db = new QuanLySanBaiContext(options))
        {
            var wsvc = MakeWebhookSvc(db);
            var res = await wsvc.ProcessSePayWebhook(MakeWebhookJson(100004, 10000m, code1), "Apikey SEPAY_TEST_API_KEY_999");
            Check(res.StatusCode == 200 && res.ProcessingStatus == "THIEU_TIEN" &&
                  await scalar($"SELECT COUNT(*) FROM Don_Dat_San WHERE id={qrOrderId} AND trang_thai='CHO_THANH_TOAN'") == 1,
                "Finance 10: Underpayment rejected and order stays pending");
        }

        // 5 & 7. Valid webhook + Duplicate webhook idempotency
        await using (var db = new QuanLySanBaiContext(options))
        {
            var wsvc = MakeWebhookSvc(db);
            var res = await wsvc.ProcessSePayWebhook(MakeWebhookJson(100005, 50000.25m, code1), "Apikey SEPAY_TEST_API_KEY_999");
            Check(res.StatusCode == 200 && res.ProcessingStatus == "HOP_LE_DA_XU_LY" &&
                  await scalar($"SELECT COUNT(*) FROM Don_Dat_San WHERE id={qrOrderId} AND trang_thai='DA_XAC_NHAN'") == 1,
                "Finance 5: Valid SePay webhook confirms deposit and order atomically");

            var dup = await wsvc.ProcessSePayWebhook(MakeWebhookJson(100005, 50000.25m, code1), "Apikey SEPAY_TEST_API_KEY_999");
            Check(dup.StatusCode == 200 && dup.ProcessingStatus == "TRUNG_LAP" &&
                  await scalar($"SELECT COUNT(*) FROM Thanh_Toan_Don WHERE don_dat_san_id={qrOrderId}") == 1,
                "Finance 7: Duplicate SePay webhook ignored idempotently");
        }

        // 11. Overpayment caps ThanhToanDon to remaining & credits excess to wallet
        long gwId2;
        string code2;
        await using (var db = new QuanLySanBaiContext(options))
        {
            gwId2 = await MakePaymentSvc(db).CreateBookingQr(customer, qrOrderId, "BO_SUNG");
            code2 = (await db.GiaoDichCongThanhToans.SingleAsync(g => g.Id == gwId2)).MaDonHangCong;
        }
        await using (var db = new QuanLySanBaiContext(options))
        {
            // Remaining on qrOrderId is 123456.78 - 50000.25 = 73456.53. Send 100000!
            var res = await MakeWebhookSvc(db).ProcessSePayWebhook(MakeWebhookJson(100006, 100000m, code2), "Apikey SEPAY_TEST_API_KEY_999");
            var paidTotal = await db.ThanhToanDons.Where(p => p.DonDatSanId == qrOrderId && p.TrangThai == "THANH_CONG").SumAsync(p => p.SoTien);
            var wallet = await db.ViNguoiDungs.SingleAsync(w => w.NguoiDungId == customer);
            Check(res.ProcessingStatus == "THUA_TIEN_DA_XU_LY" && paidTotal == 123456.78m && wallet.SoDuKhaDung == 26543.47m,
                "Finance 11: Overpayment capped at order remaining and excess credited to wallet");
        }

        // 13. Already fully paid order -> second payment QR/webhook cannot double-pay
        var extraGwCode = "QSBO" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
        await sql($"INSERT INTO dbo.Giao_Dich_Cong_Thanh_Toan (ma_don_hang_cong, don_dat_san_id, nguoi_dung_id, cong_thanh_toan, so_tien, loai_thanh_toan, trang_thai) VALUES (N'{extraGwCode}', {qrOrderId}, {customer}, N'SEPAY', 10000, N'THU_TIEN', N'KHOI_TAO')");
        await using (var db = new QuanLySanBaiContext(options))
        {
            var res = await MakeWebhookSvc(db).ProcessSePayWebhook(MakeWebhookJson(100007, 10000m, extraGwCode), "Apikey SEPAY_TEST_API_KEY_999");
            Check(res.ProcessingStatus == "DON_DA_THANH_TOAN" &&
                  await scalar($"SELECT COUNT(*) FROM Thanh_Toan_Don WHERE don_dat_san_id={qrOrderId}") == 2,
                "Finance 13: Already fully paid order cannot be confirmed twice");
        }

        // 14. Cancelled order -> webhook cannot reactivate/pay cancelled order
        var cancelWebhookOrder = await Book(customer, slot, 22);
        string cancelGwCode;
        await using (var db = new QuanLySanBaiContext(options))
        {
            var gwId = await MakePaymentSvc(db).CreateBookingQr(customer, cancelWebhookOrder, "COC");
            cancelGwCode = (await db.GiaoDichCongThanhToans.SingleAsync(g => g.Id == gwId)).MaDonHangCong;
            var ord = await db.DonDatSans.SingleAsync(d => d.Id == cancelWebhookOrder);
            await new OperationService(db, new AccessService(db), TimeProvider.System, opOpt).Cancel(customer, new OrderActionForm
            {
                Id = cancelWebhookOrder,
                Version = Convert.ToBase64String(ord.RowVersion),
                Reason = "Cancelled before webhook"
            });
        }
        await using (var db = new QuanLySanBaiContext(options))
        {
            var res = await MakeWebhookSvc(db).ProcessSePayWebhook(MakeWebhookJson(100008, 50000.25m, cancelGwCode), "Apikey SEPAY_TEST_API_KEY_999");
            Check(res.ProcessingStatus == "DON_DA_HUY" &&
                  await scalar($"SELECT COUNT(*) FROM Don_Dat_San WHERE id={cancelWebhookOrder} AND trang_thai='DA_HUY'") == 1 &&
                  await scalar($"SELECT COUNT(*) FROM Thanh_Toan_Don WHERE don_dat_san_id={cancelWebhookOrder}") == 0,
                "Finance 14: Webhook never pays or reactivates a cancelled order");
        }

        // 12 & 15. Expired QR / Late webhook after HoldExpiryWorker runs
        // Case A: Slot still free -> recovers order and confirms
        var lateFreeOrder = await Book(customer, slot, 23);
        string lateFreeCode;
        await using (var db = new QuanLySanBaiContext(options))
        {
            var gwId = await MakePaymentSvc(db).CreateBookingQr(customer, lateFreeOrder, "COC");
            lateFreeCode = (await db.GiaoDichCongThanhToans.SingleAsync(g => g.Id == gwId)).MaDonHangCong;
        }
        await sql($"UPDATE Don_Dat_San SET thoi_gian_het_han_giu_cho=DATEADD(MINUTE,-5,DATEADD(HOUR,7,SYSUTCDATETIME())) WHERE id={lateFreeOrder}");
        await using (var db = new QuanLySanBaiContext(options))
        {
            await new BookingService(db, bookingOpt).Expire();
        }
        await using (var db = new QuanLySanBaiContext(options))
        {
            var res = await MakeWebhookSvc(db).ProcessSePayWebhook(MakeWebhookJson(100009, 50000.25m, lateFreeCode), "Apikey SEPAY_TEST_API_KEY_999");
            Check(res.ProcessingStatus == "HOP_LE_DA_XU_LY" &&
                  await scalar($"SELECT COUNT(*) FROM Don_Dat_San WHERE id={lateFreeOrder} AND trang_thai='DA_XAC_NHAN'") == 1,
                "Finance 12 & 15A: Late webhook after expiry/QR expiration recovers order if slot is still free");
        }

        // Case B: Slot already taken by another user -> late webhook does NOT double-book
        var lateConflictOrder = await Book(customer, slot, 24);
        string lateConflictCode;
        await using (var db = new QuanLySanBaiContext(options))
        {
            var gwId = await MakePaymentSvc(db).CreateBookingQr(customer, lateConflictOrder, "COC");
            lateConflictCode = (await db.GiaoDichCongThanhToans.SingleAsync(g => g.Id == gwId)).MaDonHangCong;
        }
        await sql($"UPDATE Don_Dat_San SET thoi_gian_het_han_giu_cho=DATEADD(MINUTE,-5,DATEADD(HOUR,7,SYSUTCDATETIME())) WHERE id={lateConflictOrder}");
        await using (var db = new QuanLySanBaiContext(options))
            await new BookingService(db, bookingOpt).Expire();
        await Book(other, slot, 24); // Other user books the slot!
        await using (var db = new QuanLySanBaiContext(options))
        {
            var res = await MakeWebhookSvc(db).ProcessSePayWebhook(MakeWebhookJson(100010, 50000.25m, lateConflictCode), "Apikey SEPAY_TEST_API_KEY_999");
            Check(res.ProcessingStatus == "LOI_XU_LY" &&
                  await scalar($"SELECT COUNT(*) FROM Don_Dat_San WHERE id={lateConflictOrder} AND trang_thai='HET_HAN'") == 1,
                "Finance 15B: Late webhook blocked from double-booking when slot already taken");
        }

        // 16. Two concurrent webhooks arriving simultaneously
        var concOrder = await Book(customer, slot, 25);
        string concCode;
        await using (var db = new QuanLySanBaiContext(options))
        {
            var gwId = await MakePaymentSvc(db).CreateBookingQr(customer, concOrder, "COC");
            concCode = (await db.GiaoDichCongThanhToans.SingleAsync(g => g.Id == gwId)).MaDonHangCong;
        }
        var concPayload = MakeWebhookJson(100011, 50000.25m, concCode);
        var t1 = Task.Run(async () => { await using var db = new QuanLySanBaiContext(options); return await MakeWebhookSvc(db).ProcessSePayWebhook(concPayload, "Apikey SEPAY_TEST_API_KEY_999"); });
        var t2 = Task.Run(async () => { await using var db = new QuanLySanBaiContext(options); return await MakeWebhookSvc(db).ProcessSePayWebhook(concPayload, "Apikey SEPAY_TEST_API_KEY_999"); });
        var concResults = await Task.WhenAll(t1, t2);
        Check(concResults.Count(r => r.ProcessingStatus == "HOP_LE_DA_XU_LY") == 1 &&
              concResults.Count(r => r.ProcessingStatus == "TRUNG_LAP") == 1 &&
              await scalar($"SELECT COUNT(*) FROM Thanh_Toan_Don WHERE don_dat_san_id={concOrder}") == 1,
            "Finance 16: Two concurrent webhooks confirm payment strictly once");

        // 17. Database error during confirmation rolls back cleanly
        var dbErrOrder = await Book(customer, slot, 26);
        string dbErrCode;
        await using (var db = new QuanLySanBaiContext(options))
        {
            var gwId = await MakePaymentSvc(db).CreateBookingQr(customer, dbErrOrder, "COC");
            dbErrCode = (await db.GiaoDichCongThanhToans.SingleAsync(g => g.Id == gwId)).MaDonHangCong;
        }
        // Pre-insert a conflicting KhoaChongLap in Thanh_Toan_Don to force SQL unique constraint failure mid-transaction
        await sql($"INSERT INTO dbo.Thanh_Toan_Don (ma_thanh_toan, don_dat_san_id, nguoi_thanh_toan_id, nhan_vien_thu_id, phuong_thuc, muc_dich, so_tien, khoa_chong_lap, trang_thai, ngay_thanh_toan) VALUES (N'CONFLICT1', {qrOrderId}, {customer}, {owner}, N'TIEN_MAT', N'BO_SUNG', 1, N'SEPAY:100012', N'THANH_CONG', DATEADD(HOUR,7,SYSUTCDATETIME()))");
        await using (var db = new QuanLySanBaiContext(options))
        {
            var res = await MakeWebhookSvc(db).ProcessSePayWebhook(MakeWebhookJson(100012, 50000.25m, dbErrCode), "Apikey SEPAY_TEST_API_KEY_999");
            Check(await scalar($"SELECT COUNT(*) FROM Don_Dat_San WHERE id={dbErrOrder} AND trang_thai='CHO_THANH_TOAN'") == 1 &&
                  await scalar($"SELECT COUNT(*) FROM Giao_Dich_Cong_Thanh_Toan WHERE ma_don_hang_cong='{dbErrCode}' AND trang_thai='KHOI_TAO'") == 1,
                "Finance 17: DB error mid-transaction rolls back gateway and order cleanly");
        }

        // F02. Wallet deposit via webhook + concurrent wallet spend protection + immutable ledger trigger
        long depGwId;
        string depCode;
        await using (var db = new QuanLySanBaiContext(options))
        {
            depGwId = await MakeWalletSvc(db).CreateDepositQr(other, 150000m);
            depCode = (await db.GiaoDichCongThanhToans.SingleAsync(g => g.Id == depGwId)).MaDonHangCong;
        }
        await using (var db = new QuanLySanBaiContext(options))
        {
            var res = await MakeWebhookSvc(db).ProcessSePayWebhook(MakeWebhookJson(100013, 150000m, depCode), "Apikey SEPAY_TEST_API_KEY_999");
            var w = await db.ViNguoiDungs.SingleAsync(x => x.NguoiDungId == other);
            Check(res.ProcessingStatus == "HOP_LE_DA_XU_LY" && w.SoDuKhaDung == 150000m,
                "F02: Wallet deposit confirmed via SePay webhook");
        }
        var wOrderA = await Book(other, slot, 27);
        var wOrderB = await Book(other, midnightSlot, 27);
        var spendA = Task.Run(async () =>
        {
            try
            {
                await using var db = new QuanLySanBaiContext(options);
                await MakeWalletSvc(db).PayBookingWithWallet(other, new PayWalletForm { OrderId = wOrderA, Purpose = "THANH_TOAN", Key = Guid.NewGuid().ToString("N") });
                return true;
            }
            catch (BusinessException) { return false; }
        });
        var spendB = Task.Run(async () =>
        {
            try
            {
                await using var db = new QuanLySanBaiContext(options);
                await MakeWalletSvc(db).PayBookingWithWallet(other, new PayWalletForm { OrderId = wOrderB, Purpose = "THANH_TOAN", Key = Guid.NewGuid().ToString("N") });
                return true;
            }
            catch (BusinessException) { return false; }
        });
        var spendResults = await Task.WhenAll(spendA, spendB);
        await using (var db = new QuanLySanBaiContext(options))
        {
            var w = await db.ViNguoiDungs.SingleAsync(x => x.NguoiDungId == other);
            Check(spendResults.Count(x => x) == 1 && w.SoDuKhaDung == 150000m - 123456.78m && w.SoDuKhaDung >= 0,
                "F02: Concurrent wallet spends cannot overdraft balance");
        }
        await Reject(() => sql("UPDATE dbo.Giao_Dich_Vi SET so_tien = 1"), "F02: Immutable ledger trigger blocks update");

        // F03. Cancel with penalty fee & refund to wallet
        var paidWinnerOrder = spendResults[0] ? wOrderA : wOrderB;
        await using (var db = new QuanLySanBaiContext(options))
        {
            var ord = await db.DonDatSans.SingleAsync(d => d.Id == paidWinnerOrder);
            var rsvc = new RefundService(db, new AccessService(db), MakeWalletSvc(db), TimeProvider.System);
            var preview = await rsvc.PreviewCancel(other, paidWinnerOrder);
            // Day 27 is > 24 hours away -> test policy tier >= 24h has CancelPercent = 0 -> full refund 123456.78!
            await rsvc.CancelWithRefund(other, new OrderActionForm
            {
                Id = paidWinnerOrder,
                Version = Convert.ToBase64String(ord.RowVersion),
                Reason = "Đổi kế hoạch"
            });
            var w = await db.ViNguoiDungs.SingleAsync(x => x.NguoiDungId == other);
            Check(preview.RefundAmount == 123456.78m && w.SoDuKhaDung == 150000m &&
                  await scalar($"SELECT COUNT(*) FROM Hoan_Tien WHERE trang_thai='THANH_CONG' AND so_tien=123456.78") == 1 &&
                  await scalar($"SELECT COUNT(*) FROM Don_Dat_San WHERE id={paidWinnerOrder} AND trang_thai='DA_HUY'") == 1,
                "F03: Cancel paid order calculates policy refund, credits wallet, and releases slot");
        }

        // F04. Atomic Reschedule: policy AllowChange=false blocks, failure rolls back keeping old slot, success swaps and works with CheckIn/Complete
        var reschedBlockedOrder = await Book(other, slot, 28);
        await using (var db = new QuanLySanBaiContext(options))
        {
            await MakeWalletSvc(db).PayBookingWithWallet(other, new PayWalletForm { OrderId = reschedBlockedOrder, Purpose = "THANH_TOAN", Key = Guid.NewGuid().ToString("N") });
            var ord = await db.DonDatSans.SingleAsync(d => d.Id == reschedBlockedOrder);
            var rsvc = new RescheduleService(db, new AccessService(db), MakeWalletSvc(db));
            await Reject(() => rsvc.ExecuteReschedule(other, new RescheduleForm
            {
                OrderId = reschedBlockedOrder,
                Version = Convert.ToBase64String(ord.RowVersion),
                NewCourtId = court,
                NewSlotId = midnightSlot,
                NewDate = VietnamTime.Today.AddDays(28),
                PayExtraByWallet = true
            }), "F04: Reschedule blocked when facility policy disables AllowChange");
        }

        // Create a dedicated owner and facility with AllowChange=true policy for F04 reschedule & difference tests
        long f2Owner, f2, c2; int s2a, s2b;
        await using (var db = new QuanLySanBaiContext(options))
        {
            var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<NguoiDung>();
            var u2 = new NguoiDung { HoVaTen = "F2 Owner", SoDienThoai = "0988999888", TrangThai = true, NgayTao = VietnamTime.Now, NgayCapNhat = VietnamTime.Now };
            u2.MatKhauHash = hasher.HashPassword(u2, "Cluster4Test123!");
            u2.NguoiDungVaiTros.Add(new NguoiDungVaiTro { VaiTroId = (await db.VaiTros.SingleAsync(r => r.MaVaiTro == "KHACH_HANG")).Id, NgayGan = VietnamTime.Now });
            db.Add(u2);
            await db.SaveChangesAsync();
            f2Owner = u2.Id;

            var fsvc = new FacilityService(db);
            f2 = await fsvc.SaveFacility(f2Owner, new FacilityForm { Name = "Cơ sở Đổi Lịch", Address = "Hà Nội", Open = new(6, 0), Close = TimeOnly.MinValue });
            await sql($"UPDATE dbo.Co_So SET trang_thai_duyet = N'DA_DUYET' WHERE id = {f2}");
            db.ChangeTracker.Clear();
            var typeId2 = await db.LoaiSans.Select(t => t.Id).FirstAsync();
            await fsvc.SaveCourt(f2Owner, new CourtForm { FacilityId = f2, Name = "Sân Đổi 1", TypeId = typeId2 });
            c2 = await db.Sans.Where(s => s.CoSoId == f2).Select(s => s.Id).SingleAsync();
            await fsvc.AddSlot(f2Owner, new SlotForm { FacilityId = f2, Start = new(17, 0), End = new(18, 0) });
            await fsvc.AddSlot(f2Owner, new SlotForm { FacilityId = f2, Start = new(19, 0), End = new(20, 0) });
            var f2Slots = await db.KhungGioMacDinhs.Where(k => k.CoSoId == f2).OrderBy(k => k.GioBatDau).ToListAsync();
            s2a = f2Slots[0].Id; s2b = f2Slots[1].Id;
            for (int day = 1; day <= 7; day++)
            {
                await fsvc.AddPrice(f2Owner, new PriceForm { FacilityId = f2, CourtId = c2, SlotId = s2a, Weekday = day, Price = 100000m, Deposit = 50000m, From = VietnamTime.Today.AddDays(-10) });
                await fsvc.AddPrice(f2Owner, new PriceForm { FacilityId = f2, CourtId = c2, SlotId = s2b, Weekday = day, Price = 120000m, Deposit = 60000m, From = VietnamTime.Today.AddDays(-10) });
            }
            await fsvc.AddPolicy(f2Owner, new PolicyForm
            {
                FacilityId = f2,
                Name = "Chính sách cho đổi lịch",
                AllowChange = true,
                From = VietnamTime.Today,
                Fees = [new() { From = 0, Until = 24, CancelPercent = 50, ChangePercent = 10 }, new() { From = 24, CancelPercent = 0, ChangePercent = 0 }]
            });
        }
        // Top up wallet for F04 + F05 tests
        await using (var db = new QuanLySanBaiContext(options))
        {
            var topupGw = await MakeWalletSvc(db).CreateDepositQr(other, 200000m);
            var topupCode = (await db.GiaoDichCongThanhToans.SingleAsync(g => g.Id == topupGw)).MaDonHangCong;
            await MakeWebhookSvc(db).ProcessSePayWebhook(MakeWebhookJson(100014, 200000m, topupCode), "Apikey SEPAY_TEST_API_KEY_999");
        }
        long reschedOrder;
        await using (var db = new QuanLySanBaiContext(options))
        {
            var bsvc = new BookingService(db, bookingOpt);
            reschedOrder = await bsvc.Book(other, new BookingForm { CourtId = c2, SlotId = s2a, Date = VietnamTime.Today.AddDays(28) });
            await MakeWalletSvc(db).PayBookingWithWallet(other, new PayWalletForm { OrderId = reschedOrder, Purpose = "THANH_TOAN", Key = Guid.NewGuid().ToString("N") });
            // Customer books s2b on day 29 so rescheduling to occupied slot must fail and preserve old slot
            await bsvc.Book(customer, new BookingForm { CourtId = c2, SlotId = s2b, Date = VietnamTime.Today.AddDays(29) });
        }
        await using (var db = new QuanLySanBaiContext(options))
        {
            var ord = await db.DonDatSans.SingleAsync(d => d.Id == reschedOrder);
            var rsvc = new RescheduleService(db, new AccessService(db), MakeWalletSvc(db));
            await Reject(() => rsvc.ExecuteReschedule(other, new RescheduleForm
            {
                OrderId = reschedOrder,
                Version = Convert.ToBase64String(ord.RowVersion),
                NewCourtId = c2,
                NewSlotId = s2b,
                NewDate = VietnamTime.Today.AddDays(29),
                PayExtraByWallet = true
            }), "F04: Reschedule to taken slot rejected");
            Check(await scalar($"SELECT COUNT(*) FROM Chi_Tiet_Dat_San WHERE don_dat_san_id={reschedOrder} AND trang_thai_slot='DA_XAC_NHAN'") == 1,
                "F04: Failed reschedule preserves original slot");
        }
        // Now reschedule to free s2b (120,000đ vs old 100,000đ => +20,000đ paid from wallet!) on day 28 -> succeeds atomically & OperationService works on rescheduled order
        await using (var db = new QuanLySanBaiContext(options))
        {
            var ord = await db.DonDatSans.SingleAsync(d => d.Id == reschedOrder);
            var rsvc = new RescheduleService(db, new AccessService(db), MakeWalletSvc(db));
            await rsvc.ExecuteReschedule(other, new RescheduleForm
            {
                OrderId = reschedOrder,
                Version = Convert.ToBase64String(ord.RowVersion),
                NewCourtId = c2,
                NewSlotId = s2b,
                NewDate = VietnamTime.Today.AddDays(28),
                Reason = "Đổi sang khung tối",
                PayExtraByWallet = true
            });
            var updatedOrd = await db.DonDatSans.AsNoTracking().Include(d => d.ThanhToanDons).SingleAsync(d => d.Id == reschedOrder);
            Check(await scalar($"SELECT COUNT(*) FROM Chi_Tiet_Dat_San WHERE don_dat_san_id={reschedOrder} AND trang_thai_slot='DA_DOI'") == 1 &&
                  await scalar($"SELECT COUNT(*) FROM Chi_Tiet_Dat_San WHERE don_dat_san_id={reschedOrder} AND trang_thai_slot='DA_XAC_NHAN'") == 1 &&
                  await scalar($"SELECT COUNT(*) FROM Yeu_Cau_Doi_Lich WHERE trang_thai='DA_DUYET'") == 1 &&
                  updatedOrd.ThanhToanDons.Sum(p => p.SoTien) == 120000m,
                "F04: Atomic reschedule swaps slot, collects 20,000 difference from wallet, and marks old slot DA_DOI");
        }
        // Verify CheckIn works on rescheduled order!
        await using (var db = new QuanLySanBaiContext(options))
        {
            var ord = await db.DonDatSans.Include(d => d.ChiTietDatSans).SingleAsync(d => d.Id == reschedOrder);
            var active = ord.ChiTietDatSans.Single(c => c.TrangThaiSlot == "DA_XAC_NHAN");
            var start = VietnamTime.Start(active.NgaySuDung, active.GioBatDau);
            var op = new OperationService(db, new AccessService(db), new FinanceFixedClock(start), opOpt);
            await op.CheckIn(f2Owner, new OrderActionForm { Id = reschedOrder, Version = Convert.ToBase64String(ord.RowVersion), Code = ord.MaDatSan });
            Check(await scalar($"SELECT COUNT(*) FROM Don_Dat_San WHERE id={reschedOrder} AND trang_thai='DANG_SU_DUNG'") == 1,
                "F04 + O04: Check-in succeeds on rescheduled order");
        }

        // F05. Withdrawal hold, reject (release), and approve (deduct)
        long wdRejectId, wdApproveId;
        await using (var db = new QuanLySanBaiContext(options))
        {
            var ws = MakeWalletSvc(db);
            wdRejectId = await ws.RequestWithdrawal(other, new WithdrawForm { Amount = 10000m, BankName = "MB Bank", AccountNumber = "0123456789", AccountHolder = "OTHER TEST" });
            var w = await db.ViNguoiDungs.AsNoTracking().SingleAsync(x => x.NguoiDungId == other);
            Check(w.SoDuDongBang == 10000m, "F05: Withdrawal freezes funds");
        }
        await using (var db = new QuanLySanBaiContext(options))
        {
            var ws = MakeWalletSvc(db);
            await ws.ProcessWithdrawal(admin, new ProcessWithdrawalForm { RequestId = wdRejectId, Approve = false, RejectReason = "Sai tên chủ TK" });
            var w = await db.ViNguoiDungs.AsNoTracking().SingleAsync(x => x.NguoiDungId == other);
            Check(w.SoDuDongBang == 0m, "F05: Rejected withdrawal releases frozen funds back to available");

            wdApproveId = await ws.RequestWithdrawal(other, new WithdrawForm { Amount = 10000m, BankName = "MB Bank", AccountNumber = "0123456789", AccountHolder = "OTHER TEST" });
            await ws.ProcessWithdrawal(admin, new ProcessWithdrawalForm { RequestId = wdApproveId, Approve = true });
            await Reject(() => ws.ProcessWithdrawal(admin, new ProcessWithdrawalForm { RequestId = wdApproveId, Approve = true }), "F05: Double withdrawal approval rejected");
            w = await db.ViNguoiDungs.AsNoTracking().SingleAsync(x => x.NguoiDungId == other);
            Check(w.SoDuDongBang == 0m && await scalar($"SELECT COUNT(*) FROM Yeu_Cau_Rut_Tien WHERE id={wdApproveId} AND trang_thai='THANH_CONG'") == 1,
                "F05: Approved withdrawal deducts frozen funds once");
        }

        // F04/F03 Security: Bypass PayExtraByWallet=false rejected & reschedule-cheaper + cancel net-paid prevents double refund
        long secReschedOrder, cheaperOrder;
        decimal balBeforeSecRefund;
        await using (var db = new QuanLySanBaiContext(options))
        {
            var topupGw2 = await MakeWalletSvc(db).CreateDepositQr(other, 300000m);
            var topupCode2 = (await db.GiaoDichCongThanhToans.SingleAsync(g => g.Id == topupGw2)).MaDonHangCong;
            await MakeWebhookSvc(db).ProcessSePayWebhook(MakeWebhookJson(100099, 300000m, topupCode2), "Apikey SEPAY_TEST_API_KEY_999");

            var bsvc = new BookingService(db, bookingOpt);
            secReschedOrder = await bsvc.Book(other, new BookingForm { CourtId = c2, SlotId = s2a, Date = VietnamTime.Today.AddDays(25) });
            await MakeWalletSvc(db).PayBookingWithWallet(other, new PayWalletForm { OrderId = secReschedOrder, Purpose = "THANH_TOAN", Key = Guid.NewGuid().ToString("N") });

            cheaperOrder = await bsvc.Book(other, new BookingForm { CourtId = c2, SlotId = s2b, Date = VietnamTime.Today.AddDays(26) });
            await MakeWalletSvc(db).PayBookingWithWallet(other, new PayWalletForm { OrderId = cheaperOrder, Purpose = "THANH_TOAN", Key = Guid.NewGuid().ToString("N") });
            balBeforeSecRefund = (await db.ViNguoiDungs.AsNoTracking().SingleAsync(x => x.NguoiDungId == other)).SoDuKhaDung;
        }
        await using (var db = new QuanLySanBaiContext(options))
        {
            var ord = await db.DonDatSans.SingleAsync(d => d.Id == secReschedOrder);
            var rsvc = new RescheduleService(db, new AccessService(db), MakeWalletSvc(db));
            await Reject(() => rsvc.ExecuteReschedule(other, new RescheduleForm
            {
                OrderId = secReschedOrder,
                Version = Convert.ToBase64String(ord.RowVersion),
                NewCourtId = c2,
                NewSlotId = s2b,
                NewDate = VietnamTime.Today.AddDays(25),
                PayExtraByWallet = false
            }), "F04 Security: Reschedule to expensive slot with PayExtraByWallet=false rejected");
        }
        await using (var db = new QuanLySanBaiContext(options))
        {
            var ordCheaper = await db.DonDatSans.SingleAsync(d => d.Id == cheaperOrder);
            var rsvc = new RescheduleService(db, new AccessService(db), MakeWalletSvc(db));
            await rsvc.ExecuteReschedule(other, new RescheduleForm
            {
                OrderId = cheaperOrder,
                Version = Convert.ToBase64String(ordCheaper.RowVersion),
                NewCourtId = c2,
                NewSlotId = s2a,
                NewDate = VietnamTime.Today.AddDays(26),
                Reason = "Đổi xuống giờ rẻ hơn",
                PayExtraByWallet = true
            });

            var balAfterResched = (await db.ViNguoiDungs.AsNoTracking().SingleAsync(x => x.NguoiDungId == other)).SoDuKhaDung;
            var refundSvc = new RefundService(db, new AccessService(db), MakeWalletSvc(db), TimeProvider.System);
            var cancelPreview = await refundSvc.PreviewCancel(other, cheaperOrder);
            var ordUpdated = await db.DonDatSans.SingleAsync(d => d.Id == cheaperOrder);
            await refundSvc.CancelWithRefund(other, new OrderActionForm
            {
                Id = cheaperOrder,
                Version = Convert.ToBase64String(ordUpdated.RowVersion),
                Reason = "Hủy sau khi đổi xuống giờ rẻ hơn"
            });

            var balAfterCancel = (await db.ViNguoiDungs.AsNoTracking().SingleAsync(x => x.NguoiDungId == other)).SoDuKhaDung;
            Check(balAfterResched == balBeforeSecRefund + 20000m &&
                  cancelPreview.TotalPaid == 100000m &&
                  cancelPreview.RefundAmount == 100000m &&
                  balAfterCancel == balBeforeSecRefund + 120000m,
                "F04 + F03 Security: Reschedule cheaper refunds difference once and cancel only refunds remaining net paid");
        }

        // C04/A03: Price weekday range validation, bulk weekdays creation, and QUAN_LY_GIA staff permission
        await using (var db = new QuanLySanBaiContext(options))
        {
            var fsvc = new FacilityService(db);
            // 2026-11-17 (Tue) to 2026-11-19 (Thu) has no Monday (1) -> must reject dead price!
            await Reject(() => fsvc.AddPrice(f2Owner, new PriceForm
            {
                FacilityId = f2,
                CourtId = c2,
                SlotId = s2a,
                Weekday = 1,
                Price = 50000m,
                Deposit = 0m,
                From = new DateOnly(2026, 11, 17),
                Until = new DateOnly(2026, 11, 19)
            }), "C04: Dead price row (weekday outside date range) rejected");

            // Grant QUAN_LY_GIA to other on f2 and verify staff can add bulk weekdays price with ReplaceExisting=true
            var m = new ThanhVienCoSo { CoSoId = f2, NguoiDungId = other, VaiTroNoiBo = "NHAN_VIEN", TrangThaiHoatDong = true, NgayThamGia = VietnamTime.Now };
            m.PhanQuyenThanhViens.Add(new PhanQuyenThanhVien { QuyenHanId = (await db.QuyenHans.SingleAsync(q => q.MaQuyen == "QUAN_LY_GIA")).Id });
            db.ThanhVienCoSos.Add(m);
            await db.SaveChangesAsync();

            await fsvc.AddPrice(other, new PriceForm
            {
                FacilityId = f2,
                CourtId = c2,
                SlotId = s2a,
                Weekdays = [1, 2, 3, 4, 5, 6, 7],
                ReplaceExisting = true,
                Price = 80000m,
                Deposit = 20000m,
                From = VietnamTime.Today,
                Until = VietnamTime.Today.AddDays(5)
            });
            Check(await scalar($"SELECT COUNT(*) FROM dbo.Bang_Gia WHERE san_id={c2} AND khung_gio_id={s2a} AND dang_ap_dung=1 AND gia_thue=80000") > 0,
                "C04/A03: Staff with QUAN_LY_GIA can bulk-update prices across weekdays");

            // Clean up staff membership on f2 so other remains clean for later Program.cs tests
            db.PhanQuyenThanhViens.RemoveRange(m.PhanQuyenThanhViens);
            db.ThanhVienCoSos.Remove(m);
            await db.SaveChangesAsync();
        }

        return count;
    }

    private sealed class FinanceFixedClock(DateTime vietnamNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(vietnamNow.AddHours(-7), DateTimeKind.Utc));
    }
}
