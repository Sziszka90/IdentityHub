using IdentityHub.API.Extensions;
using IdentityHub.API.Middleware;
using IdentityHub.Application.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Authorization.json", optional: false, reloadOnChange: true);

builder.Services.AddEntraIdAuthentication(builder.Configuration);

builder.Services.AddGraphApi(builder.Configuration);

builder.Services.AddAuthorizationPolicies(builder.Configuration);

builder.Services.AddApplicationServices(builder.Configuration);

builder.Services.AddRedisCache(builder.Configuration);

builder.Services.AddControllers();

builder.Services.AddSwaggerDocumentation();

builder.Services.AddCorsPolicy();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerDocumentation();
}

app.UseGlobalExceptionHandler();

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();

app.UseTenantIsolation();

app.UseAuthorization();

app.MapControllers();

app.Run();
