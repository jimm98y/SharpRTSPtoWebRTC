using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using RTSPtoWebRTC.Server;
using SharpRTSPtoWebRTC.WebRTCProxy;
using System.Collections.Generic;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.Configure<List<CameraConfiguration>>(builder.Configuration.GetSection("Cameras"));
builder.Services.AddSingleton<RTSPtoWebRTCProxyService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("/index.html");

app.Run();
