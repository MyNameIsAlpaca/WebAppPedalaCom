﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAppPedalaCom.Models;

namespace WebAppPedalaCom.Controllers
{
    [Route("api/[controller]")]
    [ApiController] // to update
    public class ProductsController : ControllerBase
    {
        private readonly AdventureWorksLt2019Context _context;

        public ProductsController(AdventureWorksLt2019Context context) => _context = context;

        /*      *
         *      *
         *  GET *  
         *      *
         *      */

        // GET: api/Products
        [HttpGet]
        [ActionName("GetProducts")]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            if (_context.Products == null)
                return NotFound();

            // Execute the stored procedure using raw SQL query
            var result = await _context.Products
                .FromSqlRaw("EXECUTE GetTopSellingProductsDetails") // Execute the stored procedure
                .ToListAsync();

            return result;
        }

        // GET: api/Products/{id}
        [HttpGet("{id:int}")]
        [ActionName("GetProductsByID")]
        public async Task<ActionResult<Product>> GetProductsById(int id)
        {
            if (_context.Products == null)
                return NotFound();

            var product = await _context.Products
                .Include(prd => prd.ProductModel) // Include Table ProductModel
                .ThenInclude(mdl => mdl.ProductModelProductDescriptions) // Include Pivot SalesOrderDetails
                .ThenInclude(prdMD => prdMD.ProductDescription) // Link with Pivot to ProductDescription
                .Include(prd => prd.SalesOrderDetails) // Include Table SalesOrderDetails
                .FirstOrDefaultAsync(prd => prd.ProductId == id);

            if (product == null)
                return NotFound();
            return product;
        }

        [HttpPost("info/")]
        [ActionName("GetInfoProductsByCategory")]
        public async Task<ActionResult<IEnumerable<InfoProduct>>> GetInfoProductsByCategory([FromBody] Category[]? category = null, string searchData = "", int pageNumber = 1)
        {
            int pageSize = 6;

            if (_context.Products == null)
                return NotFound();

            IQueryable<InfoProduct> query = _context.Products
                .Include(prd => prd.ProductCategory)
                .Where(prd => category == null || category.Select(c => c.ToString()).Contains(prd.ProductCategory.Name ?? string.Empty))
                .Where(prd => EF.Functions.Like(prd.Name, $"%{searchData}%"))
                .Select(obj => new InfoProduct
                {
                    productName = obj.Name,
                    productId = obj.ProductId,
                    productPrice = obj.ListPrice,
                    photo = obj.ThumbNailPhoto,
                    productCategory = obj.ProductCategory.Name
                });

            var totalItems = await query.CountAsync();

            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            if (pageNumber > totalPages && totalPages == 0)
            {
                return Ok();
            }

            if (pageNumber > totalPages)
            {
                return NotFound();
            }

            var products = await query.Skip((pageNumber - 1) * pageSize)
                                 .Take(pageSize)
                                 .ToListAsync();
            var paginationInfo = new
            {
                pageNumber = pageNumber,
                TotalPages = totalPages
            };

            return Ok(new { Products = products, PaginationInfo = paginationInfo });
        }

        [HttpGet("info/")]
        public async Task<ActionResult<IEnumerable<InfoProduct>>> GetInfoProductsByName(string searchData = "", int pageNumber = 1)
        {
            int pageSize = 12;

            if (_context.Products == null)
                return NotFound();

            IQueryable<InfoProduct> query = _context.Products
                .Where(prd => EF.Functions.Like(prd.Name, $"%{searchData}%"))
                .Select(obj => new InfoProduct
                {
                    productName = obj.Name,
                    productId = obj.ProductId,
                    productPrice = obj.ListPrice,
                    photo = obj.ThumbNailPhoto,
                    productCategory = obj.ProductCategory.Name
                });

            var totalItems = await query.CountAsync();

            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            if (pageNumber > totalPages && totalPages == 0)
            {
                return Ok();
            }

            if (pageNumber > totalPages)
            {
                return NotFound();
            }

            var products = await query.Skip((pageNumber - 1) * pageSize)
                                 .Take(pageSize)
                                 .ToListAsync();
            var paginationInfo = new
            {
                pageNumber = pageNumber,
                TotalPages = totalPages
            };

            return Ok(new { Products = products, PaginationInfo = paginationInfo });
        }



        /*      *
         *      *
         *  PUT *  
         *      *
         *      */

        // PUT: api/Products/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduct(int id, Product product)
        {
            if (id != product.ProductId)
                return BadRequest();

            _context.Entry(product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                    return NotFound();
                else throw;
            }

            return NoContent();
        }

        // POST: api/Products
        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct(Product product)
        {
            if (_context.Products == null)
                return Problem("Entity set 'AdventureWorksLt2019Context.Products'  is null.");

            Product newProduct = new Product();

            newProduct.ProductId = product.ProductId;

            newProduct.Color = product.Color;

            newProduct.ListPrice = product.ListPrice;

            newProduct.ModifiedDate = DateTime.Now;

            newProduct.Name = product.Name;

            newProduct.ProductCategoryId = product.ProductCategoryId;

            newProduct.ProductNumber = product.ProductNumber;

            newProduct.Size = product.Size;

            newProduct.StandardCost = product.StandardCost;

            newProduct.ThumbNailPhoto = Convert.FromBase64String(product.ThumbnailPhotoFileName.Split(",")[1]);

            newProduct.Weight = product.Weight;

            newProduct.SellStartDate = DateTime.Now;


            _context.Products.Add(newProduct);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetProducts", new { id = newProduct.ProductId }, newProduct);
        }

        // DELETE: api/Products/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            if (_context.Products == null)
                return NotFound();

            var product = await _context.Products.FindAsync(id);

            if (product == null)
                return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ProductExists(int id) => (_context.Products?.Any(e => e.ProductId == id)).GetValueOrDefault();

    }
}
