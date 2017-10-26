using System;

namespace AspNetCoreWebApi.Models
{
  public class Product
  {
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Available { get; set; }
    public string Price { get; set; }
    public string Description { get; set; }
  }
}