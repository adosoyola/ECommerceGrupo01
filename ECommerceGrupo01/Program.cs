using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ECommerce.Data;
using ECommerce.Services;
using Microsoft.AspNetCore.Identity.UI.Services;
using DinkToPdf;
using DinkToPdf.Contracts;

var builder = WebApplication.CreateBuilder(args);

// --- 1. SERVICIOS (Configuración de Dependencias) ---

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Correo (Email Settings)
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddTransient<IEmailService, EmailService>();

// Conexión a SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity (Autenticación)
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false; // Sin confirmación de correo obligatoria
    options.Password.RequireDigit = false;          // (Opcional) Relajar requisitos de contraseña para pruebas
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Servicios Extra
builder.Services.AddTransient<IEmailSender, EmailSender>(); // Dummy EmailSender
builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools())); // DinkToPdf
builder.Services.AddScoped<PaymentProcessor>(); // Procesador de Pagos

// MVC y Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// ✅ SESIÓN (Configuración)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // 30 min de inactividad
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// --- 2. PIPELINE HTTP (Orden de Ejecución) ---

// Middleware de Errores y Seguridad
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ✅ SESIÓN (Debe ir ANTES de Auth)
app.UseSession();

// Autenticación y Autorización
app.UseAuthentication();
app.UseAuthorization();

// Rutas (Endpoints)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Admin}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// Swagger (Solo visualización)
app.UseSwagger();
app.UseSwaggerUI();

// --- 3. SEEDING (Crear Datos Iniciales) ---
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    // A. Crear Roles
    string[] roles = { "ADMIN", "CLIENTE", "LOGISTICA" }; // Agregué Logística por si acaso
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // B. Crear Admin por Defecto
    var adminEmail = "admin@ecommerce.com";
    var adminPassword = "Admin123!";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        adminUser = new IdentityUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "ADMIN");
        }
    }
    else
    {
        // Si ya existe, asegurar que tenga el rol
        if (!await userManager.IsInRoleAsync(adminUser, "ADMIN"))
        {
            await userManager.AddToRoleAsync(adminUser, "ADMIN");
        }
    }
}

app.Run();