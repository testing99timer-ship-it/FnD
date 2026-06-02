var builder = WebApplication.CreateBuilder(args);

// Add YARP services from appsettings.json
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseHttpsRedirection();

// Map the reverse proxy pipeline
app.MapReverseProxy();

app.Run();