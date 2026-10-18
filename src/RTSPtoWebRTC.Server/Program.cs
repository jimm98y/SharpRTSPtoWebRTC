// SharpRTSPtoWebRTC
// Copyright (C) 2026 Lukas Volf
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RTSPtoWebRTC.Server;
using SharpRTSPtoWebRTC.WebRTCProxy;
using System;
using System.Collections.Generic;
using System.Linq;

// "hash-password <password>": the hash of a password for the users of "BasicAuthentication" in appsettings.json
if (args.Length == 2 && args[0] == "hash-password")
{
    Console.WriteLine(BasicPasswords.Hash(args[1]));
    return;
}

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.Configure<List<CameraConfiguration>>(builder.Configuration.GetSection("Cameras"));
builder.Services.AddSingleton<RTSPtoWebRTCProxyService>();

// HTTP Basic authentication of the users of "BasicAuthentication": of every request - the page, its files and the API - so
//  that the browser prompts once, as the page loads, and sends the credentials of each request after it. Of no users, none.
var authentication = builder.Configuration.GetSection("BasicAuthentication");
var users = authentication.GetSection("Users").GetChildren().ToDictionary(u => u.Key, u => u.Value, StringComparer.Ordinal);
bool authenticate = users.Count > 0;
if (authenticate)
{
    builder.Services
        .AddAuthentication(BasicAuthenticationOptions.Scheme)
        .AddScheme<BasicAuthenticationOptions, BasicAuthenticationHandler>(BasicAuthenticationOptions.Scheme, options =>
        {
            options.Realm = authentication["Realm"] ?? options.Realm;
            options.Users = users;
        });
    builder.Services.AddAuthorization(options =>
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    });
}

var app = builder.Build();

if (authenticate)
{
    if (app.Urls.Any(u => u.StartsWith("http:", StringComparison.OrdinalIgnoreCase)) || (builder.Configuration["urls"] ?? builder.Configuration["ASPNETCORE_URLS"] ?? "").Contains("http:"))
    {
        app.Logger.LogWarning("Basic authentication over plain HTTP sends the passwords readable by anyone on the path: use HTTPS beyond the loopback.");
    }
}
else
{
    app.Logger.LogWarning("No users in \"BasicAuthentication\": the API is open to anyone who can reach it.");
}

app.UseHttpsRedirection();

// before the static files, so that the page itself takes the credentials
app.UseAuthentication();
app.UseAuthorization();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();
app.MapFallbackToFile("/index.html");

app.Run();
