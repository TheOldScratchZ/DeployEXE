using EXE.Data;
using EXE.Models;

namespace EXE.Services
{
    public class CartService
    {
        private readonly ApplicationDbContext _context;

        public CartService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Cart> CreateCartForUser(Guid userId)
        {
            var cart = new Cart
            {
                Id = Guid.NewGuid(),
                UserId = userId,
            };

            _context.Add(cart);
            await _context.SaveChangesAsync();
            return cart;
        }
    }
}
