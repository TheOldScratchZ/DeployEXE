namespace EXE.Models
{
    public class Order
    {
        public Guid Id { get; set; }
        public decimal Total { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }
        public List<OrderItem>? OrderItems { get; set; }
        public int Status { get; set; } // 0 = pending, 1 = completed, 2 = cancelled
        public OrderPayment? Payment { get; set; }
        public string Address { get; set; }
        
        public string FullName { get; set; } = string.Empty;
        public string Phonenumber { get; set; } = string.Empty;

        public string Note { get; set; } = string.Empty;


    }
}
