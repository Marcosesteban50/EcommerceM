using EcommerceAPI.Data;
using EcommerceAPI.Servicios.Archivos;
using EcommerceAPI.Servicios.IA;
using EcommerceAPI.Servicios.ServicioUsuarios;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger
builder.Services.AddSwaggerGen(opc =>
{
    opc.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });

    opc.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            new string[]{}
        }
    });
});

// Servicios
builder.Services.AddTransient<IServicioUsuarios, ServicioUsuarios>();
builder.Services.AddTransient<IAlmacenadorArchivos, AlmacenadorArchivosLocal>();
builder.Services.AddHttpClient<GeminiServicio>();




// DB
builder.Services.AddDbContext<ApplicationDbContext>(o =>
{
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Identity
builder.Services.AddIdentityCore<IdentityUser>(opc =>
{
    opc.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddSignInManager()
.AddDefaultTokenProviders();

builder.Services.AddOutputCache();




// Autenticación
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

  
    options.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(opc =>
{
    opc.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["llavejwt"]!)),
        ClockSkew = TimeSpan.Zero
    };
});

// Autorización
builder.Services.AddAuthorization(opc =>
{
    opc.AddPolicy("Admin", p => p.RequireClaim("Admin"));
    opc.AddPolicy("Vendedor", p => p.RequireClaim("Vendedor"));
    opc.AddPolicy("Cliente", p => p.RequireClaim("Cliente"));
    opc.AddPolicy("Admin-Vendedor", p => p.RequireRole("Admin", "Vendedor"));
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddAutoMapper(typeof(Program));

// CORS
var originesPermitidos = builder.Configuration["origenesPermitidos"]!.Split(",");
builder.Services.AddCors(x =>
{
    x.AddDefaultPolicy(o =>
    {
        o.WithOrigins(originesPermitidos)
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials(); // Necesario para Google
    });
});

var app = builder.Build();

// Middleware de errores 401 / 403
app.Use(async (context, next) =>
{
    await next();

    if (context.Response.StatusCode == 401)
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync("{\"error\":\"No estás autenticado\"}");
    }

    if (context.Response.StatusCode == 403)
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync("{\"error\":\"No tienes permiso para realizar esta acción\"}");
    }
});


// En Program.cs
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new { error = ex.Message });
    }
});


//Para Unittesting y crear DB
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (dbContext.Database.IsRelational())
    {
        dbContext.Database.Migrate();
    }
}


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseRouting();

app.UseCors();

app.UseStaticFiles();

app.UseOutputCache();


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
