using Microsoft.EntityFrameworkCore;
using MyDockerApi.Api;

var builder = WebApplication.CreateBuilder(args);

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Auto-create database and tables on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        Console.WriteLine("Ensuring database and tables are created...");

        // This will create the database if it doesn't exist AND apply all migrations
        await db.Database.EnsureCreatedAsync();

        // OR if you want to use migrations specifically:
        // await db.Database.MigrateAsync();

        Console.WriteLine("Database and tables created successfully!");

        // Optional: Add seed data
        if (!await db.Products.AnyAsync())
        {
            db.Products.Add(new Product { Name = "Sample Laptop", Price = 999.99m });
            db.Products.Add(new Product { Name = "Sample Mouse", Price = 29.99m });
            await db.SaveChangesAsync();
            Console.WriteLine("Sample data added");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database creation failed: {ex.Message}");
        Console.WriteLine($"Full error: {ex}");
    }
}

// Log all requests
app.Use(async (context, next) =>
{
    Console.WriteLine($"--> Request: {context.Request.Method} {context.Request.Path}");
    await next();
});

// Simple test endpoint to verify routing
app.MapGet("/test", () =>
{
    Console.WriteLine("Test endpoint called");
    return "Test endpoint works!";
});

// Products endpoints with better error handling
app.MapGet("/products", async (AppDbContext db) =>
{
    Console.WriteLine("GET /products endpoint called");
    try
    {
        var products = await db.Products.ToListAsync();
        Console.WriteLine($"Found {products.Count} products");
        return Results.Ok(products);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database error: {ex.Message}");
        return Results.Problem("Database error");
    }
});

app.MapPost("/products", async (AppDbContext db, Product p) =>
{
    Console.WriteLine($"POST /products called with: {p.Name}");
    db.Products.Add(p);
    await db.SaveChangesAsync();
    return Results.Created($"/products/{p.Id}", p);
});

app.MapGet("/products/{id}", async (AppDbContext db, int id) =>
{
    Console.WriteLine($"GET /products/{id} called");
    var product = await db.Products.FindAsync(id);
    return product is Product p ? Results.Ok(p) : Results.NotFound();
});

// Weather forecast (keep this as reference)
app.MapGet("/weatherforecast", () =>
{
    Console.WriteLine("GET /weatherforecast called");
    var summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
});

app.Run();

// Move this outside the main method
public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}