using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlazorPWA;
using BlazorPWA.Configuration;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.Configure<AppSettings>(builder.Configuration);
builder.Services.AddHttpClient();
builder.Services.AddSingleton<BlazorPWA.Services.Rpc.RpcClientFactory>();
builder.Services.AddSingleton<BlazorPWA.State.GameState>();
builder.Services.AddSingleton<BlazorPWA.Services.Connection.IGameConnectionService, BlazorPWA.Services.Connection.GameConnectionService>();

await builder.Build().RunAsync();
