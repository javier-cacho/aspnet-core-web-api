using AspNetCoreWebApi.Models;
using AspNetCoreWebApi.DAL;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetCoreWebApi.Services
{
    public class ProductsService : IServiceProvider
    {
        private readonly ProductsContext productsContext;

        internal ProductsService(ProductsContext productsContext)
        {
            this.productsContext = productsContext;
        }


        internal List<Product> Get(Guid? productId = null) {
            return this.productsContext.ProductSet.Where(p => !productId.HasValue || p.Id == productId).ToList(); 
        }

        internal List<Product> Upsert(Product product)
        {
            EntityState entityState = this.productsContext.ProductSet.Any(p => p.Id == product.Id) ? EntityState.Modified : EntityState.Added;
            this.productsContext.Entry(product).State = entityState;
            this.productsContext.SaveChanges();
            return this.productsContext.ProductSet.ToList(); 
        }

        internal List<Product> Delete(Product product)
        {
            this.productsContext.ProductSet.Remove(product);
            this.productsContext.SaveChanges();
            return this.productsContext.ProductSet.ToList();
        }

        public object GetService(Type products)
        {
            throw new NotImplementedException();
        }
    }
}
