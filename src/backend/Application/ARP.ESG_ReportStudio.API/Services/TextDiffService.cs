using System.Text;

namespace ARP.ESG_ReportStudio.API.Services;

/// <summary>
/// Service for computing text differences at word and sentence level.
/// Provides readable, stable diffs for narrative disclosure comparisons.
/// </summary>
public sealed class TextDiffService
{
    /// <summary>
    /// Computes a word-level diff between two text strings.
    /// Returns a list of segments with change indicators.
    /// </summary>
    public List<TextSegment> ComputeWordLevelDiff(string? oldText, string? newText)
    {
        oldText ??= string.Empty;
        newText ??= string.Empty;

        var oldWords = SplitIntoWords(oldText);
        var newWords = SplitIntoWords(newText);

        var diff = ComputeLCS(oldWords, newWords);
        return BuildSegments(diff);
    }

    /// <summary>
    /// Computes a sentence-level diff between two text strings.
    /// Returns a list of segments with change indicators.
    /// </summary>
    public List<TextSegment> ComputeSentenceLevelDiff(string? oldText, string? newText)
    {
        oldText ??= string.Empty;
        newText ??= string.Empty;

        var oldSentences = SplitIntoSentences(oldText);
        var newSentences = SplitIntoSentences(newText);

        var diff = ComputeLCS(oldSentences, newSentences);
        return BuildSegments(diff);
    }

    /// <summary>
    /// Generates a human-readable summary of changes between two texts.
    /// </summary>
    public DiffSummary GenerateSummary(string? oldText, string? newText)
    {
        oldText ??= string.Empty;
        newText ??= string.Empty;

        var segments = ComputeWordLevelDiff(oldText, newText);
        
        return new DiffSummary
        {
            TotalSegments = segments.Count,
            AddedSegments = segments.Count(s => s.ChangeType == "added"),
            RemovedSegments = segments.Count(s => s.ChangeType == "removed"),
            ModifiedSegments = segments.Count(s => s.ChangeType == "modified"),
            UnchangedSegments = segments.Count(s => s.ChangeType == "unchanged"),
            OldTextLength = oldText.Length,
            NewTextLength = newText.Length,
            HasChanges = segments.Any(s => s.ChangeType != "unchanged")
        };
    }

    /// <summary>
    /// Splits text into words while preserving whitespace and punctuation.
    /// </summary>
    private List<string> SplitIntoWords(string text)
    {
        if (string.IsNullOrEmpty(text))
            return new List<string>();

        var words = new List<string>();
        var currentWord = new StringBuilder();

        foreach (var ch in text)
        {
            if (char.IsWhiteSpace(ch) || char.IsPunctuation(ch))
            {
                if (currentWord.Length > 0)
                {
                    words.Add(currentWord.ToString());
                    currentWord.Clear();
                }
                words.Add(ch.ToString());
            }
            else
            {
                currentWord.Append(ch);
            }
        }

        if (currentWord.Length > 0)
            words.Add(currentWord.ToString());

        return words;
    }

    /// <summary>
    /// Splits text into sentences based on common sentence delimiters.
    /// </summary>
    private List<string> SplitIntoSentences(string text)
    {
        if (string.IsNullOrEmpty(text))
            return new List<string>();

        var sentences = new List<string>();
        var currentSentence = new StringBuilder();

        for (int i = 0; i < text.Length; i++)
        {
            currentSentence.Append(text[i]);

            // Check for sentence ending punctuation
            if ((text[i] == '.' || text[i] == '!' || text[i] == '?') &&
                (i + 1 >= text.Length || char.IsWhiteSpace(text[i + 1])))
            {
                sentences.Add(currentSentence.ToString().Trim());
                currentSentence.Clear();
            }
        }

        if (currentSentence.Length > 0)
            sentences.Add(currentSentence.ToString().Trim());

        return sentences;
    }

    /// <summary>
    /// Computes the Longest Common Subsequence (LCS) diff between two sequences.
    /// </summary>
    private List<DiffOperation> ComputeLCS(List<string> oldSeq, List<string> newSeq)
    {
        int m = oldSeq.Count;
        int n = newSeq.Count;
        
        // DP table for LCS length
        var dp = new int[m + 1, n + 1];
        
        // Build LCS table
        for (int i = 1; i <= m; i++)
        {
            for (int j = 1; j <= n; j++)
            {
                if (oldSeq[i - 1] == newSeq[j - 1])
                {
                    dp[i, j] = dp[i - 1, j - 1] + 1;
                }
                else
                {
                    dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]);
                }
            }
        }
        
        // Backtrack to build diff operations
        var operations = new List<DiffOperation>();
        int x = m, y = n;
        
        while (x > 0 || y > 0)
        {
            if (x > 0 && y > 0 && oldSeq[x - 1] == newSeq[y - 1])
            {
                operations.Add(new DiffOperation 
                { 
                    Type = "equal", 
                    Text = oldSeq[x - 1] 
                });
                x--;
                y--;
            }
            else if (y > 0 && (x == 0 || dp[x, y - 1] >= dp[x - 1, y]))
            {
                operations.Add(new DiffOperation 
                { 
                    Type = "insert", 
                    Text = newSeq[y - 1] 
                });
                y--;
            }
            else if (x > 0)
            {
                operations.Add(new DiffOperation 
                { 
                    Type = "delete", 
                    Text = oldSeq[x - 1] 
                });
                x--;
            }
        }
        
        operations.Reverse();
        return operations;
    }

    /// <summary>
    /// Builds text segments from diff operations, grouping consecutive operations.
    /// </summary>
    private List<TextSegment> BuildSegments(List<DiffOperation> operations)
    {
        var segments = new List<TextSegment>();
        var currentSegment = new StringBuilder();
        string? currentType = null;

        foreach (var op in operations)
        {
            var changeType = op.Type switch
            {
                "equal" => "unchanged",
                "insert" => "added",
                "delete" => "removed",
                _ => "unchanged"
            };

            // If type changes or we have both old and new text, finalize current segment
            if (currentType != null && currentType != changeType)
            {
                if (currentSegment.Length > 0)
                {
                    segments.Add(new TextSegment
                    {
                        Text = currentSegment.ToString(),
                        ChangeType = currentType
                    });
                    currentSegment.Clear();
                }
            }

            currentSegment.Append(op.Text);
            currentType = changeType;
        }

        // Add final segment
        if (currentSegment.Length > 0 && currentType != null)
        {
            segments.Add(new TextSegment
            {
                Text = currentSegment.ToString(),
                ChangeType = currentType
            });
        }

        return segments;
    }
}

/// <summary>
/// Represents a segment of text with its change type.
/// </summary>
public sealed class TextSegment
{
    public string Text { get; set; } = string.Empty;
    public string ChangeType { get; set; } = "unchanged"; // "unchanged", "added", "removed", "modified"
}

/// <summary>
/// Internal representation of a diff operation.
/// </summary>
internal sealed class DiffOperation
{
    public string Type { get; set; } = string.Empty; // "equal", "insert", "delete"
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// Summary of changes between two texts.
/// </summary>
public sealed class DiffSummary
{
    public int TotalSegments { get; set; }
    public int AddedSegments { get; set; }
    public int RemovedSegments { get; set; }
    public int ModifiedSegments { get; set; }
    public int UnchangedSegments { get; set; }
    public int OldTextLength { get; set; }
    public int NewTextLength { get; set; }
    public bool HasChanges { get; set; }
}
