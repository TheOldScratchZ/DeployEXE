using EXE.Data;
using EXE.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EXE.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Shop()
        {
            var products = _context.Products
                .Where(p => p.DeletedAt == null)
                .ToList();

            return View(products);
        }
        public IActionResult Details(Guid id)
        {
            var product = _context.Products
                .Include(p => p.Region)
                .FirstOrDefault(p => p.Id == id);

            if (product == null)
                return NotFound();

            return View(product);
        }
        public IActionResult Create()
        {
            ViewBag.Regions = new SelectList(_context.Regions, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, List<IFormFile> Images)
        {
            if (ModelState.IsValid)
            {
                product.Id = Guid.NewGuid();
                product.CreatedAt = DateTime.Now;
                if (product.ImageUrl == null)
                {
                    product.ImageUrl = new List<string>();
                }

                foreach (var image in Images)
                {
                    if (image != null && image.Length > 0)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(image.FileName);
                        var extension = Path.GetExtension(image.FileName);
                        var uniqueName = $"{fileName}_{Guid.NewGuid()}{extension}";
                        var savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/products", uniqueName);

                        using (var stream = new FileStream(savePath, FileMode.Create))
                        {
                            await image.CopyToAsync(stream);
                        }

                        product.ImageUrl.Add("/img/products/" + uniqueName);
                    }
                }
                _context.Products.Add(product);
                await _context.SaveChangesAsync();
                TempData["ToastMessage"] = "Thêm sản phẩm thành công!";
                return RedirectToAction("Shop");
            }
            ViewBag.Regions = new SelectList(_context.Regions, "Id", "Name");
            return View(product);
        }
        public IActionResult Edit(Guid id)
        {
            var product = _context.Products.Find(id);
            if (product == null) return NotFound();

            ViewBag.Regions = new SelectList(_context.Regions, "Id", "Name", product.RegionId);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Guid id, Product product, List<IFormFile> NewImages)
        {
            if (id != product.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var existingProduct = _context.Products.Find(id);
                if (existingProduct == null) return NotFound();

                existingProduct.Name = product.Name;
                existingProduct.Description = product.Description;
                existingProduct.Price = product.Price;
                existingProduct.Stock = product.Stock;
                existingProduct.RegionId = product.RegionId;
                existingProduct.UpdatedAt = DateTime.Now;

                if (NewImages != null && NewImages.Count > 0)
                {
                    if (existingProduct.ImageUrl != null)
                    {
                        foreach (var imagePath in existingProduct.ImageUrl)
                        {
                            var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", imagePath.TrimStart('/'));
                            if (System.IO.File.Exists(physicalPath))
                            {
                                System.IO.File.Delete(physicalPath);
                            }
                        }
                    }

                    var newImageUrls = new List<string>();
                    foreach (var image in NewImages)
                    {
                        if (image.Length > 0)
                        {
                            var fileName = Path.GetFileNameWithoutExtension(image.FileName);
                            var extension = Path.GetExtension(image.FileName);
                            var uniqueName = $"{fileName}_{Guid.NewGuid()}{extension}";
                            var savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/products", uniqueName);

                            using (var stream = new FileStream(savePath, FileMode.Create))
                            {
                                image.CopyTo(stream);
                            }

                            newImageUrls.Add("/img/products/" + uniqueName);
                        }
                    }
                    existingProduct.ImageUrl = newImageUrls;
                }
                _context.SaveChanges();
                TempData["ToastMessage"] = "Cập nhật sản phẩm thành công!";
                return RedirectToAction("Shop");
            }
            ViewBag.Regions = new SelectList(_context.Regions, "Id", "Name", product.RegionId);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(Guid id)
        {
            var product = _context.Products.Find(id);
            if (product == null) return NotFound();

            product.DeletedAt = DateTime.Now;
            _context.SaveChanges();

            TempData["ToastMessage"] = "Sản phẩm đã được xoá (ẩn khỏi danh sách).";
            return RedirectToAction("Shop");
        }
    }
}
