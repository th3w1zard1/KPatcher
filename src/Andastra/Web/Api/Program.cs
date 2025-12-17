using Andastra.Web.Crypto;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure ephemeral key generator as singleton
var masterSecret = GetOrCreateMasterSecret(builder.Configuration);
builder.Services.AddSingleton(new EphemeralKeyGenerator(masterSecret, timeWindowMinutes: 5));

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseHttpsRedirection();

// Serve static files (frontend)
app.UseStaticFiles();

// API Endpoints

/// <summary>
/// POST /api/runtime/key
/// Issues an ephemeral decryption key based on client identity.
/// </summary>
app.MapPost("/api/runtime/key", (HttpContext context, EphemeralKeyGenerator keyGen) =>
{
    var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    var userAgent = context.Request.Headers.UserAgent.ToString();

    if (string.IsNullOrEmpty(userAgent))
    {
        return Results.BadRequest(new { error = "User-Agent header required" });
    }

    try
    {
        // Generate ephemeral key
        var key = keyGen.GenerateKey(clientIp, userAgent);
        var keyBase64 = Convert.ToBase64String(key);

        // Return key with metadata
        return Results.Ok(new
        {
            key = keyBase64,
            validFor = "5 minutes",
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        });
    }
    catch (Exception)
    {
        return Results.Problem(
            detail: "Failed to generate key",
            statusCode: 500
        );
    }
})
.WithName("GetRuntimeKey")
.WithOpenApi()
.WithDescription("Issues an ephemeral decryption key for WASM runtime");

/// <summary>
/// GET /api/runtime/wasm
/// Serves the encrypted WASM binary.
/// </summary>
app.MapGet("/api/runtime/wasm", async (HttpContext context) =>
{
    var wasmPath = Path.Combine(
        app.Environment.ContentRootPath,
        "wwwroot",
        "wasm",
        "Andastra.Game.Wasm.wasm.encrypted"
    );

    if (!File.Exists(wasmPath))
    {
        return Results.NotFound(new { error = "WASM binary not found" });
    }

    try
    {
        var wasmBytes = await File.ReadAllBytesAsync(wasmPath);
        
        context.Response.Headers.ContentType = "application/wasm-encrypted";
        context.Response.Headers["Cache-Control"] = "public, max-age=3600";
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        
        return Results.Bytes(wasmBytes, contentType: "application/wasm-encrypted");
    }
    catch (Exception)
    {
        return Results.Problem(
            detail: "Failed to read WASM binary",
            statusCode: 500
        );
    }
})
.WithName("GetWasmRuntime")
.WithOpenApi()
.WithDescription("Serves the encrypted WASM runtime binary");

/// <summary>
/// GET /api/version
/// Returns version information about the API and WASM runtime.
/// </summary>
app.MapGet("/api/version", () =>
{
    return Results.Ok(new
    {
        api = new
        {
            version = "1.0.0",
            build = GetBuildVersion()
        },
        wasm = new
        {
            version = "1.0.0",
            engine = "Stride 4.2",
            runtime = ".NET 9.0"
        },
        security = new
        {
            encryption = "AES-256-GCM",
            keyDerivation = "HMAC-SHA256",
            keyValidity = "5 minutes"
        }
    });
})
.WithName("GetVersion")
.WithOpenApi()
.WithDescription("Returns version and security information");

// Default route - serve index.html
app.MapGet("/", () => Results.Redirect("/index.html"));

app.Run();

/// <summary>
/// Gets or creates the master secret for ephemeral key generation.
/// In production, this should be stored in a secure key vault.
/// </summary>
static byte[] GetOrCreateMasterSecret(IConfiguration config)
{
    // Try to get from configuration
    var secretBase64 = config["Security:MasterSecret"];
    
    if (!string.IsNullOrEmpty(secretBase64))
    {
        try
        {
            return Convert.FromBase64String(secretBase64);
        }
        catch
        {
            // Fall through to generate new secret
        }
    }

    // Generate new secret (WARNING: This should be persisted in production)
    var secret = EphemeralKeyGenerator.GenerateSalt();
    Console.WriteLine("WARNING: Generated new master secret. In production, store this securely:");
    Console.WriteLine($"Security:MasterSecret={Convert.ToBase64String(secret)}");
    
    return secret;
}

/// <summary>
/// Gets the build version from assembly or environment.
/// </summary>
static string GetBuildVersion()
{
    var version = System.Reflection.Assembly.GetExecutingAssembly()
        .GetName()
        .Version;
    
    return version?.ToString() ?? "unknown";
}
