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
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

// 5️⃣ Add controllers and Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 6️⃣ Seed roles, users, and venues
using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    await UserRoleSeeder.SeedRoles(serviceProvider);
    await UserRoleSeeder.SeedInitialUsers(serviceProvider);
    await VenueSeeder.SeedVenues(serviceProvider);
}

// 7️⃣ Configure middleware
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
app.UseAuthentication();   // <-- must come BEFORE UseAuthorization
app.UseAuthorization();

// 8️⃣ Map endpoints
app.MapControllers();
app.MapGet("/api/health", () => Results.Ok(new { status = "API Running" }));

app.Run();
