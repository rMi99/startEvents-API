using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using StartEvent_API.Data;
using StartEvent_API.Data.Entities;
using StartEvent_API.Data.Seeders;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1️⃣ Configure DB context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// 2️⃣ Configure Identity with custom user and role
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    
    // Configure password requirements
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    
    // Configure user requirements
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 3️⃣ Register Email sender
builder.Services.AddTransient<IEmailSender, EmailSender>();

// 4️⃣ Add JWT Authentication BEFORE building the app
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not found in configuration")))
    };
});

// 5️⃣ Register custom services
builder.Services.AddScoped<StartEvent_API.Repositories.IAuthRepository, StartEvent_API.Repositories.AuthRepository>();
builder.Services.AddScoped<StartEvent_API.Repositories.IAdminRepository, StartEvent_API.Repositories.AdminRepository>();
builder.Services.AddScoped<StartEvent_API.Repositories.ITicketRepository, StartEvent_API.Repositories.TicketRepository>();
builder.Services.AddScoped<StartEvent_API.Repositories.IPaymentRepository, StartEvent_API.Repositories.PaymentRepository>();
builder.Services.AddScoped<StartEvent_API.Repositories.IDiscountRepository, StartEvent_API.Repositories.DiscountRepository>();
builder.Services.AddScoped<StartEvent_API.Repositories.ILoyaltyPointRepository, StartEvent_API.Repositories.LoyaltyPointRepository>();
builder.Services.AddScoped<StartEvent_API.Repositories.IReportRepository, StartEvent_API.Repositories.ReportRepository>();
builder.Services.AddScoped<StartEvent_API.Repositories.IQrRepository, StartEvent_API.Repositories.QrRepository>();

builder.Services.AddScoped<StartEvent_API.Helper.IJwtService, StartEvent_API.Helper.JwtService>();
builder.Services.AddScoped<StartEvent_API.Helper.IFileStorage, StartEvent_API.Helper.LocalFileStorage>();
builder.Services.AddScoped<StartEvent_API.Helper.IQrCodeGenerator, StartEvent_API.Helper.QrCodeGenerator>();
builder.Services.AddScoped<StartEvent_API.Helper.IEmailNotificationService, StartEvent_API.Helper.EmailNotificationService>();

builder.Services.AddScoped<StartEvent_API.Business.IAuthService, StartEvent_API.Business.AuthService>();
builder.Services.AddScoped<StartEvent_API.Business.ITicketService, StartEvent_API.Business.TicketService>();
builder.Services.AddScoped<StartEvent_API.Business.IReportService, StartEvent_API.Business.ReportService>();
builder.Services.AddScoped<StartEvent_API.Business.IQrService, StartEvent_API.Business.QrService>();

// 6️⃣ Add controllers and Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "StartEvent API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Cors
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

var app = builder.Build();

app.UseCors("AllowAll");

// 7️⃣ Seed roles, users, and venues
using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
    // await UserRoleSeeder.SeedRoles(serviceProvider);
    await UserRoleSeeder.SeedInitialUsers(serviceProvider);
    await VenueSeeder.SeedVenues(serviceProvider);

    try
    {
        // Seed events
        await EventBookingSeeder.SeedAsync(context);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error seeding event data: {ex.Message}");
        throw; // Re-throw to preserve stack trace
    }
}

// 8️⃣ Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Enable static files for QR code images
app.UseStaticFiles();

app.UseAuthentication();   // <-- must come BEFORE UseAuthorization
app.UseAuthorization();

// 9️⃣ Map endpoints
app.MapControllers();

app.Run();
