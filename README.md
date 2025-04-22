Harika, ASP.NET Web API projeniz (`GymKeepWebApi`) için gelişmiş ve detaylı bir README.md dosyası taslağı aşağıdadır. Bunu projenizin kök dizinine `README.md` olarak kaydedebilirsiniz.

# GymKeepWebApi - Antrenman Takip Uygulaması API

[![License: GPL-3.0](https://img.shields.io/badge/License-GPL-yellow.svg)](https://opensource.org/licenses/GPL-3.0)
![.NET Core](https://img.shields.io/badge/.NET-8.0-blueviolet)
![Entity Framework Core](https://img.shields.io/badge/EF%20Core-8.0-blue)

## Uyarı

**Bu proje, Atatürk Üniversitesi Bilişim Sistemleri ve Teknolojileri Bölümü'nün İleri Programlama dersi kapsamında, vize sınavı için geliştirilmiştir. Projeyi bu bağlamda değerlendirmenizi rica ederiz.**

## Açıklama

Bu depo, GymKeep mobil veya web uygulaması için backend hizmetlerini sağlayan ASP.NET Core Web API projesini içerir. Kullanıcı yönetimi, egzersiz kataloğu, antrenman planları, seans takibi, kalori hesaplama ve daha fazlası için RESTful endpoint'ler sunar.

## İçindekiler

- [GymKeepWebApi - Antrenman Takip Uygulaması API](#gymkeepwebapi---antrenman-takip-uygulaması-api)
  - [Uyarı](#uyarı)
  - [Açıklama](#açıklama)
  - [İçindekiler](#i̇çindekiler)
  - [Özellikler](#özellikler)
  - [Kullanılan Teknolojiler](#kullanılan-teknolojiler)
  - [Proje Yapısı](#proje-yapısı)
  - [Kurulum ve Başlatma](#kurulum-ve-başlatma)
    - [Gereksinimler](#gereksinimler)
    - [Kurulum Adımları](#kurulum-adımları)
    - [Veritabanı Kurulumu](#veritabanı-kurulumu)
  - [Uygulamayı Çalıştırma](#uygulamayı-çalıştırma)
    - [Geliştirme Ortamı](#geliştirme-ortamı)
    - [Üretim Ortamı (Genel Bakış)](#üretim-ortamı-genel-bakış)
  - [API Endpointleri](#api-endpointleri)
    - [Swagger / OpenAPI](#swagger--openapi)
    - [Temel Endpoint Grupları](#temel-endpoint-grupları)
    - [Örnek İstekler](#örnek-i̇stekler)
  - [Veritabanı](#veritabanı)
    - [Entity Framework Core Migrations](#entity-framework-core-migrations)
    - [Veritabanı Şeması](#veritabanı-şeması)
  - [Yapılandırma](#yapılandırma)
  - [Güvenlik Hususları](#güvenlik-hususları)
  - [Katkıda Bulunma](#katkıda-bulunma)
  - [Lisans](#lisans)

## Özellikler

*   **Kullanıcı Yönetimi:** Kayıt olma, giriş yapma, kullanıcı bilgilerini alma/güncelleme/silme.
*   **Egzersiz Kataloğu:** Egzersizleri, zorluk seviyelerini ve hedef bölgelerini listeleme, detaylarını görme.
*   **Antrenman Planları:** Kullanıcıya özel antrenman planları oluşturma, listeleme, güncelleme, silme.
*   **Plan Egzersizleri:** Planlara egzersiz ekleme, çıkarma, set/tekrar bilgilerini yönetme.
*   **Antrenman Seansları:** Gerçekleşen antrenman seanslarını kaydetme, listeleme, detaylandırma.
*   **Seans Egzersizleri:** Bir seans sırasında yapılan egzersizleri kaydetme.
*   **Set Logları:** Seanslardaki her bir setin detaylarını (ağırlık, tekrar, tamamlanma durumu) kaydetme.
*   **Kalori Hesaplama:** Kullanıcı verilerine göre TDEE ve hedefe yönelik kalori hesaplamalarını kaydetme/gösterme.
*   **Başarılar (Achievements):** Kullanıcıların kazandığı başarıları yönetme.
*   **Bölgesel Antrenmanlar:** Önceden tanımlanmış bölgesel antrenman şablonlarını yönetme.
*   **Kullanıcı Ayarları:** Kullanıcıya özel uygulama ayarlarını (tema, bildirimler vb.) yönetme.

## Kullanılan Teknolojiler

*   **.NET 8** (veya kullandığınız sürüm): Backend platformu.
*   **ASP.NET Core Web API:** RESTful API oluşturmak için framework.
*   **Entity Framework Core 8** (veya kullandığınız sürüm): Veritabanı etkileşimi için ORM (Object-Relational Mapper).
    *   **Code-First Yaklaşımı:** Veritabanı şeması C# model sınıflarından oluşturulur.
    *   **Migrations:** Veritabanı şeması değişikliklerini yönetmek için kullanılır.
*   **C#:** Ana programlama dili.
*   **RESTful Prensipleri:** API tasarımı için.
*   **JSON:** Veri alışverişi formatı.
*   **Swagger (OpenAPI):** API dokümantasyonu ve testi için kullanılır (ASP.NET Core ile entegre).
*   **Veritabanı:** (Projenizin kullandığı veritabanını belirtin, örn: PostgreSQL, SQL Server, SQLite).
*   **(Opsiyonel) JWT (JSON Web Token):** Güvenli kimlik doğrulama ve yetkilendirme için önerilir (Mevcut kodda parola yönetimi GÜVENLİ DEĞİLDİR, iyileştirme gerektirir).
*   **(Opsiyonel) AutoMapper veya Mapster:** DTO ve Entity dönüşümleri için kullanılabilir.

## Proje Yapısı

Proje, sorumlulukları ayırmak amacıyla standart bir ASP.NET Core Web API yapısını takip eder:

GymKeepWebApi/
├── Controllers/        # API endpoint'lerini içeren Controller sınıfları (UserController, WorkoutPlanController vb.)
├── Data/               # Veri erişim katmanı
│   ├── MyDbContext.cs  # Entity Framework Core veritabanı bağlamı
│   └── Migrations/     # EF Core veritabanı geçiş dosyaları
├── Dtos/               # Data Transfer Object (Veri Aktarım Nesneleri) sınıfları (API istek/yanıtları için)
├── Models/             # Veritabanı varlıklarını (Entity) temsil eden C# sınıfları (User, Exercise vb.)
├── Properties/
│   └── launchSettings.json # Geliştirme ortamı başlatma ayarları
├── appsettings.json    # Ana yapılandırma dosyası (bağlantı dizeleri vb.)
├── Program.cs          # Uygulamanın giriş noktası, servislerin ve middleware'in yapılandırıldığı yer
└── GymKeepWebApi.csproj # Proje dosyası

## Kurulum ve Başlatma

### Gereksinimler

*   **.NET SDK 8.0** (veya projenizin hedeflediği sürüm) - [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)
*   **Bir Veritabanı Sunucusu:** (Kullandığınız veritabanını belirtin, örn: PostgreSQL, SQL Server, SQL Server Express)
*   **IDE veya Metin Düzenleyici:** Visual Studio 2022, Visual Studio Code veya JetBrains Rider.
*   **(Opsiyonel) Git:** Versiyon kontrolü için.

### Kurulum Adımları

1.  **Depoyu Klonlayın:**
    ```bash
    git clone https://github.com/kullanici_adiniz/GymKeepWebApi.git
    cd GymKeepWebApi
    ```
2.  **NuGet Paketlerini Geri Yükleyin:**
    ```bash
    dotnet restore
    ```

### Veritabanı Kurulumu

1.  **Veritabanı Oluşturun:** Seçtiğiniz veritabanı sunucusunda (örn: PostgreSQL, SQL Server) `GymKeepDb` (veya istediğiniz bir isimde) adında boş bir veritabanı oluşturun.
2.  **Bağlantı Dizesini Ayarlayın:**
    *   `appsettings.Development.json` dosyasını açın (yoksa `appsettings.json`'ı kopyalayıp oluşturun).
    *   `ConnectionStrings` bölümünde, `DefaultConnection` değerini kendi veritabanı sunucunuza ve oluşturduğunuz veritabanına göre güncelleyin.
        *   **Örnek (SQL Server):** `"DefaultConnection": "Server=SUNUCU_ADINIZ;Database=GymKeepDb;User ID=KULLANICI_ADINIZ;Password=SIFRENIZ;TrustServerCertificate=True;"`
        *   **Örnek (PostgreSQL):** `"DefaultConnection": "Host=localhost;Database=GymKeepDb;Username=postgres;Password=SIFRENIZ;"`
3.  **Veritabanı Şemasını Uygulayın (EF Core Migrations):**
    *   Projenin kök dizininde bir terminal veya komut istemi açın.
    *   Aşağıdaki komutu çalıştırarak EF Core Migrations ile veritabanı şemasını oluşturun/güncelleyin:
        ```bash
        dotnet ef database update
        ```
    *   Bu komut, `Data/Migrations` klasöründeki geçiş dosyalarını veritabanına uygulayacaktır.

## Uygulamayı Çalıştırma

### Geliştirme Ortamı

1.  **Visual Studio:** Projeyi Visual Studio'da açın ve `https` profilini seçerek başlatın (Genellikle F5).
2.  **VS Code veya Terminal:**
    ```bash
    dotnet run --launch-profile https
    ```
    (veya `http` profili için `--launch-profile http`)

Uygulama varsayılan olarak `launchSettings.json` dosyasında belirtilen URL'lerde (örn: `https://localhost:7091` ve `http://localhost:5091`) çalışmaya başlayacaktır.

### Üretim Ortamı (Genel Bakış)

Üretim ortamında çalıştırmak için:

1.  Uygulamayı yayınlayın: `dotnet publish -c Release -o ./publish`
2.  Yayınlanan dosyaları bir sunucuya (Azure App Service, IIS, Linux sunucusu vb.) dağıtın.
3.  Üretim ortamı için `appsettings.Production.json` dosyasını veya ortam değişkenlerini kullanarak veritabanı bağlantı dizesini ve diğer hassas ayarları yapılandırın.
4.  Uygulamayı bir web sunucusu (Kestrel, IIS, Nginx, Apache) arkasında çalıştırın ve **HTTPS'i zorunlu kılın**.

## API Endpointleri

### Swagger / OpenAPI

API endpoint'lerini keşfetmek ve test etmek için Swagger UI'ı kullanabilirsiniz. Uygulama çalışırken tarayıcınızda aşağıdaki adrese gidin:

`/swagger` (örn: `https://localhost:7091/swagger`)

### Temel Endpoint Grupları

*   `/api/User`: Kullanıcı işlemleri (kayıt, giriş, profil).
*   `/api/Exercise`: Egzersiz listeleme, detay görme.
*   `/api/ExerciseRegion`: Egzersiz bölgelerini listeleme.
*   `/api/DifficultyLevel`: Zorluk seviyelerini listeleme.
*   `/api/users/{userId}/WorkoutPlan`: Kullanıcıya ait antrenman planı işlemleri (CRUD).
*   `/api/users/{userId}/WorkoutPlan/{planId}/Exercises`: Bir plana egzersiz ekleme/çıkarma/güncelleme.
*   `/api/users/{userId}/WorkoutSession`: Kullanıcıya ait antrenman seansı işlemleri (CRUD).
*   `/api/users/{userId}/WorkoutSession/{sessionId}/exercises`: Bir seansa egzersiz ekleme/çıkarma/güncelleme.
*   `/api/users/{userId}/WorkoutSession/{sessionId}/exercises/{sessionExerciseId}/setlogs`: Bir seans egzersizine set logu ekleme/çıkarma/güncelleme/listeleme.
*   `/api/CalorieCalculation`: Kalori hesaplama işlemleri.
*   `/api/UserSetting`: Kullanıcı ayarları işlemleri.
*   *(Diğer Controller'lar için benzer endpoint'ler)*

### Örnek İstekler

**1. Kullanıcı Kaydı:**

*   **Metot:** `POST`
*   **URL:** `/api/User/register`
*   **Gövde (Body - application/json):**
    ```json
    {
      "username": "testuser",
      "email": "test@example.com",
      "password": "SecurePassword123"
    }
    ```
*   **Başarılı Yanıt:** `201 Created` (Gövdede `UserResponseDto` döner)

**2. Kullanıcı Girişi:**

*   **Metot:** `POST`
*   **URL:** `/api/User/login`
*   **Gövde (Body - application/json):**
    ```json
    {
      "username": "testuser",
      "password": "SecurePassword123"
    }
    ```
*   **Başarılı Yanıt:** `200 OK` (Gövdede `UserResponseDto` döner)
    **UYARI:** Mevcut kodda parola doğrulaması güvenli değildir!

**3. Kullanıcının Planlarını Listeleme:**

*   **Metot:** `GET`
*   **URL:** `/api/users/1/WorkoutPlan` (Örnek: userId=1)
*   **Başarılı Yanıt:** `200 OK` (Gövdede `WorkoutPlan` listesi döner)

**4. Plana Egzersiz Ekleme:**

*   **Metot:** `POST`
*   **URL:** `/api/users/1/WorkoutPlan/5/Exercises` (Örnek: userId=1, planId=5)
*   **Gövde (Body - application/json):**
    ```json
    {
      "exerciseId": 10,
      "sets": 3,
      "reps": 12,
      "restDurationSeconds": 60,
      "orderInPlan": 1
    }
    ```
*   **Başarılı Yanıt:** `201 Created` (Gövdede oluşturulan `PlanExercise` döner)

## Veritabanı

### Entity Framework Core Migrations

Bu proje, veritabanı şemasını yönetmek için EF Core Migrations kullanır.

*   **Yeni Migration Ekleme:** Model sınıflarında değişiklik yaptıktan sonra yeni bir migration oluşturmak için:
    ```bash
    dotnet ef migrations add MigrationAdi -p GymKeepWebApi.csproj -s GymKeepWebApi.csproj
    ```
*   **Veritabanını Güncelleme:** Oluşturulan migration'ları veritabanına uygulamak için:
    ```bash
    dotnet ef database update
    ```
*   **Belirli Bir Migration'a Dönme:**
    ```bash
    dotnet ef database update OncekiMigrationAdi
    ```
*   **Migration'ı Kaldırma:** Son eklenen migration'ı geri almak için:
    ```bash
    dotnet ef migrations remove
    ```

Tüm migration dosyaları `Data/Migrations` klasöründe bulunur.

### Veritabanı Şeması

Veritabanı şeması, `Models` klasöründeki entity sınıfları ve `MyDbContext` içindeki `OnModelCreating` yapılandırmaları tarafından tanımlanır. İlişkiler, kısıtlamalar ve indeksler burada belirtilmiştir. `database_schema.sql` dosyası (varsa), şemanın bir SQL temsilini içerebilir.

## Yapılandırma

Uygulamanın temel yapılandırması `appsettings.json` dosyasında bulunur. Geliştirme, Staging ve Üretim ortamları için sırasıyla `appsettings.Development.json`, `appsettings.Staging.json` ve `appsettings.Production.json` dosyaları kullanılabilir. Ortam değişkenleri de yapılandırmayı geçersiz kılmak için kullanılabilir.

Anahtar yapılandırmalar:

*   **`ConnectionStrings:DefaultConnection`**: Veritabanı bağlantı dizesi.
*   **`Logging`**: Uygulamanın loglama seviyeleri ve hedefleri.
*   **(Eklenebilir)** `JwtSettings`: JWT (JSON Web Token) ayarları (Secret Key, Issuer, Audience vb.).

## Güvenlik Hususları

*   🚨 **Parola Yönetimi:** **MEVCUT KODDA PAROLALAR GÜVENLİ BİR ŞEKİLDE SAKLANMAMAKTADIR VE DOĞRULANMAMAKTADIR!** Parolalar asla düz metin olarak saklanmamalıdır. Güçlü bir hashing algoritması (örn: Argon2, bcrypt, PBKDF2) ile salt kullanılarak hash'lenmeli ve sadece hash değeri veritabanında saklanmalıdır. Giriş sırasında da gelen parola aynı salt ile hash'lenip veritabanındaki hash ile karşılaştırılmalıdır.
*   **Kimlik Doğrulama ve Yetkilendirme:** API endpoint'lerini korumak için **JWT (JSON Web Token)** tabanlı kimlik doğrulama ve yetkilendirme mekanizması eklenmelidir. Kullanıcı giriş yaptıktan sonra bir token üretilmeli ve korumalı endpoint'lere yapılan isteklerde bu token `Authorization: Bearer <token>` başlığı ile gönderilmelidir. Rol bazlı veya policy bazlı yetkilendirme de eklenebilir.
*   **HTTPS:** Tüm iletişim **HTTPS** üzerinden yapılmalıdır. Geliştirme ortamında `UseHttpsRedirection()` middleware'i kullanılır. Üretim ortamında ters proxy (Nginx, Apache vb.) veya hosting sağlayıcısı üzerinden HTTPS zorlaması yapılmalıdır.
*   **CORS (Cross-Origin Resource Sharing):** Eğer API, farklı bir domain'de çalışan bir web uygulamasından (örn: Flutter Web) çağrılacaksa, `Program.cs` içinde CORS politikaları doğru şekilde yapılandırılmalıdır (`AddCors`, `UseCors`). Sadece güvenilen origin'lere izin verilmelidir.
*   **Input Validation:** Gelen istek verileri (DTO'lar) sunucu tarafında doğrulanmalıdır. `[ApiController]` attribute'u ve DTO'lardaki Data Annotations (`[Required]`, `[StringLength]`, `[Range]` vb.) temel doğrulamayı sağlar. Daha karmaşık iş kuralları için ek doğrulama gerekebilir. SQL Injection gibi saldırıları önlemek için Entity Framework Core gibi ORM'ler parametreli sorgular kullandığından genellikle güvenlidir, ancak ham SQL sorguları kullanılıyorsa dikkatli olunmalıdır.
*   **Hata Yönetimi:** Hassas hata detayları (stack trace vb.) üretim ortamında istemciye gönderilmemelidir. Global bir hata işleyici (exception handler middleware) kullanarak hatalar loglanmalı ve istemciye genel bir hata mesajı döndürülmelidir.
*   **Rate Limiting ve Throttling:** Kötü niyetli kullanımı veya aşırı yüklenmeyi önlemek için API endpoint'lerine hız sınırlaması eklenebilir.

## Katkıda Bulunma

Katkıda bulunmak isterseniz, lütfen aşağıdaki adımları izleyin:

1.  Depoyu Fork'layın.
2.  Yeni bir özellik veya hata düzeltmesi için ayrı bir dal (branch) oluşturun (`git checkout -b ozellik/yeni-ozellik` veya `git checkout -b hata/hata-duzeltmesi`).
3.  Değişikliklerinizi yapın ve Commit'leyin (`git commit -m 'Yeni özellik eklendi'`).
4.  Dalınızı Origin'e Push'layın (`git push origin ozellik/yeni-ozellik`).
5.  Bir Pull Request (PR) oluşturun.

Lütfen kodlama standartlarına uyun ve değişikliklerinizi açıklayıcı bir şekilde belgeleyin.

## Lisans

Bu proje [GPL-3.0 Lisansı](LICENSE) altında lisanslanmıştır. Daha fazla bilgi için `LICENSE` dosyasına bakın.

**Açıklamalar ve Öneriler:**

1.  **Yer Tutucuları Güncelleyin:**
    *   `.NET Sürümü`, `EF Core Sürümü`
    *   `Kullanılan Teknolojiler` bölümünde **veritabanınızı** belirtin.
    *   `Kurulum` bölümündeki **bağlantı dizesi örneklerini** kendi veritabanınıza göre uyarlayın.
    *   `Katkıda Bulunma` bölümündeki depo URL'sini (`kullanici_adiniz/GymKeepWebApi`) kendi URL'nizle değiştirin.
    *   `Lisans` bölümünü ve varsa `LICENSE` dosyasının adını projenizin lisansına göre ayarlayın (MIT yaygın bir seçenektir).
2.  **Güvenlik Uyarısı:** Parola yönetimi ile ilgili güvenlik uyarısı çok önemlidir. Bu sorunu çözene kadar README'de belirgin bir şekilde durmalıdır. JWT implementasyonunu planlıyorsanız bunu da belirtebilirsiniz.
3.  **Detaylandırma:** İhtiyaca göre belirli bölümleri daha da detaylandırabilirsiniz. Örneğin, belirli bir Controller'ın tüm endpoint'lerini listeleyebilir veya daha fazla örnek istek ekleyebilirsiniz.
4.  **Görseller:** Proje yapısını veya Swagger arayüzünü gösteren ekran görüntüleri eklemek README'yi daha anlaşılır kılabilir.
5.  **Türkçe/İngilizce:** Tamamen Türkçe yazdım, ancak teknik terimlerin İngilizce karşılıklarını da korumaya çalıştım. İhtiyaca göre İngilizce'ye çevirebilir veya karma kullanabilirsiniz.
6.  **Swagger Linki:** Uygulamanızın canlı bir demosu veya test ortamı varsa, Swagger linkini doğrudan README'ye ekleyebilirsiniz.