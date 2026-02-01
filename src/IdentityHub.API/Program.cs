using IdentityHub.API.Extensions;
using IdentityHub.API.Middleware;
using IdentityHub.Application.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Load authorization configuration
builder.Configuration.AddJsonFile("appsettings.Authorization.json", optional: false, reloadOnChange: true);

// Configure authentication with Azure Entra ID
builder.Services.AddEntraIdAuthentication(builder.Configuration);

// Configure authorization policies
builder.Services.AddAuthorizationPolicies(builder.Configuration);

// Register application services
builder.Services.AddApplicationServices(builder.Configuration);

// Configure controllers
builder.Services.AddControllers();

// Configure Swagger/OpenAPI
builder.Services.AddSwaggerDocumentation();

// Configure CORS
builder.Services.AddCorsPolicy();

var app = builder.Build();

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerDocumentation();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseTenantIsolation();
app.UseAuthorization();

app.MapControllers();

app.Run();
