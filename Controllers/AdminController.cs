using EXE.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EXE.Controllers
{
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;

        public AdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole<Guid>> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: /Admin/StaffList
        public async Task<IActionResult> StaffList()
        {
            var staffUsers = await _userManager.GetUsersInRoleAsync("Staff");
            return View(staffUsers.ToList());
        }

        // POST: /Admin/CreateStaff
        [HttpPost]
        public async Task<IActionResult> CreateStaff(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Vui lòng nhập email.";
                return RedirectToAction("StaffList");
            }

            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                TempData["Error"] = "Email này đã được sử dụng.";
                return RedirectToAction("StaffList");
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            string defaultPassword = "12345678"; // đảm bảo khớp với yêu cầu mật khẩu đã cấu hình

            var result = await _userManager.CreateAsync(user, defaultPassword);
            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(" | ", result.Errors.Select(e => e.Description));
                return RedirectToAction("StaffList");
            }

            // Tạo role "Staff" nếu chưa có
            if (!await _roleManager.RoleExistsAsync("Staff"))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>("Staff"));
            }

            // Gán user vào role "Staff"
            await _userManager.AddToRoleAsync(user, "Staff");

            TempData["Success"] = $"Tạo tài khoản nhân viên cho '{email}' thành công!";
            return RedirectToAction("StaffList");
        }
    }
}
