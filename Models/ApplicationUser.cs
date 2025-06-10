using Microsoft.AspNetCore.Identity;

namespace EXE.Models
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public List<Order> Orders { get; set; } = new List<Order>();
        public List<Blog> Blogs { get; set; } = new List<Blog>();
        public Guid? CartId { get; set; }
        public Cart? Cart { get; set; }
    }
}
