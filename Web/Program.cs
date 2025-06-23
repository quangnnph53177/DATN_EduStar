using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Users/Login";
        options.AccessDeniedPath = "/Users/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = true;
    });

builder.Services.AddHttpClient("EdustarAPI", client =>
{
    client.BaseAddress = new Uri("https://localhost:7298/");
    client.DefaultRequestHeaders.Accept.Add(
        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
});
// Add services to the container.
builder.Services.AddControllersWithViews();
var app = builder.Build();

//Middleware ki?m tra token cookie (b?o v? các route c?n ??ng nh?p)
//app.Use(async (context, next) =>
//{
//    var path = context.Request.Path.Value?.ToLower();
//    var method = context.Request.Method.ToUpper();

//    // N?u truy c?p ???ng d?n ngoài public (login, css, js,...)
//    if (!(path!.Contains("/users/login") && !method.Contains("GET") && !method.Contains("POST") && !method.Contains("PUT") &&
//        !path.StartsWith("/css") && !path.StartsWith("/js") &&
//        !path.StartsWith("/lib")) // n?u có th? vi?n static
//    {
//        // Ki?m tra cookie token t?n t?i không
//        if (!context.Request.Cookies.ContainsKey("JWToken"))
//        {
//            // Ch?a ??ng nh?p, chuy?n v? Login
//            context.Response.Redirect("/Users/Login");
//            return; // Không g?i next middleware n?a
//        }
//    }

//    await next();
//});
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    //app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
