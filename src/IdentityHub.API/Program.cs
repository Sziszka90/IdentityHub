using IdentityHub.API.Extensions;
using IdentityHub.API.Middleware;
using IdentityHub.Infrastructure.Seeding;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Authorization.json", optional: true, reloadOnChange: true);

// Add appsettings.Development.json if in Development environment
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

await AuthorizationDbSeeder.SeedFromConfigAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerDocumentation();
}

app.UseGlobalExceptionHandler();

app.UseHttpsRedirection();

app.UseCors("AllowAll");


app.UseAuthentication();
// Isolation must run first to populate TenantContext, then validation can check it
app.UseTenantIsolation();
app.UseMiddleware<TenantContextValidationMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
