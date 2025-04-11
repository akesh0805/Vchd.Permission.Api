using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Vchd.Permission.api.Background;
using Vchd.Permission.Api.Data;
using Vchd.Permission.Api.Services;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddSingleton<TelegramBotService>();
builder.Services.AddHostedService<TelegramBotHostedService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Db")));

builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager()
    .AddRoles<IdentityRole>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    }).AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))

        };
    });

builder.Services.AddSwaggerGen(swagger =>
{
    swagger.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Blogging Platform Api",
    });
    swagger.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    swagger.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme

            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            }, Array.Empty<string>()
        }
    });
});

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Kadr", o =>
    {
        o.RequireAuthenticatedUser();
        o.RequireRole("kadr");
    });

builder.Services.AddScoped<AuthService>();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(app => app.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
app.MapControllers();
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.Migrate();

        // После применения миграций создаём роли
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        string[] roles = { "kadr", "user" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Ошибка при миграции или создании ролей: " + ex.Message);
    }
}


app.Run();
