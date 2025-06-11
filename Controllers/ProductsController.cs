using Microsoft.AspNetCore.Mvc;
using TaskTracker.Data;
using TaskTracker.Models.Product;
using TaskTracker.Services;

namespace TaskTracker.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ProductService _productService;

        public ProductsController(ProductService productService)
        {
            _productService = productService;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _productService.GetAllProductsAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,UnitPrice,ProductSku")] Product product)
        {
            if (ModelState.IsValid)
            {
                var (success, error) = await _productService.CreateProductAsync(product);
                if (success)
                {
                    TempData["SuccessMessage"] = "Product created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                TempData["ErrorMessage"] = error;
            }
            else
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["ErrorMessage"] = string.Join("; ", errors);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductID,Name,Description,UnitPrice,ProductSku")] Product product)
        {
            if (id != product.ProductID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var (success, error) = await _productService.UpdateProductAsync(product);
                if (success)
                {
                    TempData["SuccessMessage"] = "Product updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                TempData["ErrorMessage"] = error ?? "Product not found.";
            }
            else
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["ErrorMessage"] = string.Join("; ", errors);
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _productService.GetProductByIdAsync(id.Value);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _productService.DeleteProductAsync(id);
            TempData["SuccessMessage"] = "Product deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}