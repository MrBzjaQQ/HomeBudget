using System.Globalization;
using System.Text;
using BudgetApi.Auth;
using BudgetApi.Common;
using BudgetApi.Data;
using BudgetApi.Export;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14, shared: true)
    .CreateBootstrapLogger();

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14, shared: true));

var dbProvider = builder.Configuration["Database:Provider"] ?? "Npgsql";
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured");

builder.Services.AddControllers().AddJsonOptions(options =>
{
    var json = options.JsonSerializerOptions;
    json.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    json.PropertyNameCaseInsensitive = true;
    if (!json.Converters.Any(c => c is System.Text.Json.Serialization.JsonStringEnumConverter))
    {
        json.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(new UpperCaseNamingPolicy()));
    }
});
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = new Dictionary<string, string>();
        foreach (var entry in context.ModelState)
        {
            var messages = entry.Value?.Errors.Where(e => e.ErrorMessage is not null).Select(e => e.ErrorMessage!).ToList();
            if (messages is { Count: > 0 })
            {
                errors[entry.Key] = string.Join("; ", messages);
            }
        }

        var payload = new ApiError
        {
            Code = "ValidationFailed",
            Message = "Некорректные данные",
            Errors = errors,
        };
        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(payload);
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Бюджет API",
        Version = "v1",
    });
});

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddSingleton<ExcelExportService>();
builder.Services.AddDbContext<BudgetDbContext>(options =>
{
    if (dbProvider == "InMemory")
    {
        options.UseInMemoryDatabase(builder.Configuration["Database:InMemoryName"] ?? "budget-test");
    }
    else
    {
        options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention(CultureInfo.InvariantCulture);
    }
});

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()!;

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });
builder.Services.AddAuthorization();

var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(corsOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BudgetDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db);
}

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallback(async context =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsync("{\"code\":\"NotFound\",\"message\":\"Не найдено\"}");
        return;
    }

    var webRoot = app.Environment.WebRootPath
        ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
    var indexPath = Path.Combine(webRoot, "index.html");
    if (System.IO.File.Exists(indexPath))
    {
        await context.Response.SendFileAsync(indexPath);
    }
    else
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsync("SPA is not built yet. Run 'npm run build' in frontend/ and publish.");
    }
});

app.Run();

public partial class Program;

public sealed class UpperCaseNamingPolicy : System.Text.Json.JsonNamingPolicy
{
    public override string ConvertName(string name) => name.ToUpperInvariant();
}
