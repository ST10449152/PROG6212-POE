using RaceDayMVC.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Allows the RaceDayMVC to communicate with RaceDayAPI.
builder.Services.AddHttpClient("RaceDayAPI", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["RaceDayAPI:BaseUrl"]!);
})
.ConfigurePrimaryHttpMessageHandler(() =>
    new HttpClientHandler
    {
        UseCookies = false
    });
// Allow services to access the current HTTP request.
builder.Services.AddHttpContextAccessor();

// Register the API service.
builder.Services.AddScoped<RaceDayApiService>();

// Add session services.
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);

    // Use a different cookie name from RaceDayAPI.
    options.Cookie.Name = ".RaceDayMVC.Session";

    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// Enable session management.
app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
