using EXE.Data;
using EXE.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EXE.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Thêm sản phẩm vào giỏ
        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> AddToCart(Guid productId, int quantity = 1)
        {
            if (!User.Identity.IsAuthenticated)
                return Redirect("/Identity/Account/Login");

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var product = await _context.Products.FindAsync(productId);
            if (product == null || product.DeletedAt != null) return NotFound();

            if (quantity <= 0)
            {
                TempData["ToastError"] = "Số lượng không hợp lệ.";
                return RedirectToAction("Shop", "Product");
            }

            // Tìm hoặc tạo mới giỏ hàng
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (cart == null)
            {
                cart = new Cart
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    CreatedAt = DateTime.Now,
                };
                _context.Carts.Add(cart);
                user.CartId = cart.Id;
                await _context.SaveChangesAsync();
            }

            // Kiểm tra nếu sản phẩm đã có trong giỏ
            var existingCartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
            int currentQuantity = existingCartItem?.Quantity ?? 0;

            // Kiểm tra tổng số lượng yêu cầu so với tồn kho
            if (currentQuantity + quantity > product.Stock)
            {
                TempData["ToastError"] = $"Không thể thêm vượt quá số lượng tồn kho {product.Stock}." +
                    $"Hiện bạn có {currentQuantity} sản phẩm trong giỏ hàng." +
                    $"Bạn chỉ có thể thêm tối đa {product.Stock - currentQuantity} sản phẩm";
                return RedirectToAction("Shop", "Product");
            }

            if (existingCartItem != null)
            {
                existingCartItem.Quantity += quantity;
                existingCartItem.Price = product.Price * existingCartItem.Quantity;
                _context.CartItems.Update(existingCartItem);
            }
            else
            {
                var newCartItem = new CartItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    Quantity = quantity,
                    Price = product.Price * quantity,
                    CartId = cart.Id
                };
                _context.CartItems.Add(newCartItem);
            }

            // Cập nhật lại TotalPrice
            cart.TotalPrice = cart.CartItems.Sum(ci => ci.Price);
            _context.Carts.Update(cart);

            await _context.SaveChangesAsync();
            TempData["ToastMessage"] = "Thêm sản phẩm vào giỏ hàng thành công!";
            return RedirectToAction("Shop", "Product");
        }

        // Hiển thị giỏ hàng
        public async Task<IActionResult> Index()
        {
            if (!User.Identity.IsAuthenticated)
                return Redirect("/Identity/Account/Login");

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            return View(cart);
        }
        private async Task UpdateCartTotal(Guid cartId)
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.Id == cartId);

            if (cart != null)
            {
                cart.TotalPrice = cart.CartItems.Sum(ci => ci.Price);
                _context.Carts.Update(cart);
                await _context.SaveChangesAsync();
            }
        }

        [HttpPost]
        public async Task<IActionResult> IncreaseQuantity(Guid cartItemId)
        {
            var cartItem = await _context.CartItems
                .Include(ci => ci.Product)
                .FirstOrDefaultAsync(ci => ci.Id == cartItemId);

            if (cartItem == null) return NotFound();

            // ✅ Không cho tăng vượt quá tồn kho
            if (cartItem.Quantity >= cartItem.Product.Stock)
            {
                TempData["ToastError"] = "Không thể thêm vì số lượng tồn kho không đủ!";
                return RedirectToAction("Index");
            }

            cartItem.Quantity++;
            cartItem.Price = cartItem.Quantity * cartItem.Product.Price;
            _context.CartItems.Update(cartItem);
            await _context.SaveChangesAsync();

            await UpdateCartTotal(cartItem.CartId.Value);

            return RedirectToAction("Index");
        }


        [HttpPost]
        public async Task<IActionResult> DecreaseQuantity(Guid cartItemId)
        {
            var cartItem = await _context.CartItems
                .Include(ci => ci.Product)
                .FirstOrDefaultAsync(ci => ci.Id == cartItemId);

            if (cartItem == null) return NotFound();

            if (cartItem.Quantity > 1)
            {
                cartItem.Quantity--;
                cartItem.Price = cartItem.Quantity * cartItem.Product.Price;
                _context.CartItems.Update(cartItem);
            }
            else
            {
                _context.CartItems.Remove(cartItem);
            }

            await _context.SaveChangesAsync();
            await UpdateCartTotal(cartItem.CartId.Value);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> RemoveItem(Guid cartItemId)
        {
            var cartItem = await _context.CartItems.FirstOrDefaultAsync(ci => ci.Id == cartItemId);
            if (cartItem == null)
            {
                TempData["ToastError"] = "Không tìm thấy sản phẩm trong giỏ hàng!";
                return RedirectToAction("Index");
            }

            var cartId = cartItem.CartId;

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            if (cartId.HasValue)
                await UpdateCartTotal(cartId.Value);

            TempData["ToastMessage"] = "Đã xóa sản phẩm khỏi giỏ hàng!";
            return RedirectToAction("Index");
        }

    }
}
