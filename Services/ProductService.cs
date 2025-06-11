using Microsoft.EntityFrameworkCore;
using TaskTracker.Data;
using TaskTracker.Models.Product;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace TaskTracker.Services
{
    public class ProductService
    {
        private readonly AppDbContext _context;

        public ProductService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            return await _context.Products.OrderBy(p => p.ProductSku).ToListAsync();
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _context.Products.FirstOrDefaultAsync(p => p.ProductID == id);
        }

        public async Task<(bool Success, string? ErrorMessage)> CreateProductAsync(Product product)
        {
            _context.Add(product);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateProductAsync(Product product)
        {
            try
            {
                _context.Update(product);
                await _context.SaveChangesAsync();
                return (true, null);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ProductExistsAsync(product.ProductID))
                {
                    return (false, "Product not found.");
                }
                throw;
            }
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return false;
            }
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ProductExistsAsync(int id)
        {
            return await _context.Products.AnyAsync(e => e.ProductID == id);
        }
        public async Task<List<object>> GetProductDropdownAsync()
        {
            return await _context.Products
                .OrderBy(p => p.ProductSku)
                .Select(p => new
                {
                    ProductID = p.ProductID.ToString(),
                    ProductSku = p.ProductSku,
                    Name = p.Name,
                    UnitPrice = p.UnitPrice
                })
                .ToListAsync<object>();
        }
    }
}