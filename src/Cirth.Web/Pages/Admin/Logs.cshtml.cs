using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.RegularExpressions;

namespace Cirth.Web.Pages.Admin;

[Authorize(Policy = "Admin")]
public sealed class LogsModel(IHostEnvironment env) : PageModel
{
    [BindProperty(SupportsGet = true)] public string Source { get; set; } = "web";
    [BindProperty(SupportsGet = true)] public string? Level { get; set; }
    [BindProperty(SupportsGet = true, Name = "q")] public string? Query { get; set; }
    [BindProperty(SupportsGet = true)] public int Lines { get; set; } = 300;

    public string? LogFile { get; private set; }
    public string? ExpectedPath { get; private set; }
    public string FileSize { get; private set; } = "—";
    public IReadOnlyList<LogEntry> Entries { get; private set; } = [];

    public void OnGet() => Load();

    public IActionResult OnGetTail()
    {
        Load();
        return Partial("_LogView", this);
    }

    private void Load()
    {
        var sourceDir = Source == "worker"
            ? Path.Combine(env.ContentRootPath, "..", "Cirth.Worker", "logs")
            : Path.Combine(env.ContentRootPath, "logs");
        var pattern = Source == "worker" ? "cirth-worker-*.log" : "cirth-web-*.log";
        ExpectedPath = Path.GetFullPath(Path.Combine(sourceDir, pattern));

        if (!Directory.Exists(sourceDir))
        {
            Entries = [];
            return;
        }

        var files = new DirectoryInfo(sourceDir).GetFiles(pattern)
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .ToList();
        if (files.Count == 0)
        {
            Entries = [];
            return;
        }

        var latest = files[0];
        LogFile = latest.FullName;
        FileSize = FormatBytes(latest.Length);

        // Read tail-N lines safely (open with FileShare so a live writer doesn't block).
        Lines = Math.Clamp(Lines, 50, 2000);
        var raw = ReadTail(latest, Lines * 2); // 2x to give the filter room

        var minLevel = Level?.ToUpperInvariant();
        var query = Query?.Trim();
        var levelRx = new Regex(@"\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2} (?<lvl>[A-Z]{3})\]", RegexOptions.Compiled);

        var entries = new List<LogEntry>();
        foreach (var line in raw)
        {
            var match = levelRx.Match(line);
            var lvl = match.Success ? match.Groups["lvl"].Value : "INF";
            if (minLevel is not null && !PassesLevel(lvl, minLevel)) continue;
            if (!string.IsNullOrEmpty(query) && line.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0) continue;
            entries.Add(new LogEntry(lvl, line));
            if (entries.Count >= Lines) break;
        }
        Entries = entries;
    }

    private static List<string> ReadTail(FileInfo file, int approxLines)
    {
        // Read entire file (rolling daily files are bounded by a day's traffic — fine for dev/V1).
        // For production scale, switch to seek-based tail or a structured log store.
        using var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream);
        var all = new List<string>();
        string? line;
        while ((line = reader.ReadLine()) is not null) all.Add(line);
        if (all.Count <= approxLines) { all.Reverse(); return all; }
        all = all.Skip(all.Count - approxLines).ToList();
        all.Reverse();
        return all;
    }

    private static bool PassesLevel(string entry, string min)
    {
        int rank(string l) => l switch
        {
            "VRB" => 0, "DBG" => 1, "INF" => 2, "WRN" => 3, "ERR" => 4, "FTL" => 5, _ => 2
        };
        return rank(entry) >= rank(min);
    }

    private static string FormatBytes(long b) => b switch
    {
        >= 1L * 1024 * 1024 * 1024 => $"{b / (1024.0 * 1024 * 1024):F1} GB",
        >= 1L * 1024 * 1024 => $"{b / (1024.0 * 1024):F1} MB",
        >= 1024L => $"{b / 1024.0:F1} KB",
        _ => $"{b} B"
    };

    public sealed record LogEntry(string Level, string Raw);
}
