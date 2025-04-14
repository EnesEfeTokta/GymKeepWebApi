using Microsoft.EntityFrameworkCore;
using GymKeepWebApi.Models; // Entity sınıflarınızın namespace'i

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

        // ----- Entity Yapılandırmaları -----

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users"); // snake_case
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.Username).IsUnique(); // Unique index
            entity.HasIndex(e => e.Email).IsUnique();    // Unique index

            entity.HasMany(u => u.WorkoutPlans)
                  .WithOne(wp => wp.User)
                  .HasForeignKey(wp => wp.UserId)
                  .OnDelete(DeleteBehavior.Cascade); // User silinirse planları sil

            entity.HasMany(u => u.WorkoutSessions)
                  .WithOne(ws => ws.User)
                  .HasForeignKey(ws => ws.UserId)
                  .OnDelete(DeleteBehavior.Cascade); // User silinirse seansları sil

            entity.HasMany(u => u.CalorieCalculations)
                  .WithOne(cc => cc.User)
                  .HasForeignKey(cc => cc.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(u => u.Achievements)
                 .WithOne(a => a.User)
                 .HasForeignKey(a => a.UserId)
                 .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(u => u.UserSettings) // Genellikle One-to-One olur ama modelde One-to-Many
                 .WithOne(us => us.User)
                 .HasForeignKey(us => us.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DifficultyLevel>(entity =>
        {
            entity.ToTable("difficulty_levels");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique(); // İsimler benzersiz olmalı
        });

        modelBuilder.Entity<ExerciseRegion>(entity =>
        {
            entity.ToTable("exercise_regions");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique(); // İsimler benzersiz olmalı
        });

        modelBuilder.Entity<Exercise>(entity =>
        {
            entity.ToTable("exercises");
            entity.HasKey(e => e.Id);

            // Exercise -> DifficultyLevel (Many-to-One)
            entity.HasOne(e => e.DifficultyLevel)
                  .WithMany(dl => dl.Exercises) // DifficultyLevel'daki koleksiyon
                  .HasForeignKey(e => e.DifficultyLevelId)
                  .OnDelete(DeleteBehavior.Restrict); // Seviye silinirse ilişkili egzersizler silinmesin (hata versin)

            // Exercise -> ExerciseRegion (Many-to-One)
            entity.HasOne(e => e.ExerciseRegion)
                  .WithMany(er => er.Exercises) // ExerciseRegion'daki koleksiyon
                  .HasForeignKey(e => e.RegionId)
                  .OnDelete(DeleteBehavior.Restrict); // Bölge silinirse ilişkili egzersizler silinmesin

            // Hatalı User ilişkisi kaldırıldı.
        });

        modelBuilder.Entity<WorkoutPlan>(entity =>
        {
            entity.ToTable("workout_plans");
            entity.HasKey(e => e.Id);

            // WorkoutPlan -> User ilişkisi User entity'sinde tanımlandı (WithMany/WithOne)
            // Burada tekrar tanımlamaya gerek yok ama silme davranışı override edilebilir.

            // WorkoutPlan silinince PlanExercise'ler de silinsin
            entity.HasMany(wp => wp.PlanExercises)
                  .WithOne(pe => pe.WorkoutPlan)
                  .HasForeignKey(pe => pe.WorkoutPlanId)
                  .OnDelete(DeleteBehavior.Cascade);

            // WorkoutPlan silinince WorkoutSession'lardaki referans NULL olsun
            entity.HasMany(wp => wp.WorkoutSessions)
                  .WithOne(ws => ws.WorkoutPlan)
                  .HasForeignKey(ws => ws.WorkoutPlanId)
                  .OnDelete(DeleteBehavior.SetNull); // Plan silinse bile seans kaydı kalsın
        });

        modelBuilder.Entity<PlanExercise>(entity =>
        {
            entity.ToTable("plan_exercises");
            entity.HasKey(e => e.Id);

            entity.HasOne(pe => pe.Exercise)
                  .WithMany(ex => ex.PlanExercises)
                  .HasForeignKey(pe => pe.ExerciseId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(pe => pe.SessionExercises)
                  .WithOne(se => se.PlanExercise)
                  .HasForeignKey(se => se.PlanExerciseId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<WorkoutSession>(entity =>
        {
            entity.ToTable("workout_sessions");
            entity.HasKey(e => e.Id);

            entity.HasMany(ws => ws.SessionExercises)
                  .WithOne(se => se.WorkoutSession)
                  .HasForeignKey(se => se.WorkoutSessionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SessionExercise>(entity =>
        {
            entity.ToTable("session_exercises");
            entity.HasKey(e => e.Id);

            entity.HasOne(se => se.Exercise)
                  .WithMany(ex => ex.SessionExercises)
                  .HasForeignKey(se => se.ExerciseId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(se => se.SetLogs)
                  .WithOne(sl => sl.SessionExercise)
                  .HasForeignKey(sl => sl.SessionExerciseId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SetLog>(entity =>
        {
            entity.ToTable("set_logs");
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<CalorieCalculation>(entity =>
        {
            entity.ToTable("calorie_calculations");
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<Achievement>(entity =>
        {
            entity.ToTable("achievements");
            entity.HasKey(e => e.Id);
            // Achievement -> User (Many-to-One) - User'da tanımlandı.
        });

        modelBuilder.Entity<RegionalWorkout>(entity =>
        {
            entity.ToTable("regional_workouts");
            entity.HasKey(e => e.Id);

            // RegionalWorkout silinince RegionalWorkoutExercise'ler de silinsin
            entity.HasMany(rw => rw.RegionalWorkoutExercises)
                  .WithOne(rwe => rwe.RegionalWorkout)
                  .HasForeignKey(rwe => rwe.WorkoutId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RegionalWorkoutExercise>(entity =>
        {
            entity.ToTable("regional_workout_exercises");
            // Kompozit Anahtar
            entity.HasKey(e => new { e.WorkoutId, e.ExerciseName });
        });

        modelBuilder.Entity<UserSetting>(entity =>
        {
            entity.ToTable("user_settings");
            entity.HasKey(e => e.Id);
        });
    }
}