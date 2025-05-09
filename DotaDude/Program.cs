using DotaDude.Data;
using DotaDude.Middleware; // Aggiungi questo namespace
using DotaDude.Models;
using DotaDude.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using DotaDude.Services;
using System.Security.Claims;
using AspNet.Security.OpenId.Steam;



var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<OpenDotaService>();
builder.Services.AddScoped<GeminiService>();

// Configura SQLite
builder.Services.AddDbContext<DotaDudeDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Aggiungi HttpContextAccessor necessario per AuthService
builder.Services.AddHttpContextAccessor();

// Configura le sessioni
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Aggiungi il servizio di autenticazione per cookie con configurazione migliorata
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.Name = "DotaDude.Auth";
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
    options.LoginPath = "/Login";
    options.LogoutPath = "/Logout";
    options.AccessDeniedPath = "/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});


// Registra AuthService come scoped service
builder.Services.AddScoped<AuthService>();

builder.Services.AddHttpClient<GeminiService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["GeminiApi:BaseUrl"]);
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    client.Timeout = TimeSpan.FromSeconds(15); // Timeout più breve per evitare attese lunghe
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});

// Register HTTP client for OpenDota API
builder.Services.AddHttpClient<OpenDotaService>(client =>
{
    client.BaseAddress = new Uri("https://api.opendota.com/api/");
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
})

.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});

var app = builder.Build();

// ** Crea l'utente admin al primo avvio in un contesto più dettagliato **
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<DotaDudeDbContext>();

        // Assicurati che il database esista
        dbContext.Database.EnsureCreated();

        Console.WriteLine("Database creato/verificato con successo");

        // Controlla se l'utente admin esiste
        var adminUser = dbContext.Users.FirstOrDefault(u => u.Username.ToLower() == "admin");

        if (adminUser == null)
        {
            // Crea l'utente admin
            // Usa SHA256 per generare l'hash della password "admin123"
            var hashedPassword = HashPassword("admin123");
            Console.WriteLine($"Hash password admin con SHA256: {hashedPassword}");

            dbContext.Users.Add(new User
            {
                Username = "admin",
                Email = "admin@dotadude.com", // Aggiungiamo un'email valida
                PasswordHash = hashedPassword,
                IsActive = true,
                RegistrationDate = DateTime.Now,
                AvatarUrl = "/images/avatars/admin.png",
                PreferredRole = "support" // Un valore di default
            });

            dbContext.SaveChanges();
            Console.WriteLine("Utente admin creato con successo (username: admin, password: admin123)");
        }
        else
        {
            Console.WriteLine($"Utente admin esistente - Username: {adminUser.Username}, IsActive: {adminUser.IsActive}, Hash: {adminUser.PasswordHash}");

            // Assicurati che l'utente admin sia attivo e con la password corretta
            if (!adminUser.IsActive || adminUser.PasswordHash != HashPassword("admin123"))
            {
                adminUser.IsActive = true;
                adminUser.PasswordHash = HashPassword("admin123");
                dbContext.SaveChanges();
                Console.WriteLine("Utente admin aggiornato (attivato e password resettata)");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Errore durante la creazione/verifica dell'utente admin: {ex.Message}");
        Console.WriteLine($"StackTrace: {ex.StackTrace}");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Aggiungi UseSession prima di UseAuthentication
app.UseSession();

// IMPORTANTE: UseAuthentication PRIMA di UseAuthorization
app.UseAuthentication();
app.UseAuthorization();

// Usa il middleware di autorizzazione admin personalizzato
app.UseMiddleware<AdminAuthorizationMiddleware>();
// Oppure usa l'estensione:
// app.UseAdminAuthorization();

// Mappa le pagine Razor e i controller
app.MapRazorPages();
app.MapControllers();

app.Run();

// ** Metodo per hash della password **
static string HashPassword(string password)
{
    using (var sha256 = SHA256.Create())
    {
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
    }
}