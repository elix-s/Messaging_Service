using System.Net.WebSockets;
using MessagingService.DAL;
using MessagingService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<MessageBroadcaster>();
builder.Services.AddSingleton<MessageRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var messageRepository = app.Services.GetRequiredService<MessageRepository>();
messageRepository.InitDatabase();

app.UseStaticFiles();
app.UseRouting();

app.UseWebSockets();

app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var broadcaster = context.RequestServices.GetRequiredService<MessageBroadcaster>();
        await broadcaster.RegisterAsync(webSocket);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();