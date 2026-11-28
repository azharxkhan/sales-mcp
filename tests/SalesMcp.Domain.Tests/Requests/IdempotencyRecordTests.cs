using SalesMcp.Domain.Common;
using SalesMcp.Domain.Requests;

namespace SalesMcp.Domain.Tests.Requests;

public class IdempotencyRecordTests
{
    private static readonly DateTimeOffset _created = new(2026, 11, 28, 9, 0, 0, TimeSpan.Zero);

    private static IdempotencyRecord NewRecord() =>
        IdempotencyRecord.Create("req-0001", "create_quote", CustomerId.New(), new string('a', 64), """{"quoteId":"x"}""", _created);

    [Fact]
    public void Answers_retries_for_24_hours()
    {
        var record = NewRecord();

        Assert.False(record.IsExpired(_created.AddHours(24).AddTicks(-1)));
        Assert.True(record.IsExpired(_created.AddHours(24)));
    }

    [Fact]
    public void Matches_only_its_own_request()
    {
        var record = NewRecord();

        Assert.True(record.Matches(new string('a', 64)));
        Assert.False(record.Matches(new string('b', 64)));
    }

    [Fact]
    public void Can_be_renewed_only_once_expired()
    {
        var record = NewRecord();

        Assert.Throws<InvalidOperationException>(() => record.Renew(new string('b', 64), "{}", _created.AddHours(1)));

        record.Renew(new string('b', 64), "{}", _created.AddDays(2));
        Assert.True(record.Matches(new string('b', 64)));
        Assert.Equal(_created.AddDays(2), record.CreatedAt);
    }
}
