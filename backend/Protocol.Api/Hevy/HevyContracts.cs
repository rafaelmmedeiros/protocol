using System.Text.Json.Serialization;

namespace Protocol.Api.Hevy;

/// <summary>
/// Hevy's wire shapes, transcribed from their published OpenAPI document.
/// <para>
/// These types exist so that nothing else has to know Hevy's vocabulary (root standard 17).
/// They are the only place in this system that spells <c>exercise_template_id</c>, that knows
/// <c>rpe</c> is a number from six to ten, or that a set carries a <c>type</c> string. Every
/// name is pinned with an explicit attribute rather than left to a naming policy — our own API
/// is camelCase and theirs is snake_case, and a global policy change must not silently rewrite
/// a third party's contract.
/// </para>
/// <para>
/// Nullability here mirrors <i>their</i> declarations, not our expectations. Almost everything
/// is nullable because almost everything in their schema is, and pretending otherwise moves the
/// failure from the mapper to a null reference deep in the domain.
/// </para>
/// </summary>
internal static class HevyContractDocs;

// ---------------------------------------------------------------------------------------------
// Outbound: what we send
// ---------------------------------------------------------------------------------------------

/// <summary>The envelope <c>POST /v1/routine_folders</c> expects.</summary>
public sealed record HevyFolderRequest(
    [property: JsonPropertyName("routine_folder")] HevyFolderPayload RoutineFolder);

/// <summary>A folder carries a title and nothing else.</summary>
public sealed record HevyFolderPayload(
    [property: JsonPropertyName("title")] string Title);

/// <summary>The envelope <c>POST /v1/routines</c> and <c>PUT /v1/routines/{id}</c> expect.</summary>
public sealed record HevyRoutineRequest(
    [property: JsonPropertyName("routine")] HevyRoutinePayload Routine);

/// <summary>A routine: one of our planned sessions, in their vocabulary.</summary>
public sealed record HevyRoutinePayload(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("folder_id")] long? FolderId,
    [property: JsonPropertyName("notes")] string? Notes,
    [property: JsonPropertyName("exercises")] IReadOnlyList<HevyRoutineExercise> Exercises);

/// <summary>
/// One slot of a session. <c>rest_seconds</c> is theirs to hold; <c>notes</c> is the only place
/// a prescribed effort can reach the user, because a routine set has no effort field (ADR-016).
/// </summary>
public sealed record HevyRoutineExercise(
    [property: JsonPropertyName("exercise_template_id")] string ExerciseTemplateId,
    [property: JsonPropertyName("superset_id")] int? SupersetId,
    [property: JsonPropertyName("rest_seconds")] int? RestSeconds,
    [property: JsonPropertyName("notes")] string? Notes,
    [property: JsonPropertyName("sets")] IReadOnlyList<HevyRoutineSet> Sets);

/// <summary>
/// A prescribed set. There is no <c>rpe</c> here and there should not be: effort is feedback,
/// produced after a set, and a plan does not carry an observation (ADR-016).
/// </summary>
public sealed record HevyRoutineSet(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("weight_kg")] double? WeightKg,
    [property: JsonPropertyName("reps")] int? Reps,
    [property: JsonPropertyName("rep_range")] HevyRepRange? RepRange);

/// <summary>A repetition range, which their routine sets accept natively.</summary>
public sealed record HevyRepRange(
    [property: JsonPropertyName("start")] int Start,
    [property: JsonPropertyName("end")] int End);

// ---------------------------------------------------------------------------------------------
// Inbound: what we read
// ---------------------------------------------------------------------------------------------

/// <summary>
/// What <c>POST /v1/routine_folders</c> actually answers.
/// <para>
/// **Their OpenAPI document declares the bare object and the service returns an envelope.** The
/// difference cost a silent bug: deserialising the declared shape produced a folder with
/// <c>id = 0</c>, which was stored happily and then rejected by every routine sent into it. The
/// contract was read rather than assumed, which was right — and the contract was wrong, which
/// only a live call could show.
/// </para>
/// </summary>
public sealed record HevyFolderEnvelope(
    [property: JsonPropertyName("routine_folder")] HevyRoutineFolder? RoutineFolder);

/// <summary>
/// What <c>POST /v1/routines</c> and <c>PUT /v1/routines/{id}</c> actually answer: an envelope
/// holding an **array**, where the document declares a bare object.
/// </summary>
public sealed record HevyRoutineEnvelope(
    [property: JsonPropertyName("routine")] IReadOnlyList<HevyRoutine>? Routine);

/// <summary>A routine as Hevy returns it, after creating or updating one.</summary>
public sealed record HevyRoutine(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("folder_id")] long? FolderId);

/// <summary>A routine folder as Hevy returns it. Its identifier is a number, unlike a routine's.</summary>
public sealed record HevyRoutineFolder(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("title")] string? Title);

/// <summary>
/// A logged workout.
/// <para>
/// <c>routine_id</c> is the whole reason the comparison is possible: a live experiment created a
/// routine, trained from it, and the workout came back carrying that identifier. It is null for
/// a workout started from nothing, which is ordinary and handled rather than an error (ADR-019).
/// </para>
/// </summary>
public sealed record HevyWorkout(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("routine_id")] string? RoutineId,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("start_time")] DateTimeOffset StartTime,
    [property: JsonPropertyName("end_time")] DateTimeOffset EndTime,
    [property: JsonPropertyName("updated_at")] DateTimeOffset UpdatedAt,
    [property: JsonPropertyName("created_at")] DateTimeOffset CreatedAt,
    [property: JsonPropertyName("exercises")] IReadOnlyList<HevyWorkoutExercise>? Exercises);

/// <summary>One exercise inside a logged workout.</summary>
public sealed record HevyWorkoutExercise(
    [property: JsonPropertyName("index")] int Index,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("notes")] string? Notes,
    [property: JsonPropertyName("exercise_template_id")] string? ExerciseTemplateId,
    [property: JsonPropertyName("superset_id")] int? SupersetId,
    [property: JsonPropertyName("sets")] IReadOnlyList<HevyWorkoutSet>? Sets);

/// <summary>
/// One performed set.
/// <para>
/// <c>reps</c> is declared as a number rather than an integer in their schema, and is
/// transcribed that way deliberately — narrowing it here would throw on a payload their contract
/// permits. <c>rpe</c> is optional and, in every workout observed from a real account so far,
/// absent.
/// </para>
/// </summary>
public sealed record HevyWorkoutSet(
    [property: JsonPropertyName("index")] int Index,
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("weight_kg")] double? WeightKg,
    [property: JsonPropertyName("reps")] double? Reps,
    [property: JsonPropertyName("distance_meters")] double? DistanceMeters,
    [property: JsonPropertyName("duration_seconds")] double? DurationSeconds,
    [property: JsonPropertyName("rpe")] double? Rpe,
    [property: JsonPropertyName("custom_metric")] double? CustomMetric);

/// <summary>
/// One entry of the events feed. A single shape carrying two cases: an update brings the whole
/// workout, a deletion brings only an identifier and a time.
/// </summary>
public sealed record HevyWorkoutEvent(
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("workout")] HevyWorkout? Workout,
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("deleted_at")] DateTimeOffset? DeletedAt);

/// <summary>A page of the events feed.</summary>
public sealed record HevyWorkoutEventPage(
    [property: JsonPropertyName("page")] int Page,
    [property: JsonPropertyName("page_count")] int PageCount,
    [property: JsonPropertyName("events")] IReadOnlyList<HevyWorkoutEvent>? Events);
