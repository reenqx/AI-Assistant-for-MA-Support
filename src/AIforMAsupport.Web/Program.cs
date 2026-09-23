using AIforMAsupport.Web.Services.Api;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddHttpClient<ICaseCopilotApiClient, CaseCopilotApiClient>(client =>
{
    var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5299/";
    client.BaseAddress = new Uri(apiBaseUrl);
    // Must exceed the API's own ClaudeCli:TimeoutSeconds (150s) so the API's own timeout error
    // is what surfaces, not this client giving up first.
    client.Timeout = TimeSpan.FromSeconds(180);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// No HTTPS redirect in this dev setup: the app currently only runs on plain HTTP (see
// launchSettings.json / --urls). Re-add once this is actually served behind TLS (e.g. a
// reverse proxy) in production.
app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();

app.MapRazorPages();

app.Run();
