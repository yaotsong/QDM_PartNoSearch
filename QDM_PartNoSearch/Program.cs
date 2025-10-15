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

// �t�m Serilog�A�N��x��X�챱��x�M�ɮ�
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console() // ��x�]�|��ܦb����x
    .WriteTo.File("Logs/app_log.txt", rollingInterval: RollingInterval.Day) // ��x�|�g�J�� Logs �ؿ��A�åH�����������
    .CreateLogger();

// �ϥ� Serilog �@�� ASP.NET Core ����x���Ѫ�
builder.Logging.ClearProviders(); // �M����L��x���Ѫ�
builder.Logging.AddSerilog(); // �[�J Serilog ��x���Ѫ�

// �����T���Ү����ҡ]�Ȧb�}�o���Ҥ��ϥΡ^
ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

// �t�m HttpClient
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
builder.Services.AddMemoryCache(); // �K�[���s�w�s�A��
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
