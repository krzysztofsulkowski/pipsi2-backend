using budget_api.Models;
using budget_api.Seeders;
using budget_api.Services;
using budget_api.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

const string envFileName = ".env";
var currentDirectory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
string envFilePath = null;
while (currentDirectory != null && !File.Exists(envFilePath = Path.Combine(currentDirectory.FullName, envFileName)))
{
    currentDirectory = currentDirectory.Parent;
}

if (envFilePath != null && File.Exists(envFilePath))
{
    DotNetEnv.Env.Load(envFilePath);  //wczytuje zmienne z pliku .env
}
else
{
    Console.WriteLine("OSTRZE¯ENIE: Nie znaleziono pliku .env.");
}

var builder = WebApplication.CreateBuilder(args);

string connectionString;
if (builder.Configuration.GetValue<bool>("IS_IN_CONTAINER"))
{
    // Uruchomienie w kontenerze Docker (lub na produkcji)
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}
else
{
    // Uruchomienie lokalne z Visual Studio
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection_LOCAL");
}

builder.Services.AddDbContext<BudgetApiDbContext>(options =>
    options.UseNpgsql(connectionString));


// Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = true;
})
    .AddEntityFrameworkStores<BudgetApiDbContext>()
    .AddDefaultTokenProviders();

// Konfiguracja uwierzytelniania oparta o JWT (tokeny)
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, // Sprawdza, czy token pochodzi od zaufanego serwera
        ValidateAudience = true, // Sprawdza, czy token jest przeznaczony dla naszej aplikacji
        ValidateLifetime = true, // Sprawdza, czy token nie wygas³
        ValidateIssuerSigningKey = true, // Sprawdza, czy klucz u¿yty do podpisania tokena jest prawid³owy

        ValidIssuer = builder.Configuration["JWT:Issuer"],
        ValidAudience = builder.Configuration["JWT:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"]))
    };
})
.AddFacebook(options =>
{
    options.AppId = builder.Configuration["Authentication:Facebook:AppId"];
    options.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
});


var frontendUrl = builder.Configuration["FRONTEND_BASE_URL"] ?? "http://localhost:3000";

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(frontendUrl)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Add services to the container
builder.Services.AddScoped<RoleSeeder>();
builder.Services.AddScoped<SeedManager>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<IBudgetService, BudgetService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Budget API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<BudgetApiDbContext>();
    ctx.Database.Migrate();

    var seedManager = scope.ServiceProvider.GetRequiredService<SeedManager>();
    await seedManager.Seed();
}
app.Run();

