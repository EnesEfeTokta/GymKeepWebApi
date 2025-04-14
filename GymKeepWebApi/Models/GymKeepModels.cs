using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore; // HasKey için

namespace GymKeepWebApi.Models
{
    // PostgreSQL varsayılanı snake_case olduğu için [Table] ve [Column]
    // attribute'ları kaldırıldı. Eğer veritabanında PascalCase varsa
    // veya özellikle isteniyorsa geri eklenebilir/DbContext'te yapılandırılabilir.

    public class User
    {
        public int Id { get; set; } // EF Core bunu 'users.id' olarak PK kabul eder

        [Required]
        [StringLength(100)] // Max uzunluk eklemek iyi bir pratik
        public string Username { get; set; } = null!;

        [Required]
        [EmailAddress] // E-posta formatını doğrular
        [StringLength(150)]
        public string Email { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties (Bu kullanıcıyla ilişkili diğer veriler)
        public virtual ICollection<WorkoutPlan> WorkoutPlans { get; set; } = new List<WorkoutPlan>();
        public virtual ICollection<CalorieCalculation> CalorieCalculations { get; set; } = new List<CalorieCalculation>();
        public virtual ICollection<Achievement> Achievements { get; set; } = new List<Achievement>();
        public virtual ICollection<UserSetting> UserSettings { get; set; } = new List<UserSetting>();
        public virtual ICollection<WorkoutSession> WorkoutSessions { get; set; } = new List<WorkoutSession>();
    }

    public class Exercise
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = null!;

        public string? Description { get; set; } // Nullable olabilir

        [Url] // URL formatını doğrular
        public string? VideoUrl { get; set; } // Nullable olabilir

        [Url]
        public string? ImageUrl { get; set; } // Nullable olabilir

        // Foreign Keys
        public int DifficultyLevelId { get; set; }
        public int RegionId { get; set; }

        // Navigation Properties
        [ForeignKey("DifficultyLevelId")]
        public virtual DifficultyLevel DifficultyLevel { get; set; } = null!;

        [ForeignKey("RegionId")]
        public virtual ExerciseRegion ExerciseRegion { get; set; } = null!; // İsim düzeltildi (tekil)

        public virtual ICollection<PlanExercise> PlanExercises { get; set; } = new List<PlanExercise>();
        public virtual ICollection<SessionExercise> SessionExercises { get; set; } = new List<SessionExercise>();
        // public virtual ICollection<RegionalWorkoutExercise> RegionalWorkoutExercises { get; set; } = new List<RegionalWorkoutExercise>(); // Eğer RegionalWorkoutExercise ExerciseId kullanırsa
    }

    public class DifficultyLevel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = null!; // İsim 'Name' olarak standartlaştırıldı

        // Navigation Property: Bu zorluk seviyesindeki egzersizler
        public virtual ICollection<Exercise> Exercises { get; set; } = new List<Exercise>();
    }

    public class ExerciseRegion
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!; // İsim 'Name' olarak standartlaştırıldı

        // Navigation Property: Bu bölgeye ait egzersizler
        public virtual ICollection<Exercise> Exercises { get; set; } = new List<Exercise>();
    }

    public class WorkoutPlan
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = null!; // Plan ismi eklendi (öncekinde yoktu)

        public string? Description { get; set; } // Açıklama eklendi (öncekinde yoktu)

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Oluşturma tarihi eklendi

        // Foreign Key
        public int UserId { get; set; }

        // Navigation Property
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        public virtual ICollection<PlanExercise> PlanExercises { get; set; } = new List<PlanExercise>();
        public virtual ICollection<WorkoutSession> WorkoutSessions { get; set; } = new List<WorkoutSession>(); // Bu planı temel alan seanslar
    }

    public class PlanExercise // Bir plandaki belirli bir egzersizin tanımı
    {
        public int Id { get; set; }

        public int Sets { get; set; }
        public int Reps { get; set; }
        public int? RestDurationSeconds { get; set; } // Set arası dinlenme (opsiyonel)
        public int? OrderInPlan { get; set; } // Plandaki sıra (opsiyonel)

        // Foreign Keys
        public int WorkoutPlanId { get; set; }
        public int ExerciseId { get; set; }

        // Navigation Properties
        [ForeignKey("WorkoutPlanId")]
        public virtual WorkoutPlan WorkoutPlan { get; set; } = null!;

        [ForeignKey("ExerciseId")]
        public virtual Exercise Exercise { get; set; } = null!;

        // Bu plan egzersizine karşılık gelen loglanmış egzersizler (opsiyonel bağlantı)
        public virtual ICollection<SessionExercise> SessionExercises { get; set; } = new List<SessionExercise>();
    }

    // --- Orijinal İstekteki Eksik Antrenman Takip Tabloları ---

    public class WorkoutSession // Yapılan bir antrenman seansı
    {
        public int Id { get; set; }
        public DateTime SessionDate { get; set; } = DateTime.UtcNow;
        public int? DurationMinutes { get; set; }
        public string? Notes { get; set; }

        // Foreign Keys
        public int UserId { get; set; }
        public int? WorkoutPlanId { get; set; } // Hangi plan takip edildi (opsiyonel)

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("WorkoutPlanId")]
        public virtual WorkoutPlan? WorkoutPlan { get; set; } // Takip edilen plan (null olabilir)

        public virtual ICollection<SessionExercise> SessionExercises { get; set; } = new List<SessionExercise>();
    }

    public class SessionExercise // Bir seans içinde yapılan spesifik bir egzersiz
    {
        public int Id { get; set; }
        public int? OrderInSession { get; set; } // Seans içindeki sıra

        // Foreign Keys
        public int WorkoutSessionId { get; set; }
        public int ExerciseId { get; set; }
        public int? PlanExerciseId { get; set; } // Hangi plan egzersizine karşılık geliyor (opsiyonel)

        // Navigation Properties
        [ForeignKey("WorkoutSessionId")]
        public virtual WorkoutSession WorkoutSession { get; set; } = null!;

        [ForeignKey("ExerciseId")]
        public virtual Exercise Exercise { get; set; } = null!;

        [ForeignKey("PlanExerciseId")]
        public virtual PlanExercise? PlanExercise { get; set; } // İlişkili plan egzersizi (null olabilir)

        public virtual ICollection<SetLog> SetLogs { get; set; } = new List<SetLog>(); // Bu egzersizin set kayıtları
    }

    public class SetLog // Bir egzersizin yapılan her bir setinin kaydı
    {
        public int Id { get; set; }
        public int SetNumber { get; set; } // 1, 2, 3...

        [Column(TypeName = "decimal(10, 2)")] // PostgreSQL'de DECIMAL/NUMERIC için
        public decimal? Weight { get; set; }

        public int? RepsCompleted { get; set; }
        public bool IsCompleted { get; set; } = false; // Checkbox durumu
        public DateTime? CompletedAt { get; set; } // Tamamlanma zamanı (opsiyonel)

        // Foreign Key
        public int SessionExerciseId { get; set; }

        // Navigation Property
        [ForeignKey("SessionExerciseId")]
        public virtual SessionExercise SessionExercise { get; set; } = null!;
    }

    // --- Diğer Mevcut Tablolarınız (Düzenlenmiş) ---

    public class CalorieCalculation
    {
        public int Id { get; set; }

        [Required]
        public int Age { get; set; }

        [Required]
        [Column(TypeName = "decimal(5, 2)")] // Örn: Metre cinsinden 1.75
        public decimal Height { get; set; }

        [Required]
        [Column(TypeName = "decimal(6, 2)")] // Örn: Kilogram cinsinden 75.50
        public decimal Weight { get; set; }

        [Required]
        [StringLength(10)] // Örn: Male, Female
        public string Gender { get; set; } = null!;

        [Required]
        [StringLength(50)] // Örn: Sedentary, Lightly Active...
        public string ActivityLevel { get; set; } = null!;

        [Required]
        [StringLength(50)] // Örn: Lose Weight, Maintain, Gain Muscle
        public string Goal { get; set; } = null!;

        [Required]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal Tdee { get; set; } // TDEE -> Tdee (C# Naming Convention)

        [Required]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal AdjustedCalories { get; set; }

        public DateTime CalculationDate { get; set; } = DateTime.UtcNow;

        // Foreign Key
        public int UserId { get; set; }

        // Navigation Property
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }

    public class Achievement
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string AchievementName { get; set; } = null!;

        [StringLength(500)]
        public string? AchievementDescription { get; set; } // Nullable olabilir

        public DateTime AchievedAt { get; set; } = DateTime.UtcNow;

        // Foreign Key
        public int UserId { get; set; }

        // Navigation Property
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }

    public class RegionalWorkout // Sabit, önceden tanımlanmış bölgesel antrenmanlar?
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Region { get; set; } = null!; // Belki ExerciseRegion'a FK olmalı?

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = null!; // Antrenman adı ekledim, muhtemelen gereklidir

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<RegionalWorkoutExercise> RegionalWorkoutExercises { get; set; } = new List<RegionalWorkoutExercise>();
    }

    public class RegionalWorkoutExercise
    {
        // Foreign Key 1 (Kompozit Anahtarın Parçası)
        public int WorkoutId { get; set; }

        // Foreign Key 2 (Kompozit Anahtarın Parçası) - ÖNERİ: ExerciseId (int) kullanın
        // public int ExerciseId { get; set; }
        [Required]
        [StringLength(150)]
        public string ExerciseName { get; set; } // Şimdilik orijinal haliyle bırakıldı, ama değiştirilmesi önerilir.

        public int? Sets { get; set; } // Bu bilgiler de burada olabilir
        public int? Reps { get; set; } // Bu bilgiler de burada olabilir
        public int? OrderInWorkout { get; set; } // Sıra numarası

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("WorkoutId")]
        public virtual RegionalWorkout RegionalWorkout { get; set; } = null!;
    }

    public class UserSetting
    {
        [Key] // Tek bir PK olduğu için Id yeterli, SettingId'ye gerek yok (ama kalabilir)
        public int Id { get; set; }

        public int? DailyGoal { get; set; } // Kalori hedefi mi? Adım hedefi mi? Netleştirilmeli. Nullable yaptım.

        public bool IsDarkMode { get; set; } = false;
        public bool NotificationsEnabled { get; set; } = true;

        // PostgreSQL 'time' türüne maplenir
        public TimeSpan? NotificationTime { get; set; } // Nullable olabilir

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Foreign Key (Bu genellikle User'ın Id'si ile aynı olur - One-to-One ilişki)
        public int UserId { get; set; }

        // Navigation Property
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }

    /*
    // Bu sınıfın yapısı problemli (string[], int[] UserId)
    // Normalleştirilmesi gerekiyor. Örneğin Sertifikalar, Ödüller için ayrı tablolar.
    // UserId neden int[]? Bir eğitmenin birden fazla kullanıcı hesabı mı var?
    // Yoksa bu eğitmenin öğrencilerinin UserId'leri mi? Bu durumda ayrı bir ilişki tablosu gerekir.
    [Table("SpecialTrainers")]
    public class SpecialTrainer
    {
        [Key]
        [Column("TrainerId")]
        public int Id { get; set; }

        [Required]
        [Column("Fullname")]
        public string Fullname { get; set; }

        [Required]
        [Column("Field")]
        public string Field { get; set; }

        [Required]
        [Column("Info")]
        public string Info { get; set; }

        [Required]
        [Column("Training")]
        public string Training { get; set; }

        // EF Core string[]'i doğrudan map'lemez. JSON'a serileştirme veya ayrı tablo gerekir.
        // [Column("Sertificates", TypeName = "jsonb")] // PostgreSQL JSONB kullanımı (ek yapılandırma ister)
        // public List<string> Sertificates { get; set; }
        // VEYA Ayrı Tablo: public virtual ICollection<TrainerCertificate> Certificates {get; set;}

        // [Column("Awards", TypeName = "jsonb")]
        // public List<string> Awards { get; set; }
        // VEYA Ayrı Tablo: public virtual ICollection<TrainerAward> Awards {get; set;}

        [Required]
        [Column("Experience")]
        public int ExperienceYears { get; set; } // Sadece yıl sayısı daha mantıklı

        // Bu foreign key ilişkisi net değil. Eğitmenin kendi User ID'si mi?
        // Yoksa eğittiği kullanıcılar mı? Eğer kendi User ID'si ise int UserId olmalı.
        // [Column("UserId")]
        // public int UserId { get; set; }

        // [ForeignKey("UserId")]
        // public User User { get; set; }
    }
    */
}