using Fall2024_Assignment4_CS330.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Fall2024_Assignment4_CS330.Models;
using Microsoft.AspNetCore.Builder;
using Fall2024_Assignment4_CS330.Services;
using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Configure Identity with custom options
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false; // Adjust based on security requirements.
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddScoped<OpenAIService>();
builder.Services.AddScoped<GameTimerService>();

var openAIKey = builder.Configuration["OpenAIKey"];
var openAIEndpoint = builder.Configuration["OpenAIEndpoint"];

OpenAIService openAIService = new OpenAIService(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // Default HSTS value is 30 days.
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // Ensure this is added for authentication middleware
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.UseWebSockets();
app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        await HandleWebSocketCommunication(webSocket);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

app.Run();

static async Task HandleWebSocketCommunication(WebSocket w)
{
    var buffer = new byte[4096];
    WebSocketReceiveResult result;

    do
    {
        result = await w.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        var clientMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
        var serverMessage = Encoding.UTF8.GetBytes($"Server: {clientMessage}");
        await w.SendAsync(new ArraySegment<byte>(serverMessage), result.MessageType, result.EndOfMessage, CancellationToken.None);

    } while (!result.CloseStatus.HasValue);

    await w.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
}
