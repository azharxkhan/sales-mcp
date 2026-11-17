using SalesMcp.Domain.Common;

namespace SalesMcp.Domain.Tests.Common;

public class ResultTests
{
    private static readonly DomainError _noPrice = new("NoPrice", "No price is set for this item.");

    [Fact]
    public void Success_carries_a_value()
    {
        var result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Failure_carries_an_error()
    {
        var result = Result.Failure<int>(_noPrice);

        Assert.True(result.IsFailure);
        Assert.Equal(_noPrice, result.Error);
    }

    [Fact]
    public void Reading_the_value_of_a_failure_is_a_bug() =>
        Assert.Throws<InvalidOperationException>(() => Result.Failure<int>(_noPrice).Value);

    [Fact]
    public void Reading_the_error_of_a_success_is_a_bug()
    {
        Assert.Throws<InvalidOperationException>(() => Result.Success(1).Error);
        Assert.Throws<InvalidOperationException>(() => Result.Success().Error);
    }

    [Fact]
    public void Non_generic_failure_from_code_and_message()
    {
        var result = Result.Failure("CreditHold", "The account is on hold.");

        Assert.True(result.IsFailure);
        Assert.Equal("CreditHold", result.Error.Code);
    }

    [Fact]
    public void Map_transforms_a_success_and_passes_a_failure_through()
    {
        Assert.Equal(4, Result.Success(2).Map(x => x * 2).Value);
        Assert.Equal(_noPrice, Result.Failure<int>(_noPrice).Map(x => x * 2).Error);
    }

    [Fact]
    public void Bind_chains_operations_and_stops_at_the_first_failure()
    {
        var calls = 0;
        Result<int> Next(int x)
        {
            calls++;
            return Result.Success(x + 1);
        }

        Assert.Equal(3, Result.Success(2).Bind(Next).Value);
        Assert.Equal(_noPrice, Result.Failure<int>(_noPrice).Bind(Next).Error);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void Match_handles_both_outcomes()
    {
        Assert.Equal("ok 1", Result.Success(1).Match(v => $"ok {v}", e => e.Code));
        Assert.Equal("NoPrice", Result.Failure<int>(_noPrice).Match(v => $"ok {v}", e => e.Code));
    }

    [Theory]
    [InlineData("", "message")]
    [InlineData("Code", " ")]
    public void Errors_need_a_code_and_a_message(string code, string message) =>
        Assert.ThrowsAny<ArgumentException>(() => new DomainError(code, message));
}
