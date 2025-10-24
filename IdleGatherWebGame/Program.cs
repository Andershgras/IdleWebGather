using IdleGatherWebGame;
using IdleGatherWebGame.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// ⬇️ Mount the root <App /> into the #app div in index.html
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Base HttpClient (fine to keep for later API calls)
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Browser storage (localStorage wrapper)
builder.Services.AddScoped<IBrowserStorage, BrowserStorage>();

builder.Services.AddScoped<GameState>();                       // <-- must be Scoped

await builder.Build().RunAsync();
