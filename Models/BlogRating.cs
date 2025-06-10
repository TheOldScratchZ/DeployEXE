namespace EXE.Models
{
    public class BlogRating
    {
        public Guid Id { get; set; }
        public Guid BlogId { get; set; }
        public Blog? Blog { get; set; }
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }
        public int Rating { get; set; } 
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}