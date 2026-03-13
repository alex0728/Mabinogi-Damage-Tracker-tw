using System.Diagnostics;
using System.Net.Http;
using Mabinogi_Damage_tracker;

var builder = WebApplication.CreateBuilder(args);

db_helper.Initalize_db();

Parser.Start();

// Add services to the container.
builder.Services.AddControllersWithViews();


// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            policy
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

var app = builder.Build();




// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.UseCors("AllowReactApp");

// Proxy skill images to mabires.pril.cc
app.MapGet("/res/skillimage/{region}/{skillId}/{fileName}", async (string region, string skillId, string fileName, HttpContext context) =>
{
    var targetUrl = $"https://mabires.pril.cc/skillimage/{region}/{skillId}/{fileName}";
    
    using var httpClient = new HttpClient();
    try
    {
        var response = await httpClient.GetAsync(targetUrl);
        
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsByteArrayAsync();
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "image/png";
            
            context.Response.ContentType = contentType;
            await context.Response.Body.WriteAsync(content);
        }
        else
        {
            context.Response.StatusCode = (int)response.StatusCode;
            await context.Response.WriteAsync($"Failed to fetch image: {response.StatusCode}");
        }
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync($"Proxy error: {ex.Message}");
    }
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();