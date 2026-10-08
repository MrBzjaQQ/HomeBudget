using System.Globalization;
using System.Text;
using Budget.Application;
using Budget.Application.Common;
using BudgetApi.Auth;
using BudgetApi.Common;
using Budget.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
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
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var (message, errors) = ModelStateSanitizer.Sanitize(context.ModelState);
        var payload = new ApiError
        {
            Code = ApiErrorCodes.ValidationFailed,
            Message = message,
            Errors = errors,
        };
        return new BadRequestObjectResult(payload);
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
builder.Services.AddApplicationServices();
builder.Services.AddDatabase(builder.Configuration);

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
        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json; charset=utf-8";
                return context.Response.WriteAsync($"{{\"code\":\"{ApiErrorCodes.Unauthorized}\",\"message\":\"Требуется авторизация\"}}");
            },
        };
    });
builder.Services.AddAuthorization();

var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(corsOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

if (!app.Environment.IsEnvironment(ApiEnvironments.Testing))
{
    await app.MigrateDatabaseAsync();
    await app.SeedDatabaseAsync();
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
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var path = ctx.Context.Request.Path.Value ?? string.Empty;
        if (path.EndsWith(".html", System.StringComparison.OrdinalIgnoreCase))
        {
            ctx.Context.Response.Headers.CacheControl = "no-cache";
        }
        else if (System.Text.RegularExpressions.Regex.IsMatch(path, @"-\w{8,}\.(?:js|css|svg|woff2?|ttf)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
        {
            ctx.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        }
    },
});
app.MapFallback(async context =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsync($"{{\"code\":\"{ApiErrorCodes.NotFound}\",\"message\":\"Не найдено\"}}");
        return;
    }

    var webRoot = app.Environment.WebRootPath
        ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
    var indexPath = Path.Combine(webRoot, "index.html");
    if (System.IO.File.Exists(indexPath))
    {
        context.Response.Headers.CacheControl = "no-cache";
        await context.Response.SendFileAsync(indexPath);
    }
    else
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsync("SPA is not built yet. Run 'npm run build' in frontend/ and publish.");
    }
});

app.Run();

/// <summary>Точка входа веб-приложения, доступная тестовой фабрике хоста.</summary>
public partial class Program;

/// <summary>Преобразует имена перечислений в JSON в верхний регистр.</summary>
public sealed class UpperCaseNamingPolicy : System.Text.Json.JsonNamingPolicy
{
    /// <summary>Возвращает заданное имя без изменения регистра букв.</summary>
    /// <param name="name">Имя значения перечисления до преобразования.</param>
    /// <returns>Имя в верхнем регистре.</returns>
    public override string ConvertName(string name) => name.ToUpperInvariant();
}
