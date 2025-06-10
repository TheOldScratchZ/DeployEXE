using EXE.Data;
using EXE.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NuGet.Common;
using System.Text.RegularExpressions;

namespace EXE.Controllers
{
    public class OrderController : Controller
    {


        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrderController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }



        public async Task<IActionResult> Confirm()
        {
            if (!User.Identity.IsAuthenticated)
                return Redirect("/Identity/Account/Login");

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == user.Id);
            if (cart == null|| cart.CartItems.Count==0) return Redirect("/cart");

            // Kiểm tra số lượng tồn kho
            foreach (var item in cart.CartItems)
            {
                if (item.Quantity > item.Product.Stock) 
                {
                    TempData["Error"] = $"Sản phẩm \"{item.Product.Name}\" không đủ hàng trong kho.";
                    return Redirect("/cart"); // hoặc return View("Error")
                }
            }


            ViewBag.Cart = cart;
            //return Content(""+cart.CartItems.Count);
            return View();
        }



        

        public async Task<IActionResult> CreateOrder()
        {
            if (!User.Identity.IsAuthenticated)
                return Redirect("/Identity/Account/Login");
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();
            var fullname = Request.Form["fullname"];
            var phone = Request.Form["phone"];
            var address = Request.Form["address"];
            var note = Request.Form["note"];
            bool check = true;

            if (string.IsNullOrWhiteSpace(fullname))
            {
                check = false;
                ViewBag.IsCorrectName = false; 
            }
            if (string.IsNullOrWhiteSpace(phone) || !Regex.IsMatch(phone, @"^(0|\+84)[0-9]{9}$"))
            {
                check = false;
                ViewBag.IsCorrectPhone = false;
            }
            if (string.IsNullOrWhiteSpace(address))
            {
                check = false;
                ViewBag.IsCorrectAddress = false;
            }
            if (!check)
            {
                

               

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);
                if (cart == null|| cart.CartItems.Count==0) return Redirect("/cart");
                ViewBag.Cart = cart;
                ViewBag.phone = phone;
                ViewBag.fullname = fullname;
                ViewBag.address = address;
                ViewBag.note = note;





                // Kiểm tra số lượng tồn kho
                foreach (var item in cart.CartItems)
                {
                    if (item.Quantity > item.Product.Stock)
                    {
                        TempData["Error"] = $"Sản phẩm \"{item.Product.Name}\" không đủ hàng trong kho.";
                        return Redirect("/cart"); // hoặc return View("Error")
                    }
                }


                //return Content(""+cart.CartItems.Count);
                return View("Confirm");
            }
            else
            {


                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);
                List<OrderItem> orderItems = new List<OrderItem>();
                foreach (CartItem c in cart.CartItems)
                {


                    OrderItem item = new OrderItem();
                    item.Quantity = c.Quantity;
                    item.Product = c.Product;


                    //xu li khi so luong san pham < 0 
                    //if (item.Product.Stock < item.Quantity) return Redirect("/cart");



                    item.Product.Stock -= item.Quantity;
                    item.Price = c.Price;
                    orderItems.Add(item);

                    


                }


                // tao order payment 
                OrderPayment payment = new OrderPayment();
                payment.PaymentMethod = "tienmat";
                payment.CreatedAt = DateTime.Now;

                //


                Order order = new Order();
                order.Note = note;
                order.Address = address;
                order.OrderItems = orderItems;
                order.User = user;
                order.CreatedAt = DateTime.Now;
                order.FullName = fullname;
                order.Phonenumber = phone;
                order.Status = 0;
                order.Total = cart.TotalPrice;
                order.Payment = payment;

                _context.Orders.Add(order);
                // xoa cart

                if (cart.CartItems != null && cart.CartItems.Any())
                {
                    _context.CartItems.RemoveRange(cart.CartItems);
                }
                //_context.Carts.Remove(cart);
                await _context.SaveChangesAsync();
                // show qr code neu thanh toan chuyen khoan
                //

                return Redirect("/order/orderDetails");



                
            }
                
                
                
            return Content("" + fullname + address + phone + note);
            
        }


        public async Task<IActionResult>  Remove(Guid id )
        {
            var order = await _context.Orders
         .Include(c => c.OrderItems)
         .ThenInclude(ci => ci.Product)
         .FirstOrDefaultAsync(c => c.Id == id);

            if (order == null)
            {
                return NotFound("Không tìm thấy giỏ hàng");
            }

            // da nhan 
            if(order.Status==1) return Redirect("/order/orderDetails");

            // Cộng lại số lượng sản phẩm về kho
            foreach (var item in order.OrderItems)
            {
                item.Product.Stock += item.Quantity; 
            }

            order.Status = 2;
            

            await _context.SaveChangesAsync();

            return Redirect("/order/orderDetails");
        }


        public async Task<IActionResult> orderDetails()
        {
            if (!User.Identity.IsAuthenticated)
                return Redirect("/Identity/Account/Login");

            var user = await _userManager.GetUserAsync(User);
            List<Order> list = _context.Orders
                .Where(x=>x.User==user)
                .Include(x=>x.OrderItems)
                .ThenInclude(x=>x.Product)
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            ViewBag.ListOrder = list;
            return View("Details");
            
        }

    }
}
