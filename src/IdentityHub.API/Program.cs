using IdentityHub.API.Extensions;
using IdentityHub.API.Middleware;
using IdentityHub.Infrastructure.Seeding;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Authorization.json", optional: false, reloadOnChange: true);

builder.Services.AddEntraIdAuthentication(builder.Configuration);

builder.Services.AddGraphApi(builder.Configuration);

builder.Services.AddApplicationServices(builder.Configuration);

builder.Services.AddAuthorizationDatabase(builder.Configuration);

builder.Services.AddControllers();

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
// Validate tenant context for every request
app.UseMiddleware<TenantContextValidationMiddleware>();
app.UseTenantIsolation();
app.UseAuthorization();

app.MapControllers();

app.Run();
