using FnD.LocalPOS.Client;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// CONFIGURE HTTP CLIENT: Point this directly to your running FnD.Cloud.API project port
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("https://localhost:7109/") // Change this port to match your backend running port
});

await builder.Build().RunAsync();