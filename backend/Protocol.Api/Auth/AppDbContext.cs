using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Protocol.Api.Training;

namespace Protocol.Api.Auth;

/// <summary>
/// The single EF Core context. Identity owns every table it declares; application tables are
/// added here as features land.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser>(options)
{
    public DbSet<Exercise> Exercises => Set<Exercise>();

    public DbSet<TrainingProfile> TrainingProfiles => Set<TrainingProfile>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Exercise>(exercise =>
        {
            exercise.ToTable("exercises");
            exercise.HasKey(e => e.Id);

            // Hevy's identifier lives beside ours, never as the key (root standard 8). Unique
            // because one Hevy template maps to exactly one of our rows -- that is what makes
            // the export a lookup rather than a reconciliation (ADR-002).
            exercise.Property(e => e.ExternalTemplateId).IsRequired().HasMaxLength(64);
            exercise.HasIndex(e => e.ExternalTemplateId).IsUnique();

            exercise.Property(e => e.Title).IsRequired().HasMaxLength(200);

            // Enums are stored as text, not as ordinals. An ordinal silently changes meaning
            // when a value is inserted into the enum, and training history is append-only
            // (root standard 7) -- a week generated last month must still read correctly.
            exercise.Property(e => e.MovementPattern).HasConversion<string>().IsRequired().HasMaxLength(32);
            exercise.Property(e => e.Mechanic).HasConversion<string>().IsRequired().HasMaxLength(16);
            exercise.Property(e => e.Equipment).HasConversion<string>().IsRequired().HasMaxLength(32);
            exercise.Property(e => e.OrderClass).HasConversion<string>().IsRequired().HasMaxLength(32);
            exercise.Property(e => e.Laterality).HasConversion<string>().IsRequired().HasMaxLength(16);

            exercise.Property(e => e.PreferenceRank).IsRequired();

            exercise.OwnsMany(e => e.Muscles, muscle =>
            {
                muscle.ToTable("exercise_muscles");
                muscle.WithOwner().HasForeignKey(m => m.ExerciseId);
                muscle.Property(m => m.MuscleGroup).HasConversion<string>().IsRequired().HasMaxLength(32);
                muscle.Property(m => m.Role).HasConversion<string>().IsRequired().HasMaxLength(16);
                muscle.HasKey(m => new { m.ExerciseId, m.MuscleGroup });
            });
        });

        builder.Entity<TrainingProfile>(profile =>
        {
            profile.ToTable("training_profiles");
            profile.HasKey(p => p.Id);

            // One profile per user, enforced by the database rather than by the endpoint.
            profile.Property(p => p.UserId).IsRequired().HasMaxLength(450);
            profile.HasIndex(p => p.UserId).IsUnique();

            profile.Property(p => p.Goal).HasConversion<string>().IsRequired().HasMaxLength(32);
            profile.Property(p => p.DaysPerWeek).IsRequired();

            // Seconds, never minutes -- the unit is in the field name (root standard 4).
            profile.Property(p => p.SessionDurationSeconds).IsRequired();

            // No rest column: rest is a property of the slot and the record decides it, not the
            // user (ADR-007, TD-011). The absence is the decision.
        });
    }
}
