using CameraAPI.WebRTCProxy;
using CameraStreamingServer.Model;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// https://stackoverflow.com/questions/39169701/how-to-extract-a-list-from-appsettings-json-in-net-core
builder.Services.Configure<List<CameraConfiguration>>(builder.Configuration.GetSection("Cameras"));

builder.Services.AddSingleton<RTSPtoWebRTCProxyService>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.MapFallbackToFile("index.html");

app.Run();
