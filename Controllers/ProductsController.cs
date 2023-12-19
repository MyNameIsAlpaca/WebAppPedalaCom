﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
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

            return await _context.Products
                 .Include(prd => prd.ProductModel) // Include Table ProductModel
                 .ThenInclude(mdl => mdl.ProductModelProductDescriptions) // Include Pivot SalesOrderDetails
                 .ThenInclude(prdMD => prdMD.ProductDescription) // Link with Pivot to ProductDescription
                 .Include(prd => prd.SalesOrderDetails) // Include Table SalesOrderDetails
                 .ToListAsync();
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
        public async Task<ActionResult<IEnumerable<InfoProduct>>> GetInfoProductsByCategory([FromBody] Category[]? category = null, string searchData = "")
        {
            if (_context.Products == null)
                return NotFound();

            List<SqlParameter> parameter = new() { new SqlParameter("@searchParam", searchData )};
            if (category != null && category.Any())
                parameter.Add(new SqlParameter("@categories", string.Join(',', category.Select(c => c.ToString()).ToList())));
            else parameter.Add(new SqlParameter("@categories", DBNull.Value));

            List<InfoProduct> products = await _context.Products.FromSqlRaw($@"SELECT * 
                          FROM [SalesLT].[Product] as P
                          INNER JOIN [SalesLT].[ProductCategory] AS PC ON (P.ProductCategoryID = PC.ProductCategoryID) 
                          WHERE PC.Name IN (@categories) AND 
                            P.Name LIKE '%@searchParam%'",
                            parameter.ToArray()
                ).Select(obj => new InfoProduct
                {
                    productName = obj.Name,
                    productId = obj.ProductId,
                    productPrice = obj.ListPrice
                }).ToListAsync();

            return Ok(products);

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

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetProduct", new { id = product.ProductId }, product);
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
