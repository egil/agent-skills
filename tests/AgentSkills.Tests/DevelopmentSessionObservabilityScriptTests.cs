using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace AgentSkills.Tests;

/// <summary>Contract tests for the narrow, privacy-safe Codex transcript adapter.</summary>
public sealed class DevelopmentSessionObservabilityScriptTests
{
    /// <summary>Verifies the file-free canonical marker boundary.</summary>
    [Fact]
    public async Task Emitter_writes_exactly_one_canonical_stdout_marker_without_files()
    {
        using var w = new Workspace();
        var r = await w.Run("emit-marker.ps1", "-RunId run -WorkItem item -Role implementor -Phase implementation -WorkCycle");
        Assert.True(r.Code == 0, r.Error); Assert.StartsWith("CODEX_DELIVERY_MARKER:{", r.Out, StringComparison.Ordinal);
        using var marker = JsonDocument.Parse(r.Out.Trim()["CODEX_DELIVERY_MARKER:".Length..]);
        Assert.Equal("run", marker.RootElement.GetProperty("runId").GetString());
        Assert.Empty(Directory.EnumerateFiles(w.Path));
    }

    /// <summary>Verifies owning metadata, reset-safe usage, and privacy allowlisting.</summary>
    [Fact]
    public async Task Adapter_attributes_only_own_meta_reconciles_reset_tokens_and_never_leaks_private_content()
    {
        using var w = new Workspace(); var id = "11111111-1111-1111-1111-111111111111";
        w.Write(id, new[] {
            "{\"type\":\"session_meta\",\"payload\":{\"id\":\"copied-parent\",\"cwd\":\"/private\"}}",
            $"{{\"type\":\"session_meta\",\"payload\":{{\"id\":\"{id}\",\"parent_id\":\"parent\",\"cwd\":\"/safe\",\"cli_version\":\"1\"}}}}",
            Turn("2026-01-01T00:00:00Z", "completed", 1000, 10, "CODEX_DELIVERY_MARKER:{\\\"schemaVersion\\\":1,\\\"runId\\\":\\\"run|segment\\\",\\\"workItem\\\":\\\"item\\\",\\\"role\\\":\\\"implementor\\\",\\\"workCycle\\\":true}"),
            "{\"type\":\"turn_interrupted\",\"timestamp\":\"2026-01-01T00:00:02Z\",\"payload\":{\"status\":\"interrupted\",\"duration_ms\":2000,\"input_tokens\":15,\"usage\":{\"output_tokens\":4}}}", Turn("2026-01-01T00:00:05Z", "completed", 500, 3, "CODEX_DELIVERY_MARKER:{\\\"schemaVersion\\\":1,\\\"outcome\\\":\\\"succeeded\\\"}"),
            "{\"type\":\"rate_limits\",\"payload\":{\"credits\":{\"balance\":999},\"prompt\":\"PRIVATE_SENTINEL CODEX_DELIVERY_MARKER:{\\\"schemaVersion\\\":1,\\\"runId\\\":\\\"untrusted\\\",\\\"workItem\\\":\\\"untrusted\\\",\\\"role\\\":\\\"reviewer\\\"}\"}}",
            "{\"type\":\"future_native_event\",\"payload\":{\"usage\":{\"output_tokens\":999}}}" });
        var r = await w.Summarize(); Assert.True(r.Code == 0, r.Error);
        using var output = JsonDocument.Parse(File.ReadAllText(w.Output)); var s = Assert.Single(output.RootElement.GetProperty("sessions").EnumerateArray());
        Assert.Equal("/safe", s.GetProperty("cwd").GetString()); Assert.Equal("run|segment", s.GetProperty("runId").GetString()); Assert.Equal("item", s.GetProperty("workItem").GetString()); Assert.Equal(18, s.GetProperty("inputTokens").GetInt64()); Assert.Equal(4, s.GetProperty("outputTokens").GetInt64());
        Assert.Equal(0, s.GetProperty("completedTurns").GetInt64()); Assert.Equal(1, s.GetProperty("incompleteTurns").GetInt64());
        Assert.Equal("partial", s.GetProperty("sourceCoverage").GetString()); Assert.Equal("partial", s.GetProperty("schemaCoverage").GetString()); Assert.Equal("unavailable", s.GetProperty("creditCoverage").GetString()); Assert.DoesNotContain("PRIVATE_SENTINEL", File.ReadAllText(w.Output), StringComparison.Ordinal);
    }

    /// <summary>Verifies legacy output-array handling and unsafe marker-context rejection.</summary>
    [Fact]
    public async Task Adapter_handles_array_text_markers_and_rejects_conflicting_context_with_partial_coverage()
    {
        using var w = new Workspace(); var id = "22222222-2222-2222-2222-222222222222";
        w.Write(id, new[] { $"{{\"type\":\"session_meta\",\"payload\":{{\"id\":\"{id}\"}}}}",
            "{\"type\":\"tool_output\",\"payload\":{\"output\":[{\"text\":\"CODEX_DELIVERY_MARKER:{\\\"schemaVersion\\\":1,\\\"runId\\\":\\\"a\\\",\\\"workItem\\\":\\\"x\\\",\\\"role\\\":\\\"tester\\\"}\"}]}}",
            "{\"type\":\"tool_completed\",\"payload\":{\"output\":\"CODEX_DELIVERY_MARKER:{\\\"schemaVersion\\\":1,\\\"runId\\\":\\\"b\\\",\\\"workItem\\\":\\\"x\\\",\\\"role\\\":\\\"tester\\\"}\"}}", "malformed" });
        var r = await w.Summarize(); Assert.True(r.Code == 0, r.Error); using var output = JsonDocument.Parse(File.ReadAllText(w.Output)); var s = Assert.Single(output.RootElement.GetProperty("sessions").EnumerateArray());
        Assert.Equal("rejected", s.GetProperty("markerCoverage").GetString()); Assert.Equal("partial", s.GetProperty("sourceCoverage").GetString());
    }

    /// <summary>Verifies marker-shaped tool inputs and generic text never establish attribution.</summary>
    [Fact]
    public async Task Adapter_ignores_markers_outside_structurally_known_tool_outputs()
    {
        using var w = new Workspace(); const string id = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
        const string marker = "CODEX_DELIVERY_MARKER:{\"schemaVersion\":1,\"runId\":\"untrusted\",\"workItem\":\"untrusted\",\"role\":\"reviewer\"}";
        w.Write(id, new[] {
            JsonSerializer.Serialize(new { type = "session_meta", payload = new { id } }),
            JsonSerializer.Serialize(new { type = "tool", payload = new { text = marker, content = new[] { new { text = marker } }, arguments = marker } }),
            JsonSerializer.Serialize(new { type = "response_item", payload = new { type = "custom_tool_call", arguments = marker } }),
            JsonSerializer.Serialize(new { type = "response_item", payload = new { type = "function_call", arguments = marker } }),
            JsonSerializer.Serialize(new { type = "response_item", payload = new { type = "custom_tool_call_output", text = marker, content = new[] { new { text = marker } }, output = new[] { new { input_text = marker } } } }) });

        var result = await w.Summarize(); Assert.True(result.Code == 0, result.Error);
        using var document = JsonDocument.Parse(File.ReadAllText(w.Output)); var session = Assert.Single(document.RootElement.GetProperty("sessions").EnumerateArray());
        Assert.Equal("unavailable", session.GetProperty("markerCoverage").GetString()); Assert.Equal(JsonValueKind.Null, session.GetProperty("runId").ValueKind); Assert.Equal(JsonValueKind.Null, session.GetProperty("workItem").ValueKind); Assert.Equal(JsonValueKind.Null, session.GetProperty("role").ValueKind); Assert.Empty(document.RootElement.GetProperty("workItems").EnumerateArray());
    }

    /// <summary>Verifies later markers cannot retroactively establish required session context.</summary>
    [Fact]
    public async Task Adapter_rejects_markers_when_the_first_marker_omits_required_context()
    {
        using var w = new Workspace(); var id = "55555555-5555-5555-5555-555555555555";
        w.Write(id, new[] { $"{{\"type\":\"session_meta\",\"payload\":{{\"id\":\"{id}\"}}}}",
            Turn("2026-01-01T00:00:00Z", "completed", 1, 1, "CODEX_DELIVERY_MARKER:{\\\"schemaVersion\\\":1,\\\"phase\\\":\\\"verification\\\"}"),
            Turn("2026-01-01T00:00:01Z", "completed", 1, 1, "CODEX_DELIVERY_MARKER:{\\\"schemaVersion\\\":1,\\\"runId\\\":\\\"run\\\",\\\"workItem\\\":\\\"item\\\",\\\"role\\\":\\\"tester\\\"}") });
        var r = await w.Summarize(); Assert.True(r.Code == 0, r.Error); using var output = JsonDocument.Parse(File.ReadAllText(w.Output)); var s = Assert.Single(output.RootElement.GetProperty("sessions").EnumerateArray());
        Assert.Equal("rejected", s.GetProperty("markerCoverage").GetString());
    }

    /// <summary>Verifies malformed native numeric values are omitted and downgrade coverage.</summary>
    [Fact]
    public async Task Adapter_marks_malformed_native_numeric_telemetry_partial()
    {
        using var w = new Workspace();
        const string id = "66666666-6666-6666-6666-666666666666";
        w.Write(id, new[] { $"{{\"type\":\"session_meta\",\"payload\":{{\"id\":\"{id}\"}}}}", "{\"type\":\"turn_completed\",\"timestamp\":\"2026-01-01T00:00:00Z\",\"payload\":{\"status\":\"completed\",\"duration_ms\":\"-1\",\"input_tokens\":1.5}}" });
        var result = await w.Summarize();
        Assert.True(result.Code == 0, result.Error);
        using var output = JsonDocument.Parse(File.ReadAllText(w.Output));
        var session = Assert.Single(output.RootElement.GetProperty("sessions").EnumerateArray());
        Assert.Equal("partial", session.GetProperty("sourceCoverage").GetString());
        Assert.Equal("partial", session.GetProperty("schemaCoverage").GetString());
        Assert.Equal(JsonValueKind.Null, session.GetProperty("inputTokens").ValueKind);
        Assert.Equal(JsonValueKind.Null, session.GetProperty("activeTurnSeconds").ValueKind);
    }

    /// <summary>Verifies the sanitized Codex 0.152 nested event topology.</summary>
    [Fact]
    public async Task Adapter_reads_codex_0152_nested_session_and_runtime_metadata()
    {
        using var w = new Workspace(); const string id = "77777777-7777-7777-7777-777777777777";
        w.Write(id, new[] { "{\"type\":\"session_meta\",\"payload\":{\"id\":\"copied-parent\",\"source\":{\"subagent\":{\"thread_spawn\":{\"parent_thread_id\":\"wrong-parent\",\"agent_path\":\"/private/copied\"}}}}}", "{\"type\":\"session_meta\",\"payload\":{\"id\":\"" + id + "\",\"cli_version\":\"0.152.1\",\"source\":{\"subagent\":{\"thread_spawn\":{\"parent_thread_id\":\"parent\",\"agent_path\":\"/root/tester\"}}}}}", "{\"type\":\"event_msg\",\"payload\":{\"type\":\"task_started\"}}", "{\"type\":\"turn_context\",\"payload\":{\"model\":\"model\",\"effort\":\"medium\"}}", "{\"type\":\"event_msg\",\"payload\":{\"type\":\"task_complete\",\"duration_ms\":1200,\"time_to_first_token_ms\":45}}", "{\"type\":\"response_item\",\"payload\":{\"type\":\"custom_tool_call\",\"status\":\"completed\",\"arguments\":\"PRIVATE_0152_SENTINEL\"}}", "{\"type\":\"response_item\",\"payload\":{\"type\":\"function_call\",\"status\":\"completed\"}}", "{\"type\":\"response_item\",\"payload\":{\"type\":\"custom_tool_call_output\",\"output\":[{\"text\":\"CODEX_DELIVERY_MARKER:{\\\"schemaVersion\\\":1,\\\"runId\\\":\\\"run\\\",\\\"workItem\\\":\\\"item\\\",\\\"role\\\":\\\"tester\\\"}\"}]}}", "{\"type\":\"event_msg\",\"payload\":{\"type\":\"token_count\",\"info\":{\"total_token_usage\":{\"input_tokens\":10,\"output_tokens\":4,\"cached_input_tokens\":3,\"reasoning_output_tokens\":2,\"total_tokens\":19}}}}" });
        var result = await w.Summarize(); Assert.True(result.Code == 0, result.Error); using var output = JsonDocument.Parse(File.ReadAllText(w.Output)); var s = Assert.Single(output.RootElement.GetProperty("sessions").EnumerateArray());
        Assert.Equal("parent", s.GetProperty("parentSessionId").GetString()); Assert.False(s.TryGetProperty("agentPath", out _)); Assert.Equal("model", Assert.Single(s.GetProperty("models").EnumerateArray()).GetString()); Assert.Equal("medium", Assert.Single(s.GetProperty("efforts").EnumerateArray()).GetString()); Assert.Equal(1, s.GetProperty("completedTurns").GetInt64()); Assert.Equal(1.2, s.GetProperty("activeTurnSeconds").GetDouble()); Assert.Equal(45, s.GetProperty("timeToFirstTokenMs").GetInt64()); Assert.Equal(2, s.GetProperty("toolCount").GetInt64()); Assert.Equal("completed", Assert.Single(s.GetProperty("toolStatuses").EnumerateArray()).GetString()); Assert.Equal(10, s.GetProperty("inputTokens").GetInt64()); Assert.Equal(4, s.GetProperty("outputTokens").GetInt64()); Assert.Equal(3, s.GetProperty("cachedTokens").GetInt64()); Assert.Equal(2, s.GetProperty("reasoningTokens").GetInt64()); Assert.Equal(19, s.GetProperty("totalTokens").GetInt64()); Assert.Equal("run", s.GetProperty("runId").GetString()); Assert.Equal("item", s.GetProperty("workItem").GetString()); Assert.Equal("tester", s.GetProperty("role").GetString()); Assert.DoesNotContain("PRIVATE_0152_SENTINEL", File.ReadAllText(w.Output), StringComparison.Ordinal);
    }

    /// <summary>Verifies a complete marker line is isolated from neighboring tool output.</summary>
    [Fact]
    public async Task Adapter_reads_only_complete_marker_line_from_0152_tool_output()
    {
        using var w = new Workspace(); const string id = "88888888-8888-8888-8888-888888888888";
        const string marker = "CODEX_DELIVERY_MARKER:{\"schemaVersion\":1,\"runId\":\"live-smoke-run\",\"workItem\":\"live-smoke-item\",\"role\":\"tester\",\"phase\":\"verification\",\"result\":\"succeeded\"}";
        var output = JsonSerializer.Serialize(new { type = "response_item", payload = new { type = "custom_tool_call_output", output = new object[] { new { input_text = "header text" }, new { text = marker + "\nCODEX_THREAD_ID=PRIVATE_THREAD" } } } });
        w.Write(id, new[] { "{\"type\":\"session_meta\",\"payload\":{\"id\":\"" + id + "\"}}", output });

        var result = await w.Summarize(); Assert.True(result.Code == 0, result.Error);
        var rendered = File.ReadAllText(w.Output); using var document = JsonDocument.Parse(rendered); var session = Assert.Single(document.RootElement.GetProperty("sessions").EnumerateArray()); var item = Assert.Single(document.RootElement.GetProperty("workItems").EnumerateArray());
        Assert.Equal("live-smoke-run", session.GetProperty("runId").GetString()); Assert.Equal("live-smoke-item", session.GetProperty("workItem").GetString()); Assert.Equal("tester", session.GetProperty("role").GetString()); Assert.Equal("verification", Assert.Single(session.GetProperty("phases").EnumerateArray()).GetString()); Assert.Equal("succeeded", Assert.Single(session.GetProperty("results").EnumerateArray()).GetString()); Assert.Empty(session.GetProperty("outcomes").EnumerateArray()); Assert.Equal("succeeded", Assert.Single(item.GetProperty("results").EnumerateArray()).GetString()); Assert.Equal("live-smoke-item", item.GetProperty("workItem").GetString()); Assert.DoesNotContain("CODEX_THREAD_ID", rendered, StringComparison.Ordinal); Assert.DoesNotContain("header text", rendered, StringComparison.Ordinal);
    }

    /// <summary>Verifies phase results remain distinct from terminal outcomes.</summary>
    [Fact]
    public async Task Adapter_keeps_marker_result_distinct_from_terminal_outcome()
    {
        using var w = new Workspace(); const string id = "99999999-9999-9999-9999-999999999999";
        const string marker = "CODEX_DELIVERY_MARKER:{\"schemaVersion\":1,\"runId\":\"run\",\"workItem\":\"item\",\"role\":\"tester\",\"result\":\"not-run\",\"outcome\":\"cancelled\"}";
        w.Write(id, new[] { "{\"type\":\"session_meta\",\"payload\":{\"id\":\"" + id + "\"}}", JsonSerializer.Serialize(new { type = "response_item", payload = new { type = "function_call_output", output = marker } }) });
        var result = await w.Summarize(); Assert.True(result.Code == 0, result.Error); using var document = JsonDocument.Parse(File.ReadAllText(w.Output)); var session = Assert.Single(document.RootElement.GetProperty("sessions").EnumerateArray()); var item = Assert.Single(document.RootElement.GetProperty("workItems").EnumerateArray());
        Assert.Equal("not-run", Assert.Single(session.GetProperty("results").EnumerateArray()).GetString()); Assert.Equal("cancelled", Assert.Single(session.GetProperty("outcomes").EnumerateArray()).GetString()); Assert.Equal("not-run", Assert.Single(item.GetProperty("results").EnumerateArray()).GetString());
    }

    /// <summary>Verifies an explicitly requested nested output path is created.</summary>
    [Fact]
    public async Task Adapter_creates_missing_output_directory()
    {
        using var w = new Workspace(); const string id = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
        w.Write(id, new[] { $"{{\"type\":\"session_meta\",\"payload\":{{\"id\":\"{id}\"}}}}" });
        var output = System.IO.Path.Combine(w.Path, "artifacts", "observability", "summary.json");

        var result = await w.Run("summarize-codex-sessions.ps1", $"-SessionsPath {w.Path} -OutputJson {output}");

        Assert.True(result.Code == 0, result.Error);
        Assert.True(File.Exists(output));
        using var document = JsonDocument.Parse(File.ReadAllText(output));
        Assert.Equal(1, document.RootElement.GetProperty("schemaVersion").GetInt32());
    }

    /// <summary>Verifies parallel active time is not presented as wall time.</summary>
    [Fact]
    public async Task Work_item_keeps_parallel_active_time_distinct_from_wall_span()
    {
        using var w = new Workspace(); w.WriteSimple("33333333-3333-3333-3333-333333333333", "2026-01-01T00:00:00Z"); w.WriteSimple("44444444-4444-4444-4444-444444444444", "2026-01-01T00:00:01Z");
        var r = await w.Summarize(); Assert.True(r.Code == 0, r.Error); using var output = JsonDocument.Parse(File.ReadAllText(w.Output)); var item = Assert.Single(output.RootElement.GetProperty("workItems").EnumerateArray());
        Assert.Equal(11, item.GetProperty("wallSpanSeconds").GetDouble()); Assert.Equal(20, item.GetProperty("summedActiveTurnSeconds").GetDouble());
    }

    /// <summary>Builds a sanitized native event, placing marker text only in tool output.</summary>
    private static string Turn(string time, string status, int duration, int tokens, string? marker) =>
        $"{{\"type\":\"{(marker is null ? $"turn_{status}" : "tool_completed")}\",\"timestamp\":\"{time}\",\"payload\":{{\"status\":\"{status}\",\"duration_ms\":{duration},\"input_tokens\":{tokens}{(marker is null ? "" : $",\"output\":\"{marker}\"")}}}}}";

    /// <summary>Provides an isolated transcript directory and PowerShell process boundary.</summary>
    private sealed class Workspace : IDisposable
    {
        /// <summary>Initializes an isolated directory.</summary>
        public Workspace()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"observe-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
            Output = System.IO.Path.Combine(Path, "output.json");
        }

        /// <summary>Gets the fixture directory.</summary>
        public string Path { get; }

        /// <summary>Gets the summary output path.</summary>
        public string Output { get; }

        /// <summary>Writes a rollout fixture.</summary>
        public void Write(string id, IEnumerable<string> lines) => File.WriteAllLines(System.IO.Path.Combine(Path, $"rollout-{id}.jsonl"), lines);

        /// <summary>Writes one timed session with a tool-output marker.</summary>
        public void WriteSimple(string id, string time) => Write(id, new[] { $"{{\"type\":\"session_meta\",\"payload\":{{\"id\":\"{id}\"}}}}", Turn(time, "completed", 10000, 1, null), Turn(time, "completed", 0, 1, "CODEX_DELIVERY_MARKER:{\\\"schemaVersion\\\":1,\\\"runId\\\":\\\"r\\\",\\\"workItem\\\":\\\"i\\\",\\\"role\\\":\\\"implementor\\\"}"), "{\"type\":\"turn_completed\",\"timestamp\":\"2026-01-01T00:00:11Z\",\"payload\":{\"status\":\"completed\"}}" });

        /// <summary>Runs the transcript summarizer.</summary>
        public Task<Result> Summarize() => Run("summarize-codex-sessions.ps1", $"-SessionsPath {Path} -OutputJson {Output}");

        /// <summary>Runs a repository-owned PowerShell script.</summary>
        public async Task<Result> Run(string script, string args)
        {
            var file = System.IO.Path.Combine(Repo(), "skills/observability/development-session-observability/scripts", script);
            var start = new ProcessStartInfo("pwsh") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
            start.ArgumentList.Add("-NoProfile"); start.ArgumentList.Add("-File"); start.ArgumentList.Add(file);
            foreach (var argument in args.Split(' ', StringSplitOptions.RemoveEmptyEntries)) start.ArgumentList.Add(argument);
            using var process = Process.Start(start)!;
            var output = await process.StandardOutput.ReadToEndAsync(); var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync(); return new(process.ExitCode, output, error);
        }

        /// <summary>Deletes the isolated fixture directory.</summary>
        public void Dispose() => Directory.Delete(Path, true);

        /// <summary>Finds the repository root from the test output directory.</summary>
        private static string Repo()
        {
            for (var path = AppContext.BaseDirectory; path is not null; path = Directory.GetParent(path)?.FullName)
                if (File.Exists(System.IO.Path.Combine(path, "scripts/validate-delivery-package.ps1"))) return path;
            throw new DirectoryNotFoundException();
        }
    }

    /// <summary>Captures a script process result without transcript content.</summary>
    private sealed record Result(int Code, string Out, string Error);
}
