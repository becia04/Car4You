using Car4You.Controllers;
using Car4You.DAL;
using Car4You.Data;
using Car4You.Helper;
using Car4You.Models;
using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 🔧 Ścieżka do natywnej biblioteki
var context = new CustomAssemblyLoadContext();
context.LoadUnmanagedLibrary(Path.Combine(Directory.GetCurrentDirectory(), "DinkToPdf", "libwkhtmltox.dll"));

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<CarDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DB")));
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DB")));
builder.Services.AddScoped<PhotoUploadHelper>();
builder.Services.AddScoped<ViewRenderService>();
builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 100 * 1024 * 1024; // 100 MB
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 100 * 1024 * 1024; // 100 MB
});


// Dodanie Identity
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = false;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();



var app = builder.Build();


var scope = app.Services.CreateScope();
var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

// Utwórz rolę Admin, jeśli nie istnieje
if (!await roleManager.RoleExistsAsync("Admin"))
{
    await roleManager.CreateAsync(new IdentityRole("Admin"));
}

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
