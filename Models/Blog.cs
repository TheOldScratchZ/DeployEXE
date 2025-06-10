namespace EXE.Models
{
    public class Blog
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Thumbnail { get; set; } = string.Empty;
        public string ShortContent { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty; 
        public string Content { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public string? Tags { get; set; } = null;
        public double Star { get; set; } = 0.0;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public Guid? UserId { get; set; }
        public ApplicationUser? User { get; set; }
        public Guid RegionId { get; set; }
        public Region? Region { get; set; }
    }
}
