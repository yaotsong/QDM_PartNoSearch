using Microsoft.EntityFrameworkCore;
using QDM_PartNoSearch.Models;
using QDM_PartNoSearch.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<Flavor2Context>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DBContext")));
// �K�[�A�Ȩ�e����
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache(); // �K�[���s�w�s�A��

// ���U TokenRefreshService
builder.Services.AddHostedService<TokenRefreshService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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
