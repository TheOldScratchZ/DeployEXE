using EXE.Data;
using EXE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EXE.Controllers
{
    [Authorize(Roles = "Staff,Admin")]
    public class StaffController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StaffController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Staff/Orders
        public async Task<IActionResult> Orders()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Include(o => o.Payment)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders); // Views/Staff/Orders.cshtml
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(Guid id, int status)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            // Nếu chuyển từ "Đang xử lý" sang "Đã hủy" => Cộng lại tồn kho
            if (order.Status == 0 && status == 2)
            {
                foreach (var item in order.OrderItems)
                {
                    item.Product.Stock += item.Quantity;
                }
            }

            order.Status = status;
            order.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật trạng thái thành công!";
            return RedirectToAction("Orders");
        }

        public IActionResult ViewOrder(Guid id)
        {
            var order = _context.Orders
                .Include(o => o.OrderItems) // Nếu có bảng OrderItems
                .ThenInclude(oi => oi.Product) // Nếu cần thông tin sản phẩm
                .FirstOrDefault(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }
    }
}
