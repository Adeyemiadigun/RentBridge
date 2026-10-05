using System.Text.Json.Serialization;
using Asp.Versioning;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using RentBridge.Api.Hangfire;
using RentBridge.Api.Middleware;
using RentBridge.Api.Swagger;
using RentBridge.Api.Versioning;
using RentBridge.Application;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Domain.Aggregates;
using RentBridge.Domain.Enums;
using RentBridge.Infrastructure;
using RentBridge.Infrastructure.Persistence;
using RentBridge.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Render: Postgres addons expose DATABASE_URL as postgres://user:pass@host:port/db
// Npgsql/EF Core needs Host=...;Port=...;Database=... format, so convert it here.
// If ConnectionStrings__DefaultConnection is set explicitly, it wins.
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrWhiteSpace(databaseUrl) &&
    string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("DefaultConnection")))
{
    try
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':', 2);
        var npgsqlConn =
            $"Host={uri.Host};Port={uri.Port};" +
            $"Database={uri.AbsolutePath.Trim('/')};" +
            $"Username={userInfo[0]};Password={(userInfo.Length > 1 ? userInfo[1] : string.Empty)};" +
            "SSL Mode=Require;Trust Server Certificate=true;Timeout=100";
        builder.Configuration["ConnectionStrings:DefaultConnection"] = npgsqlConn;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"WARNING: Could not parse DATABASE_URL: {ex.Message}");
    }
}

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddOpenApi();

// The hosted web client is served from a different origin than this API, so browser
// requests to /api/v1 are cross-origin and need an explicit CORS policy. Origins come
// from configuration so each environment can name its own client without a code change.
const string CorsPolicyName = "WebClients";
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? Array.Empty<string>();
builder.Services.AddCors(options => options.AddPolicy(
    CorsPolicyName,
    policy => policy
        .WithOrigins(corsOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()));

// URL-segment versioning: /api/v1/.... Unversioned requests are assumed v1,
// and clients can also pass ?api-version= or the X-Version header.
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new QueryStringApiVersionReader("api-version"),
        new HeaderApiVersionReader("X-Version"));
})
.AddMvc()
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddInfrastructure(builder.Configuration);

var hangfireConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

builder.Services.AddHangfire(config =>
{
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180);
    config.UseSimpleAssemblyNameTypeSerializer();
    config.UseRecommendedSerializerSettings();
    config.UsePostgreSqlStorage(c => c.UseNpgsqlConnection(hangfireConnectionString));
});
builder.Services.AddHangfireServer();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT settings are not configured.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
    };
});

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddTransient<IConfigureOptions<Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions>, ConfigureSwaggerOptions>();

builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste your access token. The Authorization header will be 'Bearer <token>'."
    });

    // Per-operation lock icons: only endpoints that require auth get the
    // Bearer requirement (see BearerSecuritySchemeOperationFilter).
    c.OperationFilter<BearerSecuritySchemeOperationFilter>();
});

var app = builder.Build();

// Apply EF Core migrations on startup so Render deploys don't need a manual step.
using (var migrateScope = app.Services.CreateScope())
{
    var db = migrateScope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Bootstrap the default admin (Admin:Email/Phone/Password). Skips when
// unconfigured or when an Admin already exists — never blocks startup.
// Only this default admin can create further admin accounts.
using (var seedScope = app.Services.CreateScope())
{
    var sp = seedScope.ServiceProvider;
    var db = sp.GetRequiredService<AppDbContext>();
    var passwords = sp.GetRequiredService<IPasswordService>();
    var adminEmail = builder.Configuration["Admin:Email"];
    var adminPhone = builder.Configuration["Admin:Phone"];
    var adminPassword = builder.Configuration["Admin:Password"];
    var adminFirst = builder.Configuration["Admin:FirstName"] ?? "System";
    var adminLast = builder.Configuration["Admin:LastName"] ?? "Admin";

    if (string.IsNullOrWhiteSpace(adminEmail)
        || string.IsNullOrWhiteSpace(adminPhone)
        || string.IsNullOrWhiteSpace(adminPassword))
    {
        app.Logger.LogWarning("Admin bootstrap skipped: Admin:Email/Phone/Password is not fully configured.");
    }
    else if (await db.Set<User>().AnyAsync(u => u.Role == UserRole.Admin))
    {
        app.Logger.LogInformation("Admin bootstrap skipped: an Admin user already exists.");
    }
    else
    {
        var email = RentBridge.Domain.ValueObjects.Email.Create(adminEmail);
        var phone = RentBridge.Domain.ValueObjects.PhoneNumber.Create(adminPhone);
        if (email.IsSuccess is false || phone.IsSuccess is false)
        {
            app.Logger.LogWarning("Admin bootstrap skipped: {Error}",
                email.IsSuccess is false ? email.Error : phone.Error);
        }
        else
        {
            var (hash, salt) = passwords.Generate(adminPassword);
            var admin = new User(email.Value, phone.Value, adminFirst, adminLast, UserRole.Admin);
            admin.SetPassword(hash, salt);
            db.Set<User>().Add(admin);
            await db.SaveChangesAsync();
            app.Logger.LogInformation("Admin bootstrap: default admin {Email} created.", adminEmail);
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    foreach (var description in app.DescribeApiVersions())
    {
        c.SwaggerEndpoint(
            $"/swagger/{description.GroupName}/swagger.json",
            $"Rent Bridge API {description.GroupName.ToUpperInvariant()}");
    }
});

app.UseHttpsRedirection();

// Must run before authentication so preflight requests short-circuit.
app.UseCors(CorsPolicyName);

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new DevelopmentOnlyDashboardAuthorizationFilter() }
});

app.UseMiddleware<ExceptionHandlerMiddleware>();

app.MapControllers();

// Reconciliation sweep: self-heals payouts that were claimed but never finalized
// (e.g. the process died between the claim and the provider transfer).
// Registration only upserts the schedule Hangfire already stores in its tables, so a
// lock timeout here must not take the API down. During a rolling deploy the outgoing
// instance can still hold this lock, and the previously stored schedule keeps firing.
using (var scope = app.Services.CreateScope())
{
    var dispatcher = scope.ServiceProvider.GetRequiredService<IBackgroundJobDispatcher>();
    try
    {
        dispatcher.AddOrUpdateRecurring<IEscrowReleaseService>(
            "escrow-payout-reconciliation",
            s => s.ReconcileStuckPayoutsAsync(),
            "*/10 * * * *");
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(
            ex,
            "Could not refresh the escrow-payout-reconciliation schedule on startup; " +
            "the existing Hangfire schedule stays active.");
    }
}

app.Run();