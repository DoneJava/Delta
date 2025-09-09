using DELTAAPI.Data;
using DELTAAPI.Service;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// =================== DB ===================
builder.Services.AddDbContext<DeltaContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// =================== MVC ===================
builder.Services.AddControllers();

// =================== Swagger (DEV) ===================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// =================== CORS ===================
// Prioridade: appsettings:Cors:AllowedOrigins  ->  env FRONTEND_URL  ->  fallback dev (localhost:5173)
var fromConfig = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
var fromEnv = builder.Configuration["FRONTEND_URL"];
string[] fallbackDev = new[] { "http://localhost:5173", "http://127.0.0.1:5173" };

var allowedOrigins = (fromConfig.Length > 0 ? fromConfig
                    : !string.IsNullOrWhiteSpace(fromEnv) ? new[] { fromEnv }
                    : fallbackDev);

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontOnly", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
              .SetPreflightMaxAge(TimeSpan.FromHours(1));
        // Se autenticar por cookie (credentials), descomente:
        // policy.AllowCredentials();
    });
});

// =================== Rate limit ===================
builder.Services.AddRateLimiter(opt =>
{
    opt.RejectionStatusCode = 429;
    opt.AddFixedWindowLimiter("api", o =>
    {
        o.PermitLimit = 60; // 60 req/min
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit = 100;
    });
});

// =================== DI ===================
builder.Services.AddScoped<ClienteService>();
builder.Services.AddScoped<EnvioService>();
builder.Services.AddScoped<ItemPedidoService>();
builder.Services.AddScoped<PagamentoService>();
builder.Services.AddScoped<PedidoService>();
builder.Services.AddScoped<ProdutoService>();
builder.Services.AddScoped<VersiculosService>();

var app = builder.Build();

// =================== DB ensure ===================
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DeltaContext>();
    context.Database.EnsureCreated();
}

// =================== Swagger / HSTS ===================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

// =================== HTTPS ===================
app.UseHttpsRedirection();

// =================== Security headers ===================
app.Use(async (ctx, next) =>
{
    var h = ctx.Response.Headers;
    h["X-Content-Type-Options"] = "nosniff";
    h["X-Frame-Options"] = "DENY";
    h["Referrer-Policy"] = "strict-origin-when-cross-origin";
    h["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
    // Ajuste seu CSP conforme as CDNs que o front usa:
    // h["Content-Security-Policy"] = "default-src 'self'; img-src 'self' data: https:; script-src 'self'; style-src 'self' 'unsafe-inline'";
    await next();
});

// =================== CORS ===================
app.UseCors("FrontOnly");

// =================== (Opcional) Enforce Origin server-side ===================
// Se o header Origin vier e não estiver na lista, bloqueia.
var allowedSet = new HashSet<string>(allowedOrigins, StringComparer.OrdinalIgnoreCase);
app.Use(async (ctx, next) =>
{
    var origin = ctx.Request.Headers.Origin.ToString();
    if (!string.IsNullOrEmpty(origin) && !allowedSet.Contains(origin))
    {
        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
        await ctx.Response.WriteAsync("Origin not allowed.");
        return;
    }
    await next();
});

using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<DeltaContext>();
    // se você quer criar em dev:
    ctx.Database.EnsureCreated();
    // e checar conexão:
    if (!await ctx.Database.CanConnectAsync())
        throw new Exception("Não foi possível conectar ao banco usando DefaultConnection.");
}


// =================== Rate limiting ===================
app.UseRateLimiter();

// app.UseAuthentication(); // se tiver
app.UseAuthorization();

app.MapControllers();

app.Run();
