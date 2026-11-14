using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuanLySanBai.Data;
using QuanLySanBai.Models.Entities;
using QuanLySanBai.Services;
using QuanLySanBai.ViewModels;

var root=Path.GetFullPath(args.FirstOrDefault() ?? Directory.GetCurrentDirectory());
if(args.Contains("--audit-live")) { await LiveAudit.Run(root); return; }
var database="QuanLySanBai_Cluster4Tests_"+Guid.NewGuid().ToString("N");
var master="Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True";
var connection=new SqlConnectionStringBuilder(master) { InitialCatalog=database }.ConnectionString;
var options=new DbContextOptionsBuilder<QuanLySanBaiContext>().UseSqlServer(connection).Options;
var settings=Options.Create(new BookingOptions());
var count=0;
async Task SendSocket(System.Net.WebSockets.ClientWebSocket socket,string message) => await socket.SendAsync(new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(message+"\u001e")),System.Net.WebSockets.WebSocketMessageType.Text,true,CancellationToken.None);
async Task<string> ReadSocket(System.Net.WebSockets.ClientWebSocket socket,string expected)
{
    using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(10));
    var buffer=new byte[65536]; var text="";
    while(!text.Contains(expected)) {
        var result=await socket.ReceiveAsync(new ArraySegment<byte>(buffer),timeout.Token);
        if(result.MessageType==System.Net.WebSockets.WebSocketMessageType.Close) throw new Exception("Realtime connection closed unexpectedly");
        text+=System.Text.Encoding.UTF8.GetString(buffer,0,result.Count);
    }
    return text;
}
async Task<System.Net.WebSockets.ClientWebSocket> OpenSocket(Uri baseUri,CookieContainer? cookies=null)
{
    var socket=new System.Net.WebSockets.ClientWebSocket();
    if(cookies is not null) socket.Options.Cookies=cookies;
    await socket.ConnectAsync(new UriBuilder(baseUri) { Scheme="ws",Path="/updates" }.Uri,CancellationToken.None);
    await SendSocket(socket,"{\"protocol\":\"json\",\"version\":1}");
    await ReadSocket(socket,"{}"); return socket;
}
void Check(bool ok,string name) { if(!ok) throw new Exception("FAIL: "+name); count++; Console.WriteLine("PASS: "+name); }
async Task Reject(Func<Task> operation,string name) { try { await operation(); } catch(Exception e) when(e is BusinessException or FacilityAccessException or SqlException or DbUpdateException) { Check(true,name); return; } throw new Exception("FAIL: "+name); }
async Task Sql(string sql) { await using var c=new SqlConnection(connection); await c.OpenAsync(); await using var command=new SqlCommand(sql,c); await command.ExecuteNonQueryAsync(); }
async Task<int> Scalar(string sql) { await using var c=new SqlConnection(connection); await c.OpenAsync(); await using var command=new SqlCommand(sql,c); return Convert.ToInt32(await command.ExecuteScalarAsync()); }
async Task<T> With<T>(Func<QuanLySanBaiContext,Task<T>> work) { await using var db=new QuanLySanBaiContext(options); return await work(db); }
async Task Run(Func<QuanLySanBaiContext,Task> work) { await using var db=new QuanLySanBaiContext(options); await work(db); }
Process? server=null;
try
{
    var script=File.ReadAllText(Path.Combine(root,"Data","QuanLySanBai_SQLServer.sql")).Replace("[QuanLySanBai]","["+database+"]").Replace("N'QuanLySanBai'","N'"+database+"'");
    await using(var c=new SqlConnection(master)) { await c.OpenAsync(); foreach(var batch in Regex.Split(script,@"^GO\s*$",RegexOptions.Multiline|RegexOptions.IgnoreCase)) if(!string.IsNullOrWhiteSpace(batch)) { await using var command=new SqlCommand(batch,c) { CommandTimeout=120 }; await command.ExecuteNonQueryAsync(); } }
    Check(await Scalar("SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0")==48,"N01: 48 tables installed");
    Check(await Scalar("SELECT COUNT(*) FROM sys.triggers WHERE parent_class=1 AND is_ms_shipped=0")==13,"N01: 13 triggers installed");
    Check(await Scalar("SELECT COUNT(*) FROM sys.procedures WHERE is_ms_shipped=0")==3,"N01: 3 stored procedures installed");
    var hasher=new PasswordHasher<NguoiDung>();
    async Task<long> User(string phone,string role) => await With(async db=> { var u=new NguoiDung { HoVaTen=role,SoDienThoai=phone,TrangThai=true,NgayTao=VietnamTime.Now,NgayCapNhat=VietnamTime.Now }; u.MatKhauHash=hasher.HashPassword(u,"Cluster4Test123!"); u.NguoiDungVaiTros.Add(new NguoiDungVaiTro { VaiTroId=(await db.VaiTros.SingleAsync(r=>r.MaVaiTro==role)).Id,NgayGan=VietnamTime.Now }); db.Add(u); await db.SaveChangesAsync(); return u.Id; });
    var owner=await User("0988000001","KHACH_HANG"); var customer=await User("0988000002","KHACH_HANG"); var other=await User("0988000003","KHACH_HANG"); var admin=await User("0988000004","ADMIN");
    var facility=await With(db=>new FacilityService(db).SaveFacility(owner,new FacilityForm { Name="Cơ sở kiểm thử",Address="Hồ Chí Minh",Open=new(6,0),Close=TimeOnly.MinValue }));
    Check(await Scalar($"SELECT COUNT(*) FROM Co_So WHERE id={facility} AND trang_thai_duyet=N'CHO_DUYET'")==1,"C01: facility starts pending");
    await With(async db => {
        var access=new AccessService(db);
        var member=new ThanhVienCoSo { CoSoId=facility,NguoiDungId=customer,VaiTroNoiBo="NHAN_VIEN",TrangThaiHoatDong=true,NgayThamGia=VietnamTime.Now };
        db.ThanhVienCoSos.Add(member); await db.SaveChangesAsync();
        var calendar=await db.QuyenHans.SingleAsync(q=>q.MaQuyen=="XEM_LICH");
        var management=await db.QuyenHans.SingleAsync(q=>q.MaQuyen=="QUAN_LY_THANH_VIEN");
        foreach(var role in new[] { "NHAN_VIEN", "QUAN_LY", "CHU_SAN" }) {
            member.VaiTroNoiBo=role; await db.SaveChangesAsync();
            Check(!await access.CanManage(customer,facility) && !await access.HasPermission(customer,facility,"XEM_LICH"),"A03: member role alone grants no owner privileges: "+role);
        }
        member.VaiTroNoiBo="QUAN_LY";
        var granted=new PhanQuyenThanhVien { QuyenHanId=calendar.Id };
        member.PhanQuyenThanhViens.Add(granted); await db.SaveChangesAsync();
        Check(await access.HasPermission(customer,facility,"XEM_LICH") && !await access.CanManage(customer,facility),"A03: manager only has explicitly granted calendar permission");
        var delegated=new PhanQuyenThanhVien { QuyenHanId=management.Id };
        member.PhanQuyenThanhViens.Add(delegated); await db.SaveChangesAsync();
        Check(await access.CanManage(customer,facility),"A03: explicit member management grant works");
        db.PhanQuyenThanhViens.Remove(delegated); await db.SaveChangesAsync();
        Check(!await access.CanManage(customer,facility),"A03: manager permission revoked without changing role");
        member.TrangThaiHoatDong=false; await db.SaveChangesAsync();
        Check(!await access.HasPermission(customer,facility,"XEM_LICH"),"A03: inactive membership denies existing grant");
        Check(await access.CanManage(owner,facility) && await access.HasPermission(owner,facility,"XEM_LICH"),"A03: actual facility owner retains access");
        Check(!await access.CanManage(owner,facility+99999) && !await access.HasPermission(other,facility,"XEM_LICH"),"A03: unrelated users and foreign facility denied");
        db.PhanQuyenThanhViens.Remove(granted); db.ThanhVienCoSos.Remove(member); await db.SaveChangesAsync();
        return true;
    });
    Check(await Scalar($"SELECT COUNT(*) FROM Thanh_Vien_Co_So WHERE co_so_id={facility} AND nguoi_dung_id={owner} AND vai_tro_noi_bo='CHU_SAN'")==1,"C01: owner membership created atomically");
    Check(await Scalar($"SELECT COUNT(*) FROM Nguoi_Dung_Vai_Tro n JOIN Vai_Tro v ON v.id=n.vai_tro_id WHERE n.nguoi_dung_id={owner} AND v.ma_vai_tro='CHU_SAN'")==1,"C01: owner global role linked");
    await Reject(()=>Run(db=>new FacilityService(db).SaveFacility(other,new FacilityForm { Id=facility,Name="Attack",Address="Attack" })),"A03/C01: cross-owner editing rejected");
    await Run(async db=> { var type=new LoaiSan { MonTheThaoId=(await db.MonTheThaos.FirstAsync()).Id,TenLoai="Loại test",SucChuaNguoi=4 }; db.Add(type); await db.SaveChangesAsync(); });
    var typeId=await With(db=>db.LoaiSans.Select(t=>t.Id).SingleAsync());
    await Run(db=>new FacilityService(db).SaveCourt(owner,new CourtForm { FacilityId=facility,Name="Sân 1",TypeId=typeId }));
    var court=await With(db=>db.Sans.Select(s=>s.Id).SingleAsync());
    Check((await With(db=>new BookingService(db,settings).Calendar(court,VietnamTime.Today.AddDays(1))))==null,"B01: pending facility hidden");
    await Sql($"UPDATE Co_So SET trang_thai_duyet='DA_DUYET' WHERE id={facility}");
    await Run(db=>new FacilityService(db).AddSlot(owner,new SlotForm { FacilityId=facility,Start=new(18,0),End=new(19,0) }));
    await Run(db=>new FacilityService(db).AddSlot(owner,new SlotForm { FacilityId=facility,Start=new(23,0),End=TimeOnly.MinValue }));
    await Reject(()=>Run(db=>new FacilityService(db).AddSlot(owner,new SlotForm { FacilityId=facility,Start=new(18,30),End=new(19,30) })),"C03: overlapping slots rejected by SQL");
    await Reject(()=>Run(db=>new FacilityService(db).AddSlot(owner,new SlotForm { FacilityId=facility,Start=new(20,0),End=new(19,0) })),"C03: backwards time rejected");
    var slots=await With(db=>db.KhungGioMacDinhs.OrderBy(k=>k.GioBatDau).ToListAsync()); var slot=slots[0].Id; var midnight=slots[1].Id;
    for(int day=1;day<=7;day++) foreach(var s in slots) await Run(db=>new FacilityService(db).AddPrice(owner,new PriceForm { FacilityId=facility,CourtId=court,SlotId=s.Id,Weekday=day,Price=123456.78m,Deposit=50000.25m,From=VietnamTime.Today.AddDays(-30) }));
    await Reject(()=>Run(db=>new FacilityService(db).AddPrice(owner,new PriceForm { FacilityId=facility,CourtId=court,SlotId=slot,Weekday=1,Price=100,Deposit=101 })),"C04: deposit above price rejected");
    await Reject(()=>Run(db=>new FacilityService(db).AddPrice(owner,new PriceForm { FacilityId=facility,CourtId=court,SlotId=slot,Weekday=1,Price=100,Deposit=10 })),"C04: overlapping price effective ranges rejected");
    await Reject(()=>Run(db=>new FacilityService(db).AddPolicy(owner,new PolicyForm { FacilityId=facility,Name="Bad",Fees=[new() { From=0,Until=2 },new() { From=3 }] })),"C05: policy fee gap rejected");
    await Run(db=>new FacilityService(db).AddPolicy(owner,new PolicyForm { FacilityId=facility,Name="Chính sách test",From=VietnamTime.Today,Fees=[new() { From=0,Until=24,CancelPercent=50,ChangePercent=20 },new() { From=24,CancelPercent=0,ChangePercent=0 }] }));
    Check(await Scalar("SELECT COUNT(*) FROM Muc_Phi_Huy_Doi")==2,"C05/N03: policy and fee writes compatible with triggers");
    await Reject(()=>Run(db=>new FacilityService(db).AddPrice(owner,new PriceForm { FacilityId=facility,CourtId=court,SlotId=slot,Weekday=1,Price=1.001m })),"C04: fractional currency beyond precision rejected");
    await Reject(()=>Run(db=>new FacilityService(db).AddPrice(other,new PriceForm { FacilityId=facility,CourtId=court,SlotId=slot,Weekday=1,Price=100 })),"C04: foreign owner cannot set prices");
    var sportId=await With(db=>db.LoaiSans.Where(t=>t.Id==typeId).Select(t=>t.MonTheThaoId).SingleAsync());
    await Sql($"UPDATE Mon_The_Thao SET trang_thai=0 WHERE id={sportId}");
    await Reject(async()=>{ await With(db=>new BookingService(db,settings).Book(customer,new BookingForm { CourtId=court,SlotId=slot,Date=VietnamTime.Today.AddDays(1) })); },"C02/B03: inactive sport cannot be booked via forged POST");
    await Sql($"UPDATE Mon_The_Thao SET trang_thai=1 WHERE id={sportId}");
    var tomorrow=VietnamTime.Today.AddDays(1);
    var policyId=await With(db=>db.ChinhSachHuyDois.Select(p=>p.Id).SingleAsync());
    await Sql($"UPDATE Chinh_Sach_Huy_Doi SET ngay_hieu_luc_tu=DATEADD(DAY,1,CONVERT(date,DATEADD(HOUR,7,SYSUTCDATETIME()))) WHERE id={policyId}");
    await Reject(async()=>{ await With(db=>new BookingService(db,settings).Book(customer,new BookingForm { CourtId=court,SlotId=slot,Date=tomorrow })); },"C05/B03: missing current policy blocks booking");
    await Sql($"UPDATE Chinh_Sach_Huy_Doi SET ngay_hieu_luc_tu=CONVERT(date,DATEADD(HOUR,7,SYSUTCDATETIME())) WHERE id={policyId}");
    var calendar=await With(db=>new BookingService(db,settings).Calendar(court,tomorrow));
    Check(calendar!.Slots.All(s=>s.CanBook) && calendar.Slots[0].Price==123456.78m,"B02/C04: available slots and exact decimal price");
    Check(VietnamTime.Weekday(new DateOnly(2026,11,9))==1 && VietnamTime.End(tomorrow,TimeOnly.MinValue)==tomorrow.AddDays(1).ToDateTime(TimeOnly.MinValue),"B05: Monday=1 and midnight is next day");
    async Task<long> Book(long actor,int k,DateOnly day) => await With(db=>new BookingService(db,settings).Book(actor,new BookingForm { CourtId=court,SlotId=k,Date=day }));
    await Reject(async()=>{ await Book(customer,slot,VietnamTime.Today.AddDays(-1)); },"B05: past booking rejected");
    await Run(db=>new FacilityService(db).SaveHours(owner,new HoursForm { FacilityId=facility,Weekday=VietnamTime.Weekday(tomorrow),Closed=true }));
    Check(!(await With(db=>new BookingService(db,settings).Calendar(court,tomorrow)))!.Slots.Any(k=>k.CanBook),"C03/B02: closed weekday visible");
    await Reject(async()=>{ await Book(customer,slot,tomorrow); },"B05: closed weekday booking rejected");
    await Run(db=>new FacilityService(db).SaveHours(owner,new HoursForm { FacilityId=facility,Weekday=VietnamTime.Weekday(tomorrow),Open=new(6,0),Close=TimeOnly.MinValue }));
    await Run(db=>new FacilityService(db).SaveCourt(owner,new CourtForm { Id=court,FacilityId=facility,Name="Sân 1",TypeId=typeId,Maintenance=true }));
    await Reject(async()=>{ await Book(customer,slot,tomorrow); },"B05: maintenance booking rejected");
    await Run(db=>new FacilityService(db).SaveCourt(owner,new CourtForm { Id=court,FacilityId=facility,Name="Sân 1",TypeId=typeId }));
    await Run(db=>new FacilityService(db).AddLock(owner,new LockForm { FacilityId=facility,CourtId=court,Start=tomorrow.ToDateTime(new(18,0)),End=tomorrow.ToDateTime(new(19,0)),Reason="Bảo trì test" }));
    Check((await With(db=>new BookingService(db,settings).Calendar(court,tomorrow)))!.Slots[0].Status=="Khóa / bảo trì","C06/B02: lock displayed");
    await Reject(async()=>{ await Book(customer,slot,tomorrow); },"C06: lock blocks booking");
    var lockId=await With(db=>db.LichKhoaSans.Select(l=>l.Id).SingleAsync()); await Run(db=>new FacilityService(db).CancelLock(owner,facility,lockId));
    var before=await Scalar("SELECT COUNT(*) FROM Don_Dat_San");
    var attempts=await Task.WhenAll(new[] {customer,other}.Select(async actor=> { try { return await Book(actor,slot,tomorrow); } catch(SqlException) { return 0L; } }));
    Check(attempts.Count(id=>id>0)==1,"B05: concurrent requests exactly one success");
    Check(await Scalar("SELECT COUNT(*) FROM Don_Dat_San")==before+1 && await Scalar("SELECT COUNT(*) FROM Don_Dat_San d WHERE NOT EXISTS(SELECT 1 FROM Chi_Tiet_Dat_San c WHERE c.don_dat_san_id=d.id)")==0,"B05: failed booking leaves no orphan order");
    var order=attempts.Single(id=>id>0);
    var stored=await With(db=>db.DonDatSans.AsNoTracking().SingleAsync(d=>d.Id==order));
    Check(stored.TongTienPhaiTra==123456.78m && stored.TienCocYeuCau==50000.25m,"N03/B03: computed total and deposit loaded correctly");
    Check((await With(db=>new BookingService(db,settings).Calendar(court,tomorrow)))!.Slots[0].Status=="Đang giữ chỗ","B02: valid hold visible");
    var snapshotPrice=await With(db=>db.ChiTietDatSans.Select(c=>c.BangGiaId).SingleAsync());
    await Reject(()=>Run(async db=> { var p=await db.BangGia.FindAsync(snapshotPrice); p!.GiaThue=1; await db.SaveChangesAsync(); }),"C04: used price immutable");
    await Reject(()=>Run(async db=> { var f=await db.MucPhiHuyDois.FirstAsync(); f.TiLePhiHuy=1; await db.SaveChangesAsync(); }),"C05: used fee terms immutable");
    await Reject(()=>Run(db=>new FacilityService(db).AddLock(owner,new LockForm { FacilityId=facility,CourtId=court,Start=tomorrow.ToDateTime(new(18,0)),End=tomorrow.ToDateTime(new(19,0)),Reason="conflict" })),"C06: lock cannot overwrite active booking");
    await Sql($"UPDATE Don_Dat_San SET thoi_gian_het_han_giu_cho=DATEADD(MINUTE,-1,DATEADD(HOUR,7,SYSUTCDATETIME())) WHERE id={order}");
    await Run(db=>new BookingService(db,settings).Expire()); await Run(db=>new BookingService(db,settings).Expire());
    Check(await Scalar($"SELECT COUNT(*) FROM Don_Dat_San d JOIN Chi_Tiet_Dat_San c ON c.don_dat_san_id=d.id WHERE d.id={order} AND d.trang_thai='HET_HAN' AND c.trang_thai_slot='HET_HAN'")==1,"B04: expiry idempotent, order and slot consistent");
    Check((await With(db=>new BookingService(db,settings).Calendar(court,tomorrow)))!.Slots[0].CanBook,"B04: expired slot available again");
    var midnightOrder=await Book(customer,midnight,tomorrow); Check(midnightOrder>0,"B05: 23:00 to 24:00 booking succeeds");
    // Start actual MVC with the disposable DB to exercise auth, CSRF, binding, errors and views.
    var listener=new System.Net.Sockets.TcpListener(IPAddress.Loopback,0); listener.Start(); var port=((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
    var start=new ProcessStartInfo("dotnet") { UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=root };
    start.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory,"QuanLySanBai.dll")); start.ArgumentList.Add("--contentRoot"); start.ArgumentList.Add(Path.Combine(root,"QuanLySanBai")); start.ArgumentList.Add("--urls"); start.ArgumentList.Add("http://127.0.0.1:"+port);
    start.Environment["ConnectionStrings__DefaultConnection"]=connection; start.Environment["ASPNETCORE_ENVIRONMENT"]="Development"; start.Environment["Booking__ExpiryIntervalSeconds"]="5";
    server=Process.Start(start)!; var log=new System.Collections.Concurrent.ConcurrentQueue<string>(); server.OutputDataReceived+=(_,e)=>{if(e.Data!=null)log.Enqueue(e.Data);}; server.ErrorDataReceived+=(_,e)=>{if(e.Data!=null)log.Enqueue(e.Data);}; server.BeginOutputReadLine(); server.BeginErrorReadLine();
    var handler=new HttpClientHandler { AllowAutoRedirect=false,CookieContainer=new CookieContainer() };
    using var client=new HttpClient(handler) { BaseAddress=new Uri("http://127.0.0.1:"+port) };
    bool ready=false; for(int i=0;i<60;i++) { try { if((await client.GetAsync("/Account/Login")).StatusCode==HttpStatusCode.OK) { ready=true; break; } } catch(HttpRequestException) { } await Task.Delay(250); }
    Check(ready,"HTTP server starts");
    async Task<HttpResponseMessage> Post(string action,Dictionary<string,string> values,string tokenPath)
    {
        var html=await client.GetStringAsync(tokenPath); var token=Regex.Match(html,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        Check(token.Length>0,"HTTP token: "+action); values["__RequestVerificationToken"]=WebUtility.HtmlDecode(token); return await client.PostAsync(action,new FormUrlEncodedContent(values));
    }
    Check((int)(await client.GetAsync("/Courts")).StatusCode==200,"B01: public search renders");
    var detail="/Courts/Details/"+court+"?date="+tomorrow.ToString("yyyy-MM-dd");
    Check((int)(await client.GetAsync(detail)).StatusCode==200,"B02: calendar renders");
    Check((int)(await client.PostAsync("/Bookings/Create",new FormUrlEncodedContent(new Dictionary<string,string>()))).StatusCode is 302 or 400,"B03: anonymous POST rejected");
    var response=await Post("/Account/Login",new() { ["Login"]="0988000002",["Password"]="Cluster4Test123!" },"/Account/Login"); Check((int)response.StatusCode==302,"HTTP customer login");
    var customerHtml=WebUtility.HtmlDecode(await client.GetStringAsync("/Courts"));
    Check(!customerHtml.Contains("href=\"/Admin") && !customerHtml.Contains("href=\"/CoSo\""),"UI: customer menu hides admin and staff tools");
    Check(customerHtml.Contains("Lịch đặt của tôi") && customerHtml.Contains("Đăng ký cơ sở kinh doanh"),"UI: customer sees bookings and secondary business signup");
    Check((int)(await client.GetAsync("/Bookings/Details/"+midnightOrder)).StatusCode==200,"B04: own order view renders");
    var privateOrder=await Book(owner,slot,tomorrow.AddDays(3));
    Check((int)(await client.GetAsync("/Bookings/Details/"+privateOrder)).StatusCode==404,"B04: unrelated order always hidden");
    var winner=stored.NguoiDungId;
    if(winner!=customer) Check((int)(await client.GetAsync("/Bookings/Details/"+order)).StatusCode==404,"B04: other customer's order hidden");
    Check((int)(await client.GetAsync("/CoSo/Configuration/Index/"+facility)).StatusCode==302,"A03/C01: non-owner configuration forbidden");
    using var calendarSocket=await OpenSocket(client.BaseAddress!);
    await SendSocket(calendarSocket,$"{{\"type\":1,\"invocationId\":\"watch\",\"target\":\"WatchCourt\",\"arguments\":[{court}]}}");
    var subscription=await ReadSocket(calendarSocket,"\"invocationId\":\"watch\"");
    Check(!subscription.Contains("error"),"Realtime: public court subscription accepted");
    response=await Post("/Bookings/Create",new() { ["CourtId"]=court.ToString(),["SlotId"]=slot.ToString(),["Date"]=tomorrow.AddDays(2).ToString("yyyy-MM-dd"),["Price"]="1",["NguoiDungId"]=owner.ToString() },detail);
    Check((await ReadSocket(calendarSocket,"CalendarChanged")).Contains("CalendarChanged"),"Realtime: committed HTTP booking notifies court viewers");
    Check((int)response.StatusCode==302,"B03: HTTP booking redirects to created order");
    var createdId=long.Parse(response.Headers.Location!.ToString().Split('/').Last());
    var created=await With(db=>db.DonDatSans.SingleAsync(d=>d.Id==createdId)); Check(created.NguoiDungId==customer && created.TongTienPhaiTra==123456.78m,"B03: forged price and identity ignored");
    await Sql($"UPDATE Don_Dat_San SET thoi_gian_het_han_giu_cho=DATEADD(MINUTE,-1,DATEADD(HOUR,7,SYSUTCDATETIME())) WHERE id={createdId}");
    for(int i=0;i<20 && await Scalar($"SELECT COUNT(*) FROM Don_Dat_San WHERE id={createdId} AND trang_thai='HET_HAN'")==0;i++) await Task.Delay(500);
    Check(await Scalar($"SELECT COUNT(*) FROM Don_Dat_San WHERE id={createdId} AND trang_thai='HET_HAN'")==1,"B04: background worker releases expired holds");
    Check((await ReadSocket(calendarSocket,"CalendarChanged")).Contains("CalendarChanged"),"Realtime: expired hold notifies court viewers");
    response=await Post("/Bookings/Create",new() { ["CourtId"]=court.ToString(),["SlotId"]=slot.ToString(),["Date"]=VietnamTime.Today.AddDays(-1).ToString("yyyy-MM-dd") },detail);
    Check((int)response.StatusCode==302 && response.Headers.Location!.ToString().StartsWith("/Courts/Details/"),"N04: invalid booking redirects back to calendar");
    var error=await client.GetStringAsync(response.Headers.Location);
    Check(!error.Contains("SqlException") && !error.Contains("Server=(localdb)") && error.Contains("alert"),"N04: redirected business error is safely displayed");
    await Post("/Account/Logout",new(),"/Account/Profile");
    await Post("/Account/Login",new() { ["Login"]="0988000001",["Password"]="Cluster4Test123!" },"/Account/Login");
    var ownerHtml=WebUtility.HtmlDecode(await client.GetStringAsync("/CoSo"));
    Check(ownerHtml.Contains("Quản lý sân và cấu hình") && ownerHtml.Contains("Quản lý nhân viên") && !ownerHtml.Contains("href=\"/Admin"),"UI: owner sees own management actions");
    foreach(var path in new[] {"/CoSo/Configuration/Index/"+facility,"/CoSo/Configuration/Facility/"+facility,"/CoSo/Configuration/Court?facilityId="+facility,"/CoSo/Configuration/Policy?facilityId="+facility}) { var r=await client.GetAsync(path); if((int)r.StatusCode!=200) Console.WriteLine(await r.Content.ReadAsStringAsync()); Check((int)r.StatusCode==200,"C01-C06 view renders "+path); }
    await With(async db => {
        var member=new ThanhVienCoSo { CoSoId=facility,NguoiDungId=other,VaiTroNoiBo="QUAN_LY",TrangThaiHoatDong=true,NgayThamGia=VietnamTime.Now };
        member.PhanQuyenThanhViens.Add(new PhanQuyenThanhVien { QuyenHanId=(await db.QuyenHans.SingleAsync(q=>q.MaQuyen=="QUAN_LY_THANH_VIEN")).Id });
        db.ThanhVienCoSos.Add(member); await db.SaveChangesAsync(); return true;
    });
    await Post("/Account/Logout",new(),"/Account/Profile");
    await Post("/Account/Login",new() { ["Login"]="0988000003",["Password"]="Cluster4Test123!" },"/Account/Login");
    var membersPath="/CoSo/Members?coSoId="+facility;
    Check((int)(await client.GetAsync(membersPath)).StatusCode==200,"A03 HTTP: explicitly granted manager can open members");
    await With(async db => {
        var grants=await db.PhanQuyenThanhViens.Where(p=>p.ThanhVienCoSo.CoSoId==facility && p.ThanhVienCoSo.NguoiDungId==other).ToListAsync();
        db.RemoveRange(grants); await db.SaveChangesAsync(); return true;
    });
    Check(!WebUtility.HtmlDecode(await client.GetStringAsync("/CoSo")).Contains("Quản lý nhân viên"),"A03 HTTP: revoked manager menu hidden in same session");
    response=await client.GetAsync(membersPath);
    Check((int)response.StatusCode==302 && response.Headers.Location!.ToString().Contains("/Account/Denied"),"A03 HTTP: direct members URL denied after revocation");
    response=await Post("/CoSo/Members/Add",new() { ["coSoId"]=facility.ToString(),["phone"]="0988000002" },"/Account/Profile");
    Check((int)response.StatusCode==302 && response.Headers.Location!.ToString().Contains("/Account/Denied"),"A03 HTTP: forged member POST denied after revocation");
    using var staffSocket=await OpenSocket(client.BaseAddress!,handler.CookieContainer);
    using var unrelatedSocket=await OpenSocket(client.BaseAddress!);
    var staffMemberId=await Scalar($"SELECT id FROM Thanh_Vien_Co_So WHERE co_so_id={facility} AND nguoi_dung_id={other}");
    var managementId=await Scalar("SELECT id FROM Quyen_Han WHERE ma_quyen='QUAN_LY_THANH_VIEN'");
    await Post("/Account/Logout",new(),"/Account/Profile");
    await Post("/Account/Login",new() { ["Login"]="0988000001",["Password"]="Cluster4Test123!" },"/Account/Login");
    var staffFields=new Dictionary<string,string> { ["coSoId"]=facility.ToString(),["memberId"]=staffMemberId.ToString(),["vaiTro"]="QUAN_LY",["active"]="true",["permissions"]=managementId.ToString() };
    await Post("/CoSo/Members/Update",staffFields,membersPath);
    Check((await ReadSocket(staffSocket,"PermissionsChanged")).Contains("PermissionsChanged"),"Realtime: permission grant targets existing staff session");
    staffFields.Remove("permissions");
    await Post("/CoSo/Members/Update",staffFields,membersPath);
    Check((await ReadSocket(staffSocket,"PermissionsChanged")).Contains("PermissionsChanged"),"Realtime: permission revocation targets existing staff session");
    try { await ReadSocket(unrelatedSocket,"PermissionsChanged"); throw new Exception("Permission notification leaked to unrelated connection"); }
    catch(OperationCanceledException) { Check(true,"Realtime: unrelated connection receives no permission notification"); }
    await With(async db => {
        db.ThanhVienCoSos.Remove(await db.ThanhVienCoSos.SingleAsync(m=>m.CoSoId==facility && m.NguoiDungId==other));
        await db.SaveChangesAsync(); return true;
    });
    await Post("/Account/Logout",new(),"/Account/Profile"); await Post("/Account/Login",new() { ["Login"]="0988000004",["Password"]="Cluster4Test123!" },"/Account/Login");
    var adminHtml=WebUtility.HtmlDecode(await client.GetStringAsync("/Admin"));
    Check(adminHtml.Contains("Quản trị viên") && !adminHtml.Contains("href=\"/Courts\"") && !adminHtml.Contains("href=\"/Bookings\"") && !adminHtml.Contains("href=\"/CoSo\""),"UI: admin sees management navigation only");
    Check(Regex.Matches(adminHtml,"aria-label=\"Điều hướng chính\"").Count==1,"UI: exactly one primary navigation");
    Check((int)(await client.GetAsync("/")).StatusCode==302 && (await client.GetAsync("/")).Headers.Location!.ToString().Contains("Admin"),"UI: admin lands in admin area");
    Check((int)(await client.GetAsync("/Admin/Catalog")).StatusCode==200,"C02: admin catalog renders");
    response=await Post("/Admin/Catalog/Sport",new() { ["Id"]=sportId.ToString(),["Code"]="TEST_UPDATED",["Name"]="Test sport",["Active"]="false" },"/Admin/Catalog");
    Check((int)response.StatusCode==302 && await Scalar($"SELECT COUNT(*) FROM Mon_The_Thao WHERE id={sportId} AND trang_thai=0")==1,"C02: admin deactivates sport through form");
    await Sql($"UPDATE Mon_The_Thao SET trang_thai=1 WHERE id={sportId}");
    await Post("/Account/Logout",new(),"/Account/Profile");
    await Run(async db=> { db.ThanhVienCoSos.Add(new ThanhVienCoSo { CoSoId=facility,NguoiDungId=other,VaiTroNoiBo="NHAN_VIEN",TrangThaiHoatDong=true,NgayThamGia=VietnamTime.Now }); await db.SaveChangesAsync(); });
    await Post("/Account/Login",new() { ["Login"]="0988000003",["Password"]="Cluster4Test123!" },"/Account/Login");
    var employeeHtml=WebUtility.HtmlDecode(await client.GetStringAsync("/CoSo"));
    Check(employeeHtml.Contains("Cơ sở làm việc") && !employeeHtml.Contains("Quản lý sân và cấu hình") && !employeeHtml.Contains(">Quản lý nhân viên</a>") && !employeeHtml.Contains(">Thêm cơ sở</a>"),"UI: staff sees no owner or ungranted member actions");
    Check((int)(await client.GetAsync("/CoSo/Members?coSoId="+facility)).StatusCode==302,"UI: hidden staff action also denied by server");
    await Run(async db=> { var seed=new DevelopmentSeeder(db,hasher); await seed.Seed(); await seed.Seed(); });
    Check(await Scalar("SELECT COUNT(*) FROM Nguoi_Dung WHERE email='owner@qlsb.example.test'")==1,"C06: development seed idempotent");
    Console.WriteLine($"ALL {count} CHECKS PASSED. Disposable database: {database}");
}
finally
{
    if(server!=null && !server.HasExited) { server.Kill(true); await server.WaitForExitAsync(); }
    SqlConnection.ClearAllPools();
    if(!Regex.IsMatch(database,@"^QuanLySanBai_Cluster4Tests_[a-f0-9]{32}$")) throw new Exception("Unsafe test database name");
    await using var cleanup=new SqlConnection(master); await cleanup.OpenAsync(); await using var drop=new SqlCommand($"IF DB_ID(N'{database}') IS NOT NULL BEGIN ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}]; END",cleanup); await drop.ExecuteNonQueryAsync();
    Console.WriteLine("Disposable test database removed.");
}
