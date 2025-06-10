namespace EXE.Models
{
    public class Cart
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public ApplicationUser? User { get; set; }
        public decimal TotalPrice { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<CartItem>? CartItems { get; set; } = new List<CartItem>();
    }
}
