using System.Text.Json;
using Protocol.Api.Hevy;

namespace Protocol.Api.Tests.Unit.Hevy;

/// <summary>
/// Hevy's write responses, read from bodies the live service actually returned.
/// <para>
/// **This file exists because their OpenAPI document is wrong about them.** It declares
/// <c>POST /v1/routine_folders</c> as answering a bare <c>RoutineFolder</c> and
/// <c>POST /v1/routines</c> as answering a bare <c>Routine</c>. The service answers an envelope
/// for the first and an envelope holding an **array** for the second. Deserialising the declared
/// shape produced a folder identifier of zero, which was stored without complaint and then made
/// Hevy refuse every routine sent into it — a 400 whose reason we were also discarding.
/// </para>
/// <para>
/// The payloads below are captured verbatim from real calls. A contract read rather than assumed
/// was the right instinct and was not enough: only the live call showed the difference, so the
/// live shape is what this pins.
/// </para>
/// </summary>
public class HevyResponseShapeTests
{
    /// <summary>Captured from <c>POST /v1/routine_folders</c>.</summary>
    private const string FolderBody = """
        {"routine_folder":{"id":3492621,"index":0,"title":"protocol-diagnostic",
        "updated_at":"2026-08-24T00:01:12.602Z","created_at":"2026-08-24T00:01:12.602Z"}}
        """;

    /// <summary>Captured from <c>POST /v1/routines</c>. Note the array.</summary>
    private const string RoutineBody = """
        {"routine":[{"id":"7b4abdd7-bac4-44b2-bf32-c53db7f1e7f9","title":"FullBody",
        "folder_id":3492621,"updated_at":"2026-08-24T00:01:13.478Z",
        "created_at":"2026-08-24T00:01:13.478Z","exercises":[]}]}
        """;

    [Fact]
    public void A_created_folder_yields_its_real_identifier()
    {
        var envelope = JsonSerializer.Deserialize<HevyFolderEnvelope>(FolderBody);

        Assert.Equal(3492621, envelope?.RoutineFolder?.Id);
    }

    [Fact]
    public void A_folder_identifier_is_never_zero_when_the_body_is_read_correctly()
    {
        // The exact failure, stated as an assertion: zero is what the declared shape produced,
        // and zero is not a folder Hevy has.
        var envelope = JsonSerializer.Deserialize<HevyFolderEnvelope>(FolderBody);

        Assert.NotNull(envelope?.RoutineFolder);
        Assert.True(envelope.RoutineFolder.Id > 0);
    }

    [Fact]
    public void A_created_routine_yields_its_identifier_out_of_the_array()
    {
        var envelope = JsonSerializer.Deserialize<HevyRoutineEnvelope>(RoutineBody);

        Assert.Equal("7b4abdd7-bac4-44b2-bf32-c53db7f1e7f9", envelope?.Routine?.Single().Id);
    }

    [Fact]
    public void The_shape_their_document_declares_would_have_lost_both_identifiers()
    {
        // Kept as a test rather than as a comment, because it is the whole lesson. Reading the
        // published contract was right; trusting it without one live call was not.
        Assert.Equal(0, JsonSerializer.Deserialize<HevyRoutineFolder>(FolderBody)?.Id);
        Assert.Null(JsonSerializer.Deserialize<HevyRoutine>(RoutineBody)?.Id);
    }
}
