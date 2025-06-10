namespace EXE.Models
{
    public class Region
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public List<Product> Products { get; set; } = new List<Product>();
        public List<Blog> Blogs { get; set; } = new List<Blog>();
    }
}
