using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuanLySanBai.Data;
using QuanLySanBai.Models.Entities;
using QuanLySanBai.Services;
using QuanLySanBai.ViewModels;
using Microsoft.Data.SqlClient;
public static class OperationChecks
{
 public static async Task<int> Run(DbContextOptions<QuanLySanBaiContext> options,long owner,long customer,long other,long court,int slot,long facility, Func<string,Task> sql)
 {
 int count=0;void Check(bool ok,string name){if(!ok)throw new Exception("FAIL: "+name);count++;Console.WriteLine("PASS: "+name);}
 async Task Reject(Func<Task> work,string name){try{await work();}catch(Exception e) when(e is BusinessException or FacilityAccessException or DbUpdateException){Check(true,name);return;}throw new Exception("FAIL: "+name);}
 async Task<long> Book(int days){await using var db=new QuanLySanBaiContext(options);return await new BookingService(db,Options.Create(new BookingOptions())).Book(customer,new(){CourtId=court,SlotId=slot,Date=VietnamTime.Today.AddDays(days)});}
 async Task<DonDatSan> Read(long id){await using var db=new QuanLySanBaiContext(options);return await db.DonDatSans.AsNoTracking().Include(d=>d.ThanhToanDons).Include(d=>d.ChiTietDatSans).SingleAsync(d=>d.Id==id);}
 async Task Invoke(Func<OperationService,Task> work,DateTime? now=null){await using var db=new QuanLySanBaiContext(options);await work(new OperationService(db,new AccessService(db),now.HasValue?new FixedClock(now.Value):TimeProvider.System,Options.Create(new OperationOptions())));}
 async Task<CollectForm> Form(long id,decimal amount){var d=await Read(id);return new(){Id=id,Version=Convert.ToBase64String(d.RowVersion),Amount=amount};}
 async Task<OrderActionForm> Action(long id){var d=await Read(id);return new(){Id=id,Version=Convert.ToBase64String(d.RowVersion)};}
 var id=await Book(10);var first=await Form(id,50000.25m);
 await Reject(()=>Invoke(s=>s.Collect(other,first)),"O02: customer cannot collect");
 var low=await Form(id,1);await Reject(()=>Invoke(s=>s.Collect(owner,low)),"O03: insufficient first deposit rejected atomically");
 await Task.WhenAll(Invoke(s=>s.Collect(owner,first)),Invoke(s=>s.Collect(owner,first)));
 var d=await Read(id);Check(d.ThanhToanDons.Count==1 && d.TrangThai=="DA_XAC_NHAN" && d.ChiTietDatSans.Single().TrangThaiSlot=="DA_XAC_NHAN","O02/O03: duplicate concurrent collect exactly once and atomic confirmation");
 var wrong=await Form(id,1);wrong.Key=first.Key;await Reject(()=>Invoke(s=>s.Collect(owner,wrong)),"O02: reused key with changed payload rejected");
 var over=await Form(id,999999);await Reject(()=>Invoke(s=>s.Collect(owner,over)),"O02: overpayment rejected");
 var stale=await Form(id,1);stale.Version=first.Version;await Reject(()=>Invoke(s=>s.Collect(owner,stale)),"O03: stale form rejected");
 var cancel=await Action(id);cancel.Reason="Test";await Reject(()=>Invoke(s=>s.Cancel(customer,cancel)),"O05: paid order requires refund workflow");
 var start=VietnamTime.Start(d.ChiTietDatSans.Single().NgaySuDung,d.ChiTietDatSans.Single().GioBatDau);var end=VietnamTime.End(d.ChiTietDatSans.Single().NgaySuDung,d.ChiTietDatSans.Single().GioKetThuc);
 var ci=await Action(id);ci.Code=d.MaDatSan;
 await Reject(()=>Invoke(s=>s.CheckIn(owner,ci),start.AddMinutes(-16)),"O04: too early check-in rejected");
 var bad=await Action(id);bad.Code="wrong";await Reject(()=>Invoke(s=>s.CheckIn(owner,bad),start),"O04: wrong check-in code rejected");
 await Invoke(s=>s.CheckIn(owner,ci),start.AddMinutes(-10));
 Check((await Read(id)).TrangThai=="DANG_SU_DUNG","O04: check-in updates order and slot");
 var repeat=await Action(id);repeat.Code=d.MaDatSan;await Reject(()=>Invoke(s=>s.CheckIn(owner,repeat),start),"O04: check-in cannot repeat");
 await Reject(()=>Invoke(async s=>await s.Complete(owner,await Action(id)),end),"O04: unpaid balance prevents completion");
 var balance=await Form(id,73456.53m);balance.Purpose="BO_SUNG";await Invoke(s=>s.Collect(owner,balance));
 await Reject(()=>Invoke(async s=>await s.Complete(owner,await Action(id)),end.AddMinutes(-1)),"O04: cannot finish before end");
 await Invoke(async s=>await s.Complete(owner,await Action(id)),end);
 d=await Read(id);Check(d.TrangThai=="HOAN_THANH" && d.ChiTietDatSans.Single().TrangThaiSlot=="HOAN_THANH" && d.ThanhToanDons.Sum(p=>p.SoTien)==123456.78m,"Q02: book collect check-in balance complete survives reload");
 await Reject(()=>Invoke(async s=>await s.Complete(owner,await Action(id)),end),"O04: completion cannot repeat");
 var unpaid=await Book(11);var uf=await Action(unpaid);uf.Reason="Khách đổi kế hoạch";
 await Reject(()=>Invoke(s=>s.Cancel(other,uf)),"O05: stranger cannot cancel");await Invoke(s=>s.Cancel(customer,uf));
 d=await Read(unpaid);Check(d.TrangThai=="DA_HUY" && d.ChiTietDatSans.Single().TrangThaiSlot=="DA_HUY","O05: unpaid cancellation releases slot");
 await using(var db=new QuanLySanBaiContext(options)){Check(await db.LichSuHuySans.AnyAsync(h=>h.ChiTietDatSanId==d.ChiTietDatSans.Single().Id),"O05: cancellation audit persists");Check(await db.LichSuCheckIns.CountAsync(h=>h.ChiTietDatSan.DonDatSanId==id)==1,"O04: exactly one check-in history");}
 var expired=await Book(12);var ef=await Form(expired,50000.25m);await sql($"UPDATE Don_Dat_San SET thoi_gian_het_han_giu_cho=DATEADD(SECOND,-1,DATEADD(HOUR,7,SYSUTCDATETIME())) WHERE id={expired}");
 var collectRace=Task.Run(async()=>{try{await Invoke(s=>s.Collect(owner,ef));return true;}catch(BusinessException){return false;}});
 await using(var db=new QuanLySanBaiContext(options)){await new BookingService(db,Options.Create(new BookingOptions())).Expire();}
 Check(!await collectRace,"Q01: expiry wins, late cash not recorded");
 d=await Read(expired);Check(d.TrangThai=="HET_HAN" && d.ThanhToanDons.Count==0,"Q01: expired order has no accepted payment");
 var winner=await Book(13);var wf=await Form(winner,50000.25m);await Invoke(s=>s.Collect(owner,wf));await sql($"UPDATE Don_Dat_San SET thoi_gian_het_han_giu_cho=DATEADD(SECOND,-1,DATEADD(HOUR,7,SYSUTCDATETIME())) WHERE id={winner}");await using(var db=new QuanLySanBaiContext(options)){await new BookingService(db,Options.Create(new BookingOptions())).Expire();}
 Check((await Read(winner)).TrangThai=="DA_XAC_NHAN","Q01: payment wins, expiry preserves confirmed slot");
 var cashA=await Form(winner,40000m);var cashB=await Form(winner,40000m);
 async Task<bool> Try(Func<Task> action){try{await action();return true;}catch(BusinessException){return false;}}
 var parallel=await Task.WhenAll(Try(()=>Invoke(s=>s.Collect(owner,cashA))),Try(()=>Invoke(s=>s.Collect(owner,cashB))));
 Check(parallel.Count(x=>x)==1 && (await Read(winner)).ThanhToanDons.Sum(p=>p.SoTien)==90000.25m,"Q01: two different concurrent payments cannot reuse stale balance/version");
 var cancelRaceId=await Book(15);var cancelRace=await Action(cancelRaceId);cancelRace.Reason="Race test";var racePay=await Form(cancelRaceId,50000.25m);
 await Task.WhenAll(Try(()=>Invoke(s=>s.Cancel(customer,cancelRace))),Try(()=>Invoke(s=>s.Collect(owner,racePay))));
 var raceResult=await Read(cancelRaceId);
 Check(raceResult.TrangThai=="DA_HUY" && raceResult.ThanhToanDons.Count==0 || raceResult.TrangThai=="DA_XAC_NHAN" && raceResult.ThanhToanDons.Count==1,"Q01: cancel versus cash remains consistent");
 await Reject(()=>Invoke(s=>s.Counter(other,new(){FacilityId=facility,CourtId=court,SlotId=slot,Date=VietnamTime.Today.AddDays(14),Name="Walk-in",Phone="0988776655"})),"O01: unauthorized counter booking denied");
 long counter=0;await Invoke(async s=>counter=await s.Counter(owner,new(){FacilityId=facility,CourtId=court,SlotId=slot,Date=VietnamTime.Today.AddDays(14),Name="Walk-in",Phone="0988776655"}));
 d=await Read(counter);Check(d.NguonDat=="TAI_QUAY" && d.NhanVienTaoId==owner && d.NguoiDungId==null,"O01: walk-in identity and staff recorded");
 return count;
 }
 private sealed class FixedClock(DateTime vn):TimeProvider {public override DateTimeOffset GetUtcNow()=>new(DateTime.SpecifyKind(vn.AddHours(-7),DateTimeKind.Utc));}
}


