using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Aristokeides.Api.Services;

public class DiffFile
{
    public string Path { get; set; } = string.Empty;
    public List<DiffHunk> Hunks { get; set; } = new();
}

public class DiffHunk
{
    public string Header { get; set; } = string.Empty;
    public List<DiffLine> Lines { get; set; } = new();
}

public class DiffLine
{
    public int? OldLineNumber { get; set; }
    public int? NewLineNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public string LineType { get; set; } = string.Empty; // "Addition", "Deletion", "Context"
}

public static class DiffParser
{
    private static readonly Regex HunkHeaderRegex = new(@"^@@\s+-(\d+)(?:,\d+)?\s+\+(\d+)(?:,\d+)?\s+@@");

    public static List<DiffFile> Parse(string diffText)
    {
        var files = new List<DiffFile>();
        if (string.IsNullOrWhiteSpace(diffText)) return files;

        using var reader = new StringReader(diffText);
        string? line;
        DiffFile? currentFile = null;
        DiffHunk? currentHunk = null;

        int oldLine = 0;
        int newLine = 0;

        while ((line = reader.ReadLine()) != null)
        {
            if (line.StartsWith("--- a/"))
            {
                currentFile = new DiffFile { Path = line.Substring(6) };
                files.Add(currentFile);
                currentHunk = null;
                continue;
            }
            if (line.StartsWith("+++ b/"))
            {
                if (currentFile != null)
                {
                    currentFile.Path = line.Substring(6);
                }
                else
                {
                    currentFile = new DiffFile { Path = line.Substring(6) };
                    files.Add(currentFile);
                }
                currentHunk = null;
                continue;
            }

            var match = HunkHeaderRegex.Match(line);
            if (match.Success)
            {
                if (currentFile == null)
                {
                    currentFile = new DiffFile { Path = "unknown" };
                    files.Add(currentFile);
                }

                currentHunk = new DiffHunk { Header = line };
                currentFile.Hunks.Add(currentHunk);

                oldLine = int.Parse(match.Groups[1].Value);
                newLine = int.Parse(match.Groups[2].Value);
                continue;
            }

            if (currentHunk != null)
            {
                if (line.StartsWith("+"))
                {
                    currentHunk.Lines.Add(new DiffLine
                    {
                        LineType = "Addition",
                        OldLineNumber = null,
                        NewLineNumber = newLine++,
                        Content = line.Substring(1)
                    });
                }
                else if (line.StartsWith("-"))
                {
                    currentHunk.Lines.Add(new DiffLine
                    {
                        LineType = "Deletion",
                        OldLineNumber = oldLine++,
                        NewLineNumber = null,
                        Content = line.Substring(1)
                    });
                }
                else if (line.StartsWith("\\"))
                {
                    // No newline at end of file 등 메타 라인. 라인 번호 증가 없음
                    currentHunk.Lines.Add(new DiffLine
                    {
                        LineType = "Context",
                        OldLineNumber = null,
                        NewLineNumber = null,
                        Content = line
                    });
                }
                else
                {
                    // 공백으로 시작하거나 일반 줄
                    string content = line.Length > 0 ? (line.StartsWith(" ") ? line.Substring(1) : line) : string.Empty;
                    currentHunk.Lines.Add(new DiffLine
                    {
                        LineType = "Context",
                        OldLineNumber = oldLine++,
                        NewLineNumber = newLine++,
                        Content = content
                    });
                }
            }
        }

        return files;
    }
}
