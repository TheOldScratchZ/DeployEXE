using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EXE.Data;
using EXE.Models;

[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        var roles = await _userManager.GetRolesAsync(user);
        var isAdmin = roles.Contains("Admin");
        var isStaff = roles.Contains("Staff");

        ViewBag.IsAdmin = isAdmin;
        ViewBag.IsStaff = isStaff;

        if (isAdmin)
        {
            // Doanh thu theo tháng (cho biểu đồ chính)
            var revenueData = await _context.Orders
                .Where(o => o.Status == 1)
                .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Total = g.Sum(o => o.Total)
                })
                .OrderBy(g => g.Year).ThenBy(g => g.Month)
                .ToListAsync();
            ViewBag.RevenueData = revenueData;

            // Doanh thu 7 ngày gần nhất
            var last7Days = DateTime.Now.AddDays(-6).Date;
            var dailyRevenue = await _context.Orders
                .Where(o => o.Status == 1 && o.CreatedAt.Date >= last7Days)
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Total = g.Sum(o => o.Total)
                })
                .OrderBy(g => g.Date)
                .ToListAsync();
            ViewBag.DailyRevenue = dailyRevenue;

            // Doanh thu tuần này
            var startOfWeek = DateTime.Now.AddDays(-(int)DateTime.Now.DayOfWeek).Date;
            var weeklyRevenue = await _context.Orders
                .Where(o => o.Status == 1 && o.CreatedAt.Date >= startOfWeek)
                .SumAsync(o => o.Total);
            ViewBag.WeeklyRevenue = weeklyRevenue;

            // Doanh thu hôm nay
            var todayRevenue = await _context.Orders
                .Where(o => o.Status == 1 && o.CreatedAt.Date == DateTime.Now.Date)
                .SumAsync(o => o.Total);
            ViewBag.TodayRevenue = todayRevenue;

            // Thống kê sản phẩm
            ViewBag.TotalProducts = await _context.Products.Where(p => p.DeletedAt == null).CountAsync();
            ViewBag.TotalStock = await _context.Products.Where(p => p.DeletedAt == null).SumAsync(p => p.Stock);
            ViewBag.LowStockProducts = await _context.Products.Where(p => p.DeletedAt == null && p.Stock < 10).CountAsync();

            // Thống kê đơn hàng
            ViewBag.TotalOrders = await _context.Orders.CountAsync();
            ViewBag.PendingOrders = await _context.Orders.CountAsync(o => o.Status == 0);
            ViewBag.CompletedOrders = await _context.Orders.CountAsync(o => o.Status == 1);
            ViewBag.CancelledOrders = await _context.Orders.CountAsync(o => o.Status == 2);

            // Sản phẩm bán chạy nhất
            var topProducts = await _context.OrderItems
                .Include(oi => oi.Product)
                .Where(oi => oi.Order.Status == 1)
                .GroupBy(oi => new { oi.ProductId, oi.Product.Name })
                .Select(g => new
                {
                    ProductName = g.Key.Name,
                    TotalSold = g.Sum(oi => oi.Quantity)
                })
                .OrderByDescending(g => g.TotalSold)
                .Take(5)
                .ToListAsync();
            ViewBag.TopProducts = topProducts;
        }

        if (isStaff)
        {
            // Doanh thu 7 ngày gần nhất cho staff
            var last7Days = DateTime.Now.AddDays(-6).Date;
            var dailyRevenue = await _context.Orders
                .Where(o => o.Status == 1 && o.CreatedAt.Date >= last7Days)
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Total = g.Sum(o => o.Total)
                })
                .OrderBy(g => g.Date)
                .ToListAsync();
            ViewBag.DailyRevenue = dailyRevenue;

            // Doanh thu tuần này
            var startOfWeek = DateTime.Now.AddDays(-(int)DateTime.Now.DayOfWeek).Date;
            var weeklyRevenue = await _context.Orders
                .Where(o => o.Status == 1 && o.CreatedAt.Date >= startOfWeek)
                .SumAsync(o => o.Total);
            ViewBag.WeeklyRevenue = weeklyRevenue;

            // Doanh thu hôm nay
            var todayRevenue = await _context.Orders
                .Where(o => o.Status == 1 && o.CreatedAt.Date == DateTime.Now.Date)
                .SumAsync(o => o.Total);
            ViewBag.TodayRevenue = todayRevenue;

            // Thống kê cơ bản
            ViewBag.BlogCount = await _context.Blogs.CountAsync(b => b.DeletedAt == null);
            ViewBag.ProductCount = await _context.Products.CountAsync(p => p.DeletedAt == null);
            ViewBag.OrderPending = await _context.Orders.CountAsync(o => o.Status == 0);
            ViewBag.OrderCompleted = await _context.Orders.CountAsync(o => o.Status == 1);
        }

        return View();
    }
}