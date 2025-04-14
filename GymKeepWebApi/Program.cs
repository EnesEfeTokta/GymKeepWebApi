using Microsoft.AspNetCore.Authentication.Cookies; // Cookie Authentication için
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Microsoft.OpenApi.Models; // Swagger için eklendi

var builder = WebApplication.CreateBuilder(args);

// --- Veritabanı Yapılandırması ---
builder.Services.AddDbContext<MyDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresqlConnection"),
        npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);
        });
    // Eğer DbContext'te tanımlamadıysanız ve snake_case kullanıyorsanız:
    // options.UseSnakeCaseNamingConvention();
});

// --- Session ve Cache Yapılandırması ---
// Cookie Authentication kullanıldığı için HttpContext.Session genellikle
// kimlik doğrulama dışındaki verileri saklamak için kullanılır.
// Eğer sadece kimlik doğrulaması için eklediyseniz, gereksiz olabilir.
// Ancak başka amaçlar için kullanıyorsanız kalmalıdır.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Session süresi
    options.Cookie.Name = ".GymKeep.SessionDataCookie"; // Authentication cookie'sinden farklı bir isim verelim
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // HTTPS durumuna göre ayarla
});

// --- Kimlik Doğrulama Servisleri (Cookie Authentication) ---
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = ".GymKeep.AuthCookie"; // Kimlik doğrulama cookie adı
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60); // Örnek: 60 dakika
        options.SlidingExpiration = true; // Aktivite oldukça süreyi uzat
        options.Cookie.HttpOnly = true; // Client-side script erişimini engelle
        options.Cookie.IsEssential = true;
        // Production'da HTTPS zorunluysa Always kullanın, geliştirme için SameAsRequest daha esnek olabilir.
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax; // Çapraz site isteklerinde cookie gönderimini yönetir (Lax veya Strict genellikle iyidir)

        // API'ler için Önemli: Otomatik Yönlendirme Yapma, Durum Kodu Dön
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }
            // Cookie geçerliliğini her istekte kontrol etmek için (opsiyonel, performansı etkileyebilir):
            // OnValidatePrincipal = context => { /* Kullanıcının hala geçerli olup olmadığını kontrol et (örn: DB'den) */ return Task.CompletedTask; }
        };
    });

// --- Yetkilendirme Servisleri ---
// Rol bazlı yetkilendirme vb. için politikalar burada tanımlanabilir.
builder.Services.AddAuthorization();

// --- CORS Yapılandırması ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFlutterDev",
        policy =>
        {
            // İstemcinizin Geliştirme Sırasındaki Doğru Origin'ini Buraya Ekleyin!
            // Örneğin Flutter Web (debug): "http://localhost:port"
            // Yayınlanmışsa: "https://yourdomain.com" vb.
            policy.WithOrigins("http://localhost:58522") // <<< BU ORIGIN'İN DOĞRU OLDUĞUNDAN EMİN OLUN!
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // Cookie tabanlı kimlik doğrulama için KRİTİK!
        });
});


// --- Diğer Servisler ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// --- Swagger Yapılandırması (Cookie Authentication ile Test İçin) ---
builder.Services.AddSwaggerGen(options =>
{
    // API'niz hakkında temel bilgileri tanımlayın
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "GymKeep API", Version = "v1" });

    // Güvenlik Tanımı: Cookie Authentication'ı Swagger'a tanıtın
    // Bu, Swagger UI'ın cookie'leri (manuel veya otomatik) yönetmesine yardımcı olmaz
    // ancak API'nin cookie beklediğini belgeler. Swagger UI üzerinden test için
    // genellikle tarayıcının cookie yönetimini kullanmanız veya manuel eklemeniz gerekir.
    // Gerçek bir "Authorize" butonu eklemek genellikle Bearer token (JWT) içindir.
    // Cookie için belgelemek yeterli olabilir.
    options.AddSecurityDefinition(CookieAuthenticationDefaults.AuthenticationScheme, new OpenApiSecurityScheme
    {
        Name = "Cookie", // Sadece bir isim
        Type = SecuritySchemeType.ApiKey, // Cookie'yi temsil etmek için ApiKey kullanılabilir
        In = ParameterLocation.Cookie, // Cookie'nin nerede olduğunu belirtir
        Description = "ASP.NET Core Cookie Authentication"
    });

    // Güvenlik Gereksinimi: Hangi endpoint'lerin bu cookie'yi gerektirdiğini belirtin
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = CookieAuthenticationDefaults.AuthenticationScheme
                }
            },
            new string[] {}
        }
    });
});


// --- Uygulamayı Oluştur ---
var app = builder.Build();

// --- HTTP Request Pipeline (Middleware Sırası Önemli!) ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "GymKeep API v1");
        // options.RoutePrefix = string.Empty; // API dokümantasyonunu kök dizinde açmak için
    });
    app.UseDeveloperExceptionPage();
}
else
{
    // app.UseExceptionHandler("/Error"); // API için özelleştirilmiş hata işleyici daha iyi olabilir
    app.UseHsts();
    // Production'da HTTPS yönlendirmesini etkinleştirin
    app.UseHttpsRedirection(); // <<< PRODUCTION İÇİN ÖNEMLİ
}

// HTTPS Yönlendirmesi (opsiyonel, production'da önerilir)
// app.UseHttpsRedirection(); // Eğer yukarıda else bloğunda değilse buraya alınabilir.

app.UseRouting(); // Yönlendirme middleware'i

// CORS'u Authentication/Authorization'dan ÖNCE çağırın
app.UseCors("AllowFlutterDev");

// Session middleware'i (Eğer Session kullanılıyorsa)
app.UseSession();

// Authentication ve Authorization middleware'lerini doğru sırada çağırın
app.UseAuthentication(); // Kimlik doğrulama (Cookie'yi kontrol eder)
app.UseAuthorization();  // Yetkilendirme ([Authorize] attribute'larını kontrol eder)

app.MapControllers(); // Controller endpoint'lerini eşle

app.Run(); // Uygulamayı çalıştır