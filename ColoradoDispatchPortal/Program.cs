using ColoradoDispatchPortal.Data;
using ColoradoDispatchPortal.Mapping;
using ColoradoDispatchPortal.Repositories;
using ColoradoDispatchPortal.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<DispatchPortalContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DispatchPortal")));
builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);
builder.Services.AddScoped<ISelfDispatchRepo, SelfDispatchRepo>();
builder.Services.AddScoped<DemoAccessService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dispatch}/{action=Dashboard}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DispatchPortalContext>();
    await db.Database.EnsureCreatedAsync();
    await DemoDataSeeder.SeedAsync(db);
}

app.Run();

public partial class Program { }
