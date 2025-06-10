using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using EXE.Data;
using EXE.Models;
using EXE.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.  
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Configure Entity Framework Core with SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
   options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Configure Identity services
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
   {
       options.SignIn.RequireConfirmedEmail = false;
       options.User.RequireUniqueEmail = true;
       options.SignIn.RequireConfirmedAccount = false;
       options.Password.RequireDigit = false;
       options.Password.RequireLowercase = false;
       options.Password.RequireUppercase = false;
       options.Password.RequireNonAlphanumeric = false;
       options.Password.RequiredLength = 8;
       options.Password.RequiredUniqueChars = 1;
   })
   .AddEntityFrameworkStores<ApplicationDbContext>()
   .AddDefaultTokenProviders();

// Configure Identity options
//builder.Services.AddAuthentication()
//   .AddGoogle(googleOptions =>
//   {
//       googleOptions.ClientId = Environment.GetEnvironmentVariable("ENV_GOOGLE_CLIENT_ID") ?? throw new InvalidOperationException("ENV_GOOGLE_CLIENT_ID not found.");
//       googleOptions.ClientSecret = Environment.GetEnvironmentVariable("ENV_GOOGLE_CLIENT_SECRET") ?? throw new InvalidOperationException("ENV_GOOGLE_CLIENT_SECRET not found.");
//   });


builder.Services.AddSingleton<IEmailSender, EmailSender>();
builder.Services.AddScoped<CartService>();

var app = builder.Build();

// Configure the HTTP request pipeline.  
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.  
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
   name: "default",
   pattern: "{controller=Home}/{action=Index}/{id?}")
   .WithStaticAssets();
app.MapControllerRoute(
   name: "blog-edit",
   pattern: "Blog/Edit/{id:guid}",
   defaults: new { controller = "Blog", action = "EditBlog" });

app.MapControllerRoute(
   name: "blog-delete",
   pattern: "Blog/Delete/{id:guid}",
   defaults: new { controller = "Blog", action = "DeleteBlog" });
app.MapRazorPages()
  .WithStaticAssets();

// Seed roles and admin user  
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    // Create roles  
    var roles = new[] { "Customer", "Staff", "Admin" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }
    }

    // Create admin user  
    var adminEmail = configuration["Admin:Email"];
    var adminPassword = configuration["Admin:Password"];

    if (!string.IsNullOrEmpty(adminEmail) && !string.IsNullOrEmpty(adminPassword))
    {
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser { UserName = "ADMIN", Email = adminEmail, EmailConfirmed = true };
            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
    }
    var staffEmail = configuration["Staff:Email"];
    var staffPassword = configuration["Staff:Password"];
    if (!string.IsNullOrEmpty(staffEmail) && !string.IsNullOrEmpty(staffPassword))
    {
        var staffUser = await userManager.FindByEmailAsync(staffEmail);
        if (staffUser == null)
        {
            staffUser = new ApplicationUser { UserName = "STAFF", Email = staffEmail, EmailConfirmed = true };
            var result = await userManager.CreateAsync(staffUser, staffPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(staffUser, "Staff");
            }
        }
    }
}

app.Run();
