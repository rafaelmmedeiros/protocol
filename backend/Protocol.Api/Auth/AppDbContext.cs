using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Protocol.Api.Hevy;
using Protocol.Api.Training;

namespace Protocol.Api.Auth;

/// <summary>
/// The single EF Core context. Identity owns every table it declares; application tables are
/// added here as features land.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser>(options), IDataProtectionKeyContext
{
    /// <summary>
    /// The Data Protection key ring, kept in the database rather than on disk.
    /// <para>
    /// ADR-014 names losing this as the trap: the container's default key ring is ephemeral, so
    /// a restart would leave every stored Hevy key silently undecryptable. Keeping it here puts
    /// the keys in the same place as the ciphertext they open, so a database restored from
    /// backup brings its own keys with it — a filesystem ring and a database can drift apart,
    /// and the only symptom is that nothing decrypts.
    /// </para>
    /// </summary>
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    public DbSet<HevyConnection> HevyConnections => Set<HevyConnection>();

    public DbSet<Exercise> Exercises => Set<Exercise>();

    public DbSet<TrainingProfile> TrainingProfiles => Set<TrainingProfile>();

    public DbSet<GeneratedWeek> GeneratedWeeks => Set<GeneratedWeek>();

    public DbSet<UserEquipment> UserEquipment => Set<UserEquipment>();

    public DbSet<ExerciseExclusion> ExerciseExclusions => Set<ExerciseExclusion>();

    public DbSet<PreferredVariant> PreferredVariants => Set<PreferredVariant>();

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

            exercise.OwnsMany(e => e.Requirements, requirement =>
            {
                requirement.ToTable("exercise_requirements");
                requirement.WithOwner().HasForeignKey(r => r.ExerciseId);
                requirement.Property(r => r.Item).HasConversion<string>().IsRequired().HasMaxLength(32);
                requirement.HasKey(r => new { r.ExerciseId, r.Item });
            });
        });

        builder.Entity<ExerciseExclusion>(exclusion =>
        {
            exclusion.ToTable("exercise_exclusions");
            exclusion.HasKey(e => e.Id);
            exclusion.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            exclusion.HasIndex(e => new { e.UserId, e.ExerciseId }).IsUnique();

            // Restrict: an exercise a user has excluded cannot be deleted out from under the
            // exclusion, for the same reason a stored week pins its exercises.
            exclusion.HasOne<Exercise>()
                .WithMany()
                .HasForeignKey(e => e.ExerciseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PreferredVariant>(preferred =>
        {
            preferred.ToTable("preferred_variants");
            preferred.HasKey(p => p.Id);
            preferred.Property(p => p.UserId).IsRequired().HasMaxLength(450);
            preferred.Property(p => p.MovementPattern).HasConversion<string>().IsRequired().HasMaxLength(32);
            // One preferred exercise per movement pattern, enforced by the database.
            preferred.HasIndex(p => new { p.UserId, p.MovementPattern }).IsUnique();

            preferred.HasOne<Exercise>()
                .WithMany()
                .HasForeignKey(p => p.ExerciseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<UserEquipment>(item =>
        {
            item.ToTable("user_equipment");
            item.HasKey(i => i.Id);
            item.Property(i => i.UserId).IsRequired().HasMaxLength(450);
            item.Property(i => i.Item).HasConversion<string>().IsRequired().HasMaxLength(32);
            // One row per user per item, so owning a thing twice is impossible.
            item.HasIndex(i => new { i.UserId, i.Item }).IsUnique();
        });

        builder.Entity<HevyConnection>(connection =>
        {
            connection.ToTable("hevy_connections");
            connection.HasKey(c => c.Id);

            // One connection per user, enforced by the database rather than by the endpoint.
            connection.Property(c => c.UserId).IsRequired().HasMaxLength(450);
            connection.HasIndex(c => c.UserId).IsUnique();

            // Ciphertext, as text. Data Protection's string overload already returns base64url,
            // so storing bytes would mean decoding on the way in and encoding on the way out for
            // no gain.
            connection.Property(c => c.ProtectedApiKey).IsRequired();

            // timestamptz, both of them. UTC in, UTC out (root standard 5).
            connection.Property(c => c.ConnectedAt).IsRequired();
            connection.Property(c => c.SyncCursor);
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

        builder.Entity<GeneratedWeek>(week =>
        {
            week.ToTable("generated_weeks");
            week.HasKey(w => w.Id);

            week.Property(w => w.UserId).IsRequired().HasMaxLength(450);
            // Read pattern is "this user's most recent week", so the index matches it.
            week.HasIndex(w => new { w.UserId, w.GeneratedAt });

            week.Property(w => w.WeekStartDate).IsRequired();

            // timestamptz. UTC in, UTC out (root standard 5).
            week.Property(w => w.GeneratedAt).IsRequired();

            // The profile, snapshotted (ADR-003). Editing the profile must not reach back into
            // a week the user already trained.
            week.Property(w => w.Goal).HasConversion<string>().IsRequired().HasMaxLength(32);
            week.Property(w => w.DaysPerWeek).IsRequired();
            week.Property(w => w.SessionDurationSeconds).IsRequired();

            week.HasMany(w => w.Sessions)
                .WithOne()
                .HasForeignKey(s => s.GeneratedWeekId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<GeneratedSession>(session =>
        {
            session.ToTable("generated_sessions");
            session.HasKey(s => s.Id);

            session.Property(s => s.Position).IsRequired();
            session.Property(s => s.Day).HasConversion<string>().IsRequired().HasMaxLength(16);
            session.Property(s => s.Kind).HasConversion<string>().IsRequired().HasMaxLength(16);

            session.HasMany(s => s.Prescriptions)
                .WithOne()
                .HasForeignKey(p => p.GeneratedSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<GeneratedPrescription>(prescription =>
        {
            prescription.ToTable("generated_prescriptions");
            prescription.HasKey(p => p.Id);

            prescription.Property(p => p.Position).IsRequired();
            prescription.Property(p => p.Sets).IsRequired();
            prescription.Property(p => p.MinReps).IsRequired();
            prescription.Property(p => p.MaxReps).IsRequired();
            prescription.Property(p => p.RepsInReserve).IsRequired();
            prescription.Property(p => p.RestSeconds).IsRequired();

            // Restrict, not cascade: an exercise that a stored week references cannot be deleted
            // out from under it. Training history is append-only (root standard 7).
            prescription.HasOne(p => p.Exercise)
                .WithMany()
                .HasForeignKey(p => p.ExerciseId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
