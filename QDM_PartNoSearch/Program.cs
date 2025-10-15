using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QDM_PartNoSearch.Models;
using QDM_PartNoSearch.Services;
using Serilog;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// 配置 Serilog，將日誌輸出到控制台和檔案
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console() // 日誌也會顯示在控制台
    .WriteTo.File("Logs/app_log.txt", rollingInterval: RollingInterval.Day) // 日誌會寫入到 Logs 目錄，並以日期為單位分割
    .CreateLogger();

// 使用 Serilog 作為 ASP.NET Core 的日誌提供者
builder.Logging.ClearProviders(); // 清除其他日誌提供者
builder.Logging.AddSerilog(); // 加入 Serilog 日誌提供者

// 全局禁用證書驗證（僅在開發環境中使用）
ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

// 配置 HttpClient
builder.Services.AddHttpClient("NoCertValidationClient")
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        return new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, certificate, chain, sslPolicyErrors) => true
        };
    });

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<Flavor2Context>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("Flavor2Context")));
builder.Services.AddDbContext<GSTContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("GSTContext")));
builder.Services.AddDbContext<DeanContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DeanContext")));

builder.Services.AddHostedService<TokenRefreshService>();
builder.Services.AddMemoryCache(); // 添加內存緩存服務
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=HomePage}/{id?}");

app.Run();
