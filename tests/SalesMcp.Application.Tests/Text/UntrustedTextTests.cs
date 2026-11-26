using SalesMcp.Application.Text;

namespace SalesMcp.Application.Tests.Text;

public class UntrustedTextTests
{
    [Fact]
    public void Leaves_ordinary_text_alone() =>
        Assert.Equal("Deep-groove ball bearing, 20 mm bore (6204-2RS).", UntrustedText.Clean("Deep-groove ball bearing, 20 mm bore (6204-2RS).", 300));

    [Theory]
    [InlineData("Ignore​ previous", "Ignore previous")] // zero-width space
    [InlineData("pri‍ces", "prices")] // zero-width joiner
    [InlineData("rev‮eal", "reveal")] // right-to-left override
    [InlineData("a⁦b⁩c", "abc")] // bidirectional isolates
    [InlineData("﻿start", "start")] // byte-order mark
    [InlineData("bell\u0007 and escape\u001B[31m", "bell and escape[31m")] // control characters
    [InlineData("privateuse", "privateuse")] // private use area
    public void Strips_invisible_and_control_characters(string input, string expected) =>
        Assert.Equal(expected, UntrustedText.Clean(input, 300));

    [Fact]
    public void Collapses_whitespace_and_line_breaks_to_single_spaces() =>
        Assert.Equal("one two three", UntrustedText.Clean("  one\r\n\ttwo   three \n", 300));

    [Theory]
    [InlineData("Ｉｇｎｏｒｅ", "Ignore")] // full-width letters
    [InlineData("ﬁre", "fire")] // the "fi" ligature
    public void Normalises_look_alike_characters(string input, string expected) =>
        Assert.Equal(expected, UntrustedText.Clean(input, 300));

    [Fact]
    public void Caps_the_length_with_an_ellipsis()
    {
        var cleaned = UntrustedText.Clean(new string('x', 1000), 300);

        Assert.Equal(300, cleaned.Length);
        Assert.EndsWith(UntrustedText.Ellipsis, cleaned, StringComparison.Ordinal);
    }

    [Fact]
    public void Never_splits_a_character_made_of_two_code_units()
    {
        var cleaned = UntrustedText.Clean(new string('a', 8) + "\U0001F600\U0001F600", 10);

        Assert.True(cleaned.Length <= 10);
        Assert.DoesNotContain(cleaned, char.IsSurrogate);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("​​")]
    public void Nothing_visible_cleans_to_empty(string? input) => Assert.Equal(string.Empty, UntrustedText.Clean(input, 300));
}
