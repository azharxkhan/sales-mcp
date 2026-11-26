using System.Globalization;
using System.Text;

namespace SalesMcp.Application.Text;

/// <summary>
/// Cleans free text that came from the database before a tool returns it (guardrail D4). Such text,
/// like a product description, could have been written to steer the model, so it is returned as
/// plain, visible, bounded data.
/// </summary>
public static class UntrustedText
{
    /// <summary>The marker appended when text is cut short.</summary>
    public const string Ellipsis = "…";

    /// <summary>
    /// Normalises <paramref name="text"/> to Unicode NFKC, removes control, format (zero-width,
    /// bidirectional override) and private-use characters, collapses whitespace to single spaces,
    /// and caps the length at <paramref name="maxLength"/> characters, ending with <see cref="Ellipsis"/>
    /// when cut.
    /// </summary>
    public static string Clean(string? text, int maxLength)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxLength, 1);
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var normalized = text.Normalize(NormalizationForm.FormKC);
        var builder = new StringBuilder(normalized.Length);
        var pendingSpace = false;

        foreach (var rune in normalized.EnumerateRunes())
        {
            if (Rune.IsWhiteSpace(rune))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            var category = Rune.GetUnicodeCategory(rune);
            if (category is UnicodeCategory.Control or UnicodeCategory.Format or UnicodeCategory.PrivateUse
                or UnicodeCategory.Surrogate or UnicodeCategory.OtherNotAssigned)
            {
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(rune.ToString());
        }

        return Cap(builder.ToString(), maxLength);
    }

    private static string Cap(string text, int maxLength)
    {
        if (text.Length <= maxLength)
        {
            return text;
        }

        // Cut on a text-element boundary so a character made of several code units is never split.
        var cut = 0;
        var elements = StringInfo.GetTextElementEnumerator(text);
        while (elements.MoveNext())
        {
            var next = elements.ElementIndex + elements.GetTextElement().Length;
            if (next > maxLength - Ellipsis.Length)
            {
                break;
            }

            cut = next;
        }

        return text[..cut].TrimEnd() + Ellipsis;
    }
}
