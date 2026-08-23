using System.Reflection;
using Protocol.Api.Training;

namespace Protocol.Api.Tests.Unit.Hevy;

/// <summary>
/// The boundary, enforced by construction rather than by discipline (root standard 17).
/// <para>
/// A domain type that cannot be built without a Hevy payload is the failure standard 17 exists
/// to prevent, and it is the kind that arrives one convenient property at a time. These tests
/// are what makes it fail at build time instead of on the day the logging surface becomes ours.
/// </para>
/// </summary>
public class BoundaryIsolationTests
{
    private const string DomainNamespace = "Protocol.Api.Training";
    private const string BoundaryNamespace = "Protocol.Api.Hevy";

    private static IReadOnlyList<Type> DomainTypes =>
        [.. typeof(Exercise).Assembly
            .GetTypes()
            .Where(type => type.Namespace == DomainNamespace)];

    /// <summary>Unwraps arrays, collections, nullables and tasks to the types actually referenced.</summary>
    private static IEnumerable<Type> Referenced(Type type)
    {
        yield return type;

        if (type.IsArray && type.GetElementType() is { } element)
        {
            foreach (var inner in Referenced(element))
            {
                yield return inner;
            }
        }

        if (type.IsGenericType)
        {
            foreach (var argument in type.GetGenericArguments())
            {
                foreach (var inner in Referenced(argument))
                {
                    yield return inner;
                }
            }
        }
    }

    [Fact]
    public void No_domain_type_references_a_Hevy_contract()
    {
        var offenders = new List<string>();

        foreach (var type in DomainTypes)
        {
            var surface = type
                .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .Select(property => (Member: property.Name, property.PropertyType))
                .Concat(type
                    .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                    .Select(field => (Member: field.Name, PropertyType: field.FieldType)))
                .Concat(type
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .SelectMany(method => method
                        .GetParameters()
                        .Select(parameter => (Member: $"{method.Name}({parameter.Name})", PropertyType: parameter.ParameterType))
                        .Append((Member: $"{method.Name}()", PropertyType: method.ReturnType))));

            foreach (var (member, referencedType) in surface)
            {
                if (Referenced(referencedType).Any(t => t.Namespace == BoundaryNamespace))
                {
                    offenders.Add($"{type.Name}.{member}");
                }
            }
        }

        Assert.True(
            offenders.Count == 0,
            $"Hevy reached the domain through: {string.Join(", ", offenders)}. "
                + "Translate it in a mapper instead (root standard 17).");
    }

    [Fact]
    public void The_domain_has_no_symbol_named_for_a_rating_of_perceived_exertion()
    {
        // TD-017: RPE is Hevy's representation and RIR is ours. The domain stores a count of
        // repetitions, and the absence of the word is what keeps the two from being conflated by
        // someone adding "just one convenient field".
        var offenders = DomainTypes
            .SelectMany(type => type
                .GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Select(member => $"{type.Name}.{member.Name}")
                .Append(type.Name))
            .Where(name => name.Contains("rpe", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.True(
            offenders.Count == 0,
            $"The domain names an RPE in: {string.Join(", ", offenders)}. It stores reserve (TD-017).");
    }

    [Fact]
    public void Performed_training_can_be_built_with_no_Hevy_payload_in_sight()
    {
        // The positive form of the same rule, and the one that would actually break first. If
        // this ever needs a Hevy type to compile, the boundary has already been crossed.
        var workout = new PerformedWorkout
        {
            Id = Guid.CreateVersion7(),
            UserId = "user-1",
            ExternalWorkoutId = "an-identifier",
            ExternalRoutineId = null,
            ExternalTitle = null,
            StartedAt = DateTimeOffset.UnixEpoch,
            EndedAt = DateTimeOffset.UnixEpoch,
            ExternallyUpdatedAt = DateTimeOffset.UnixEpoch,
            Version = 1,
            Exercises =
            [
                new PerformedExercise
                {
                    Position = 0,
                    ExerciseId = null,
                    ExternalTemplateId = "a-template",
                    ExternalTitle = null,
                    Sets =
                    [
                        new PerformedSet
                        {
                            Position = 0,
                            Kind = SetKind.Working,
                            WeightKg = 50,
                            Reps = 11,
                            RepsInReserve = null,
                        },
                    ],
                },
            ],
        };

        Assert.Equal(SetKind.Working, workout.Exercises.Single().Sets.Single().Kind);
    }
}
