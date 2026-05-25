using IdentityHub.API.Extensions;
using IdentityHub.API.Middleware;
using IdentityHub.Infrastructure.Seeding;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);
}

builder.Services.AddEntraIdAuthentication(builder.Configuration);

builder.Services.AddGraphApi(builder.Configuration);

builder.Services.AddApplicationServices(builder.Configuration);

builder.Services.AddAuthorizationDatabase(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

builder.Services.AddAutoMapper(typeof(IdentityHub.API.Mapping.UserMappingProfile).Assembly);

builder.Services.AddSwaggerDocumentation();

builder.Services.AddCorsPolicy();

var app = builder.Build();

var tenantConfiguration = app.Configuration
    .GetSection(IdentityHub.Domain.Models.TenantConfigurationOptions.SectionName)
    .Get<IdentityHub.Domain.Models.TenantConfigurationOptions>();

if (tenantConfiguration?.EnableStartupSeeding != false)
{
    await AuthorizationDbSeeder.SeedFromConfigAsync(app.Services);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerDocumentation();
}

app.UseGlobalExceptionHandler();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseTenantIsolation();
app.UseMiddleware<TenantContextValidationMiddleware>();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.MapControllers();

app.Run();

public partial class Program { }
