using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;
using AspNetCoreWebApi.Models;
using AspNetCoreWebApi.Services;
using AspNetCoreWebApi.DAL;

namespace AspNetCoreWebApi.Controllers
{
    [Route("api/[controller]")]
    public class ProductsController : Controller
    {
        private readonly ProductsContext productsContext;

        private readonly JsonSerializerSettings serializerSettings;

        public ProductsController(ProductsContext productsContext)
        {
            this.productsContext = productsContext;

            this.serializerSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            };
        }

        [HttpGet]
        [Authorize(Policy = "AdminPolicy")]
        public IActionResult Get()
        {
            ProductsService productService = new ProductsService(productsContext);

            var response = productService.Get().ToArray();

            var json = JsonConvert.SerializeObject(response, serializerSettings);
            return new OkObjectResult(json);
        }


        // GET api/products/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "product";
        }

        // POST api/products
        [HttpPost]
        public void Post([FromBody]string product)
        {
        }

        // PUT api/products/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string product)
        {
        }

        // DELETE api/products/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
