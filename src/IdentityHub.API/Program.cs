using IdentityHub.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configure authentication with Azure Entra ID
builder.Services.AddEntraIdAuthentication(builder.Configuration);

// Configure authorization policies
builder.Services.AddAuthorizationPolicies();

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
app.UseAuthorization();

app.MapControllers();

app.Run();
