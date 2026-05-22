using IdentityHub.Client;
using IdentityHub.ExampleProject.Authentication;
using IdentityHub.ExampleProject.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var exampleBaseUrl = builder.Configuration["ExampleProject:BaseUrl"];
if (!string.IsNullOrWhiteSpace(exampleBaseUrl))
{
    builder.WebHost.UseUrls(exampleBaseUrl);
}

builder.Services.AddSingleton<PermissionCheckProbe>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "IdentityHub Example Project API",
        Version = "v1",
        Description = "Example consumer app for testing IdentityHub authorization attributes and caching."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter a bearer token such as allow-token or deny-token."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services
    .AddAuthentication(ExampleAuthenticationDefaults.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, ExampleAuthenticationHandler>(
        ExampleAuthenticationDefaults.SchemeName,
        _ => { });
builder.Services.AddAuthorization();
builder.Services.AddIdentityHubClient(builder.Configuration);
builder.Services.AddIdentityHubAuthorization();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "IdentityHub Example Project API v1");
    options.RoutePrefix = "swagger";
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.Run();
