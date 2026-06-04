var builder = WebApplication.CreateBuilder(args);

// Register CORS Services matching your Client port
builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorDevPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5090", "https://localhost:5090")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Add YARP services from appsettings.json
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Enable CORS right at the start of the pipeline
app.UseCors("BlazorDevPolicy");

app.UseHttpsRedirection();

// Inject your updated Tenant validation middleware
app.UseMiddleware<FnD.Gateway.API.Middleware.TenantValidationMiddleware>();

// Map the reverse proxy pipeline
app.MapReverseProxy();

app.Run();