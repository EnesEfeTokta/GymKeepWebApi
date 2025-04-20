using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GymKeepWebApi.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        [Required]
        public int UserId { get; set; }

        [Required]
        [Column("Username")]
        public string Username { get; set; } = null!;

        [Required]
        [EmailAddress]
        [Column("Email")]
        public string Email { get; set; } = null!;

        [Required]
        [Column("Password")]
        public string Password { get; set; } = null!;

        [Required]
        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Column("UpdatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("Exercises")]
    public class Exercise
    {
        [Key]
        [Required]
        public int ExerciseId { get; set; }

        [Required]
        [Column("Name")]
        public string Name { get; set; } = null!;

        [Column("Description")]
        public string? Description { get; set; } // Made nullable, removed [Required]

        [Url]
        [Column("VideoUrl")]
        public string? VideoUrl { get; set; } // Made nullable, removed [Required]

        [Url]
        [Column("ImageUrl")]
        public string? ImageUrl { get; set; } // Made nullable, removed [Required]

        [Required]
        [Column("DifficultyLevelId")]
        public int DifficultyLevelId { get; set; }

        [ForeignKey("DifficultyLevelId")]
        public virtual DifficultyLevel DifficultyLevel { get; set; } = null!;

        [Required]
        [Column("RegionId")]
        public int RegionId { get; set; }

        [ForeignKey("RegionId")]
        public virtual ExerciseRegion ExerciseRegion { get; set; } = null!;
    }

    [Table("DifficultyLevels")]
    public class DifficultyLevel
    {
        [Key]
        [Required]
        public int DifficultyLevelId { get; set; }

        [Required]
        [Column("Name")]
        public string Name { get; set; } = null!;
    }

    [Table("ExerciseRegions")]
    public class ExerciseRegion
    {
        [Key]
        [Required]
        public int ExerciseRegionId { get; set; }

        [Required]
        [Column("Name")]
        public string Name { get; set; } = null!;
    }

    [Table("WorkoutPlans")]
    public class WorkoutPlan
    {
        [Key]
        [Required]
        public int WorkoutPlanId { get; set; }

        [Required]
        [Column("Name")]
        public string Name { get; set; } = null!;

        [Column("Description")]
        public string? Description { get; set; } // Made nullable, removed [Required]

        [Required]
        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Column("UserId")]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }

    [Table("PlanExercises")]
    public class PlanExercise
    {
        [Key]
        [Required]
        public int PlanExerciseId { get; set; }

        [Required]
        [Column("Sets")]
        public int Sets { get; set; }

        [Required]
        [Column("Reps")]
        public int Reps { get; set; }

        [Column("RestDurationSeconds")]
        public int? RestDurationSeconds { get; set; } // Made nullable, removed [Required]

        [Column("OrderInPlan")]
        public int? OrderInPlan { get; set; } // Made nullable, removed [Required]

        [Required]
        [Column("WorkoutPlanId")] // Corrected column mapping
        public int WorkoutPlanId { get; set; }

        [Required]
        [Column("ExerciseId")] // Corrected column mapping
        public int ExerciseId { get; set; }

        [ForeignKey("WorkoutPlanId")]
        public virtual WorkoutPlan WorkoutPlan { get; set; } = null!;

        [ForeignKey("ExerciseId")]
        public virtual Exercise Exercise { get; set; } = null!;
    }

    [Table("WorkoutSessions")]
    public class WorkoutSession
    {
        [Key]
        [Required]
        public int WorkoutSessionId { get; set; }

        [Required]
        [Column("SessionDate")]
        public DateTime SessionDate { get; set; } = DateTime.UtcNow;

        [Column("DurationMinutes")]
        public int? DurationMinutes { get; set; } // Made nullable, removed [Required]

        [Column("Notes")]
        public string? Notes { get; set; } // Made nullable, removed [Required]

        [Required]
        [Column("UserId")]
        public int UserId { get; set; }

        [Column("WorkoutPlanId")] // Nullable FK
        public int? WorkoutPlanId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("WorkoutPlanId")]
        public virtual WorkoutPlan? WorkoutPlan { get; set; }
    }

    [Table("SessionExercises")]
    public class SessionExercise
    {
        [Key]
        [Required]
        public int SessionExerciseId { get; set; }

        [Column("OrderInSession")]
        public int? OrderInSession { get; set; } // Made nullable, removed [Required]

        [Required]
        [Column("WorkoutSessionId")]
        public int WorkoutSessionId { get; set; }

        [Required]
        [Column("ExerciseId")]
        public int ExerciseId { get; set; }

        [Column("PlanExerciseId")] // Nullable FK
        public int? PlanExerciseId { get; set; }

        [ForeignKey("WorkoutSessionId")]
        public virtual WorkoutSession WorkoutSession { get; set; } = null!;

        [ForeignKey("ExerciseId")]
        public virtual Exercise Exercise { get; set; } = null!;

        [ForeignKey("PlanExerciseId")]
        public virtual PlanExercise? PlanExercise { get; set; }
    }

    [Table("SetLogs")]
    public class SetLog
    {
        [Key]
        [Required]
        public int SetLogId { get; set; }

        [Required]
        [Column("SetNumber")]
        public int SetNumber { get; set; }

        [Column("Weight")]
        public decimal? Weight { get; set; } // Made nullable, removed [Required]

        [Column("RepsCompleted")]
        public int? RepsCompleted { get; set; } // Made nullable, removed [Required]

        [Required]
        [Column("IsCompleted")]
        public bool IsCompleted { get; set; } = false;

        [Column("CreatedAt")] // Renamed from CompletedAt for consistency? Or keep CompletedAt? Assuming CreatedAt for now.
        public DateTime? CreatedAt { get; set; } // Made nullable, removed [Required]

        [Required]
        [Column("SessionExerciseId")]
        public int SessionExerciseId { get; set; }

        [ForeignKey("SessionExerciseId")]
        public virtual SessionExercise SessionExercise { get; set; } = null!;
    }

    [Table("CalorieCalculations")]
    public class CalorieCalculation
    {
        [Key]
        [Required]
        public int CalorieCalculationId { get; set; }

        [Required]
        [Column("Age")]
        public int Age { get; set; }

        [Required]
        [Column("Height", TypeName = "decimal(5, 2)")] // Specify precision for consistency
        public decimal Height { get; set; }

        [Required]
        [Column("Weight", TypeName = "decimal(5, 2)")] // Specify precision for consistency
        public decimal Weight { get; set; }

        [Required]
        [Column("Gender")]
        public string Gender { get; set; } = null!;

        [Required]
        [Column("ActivityLevel")]
        public string ActivityLevel { get; set; } = null!;

        [Required]
        [Column("Goal")]
        public string Goal { get; set; } = null!;

        [Required]
        [Column("Tdee", TypeName = "decimal(10, 2)")] // Specify precision
        public decimal Tdee { get; set; }

        [Required]
        [Column("AdjustedCalories", TypeName = "decimal(10, 2)")] // Specify precision
        public decimal AdjustedCalories { get; set; }

        [Required]
        [Column("CalculationDate")]
        public DateTime CalculationDate { get; set; } = DateTime.UtcNow;

        [Required]
        [Column("UserId")]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }

    [Table("Achievements")]
    public class Achievement
    {
        [Key]
        [Required]
        public int AchievementId { get; set; }

        [Required]
        [Column("AchievementName")]
        public string AchievementName { get; set; } = null!;

        [Column("AchievementDescription")]
        public string? AchievementDescription { get; set; } // Made nullable, removed [Required]

        [Required]
        [Column("AchievedAt")]
        public DateTime AchievedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Column("UserId")]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }

    [Table("RegionalWorkouts")]
    public class RegionalWorkout
    {
        [Key]
        [Required]
        public int RegionalWorkoutId { get; set; }

        [Required]
        [Column("Name")]
        public string Name { get; set; } = null!;

        [Required]
        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Column("ExerciseRegionId")] // Changed from string Region
        public int ExerciseRegionId { get; set; }

        [ForeignKey("ExerciseRegionId")]
        public virtual ExerciseRegion ExerciseRegion { get; set; } = null!;
    }

    [Table("RegionalWorkoutExercises")]
    public class RegionalWorkoutExercise
    {
        [Key]
        [Required]
        public int RegionalWorkoutExerciseId { get; set; }

        [Column("Sets")]
        public int? Sets { get; set; } // Made nullable, removed [Required]

        [Column("Reps")]
        public int? Reps { get; set; } // Made nullable, removed [Required]

        [Column("OrderInWorkout")]
        public int? OrderInWorkout { get; set; } // Made nullable, removed [Required]

        [Required]
        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Column("WorkoutId")]
        public int WorkoutId { get; set; }

        [Required]
        [Column("ExerciseId")] // Changed from ExerciseName
        public int ExerciseId { get; set; }

        [ForeignKey("WorkoutId")]
        public virtual RegionalWorkout RegionalWorkout { get; set; } = null!;

        [ForeignKey("ExerciseId")]
        public virtual Exercise Exercise { get; set; } = null!;
    }

    [Table("UserSettings")]
    public class UserSetting
    {
        [Key]
        [Required]
        public int Id { get; set; }

        [Column("DailyGoal")]
        public int? DailyGoal { get; set; } // Made nullable, removed [Required]

        [Required]
        [Column("IsDarkMode")]
        public bool IsDarkMode { get; set; } = false;

        [Required]
        [Column("NotificationsEnabled")]
        public bool NotificationsEnabled { get; set; } = true;

        [Column("NotificationTime")]
        public TimeSpan? NotificationTime { get; set; } // Made nullable, removed [Required]

        [Required]
        [Column("UpdatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Column("UserId")]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}