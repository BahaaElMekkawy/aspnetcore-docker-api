namespace MyDockerApi.Api
{
    public class Product
    {
        
            public int Id { get; set; }  // Primary key
            public string Name { get; set; } = string.Empty;
            public decimal Price { get; set; }
    }
}
