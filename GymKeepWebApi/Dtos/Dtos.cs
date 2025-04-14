using System.ComponentModel.DataAnnotations;

namespace GymKeepWebApi.Dtos // Veya GymKeepWebApi.Dtos
{
    // Egzersizleri listelerken veya detayını gösterirken kullanılacak DTO
    public record ExerciseDto(
        int Id,
        string Name,
        string? Description,
        string? VideoUrl,
        string? ImageUrl,
        int DifficultyLevelId,
        string DifficultyLevelName, // Kullanıcı dostu olması için seviye adı
        int RegionId,
        string RegionName // Kullanıcı dostu olması için bölge adı
    );

    // Yeni egzersiz oluştururken kullanılacak DTO
    public record CreateExerciseDto(
        [Required][StringLength(150)] string Name,
        string? Description,
        [Url] string? VideoUrl,
        [Url] string? ImageUrl,
        [Required] int DifficultyLevelId,
        [Required] int RegionId
    );

    // Egzersiz güncellerken kullanılacak DTO
    public record UpdateExerciseDto(
        [Required][StringLength(150)] string Name,
        string? Description,
        [Url] string? VideoUrl,
        [Url] string? ImageUrl,
        [Required] int DifficultyLevelId,
        [Required] int RegionId
    );

    // Kullanıcıdan gelen zorluk seviyesini DTO olarak alırken kullanılacak
    public record DifficultyLevelDto(int Id, string Name);

    // Kullanıcıdan gelen zorluk seviyesini DTO olarak alırken kullanılacak
    public record ExerciseRegionDto(int Id, string Name);


    // Plan içindeki egzersiz detayı için DTO
    public record PlanExerciseDetailDto(
        int PlanExerciseId, // PlanExercise tablosunun ID'si
        int ExerciseId,
        string ExerciseName,
        int Sets,
        int Reps,
        int? RestDurationSeconds,
        int? OrderInPlan
    );

    // Bir antrenman planının tüm detayları için DTO
    public record WorkoutPlanDetailDto(
        int Id,
        string Name,
        string? Description,
        DateTime CreatedAt,
        List<PlanExerciseDetailDto> Exercises // Plandaki egzersizler
    );

    // Sadece plan listesi için DTO
    public record WorkoutPlanSummaryDto(
        int Id,
        string Name,
        string? Description,
        DateTime CreatedAt,
        int ExerciseCount // Plandaki egzersiz sayısı
    );

    // Yeni plan oluşturma DTO'su
    public record CreateWorkoutPlanDto(
        [Required][StringLength(200)] string Name,
        string? Description
    );

    // Plan güncelleme DTO'su
    public record UpdateWorkoutPlanDto(
        [Required][StringLength(200)] string Name,
        string? Description
    );

    // Plana egzersiz ekleme/güncelleme DTO'su
    public record AddOrUpdatePlanExerciseDto(
        [Required] int ExerciseId,
        [Required][Range(1, 100)] int Sets,
        [Required][Range(1, 200)] int Reps,
        [Range(0, 600)] int? RestDurationSeconds,
        int? OrderInPlan
    );

    // Set log detayı
    public record SetLogDto(
        int Id,
        int SetNumber,
        decimal? Weight,
        int? RepsCompleted,
        bool IsCompleted,
        DateTime? CompletedAt
    );

    // Seans içindeki egzersiz detayı
    public record SessionExerciseDetailDto(
        int Id, // SessionExercise ID
        int ExerciseId,
        string ExerciseName,
        int? OrderInSession,
        List<SetLogDto> Sets // Tamamlanan setler
    );

    // Seans özeti (liste için)
    public record WorkoutSessionSummaryDto(
        int Id,
        DateTime SessionDate,
        int? DurationMinutes,
        string? Notes,
        int? WorkoutPlanId, // Hangi plan takip edildi
        string? WorkoutPlanName, // Planın adı
        int ExerciseCount,
        int CompletedSetCount
    );

    // Seans detayı (tek bir seans için)
    public record WorkoutSessionDetailDto(
        int Id,
        DateTime SessionDate,
        int? DurationMinutes,
        string? Notes,
        int? WorkoutPlanId,
        string? WorkoutPlanName,
        List<SessionExerciseDetailDto> Exercises
    );

    // Yeni seans başlatma DTO'su
    public record StartWorkoutSessionDto(
        int? WorkoutPlanId = null, // Opsiyonel: Bir plana göre başlat
        string? Notes = null
    );

    // Seansa egzersiz ekleme DTO'su
    public record AddSessionExerciseDto(
        [Required] int ExerciseId,
        int? OrderInSession = null
    );

    // Set loglama DTO'su
    public record LogSetDto(
        [Required] int SetNumber,
        decimal? Weight,
        int? RepsCompleted,
        [Required] bool IsCompleted
    );

    // Seans bitirme/güncelleme DTO'su
    public record EndWorkoutSessionDto(
        int? DurationMinutes,
        string? Notes
    );


    public record UserSettingDto(
    int? DailyGoal,
    bool IsDarkMode,
    bool NotificationsEnabled,
    TimeSpan? NotificationTime,
    DateTime UpdatedAt
);

    public record UpdateUserSettingDto(
        int? DailyGoal,
        bool IsDarkMode,
        bool NotificationsEnabled,
        TimeSpan? NotificationTime // HH:mm:ss formatında gönderilebilir
    );
}