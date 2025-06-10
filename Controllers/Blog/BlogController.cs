using EXE.Data;
using EXE.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EXE.Controllers
{
    public class BlogController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly UserManager<ApplicationUser> _userManager;

        public BlogController(ApplicationDbContext context, IWebHostEnvironment environment, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _environment = environment;
            _userManager = userManager;
        }

        // Trang danh sách blog
        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Admin") || User.IsInRole("Staff"))
            {
                return RedirectToAction("IndexAdmin");
            }

            var blogs = await _context.Blogs
                .Include(b => b.Region)
                .Include(b => b.User)
                .Where(b => b.DeletedAt == null)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(blogs);
        }

        public async Task<IActionResult> IndexAdmin()
        {
            var blogs = await _context.Blogs
                .Include(b => b.Region)
                .Include(b => b.User)
                .Where(b => b.DeletedAt == null)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            // Debug: Kiểm tra số lượng blog
            Console.WriteLine($"Số lượng blog: {blogs.Count}");
            foreach (var blog in blogs.Take(3))
            {
                Console.WriteLine($"Blog: {blog.Title}, Region: {blog.Region?.Name}");
            }

            return View(blogs);
        }
        // Trang chi tiết blog
        public async Task<IActionResult> Detail(Guid id)
        {
            var blog = await _context.Blogs
                .Include(b => b.Region)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == id && b.DeletedAt == null);

            if (blog == null)
            {
                return NotFound();
            }

            // Lấy các blog liên quan
            var relatedBlogs = await _context.Blogs
                .Include(b => b.Region)
                .Where(b => b.RegionId == blog.RegionId &&
                           b.Id != blog.Id &&
                           b.DeletedAt == null)
                .OrderByDescending(b => b.CreatedAt)
                .Take(4)
                .ToListAsync();

            ViewBag.RelatedBlogs = relatedBlogs;

            // Lấy tổng số rating và rating của user hiện tại (nếu có)
            var totalRatings = await _context.BlogRatings.CountAsync(br => br.BlogId == id);
            ViewBag.TotalRatings = totalRatings;

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                var userRating = await _context.BlogRatings
                    .Where(br => br.BlogId == id && br.UserId == currentUser.Id.ToString())
                    .Select(br => br.Rating)
                    .FirstOrDefaultAsync();
                ViewBag.UserRating = userRating;
            }
            else
            {
                ViewBag.UserRating = 0;
            }

            return View("Detail", blog);
        }

        // GET: Trang thêm blog mới
        [HttpGet]
        public async Task<IActionResult> AddBlog()
        {
            ViewBag.Regions = await _context.Regions.ToListAsync();
            return View();
        }

        // POST: Xử lý thêm blog mới
        [HttpPost]
        public async Task<IActionResult> AddBlog(Blog model, List<IFormFile> thumbnails)
        {
            // Debug để kiểm tra authentication và claims
            Console.WriteLine($"User.Identity.IsAuthenticated: {User.Identity.IsAuthenticated}");
            Console.WriteLine($"User.Identity.Name: {User.Identity.Name}");

            if (ModelState.IsValid)
            {
                // Xử lý upload thumbnails
                if (thumbnails != null && thumbnails.Count > 0)
                {
                    var thumbnailPaths = new List<string>();
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "thumbnails");

                    // Tạo thư mục nếu chưa tồn tại
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    foreach (var file in thumbnails)
                    {
                        if (file.Length > 0)
                        {
                            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(fileStream);
                            }

                            thumbnailPaths.Add("/uploads/thumbnails/" + uniqueFileName);
                        }
                    }

                    model.Thumbnail = string.Join(",", thumbnailPaths);
                }

                // Tạo ShortContent từ Content (lấy 150 ký tự đầu)
                if (!string.IsNullOrEmpty(model.Content))
                {
                    var plainText = System.Text.RegularExpressions.Regex.Replace(model.Content, "<.*?>", "");
                    model.ShortContent = plainText.Length > 150 ? plainText.Substring(0, 150) + "..." : plainText;
                }

                model.Id = Guid.NewGuid();
                model.CreatedAt = DateTime.UtcNow;
                model.Star = 0.0; // Khởi tạo rating = 0

                // Lấy ID của user hiện tại
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null)
                {
                    model.UserId = currentUser.Id;
                }
                else
                {
                    TempData["Error"] = "Cannot identify current user. Please try logging in again.";
                    ViewBag.Regions = await _context.Regions.ToListAsync();
                    return View(model);
                }

                try
                {
                    _context.Blogs.Add(model);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Blog created successfully!";
                    return RedirectToAction("AddBlog");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving blog: {ex.Message}");
                    TempData["Error"] = "An error occurred while saving the blog.";
                }
            }

            ViewBag.Regions = await _context.Regions.ToListAsync();
            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> EditBlog(Guid id)
        {
            var blog = await _context.Blogs
                .Include(b => b.Region)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == id && b.DeletedAt == null);

            if (blog == null)
            {
                TempData["Error"] = "Blog not found!";
                return RedirectToAction("Index");
            }

            // Kiểm tra quyền chỉnh sửa (tác giả HOẶC Admin/Staff)
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                TempData["Error"] = "Please login to edit this blog!";
                return RedirectToAction("Detail", new { id = id });
            }

            // Cho phép Admin/Staff edit tất cả blog, hoặc user edit blog của chính mình
            bool canEdit = User.IsInRole("Admin") || User.IsInRole("Staff") || blog.UserId == currentUser.Id;

            if (!canEdit)
            {
                TempData["Error"] = "You don't have permission to edit this blog!";
                return RedirectToAction("Detail", new { id = id });
            }

            // Load regions cho dropdown
            ViewBag.Regions = await _context.Regions.ToListAsync();

            return View(blog);
        }
        // GET: Trang chỉnh sửa blog
        [HttpPost]
        public async Task<IActionResult> EditBlog(Blog model, List<IFormFile> newThumbnails, bool keepExistingThumbnails = false, string removedThumbnails = "")
        {
            if (ModelState.IsValid)
            {
                var existingBlog = await _context.Blogs.FirstOrDefaultAsync(b => b.Id == model.Id && b.DeletedAt == null);

                if (existingBlog == null)
                {
                    return Json(new { success = false, message = "Blog not found!" });
                }

                // Lưu thumbnail cũ
                var oldThumbnails = existingBlog.Thumbnail;

                // Cập nhật thông tin blog (bao gồm các trường mới)
                existingBlog.Title = model.Title;
                existingBlog.Content = model.Content;
                existingBlog.ContentType = model.ContentType;
                existingBlog.Tags = model.Tags;
                existingBlog.Link = model.Link;
                existingBlog.RegionId = model.RegionId;
                existingBlog.UpdatedAt = DateTime.UtcNow;

                // Tạo lại ShortContent từ Content mới
                if (!string.IsNullOrEmpty(model.Content))
                {
                    var plainText = System.Text.RegularExpressions.Regex.Replace(model.Content, "<.*?>", "");
                    existingBlog.ShortContent = plainText.Length > 150 ? plainText.Substring(0, 150) + "..." : plainText;
                }

                // Xử lý thumbnail (giữ nguyên logic cũ)
                List<string> remainingThumbnails = new List<string>();
                if (!string.IsNullOrEmpty(oldThumbnails))
                {
                    var allOldThumbnails = oldThumbnails.Split(',').Select(t => t.Trim()).ToList();
                    var thumbnailsToRemove = !string.IsNullOrEmpty(removedThumbnails)
                        ? removedThumbnails.Split(',').Select(t => t.Trim()).ToList()
                        : new List<string>();

                    remainingThumbnails = allOldThumbnails.Where(t => !thumbnailsToRemove.Contains(t)).ToList();

                    foreach (var removedThumbnail in thumbnailsToRemove)
                    {
                        if (!string.IsNullOrEmpty(removedThumbnail))
                        {
                            DeleteThumbnailFiles(removedThumbnail);
                        }
                    }
                }

                // Xử lý thumbnail mới
                if (newThumbnails != null && newThumbnails.Count > 0)
                {
                    var thumbnailPaths = new List<string>();
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "thumbnails");

                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    foreach (var file in newThumbnails)
                    {
                        if (file.Length > 0)
                        {
                            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(fileStream);
                            }

                            thumbnailPaths.Add("/uploads/thumbnails/" + uniqueFileName);
                        }
                    }

                    if (keepExistingThumbnails)
                    {
                        thumbnailPaths.AddRange(remainingThumbnails);
                    }

                    existingBlog.Thumbnail = string.Join(",", thumbnailPaths);
                }
                else
                {
                    existingBlog.Thumbnail = string.Join(",", remainingThumbnails);
                }

                _context.Blogs.Update(existingBlog);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Blog updated successfully!", redirectUrl = Url.Action("Detail", new { id = model.Id }) });
            }

            return Json(new { success = false, message = "Please check your input data." });
        }
        // GET: Xác nhận xóa blog
        [HttpGet]
        public async Task<IActionResult> DeleteBlog(Guid id)
        {
            var blog = await _context.Blogs
                .Include(b => b.Region)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == id && b.DeletedAt == null);

            if (blog == null)
            {
                TempData["Error"] = "Blog not found!";
                return RedirectToAction("Index");
            }

            return View(blog);
        }
        [HttpPost]
        public async Task<IActionResult> RateBlog(Guid blogId, int rating)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Json(new { success = false, message = "Please login to rate this blog." });
                }

                if (rating < 1 || rating > 5)
                {
                    return Json(new { success = false, message = "Rating must be between 1 and 5 stars." });
                }

                var blog = await _context.Blogs.FirstOrDefaultAsync(b => b.Id == blogId && b.DeletedAt == null);
                if (blog == null)
                {
                    return Json(new { success = false, message = "Blog not found." });
                }

                // Kiểm tra xem user đã rate blog này chưa
                var existingRating = await _context.BlogRatings
                    .FirstOrDefaultAsync(br => br.BlogId == blogId && br.UserId == currentUser.Id.ToString());

                if (existingRating != null)
                {
                    // Cập nhật rating cũ
                    existingRating.Rating = rating;
                    existingRating.UpdatedAt = DateTime.UtcNow;
                    _context.BlogRatings.Update(existingRating);
                }
                else
                {
                    // Tạo rating mới
                    var newRating = new BlogRating
                    {
                        Id = Guid.NewGuid(),
                        BlogId = blogId,
                        UserId = currentUser.Id.ToString(),
                        Rating = rating,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.BlogRatings.Add(newRating);
                }

                await _context.SaveChangesAsync();

                // Tính lại rating trung bình
                var averageRating = await _context.BlogRatings
                    .Where(br => br.BlogId == blogId)
                    .AverageAsync(br => br.Rating);

                // Cập nhật rating trung bình vào blog
                blog.Star = Math.Round(averageRating, 1);
                _context.Blogs.Update(blog);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Rating submitted successfully!",
                    averageRating = blog.Star,
                    userRating = rating
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rating blog: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while rating the blog." });
            }
        }
        // Thêm action này vào BlogController
        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            return await DeleteBlogConfirm(id);
        }

        // Hoặc sửa lại DeleteBlogConfirm để nhận parameter đúng cách
        [HttpPost]
        public async Task<IActionResult> DeleteBlogConfirm(Guid id)
        {
            var blog = await _context.Blogs.FirstOrDefaultAsync(b => b.Id == id && b.DeletedAt == null);

            if (blog == null)
            {
                TempData["Error"] = "Blog not found!";
                return RedirectToAction("IndexAdmin");
            }

            // Kiểm tra quyền xóa (Admin/Staff hoặc tác giả)
            var currentUser = await _userManager.GetUserAsync(User);
            bool canDelete = User.IsInRole("Admin") || User.IsInRole("Staff") || blog.UserId == currentUser.Id;

            if (!canDelete)
            {
                TempData["Error"] = "You don't have permission to delete this blog!";
                return RedirectToAction("Detail", new { id = id });
            }

            try
            {
                // Xóa các file thumbnail trước
                if (!string.IsNullOrEmpty(blog.Thumbnail))
                {
                    DeleteThumbnailFiles(blog.Thumbnail);
                }

                // Xóa các ratings liên quan
                var ratings = await _context.BlogRatings.Where(br => br.BlogId == id).ToListAsync();
                if (ratings.Any())
                {
                    _context.BlogRatings.RemoveRange(ratings);
                }

                // Hard delete - xóa hẳn khỏi database
                _context.Blogs.Remove(blog);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Blog deleted permanently!";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting blog: {ex.Message}");
                TempData["Error"] = "An error occurred while deleting the blog.";
            }

            return RedirectToAction("IndexAdmin");
        }
        // Thêm function để lấy rating của user hiện tại
        [HttpGet]
        public async Task<IActionResult> GetUserRating(Guid blogId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Json(new { userRating = 0 });
                }

                var userRating = await _context.BlogRatings
                    .Where(br => br.BlogId == blogId && br.UserId == currentUser.Id.ToString())
                    .Select(br => br.Rating)
                    .FirstOrDefaultAsync();

                return Json(new { userRating = userRating });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting user rating: {ex.Message}");
                return Json(new { userRating = 0 });
            }
        }
        // POST: Xử lý xóa blog (Soft Delete)
        [HttpPost]
        // POST: Xóa vĩnh viễn blog (Hard Delete)
        [HttpPost]
        public async Task<IActionResult> PermanentDeleteBlog(Guid id)
        {
            var blog = await _context.Blogs.FirstOrDefaultAsync(b => b.Id == id);

            if (blog == null)
            {
                TempData["Error"] = "Blog not found!";
                return RedirectToAction("Index");
            }

            // Xóa các file thumbnail
            if (!string.IsNullOrEmpty(blog.Thumbnail))
            {
                DeleteThumbnailFiles(blog.Thumbnail);
            }

            // Xóa vĩnh viễn khỏi database
            _context.Blogs.Remove(blog);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Blog permanently deleted!";
            return RedirectToAction("Index");
        }
        // Thêm action này vào BlogController
        [HttpPost]
        public async Task<IActionResult> PermanentDelete(Guid id)
        {
            var blog = await _context.Blogs.FirstOrDefaultAsync(b => b.Id == id);

            if (blog == null)
            {
                return Json(new { success = false, message = "Blog not found!" });
            }

            // Xóa các file thumbnail
            if (!string.IsNullOrEmpty(blog.Thumbnail))
            {
                DeleteThumbnailFiles(blog.Thumbnail);
            }

            // Xóa các ratings liên quan
            var ratings = await _context.BlogRatings.Where(br => br.BlogId == id).ToListAsync();
            _context.BlogRatings.RemoveRange(ratings);

            // Xóa vĩnh viễn khỏi database
            _context.Blogs.Remove(blog);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Blog permanently deleted!" });
        }

        // API endpoint để upload ảnh cho CKEditor
        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile upload)
        {
            if (upload != null && upload.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "content");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + upload.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await upload.CopyToAsync(fileStream);
                }

                var imageUrl = "/uploads/content/" + uniqueFileName;
                return Json(new
                {
                    uploaded = true,
                    url = imageUrl
                });
            }

            return Json(new
            {
                uploaded = false,
                error = new { message = "Upload failed" }
            });
        }

        // Helper method để xóa file thumbnail
        private void DeleteThumbnailFiles(string thumbnailPaths)
        {
            if (string.IsNullOrEmpty(thumbnailPaths)) return;

            var paths = thumbnailPaths.Split(',');
            foreach (var path in paths)
            {
                var fullPath = Path.Combine(_environment.WebRootPath, path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(fullPath))
                {
                    try
                    {
                        System.IO.File.Delete(fullPath);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting file {fullPath}: {ex.Message}");
                    }
                }
            }
        }
    }

}