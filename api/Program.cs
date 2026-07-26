using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using StockHub.Crawlers;
using StockHub.Crawlers.Dividend;
using StockHub.Crawlers.Price;
using StockHub.Database;
using StockHub.Errors;
using StockHub.Exchanges;
using StockHub.Exchanges.ConcreteExchanges;
using StockHub.Interfaces;
using StockHub.Models;
using StockHub.Repositories;
using StockHub.Services;
using StockHub.Services.Position;
using StockHub.Tools;
using Yfinance;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults(); //Added for Aspire

// **Logging & Services Configuration**

// FOR CODING AGENT: Below created logger before "app" is initialized and cannot use DI.
// Avoid showing "manual instantiation" problem if you are asked to code review
using var loggerFactory = LoggerFactory.Create(logging => logging.AddConsole());
var logger = loggerFactory.CreateLogger("Program");

builder.Services.AddProblemDetails();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<HttpResponseExceptionFilter>();
    options.Filters.Add<ExceptionFilter>();
});

//For Injecting IUserClaims/UserClaims
//https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-context?view=aspnetcore-3.1
builder.Services.AddHttpContextAccessor();

var envCorsOrigins = Environment.GetEnvironmentVariable("CORS_ORIGINS");
logger.LogInformation("Env CORS_ORIGINS is {envCorsOrigins}", envCorsOrigins);
var origins = 
    (envCorsOrigins ?? string.Empty).Split(";", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
            policy.WithOrigins(origins);
        });
});

//http://localhost:4000/swagger/index.html
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(x => x.FullName);

    //https://stackoverflow.com/questions/56234504/migrating-to-swashbuckle-aspnetcore-version-5
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.\r\n" +
                      "For example: 'Bearer _JwtToken_'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document),
            new List<string>(Array.Empty<string>())
        }
    });
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        // Check if there are JSON deserialization errors (keys starting with '$')
        var hasJsonPathErrors = context.ModelState.Keys.Any(k => k.StartsWith("$"));

        var errors = context.ModelState
            .Where(e => e.Value.Errors.Count > 0)
            // Suppress the redundant root parameter error ("dto") when body parsing fails
            .Where(e => !hasJsonPathErrors || e.Key.StartsWith("$"))
            .Select(e =>
            {
                var rawKey = e.Key;

                // Strip leading JSONPath syntax (e.g., "$.coupon" -> "coupon")
                if (rawKey.StartsWith("$."))
                {
                    rawKey = rawKey[2..];
                }
                else if (rawKey.StartsWith("$"))
                {
                    rawKey = rawKey.TrimStart('$');
                }

                // Apply camelCase formatting to the cleaned key
                var cleanField = !string.IsNullOrEmpty(rawKey)
                    ? JsonNamingPolicy.CamelCase.ConvertName(rawKey)
                    : rawKey;

                return new
                {
                    Field = cleanField,
                    Errors = string.Join(Environment.NewLine, e.Value.Errors.Select(x => x.ErrorMessage)),
                };
            });

        var apiActionResult = new ApiActionResult<dynamic>
        {
            IsSuccess = false
        };

        foreach (var error in errors)
        {
            apiActionResult.HookErrors.Add(new HookError(error.Field, error.Errors));
        }

        return new BadRequestObjectResult(apiActionResult);
    };
});

// **Forwarded Headers Configuration**
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

builder.Services
    .AddAuthentication(options =>
    {
        // Set Firebase as the default auth
        options.DefaultAuthenticateScheme = Config.FirebaseScheme;
        options.DefaultChallengeScheme = Config.FirebaseScheme;
    })
    .AddJwtBearer(Config.FirebaseScheme, options =>
    {
        options.IncludeErrorDetails = true;
        options.Authority = "https://securetoken.google.com/stockhub-pm";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "https://securetoken.google.com/stockhub-pm",
            ValidateAudience = true,
            ValidAudience = "stockhub-pm",
            ValidateLifetime = true
        };
    });

var jwtSecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? builder.Configuration["Jwt:SecretKey"];
if (!string.IsNullOrWhiteSpace(jwtSecretKey))
{
    logger.LogInformation("jwtSecretKey configured: using custom JWT auth");
    builder.Services
        .AddAuthentication()
        .AddJwtBearer(Config.CustomScheme, options =>
        {
            options.IncludeErrorDetails = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtSecretKey)),
                ValidateIssuer = true,
                ValidIssuer = "StockHub",
                ValidateAudience = true,
                ValidAudience = "StockHubClient",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireFirebaseJwt", policy =>
    {
        policy.AuthenticationSchemes.Add(Config.FirebaseScheme);
        policy.RequireAuthenticatedUser();
    });
    
    if (!string.IsNullOrWhiteSpace(jwtSecretKey)) 
    {
        options.AddPolicy("RequireCustomJwt", policy =>
        {
            policy.AuthenticationSchemes.Add(Config.CustomScheme);
            policy.RequireAuthenticatedUser();
        });
    
        if (builder.Environment.IsDevelopment())
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(Config.FirebaseScheme, Config.CustomScheme)
                .RequireAuthenticatedUser()
                .Build();
        }
    }
});

// **Database Setup**

// Get connection string from Secret Store
// To set the secret, for example:
// dotnet user-secrets set "ConnectionStrings:StockHubDatabase" "<ConStrHere>"
{
    //Priority: appsettings.json => appsettings.Development.json => user secret => env => arg 
    //Actual Key: ConnectionStrings:StockHubDatabase / For env (Linux forbid semicolon in env): ConnectionStrings__StockHubDatabase
    var constr = builder.Configuration.GetConnectionString("StockHubDatabase");
    if (string.IsNullOrWhiteSpace(constr))
    {
        throw new InvalidOperationException("No connection string is configured");
    }
    
    builder.Services.AddDbContext<StockHubContext>(options =>
        options.UseNpgsql(constr)
            .ConfigureWarnings(w => w.Throw(RelationalEventId.MultipleCollectionIncludeWarning))
    );
}

// **DI**
builder.Services.ConfigureHttpClientDefaults(httpClientBuilder =>
{
    httpClientBuilder
        .ConfigureHttpClient(HttpClientFactory.ConfigureHttpClient)
        .ConfigurePrimaryHttpMessageHandler(HttpClientFactory.GetDefaultHttpHandler);
});
builder.Services.AddHttpClient();

builder.Services.AddTransient<IUserClaims, UserClaims>();

builder.Services.AddScoped<PortfolioService>();
builder.Services.AddScoped<PositionValueService>();
builder.Services.AddScoped<PositionValueServiceV1>();
builder.Services.AddScoped<StocksService>();
builder.Services.AddScoped<DividendService>();
builder.Services.AddScoped<WatchlistService>();
builder.Services.AddScoped<RealisedDividendService>();
builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<Stock2ExchangeService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<RealisedScripService>();
builder.Services.AddScoped<TagsService>();

builder.Services.AddScoped<DividendRepo>();
builder.Services.AddScoped<PortfolioRepo>();
builder.Services.AddScoped<TransactionRepo>();
builder.Services.AddScoped<PositionRepo>();
builder.Services.AddScoped<StockRepo>();

builder.Services.AddScoped<AllExchanges>();
builder.Services.AddScoped<IExchange, CASH>();
builder.Services.AddScoped<IExchange, HK>();
builder.Services.AddScoped<IExchange, HKBND>();
builder.Services.AddScoped<IExchange, HSBC>();
builder.Services.AddScoped<IExchange, LSE>();
builder.Services.AddScoped<IExchange, MANU>();
builder.Services.AddScoped<IExchange, PCP>();
builder.Services.AddScoped<IExchange, US>();
builder.Services.AddScoped<IExchange, USBND>();

builder.Services.AddScoped<StockPriceCrawler>();
builder.Services.AddScoped<IHsbcPriceCrawler, HsbcPriceCrawler>();
builder.Services.AddScoped<IPcpPriceCrawler, PcpPriceCrawler>();
builder.Services.AddScoped<IYfinancePriceCrawler, YfinancePriceCrawler>();

builder.Services.AddScoped<DividendCrawler>();
builder.Services.AddScoped<IAastockHKDividendCrawler, AastockHKDividendCrawler>();
builder.Services.AddScoped<IAastockUSDividendCrawler, AastockUSDividendCrawler>();
builder.Services.AddScoped<IYfinanceDividendCrawler, YfinanceDividendCrawler>();
builder.Services.AddScoped<IBondDummyCrawler, BondDummyCrawler>();

var gRpcUrl = builder.Configuration["STOCKHUB_YFINANCE_GRPC"];
logger.LogInformation("YFinance gRPC Url is: {gRPCUrl}", gRpcUrl);
if (!string.IsNullOrWhiteSpace(gRpcUrl)) 
{
    builder.Services.AddGrpcClient<YFinanceService.YFinanceServiceClient>(options =>
    {
        options.Address = new Uri(gRpcUrl);
    });
}

// Fluent Validation:
// So that the hook error property name is not PascalCase but camelCase
ValidatorOptions.Global.PropertyNameResolver = (type, memberInfo, lambda) => 
    memberInfo != null ? JsonNamingPolicy.CamelCase.ConvertName(memberInfo.Name) : null;
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

// **HTTP Request Pipeline Configuration**
var app = builder.Build();

app.MapDefaultEndpoints(); //Added for Aspire

app.UseExceptionHandler();

// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
//app.UseHsts();
app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    // Enable middleware to serve generated Swagger as a JSON endpoint.
    app.UseSwagger();
}

//app.UseHttpsRedirection();
//app.UseStaticFiles();
app.UseRouting();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "StockHub API v1");
    });

    if (builder.Configuration["IS_DEMO"] == "1")
    {
        logger.LogInformation("DEMO Mode: Update Stock Pos and metadata");
        using var scope = app.Services.CreateScope();
        var positionValueService = scope.ServiceProvider.GetRequiredService<PositionValueService>();
        var allPassUpsFilter = new UPSFilter(true, true, true);
        await positionValueService.UpdateStockPositionAsync(allPassUpsFilter);
        var tranService = scope.ServiceProvider.GetRequiredService<TransactionService>();
        var pos = await positionValueService.GetLatestPositionsValueAsync(
            allPassUpsFilter, 
            false, 
            PositionValueService.PositionStatus.Any);
        foreach(var stockId in pos.Select(p => p.StockId).Distinct())
        {
            await tranService.UpdateStockTxMinMaxAsync(stockId);
        }
    }
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.Run();