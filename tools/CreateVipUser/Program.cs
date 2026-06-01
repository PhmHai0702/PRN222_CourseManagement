using CourseManagement.DataAccess.Data;
using CourseManagement.Model.Constant;
using CourseManagement.Model.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

const string connectionString = "Server=localhost\\MSSQLSERVER02;Database=FUNewsManagement;User Id=YOUR_DB_USER;Password=YOUR_DB_PASSWORD;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True";
const string email = "vip@vip.com";
const string password = "Vip123.";

var services = new ServiceCollection();
services.AddDbContext<CourseManagementDb>(options => options.UseSqlServer(connectionString));
services
    .AddIdentityCore<AppUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<CourseManagementDb>();

await using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();

var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
var db = scope.ServiceProvider.GetRequiredService<CourseManagementDb>();

var userRole = await roleManager.FindByNameAsync(Role.Role_User_Customer);
if (userRole == null)
{
    userRole = new IdentityRole
    {
        Name = Role.Role_User_Customer,
        NormalizedName = Role.Role_User_Customer.ToUpperInvariant()
    };

    var roleResult = await roleManager.CreateAsync(userRole);
    if (!roleResult.Succeeded)
    {
        throw new InvalidOperationException(string.Join("; ", roleResult.Errors.Select(e => e.Description)));
    }
}

var vipExpirationDate = DateTime.Now.AddYears(1);
var user = await userManager.FindByEmailAsync(email);
if (user == null)
{
    user = new AppUser
    {
        FullName = "VIP User",
        UserName = email,
        NormalizedUserName = email.ToUpperInvariant(),
        Email = email,
        NormalizedEmail = email.ToUpperInvariant(),
        EmailConfirmed = true,
        LockoutEnabled = true,
        VipStatus = VipStatus.Premium,
        VipExpirationDate = vipExpirationDate
    };

    var createResult = await userManager.CreateAsync(user, password);
    if (!createResult.Succeeded)
    {
        throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(e => e.Description)));
    }
}
else
{
    user.FullName = string.IsNullOrWhiteSpace(user.FullName) ? "VIP User" : user.FullName;
    user.EmailConfirmed = true;
    user.VipStatus = VipStatus.Premium;
    user.VipExpirationDate = vipExpirationDate;
    user.PasswordHash = new PasswordHasher<AppUser>().HashPassword(user, password);
    user.SecurityStamp = Guid.NewGuid().ToString();

    var updateResult = await userManager.UpdateAsync(user);
    if (!updateResult.Succeeded)
    {
        throw new InvalidOperationException(string.Join("; ", updateResult.Errors.Select(e => e.Description)));
    }
}

if (!await userManager.IsInRoleAsync(user, Role.Role_User_Customer))
{
    var addRoleResult = await userManager.AddToRoleAsync(user, Role.Role_User_Customer);
    if (!addRoleResult.Succeeded)
    {
        throw new InvalidOperationException(string.Join("; ", addRoleResult.Errors.Select(e => e.Description)));
    }
}

var savedUser = await db.Users
    .Where(u => u.Email == email)
    .Select(u => new { u.Email, u.FullName, u.VipStatus, u.VipExpirationDate })
    .SingleAsync();

Console.WriteLine($"VIP account ready: {savedUser.Email}");
Console.WriteLine($"Password: {password}");
Console.WriteLine($"Status: {savedUser.VipStatus}");
Console.WriteLine($"Expires: {savedUser.VipExpirationDate:yyyy-MM-dd HH:mm:ss}");
