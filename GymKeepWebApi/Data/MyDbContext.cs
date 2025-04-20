using Microsoft.EntityFrameworkCore;
using GymKeepWebApi.Models; // Entity sınıflarınızın namespace'i olduğundan emin olun

public class MyDbContext : DbContext
{
    public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
    {
    }

    // --- DbSets (Tüm entity'ler için) ---
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<DifficultyLevel> DifficultyLevels { get; set; } = null!;
    public DbSet<ExerciseRegion> ExerciseRegions { get; set; } = null!;
    public DbSet<Exercise> Exercises { get; set; } = null!;
    public DbSet<WorkoutPlan> WorkoutPlans { get; set; } = null!;
    public DbSet<PlanExercise> PlanExercises { get; set; } = null!;
    public DbSet<WorkoutSession> WorkoutSessions { get; set; } = null!;
    public DbSet<SessionExercise> SessionExercises { get; set; } = null!;
    public DbSet<SetLog> SetLogs { get; set; } = null!;
    public DbSet<CalorieCalculation> CalorieCalculations { get; set; } = null!;
    public DbSet<Achievement> Achievements { get; set; } = null!;
    public DbSet<RegionalWorkout> RegionalWorkouts { get; set; } = null!;
    public DbSet<RegionalWorkoutExercise> RegionalWorkoutExercises { get; set; } = null!;
    public DbSet<UserSetting> UserSettings { get; set; } = null!;


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ----- Entity Yapılandırmaları (Basitleştirilmiş) -----

        // --- User ---
        modelBuilder.Entity<User>(entity =>
        {
            // entity.ToTable("users"); // İsteğe bağlı: Tablo adını belirtin
            entity.HasKey(e => e.UserId); // Doğru anahtar adı
            entity.HasIndex(e => e.Username).IsUnique(); // Kullanıcı adı benzersiz olsun
            entity.HasIndex(e => e.Email).IsUnique(); // E-posta benzersiz olsun

            // User silinirse ilişkili kayıtlar ne olacak?
            // EF Core varsayılanları genellikle yeterlidir, ancak açıkça belirtmek daha iyidir.
            // Aşağıdaki ilişkiler diğer entity'lerde tanımlanmıştır.
        });

        // --- DifficultyLevel ---
        modelBuilder.Entity<DifficultyLevel>(entity =>
        {
            // entity.ToTable("difficulty_levels");
            entity.HasKey(e => e.DifficultyLevelId); // Doğru anahtar adı
            entity.Property(e => e.Name).IsRequired(); // İsim zorunlu
            entity.HasIndex(e => e.Name).IsUnique(); // İsim benzersiz
        });

        // --- ExerciseRegion ---
        modelBuilder.Entity<ExerciseRegion>(entity =>
        {
            // entity.ToTable("exercise_regions");
            entity.HasKey(e => e.ExerciseRegionId); // Doğru anahtar adı
            entity.Property(e => e.Name).IsRequired(); // İsim zorunlu
            entity.HasIndex(e => e.Name).IsUnique(); // İsim benzersiz
        });

        // --- Exercise ---
        modelBuilder.Entity<Exercise>(entity =>
        {
            // entity.ToTable("exercises");
            entity.HasKey(e => e.ExerciseId); // Doğru anahtar adı
            entity.Property(e => e.Name).IsRequired();

            // Exercise -> DifficultyLevel (Many-to-One)
            entity.HasOne(e => e.DifficultyLevel)
                  .WithMany() // DifficultyLevel'da ters navigation property yoksa boş bırakılabilir
                  .HasForeignKey(e => e.DifficultyLevelId)
                  .OnDelete(DeleteBehavior.Restrict); // Seviye silinirse egzersiz silinmesin

            // Exercise -> ExerciseRegion (Many-to-One)
            entity.HasOne(e => e.ExerciseRegion)
                  .WithMany() // ExerciseRegion'da ters navigation property yoksa boş bırakılabilir
                  .HasForeignKey(e => e.RegionId)
                  .OnDelete(DeleteBehavior.Restrict); // Bölge silinirse egzersiz silinmesin
        });

        // --- WorkoutPlan ---
        modelBuilder.Entity<WorkoutPlan>(entity =>
        {
            // entity.ToTable("workout_plans");
            entity.HasKey(e => e.WorkoutPlanId); // Doğru anahtar adı
            entity.Property(e => e.WorkoutPlanId)
              .UseIdentityByDefaultColumn(); // Veya .ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired();

            // WorkoutPlan -> User (Many-to-One)
            entity.HasOne(wp => wp.User)
                  .WithMany() // User'da ters navigation property yoksa boş bırakılabilir
                  .HasForeignKey(wp => wp.UserId)
                  .OnDelete(DeleteBehavior.Cascade); // User silinirse planları da silinsin (ya da Restrict)
        });

        // --- PlanExercise (Ara Tablo) ---
        modelBuilder.Entity<PlanExercise>(entity =>
        {
            // entity.ToTable("plan_exercises");
            entity.HasKey(e => e.PlanExerciseId); // Doğru anahtar adı

            // PlanExercise -> WorkoutPlan (Many-to-One)
            entity.HasOne(pe => pe.WorkoutPlan)
                  .WithMany() // WorkoutPlan'da ters navigation property yoksa boş bırakılabilir
                  .HasForeignKey(pe => pe.WorkoutPlanId)
                  .OnDelete(DeleteBehavior.Cascade); // Plan silinirse bu kayıtlar da silinsin

            // PlanExercise -> Exercise (Many-to-One)
            entity.HasOne(pe => pe.Exercise)
                  .WithMany() // Exercise'da ters navigation property yoksa boş bırakılabilir
                  .HasForeignKey(pe => pe.ExerciseId)
                  .OnDelete(DeleteBehavior.Restrict); // Egzersiz silinirse bu kayıt silinmesin (hata versin)
        });

        // --- WorkoutSession ---
        modelBuilder.Entity<WorkoutSession>(entity =>
        {
            // entity.ToTable("workout_sessions");
            entity.HasKey(e => e.WorkoutSessionId); // Doğru anahtar adı

            // WorkoutSession -> User (Many-to-One)
            entity.HasOne(ws => ws.User)
                  .WithMany()
                  .HasForeignKey(ws => ws.UserId)
                  .OnDelete(DeleteBehavior.Cascade); // User silinirse seansları da silinsin

            // WorkoutSession -> WorkoutPlan (Many-to-One, Opsiyonel)
            entity.HasOne(ws => ws.WorkoutPlan)
                  .WithMany()
                  .HasForeignKey(ws => ws.WorkoutPlanId)
                  .IsRequired(false) // Null olabilir
                  .OnDelete(DeleteBehavior.SetNull); // Plan silinirse bu alandaki FK null olsun
        });

        // --- SessionExercise ---
        modelBuilder.Entity<SessionExercise>(entity =>
        {
            // entity.ToTable("session_exercises");
            entity.HasKey(e => e.SessionExerciseId); // Doğru anahtar adı

            // SessionExercise -> WorkoutSession (Many-to-One)
            entity.HasOne(se => se.WorkoutSession)
                  .WithMany()
                  .HasForeignKey(se => se.WorkoutSessionId)
                  .OnDelete(DeleteBehavior.Cascade); // Seans silinirse bu kayıtlar da silinsin

            // SessionExercise -> Exercise (Many-to-One)
            entity.HasOne(se => se.Exercise)
                  .WithMany()
                  .HasForeignKey(se => se.ExerciseId)
                  .OnDelete(DeleteBehavior.Restrict); // Ana egzersiz silinirse bu kayıt silinmesin

            // SessionExercise -> PlanExercise (Many-to-One, Opsiyonel)
            entity.HasOne(se => se.PlanExercise)
                  .WithMany()
                  .HasForeignKey(se => se.PlanExerciseId)
                  .IsRequired(false) // Null olabilir
                  .OnDelete(DeleteBehavior.SetNull); // Plan egzersizi silinirse bu alandaki FK null olsun
        });

        // --- SetLog ---
        modelBuilder.Entity<SetLog>(entity =>
        {
            // entity.ToTable("set_logs");
            entity.HasKey(e => e.SetLogId); // Doğru anahtar adı

            // SetLog -> SessionExercise (Many-to-One)
            entity.HasOne(sl => sl.SessionExercise)
                  .WithMany() // SessionExercise'da ters navigation property yoksa boş bırakılabilir
                  .HasForeignKey(sl => sl.SessionExerciseId)
                  .OnDelete(DeleteBehavior.Cascade); // SessionExercise silinirse set logları da silinsin
        });

        // --- CalorieCalculation ---
        modelBuilder.Entity<CalorieCalculation>(entity =>
        {
            // entity.ToTable("calorie_calculations");
            entity.HasKey(e => e.CalorieCalculationId); // Doğru anahtar adı

            // Decimal alanlar için hassasiyet belirtmek iyi bir pratiktir
            entity.Property(e => e.Height).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Weight).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Tdee).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.AdjustedCalories).HasColumnType("decimal(10, 2)");

            // CalorieCalculation -> User (Many-to-One)
            entity.HasOne(cc => cc.User)
                  .WithMany()
                  .HasForeignKey(cc => cc.UserId)
                  .OnDelete(DeleteBehavior.Cascade); // User silinirse hesaplamaları da silinsin
        });

        // --- Achievement ---
        modelBuilder.Entity<Achievement>(entity =>
        {
            // entity.ToTable("achievements");
            entity.HasKey(e => e.AchievementId); // Doğru anahtar adı
            entity.Property(e => e.AchievementName).IsRequired();

            // Achievement -> User (Many-to-One)
            entity.HasOne(a => a.User)
                  .WithMany()
                  .HasForeignKey(a => a.UserId)
                  .OnDelete(DeleteBehavior.Cascade); // User silinirse başarıları da silinsin
        });

        // --- RegionalWorkout ---
        modelBuilder.Entity<RegionalWorkout>(entity =>
        {
            // entity.ToTable("regional_workouts");
            entity.HasKey(e => e.RegionalWorkoutId); // Doğru anahtar adı
            entity.Property(e => e.Name).IsRequired();

            // RegionalWorkout -> ExerciseRegion (Many-to-One)
            entity.HasOne(rw => rw.ExerciseRegion)
                  .WithMany()
                  .HasForeignKey(rw => rw.ExerciseRegionId) // Düzeltilmiş FK adı
                  .OnDelete(DeleteBehavior.Restrict); // Bölge silinirse antrenman silinmesin
        });

        // --- RegionalWorkoutExercise ---
        modelBuilder.Entity<RegionalWorkoutExercise>(entity =>
        {
            // entity.ToTable("regional_workout_exercises");
            entity.HasKey(e => e.RegionalWorkoutExerciseId); // Doğru anahtar adı

            // RegionalWorkoutExercise -> RegionalWorkout (Many-to-One)
            entity.HasOne(rwe => rwe.RegionalWorkout)
                  .WithMany() // RegionalWorkout'da ters navigation property yoksa boş bırakılabilir
                  .HasForeignKey(rwe => rwe.WorkoutId)
                  .OnDelete(DeleteBehavior.Cascade); // Bölgesel antrenman silinirse egzersizleri de silinsin

            // RegionalWorkoutExercise -> Exercise (Many-to-One)
            entity.HasOne(rwe => rwe.Exercise)
                  .WithMany()
                  .HasForeignKey(rwe => rwe.ExerciseId) // Düzeltilmiş FK adı (ExerciseName yerine)
                  .OnDelete(DeleteBehavior.Restrict); // Ana egzersiz silinirse bu kayıt silinmesin
        });

        // --- UserSetting ---
        modelBuilder.Entity<UserSetting>(entity =>
        {
            // entity.ToTable("user_settings");
            entity.HasKey(e => e.Id); // Anahtar adı 'Id' olarak kalmış

            // UserSetting -> User (One-to-One)
            entity.HasOne(us => us.User)
                  .WithOne() // User'da ters navigation property yoksa boş bırakılabilir
                  .HasForeignKey<UserSetting>(us => us.UserId) // Doğru FK
                  .OnDelete(DeleteBehavior.Cascade); // User silinirse ayarları da silinsin

            entity.HasIndex(us => us.UserId).IsUnique(); // Her kullanıcının tek bir ayarı olabilir
        });
    }
}