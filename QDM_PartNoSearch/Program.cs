using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QDM_PartNoSearch.Models;
using QDM_PartNoSearch.Services;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddDbContext<Flavor2Context>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DBContext")));
builder.Services.AddMemoryCache(); // 添加內存緩存服務
builder.Services.AddHostedService<TokenRefreshService>();

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
