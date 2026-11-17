using SalesMcp.Domain.Common;

namespace SalesMcp.Domain.Tests.Common;

public class IdTests
{
    [Fact]
    public void New_ids_are_unique_and_not_empty()
    {
        var a = CustomerId.New();
        var b = CustomerId.New();

        Assert.NotEqual(a, b);
        Assert.NotEqual(Guid.Empty, a.Value);
    }

    [Fact]
    public void New_ids_are_version_7_so_they_sort_by_creation_time() =>
        Assert.Equal(7, QuoteId.New().Value.Version);

    [Fact]
    public void Ids_with_the_same_guid_are_equal()
    {
        var guid = Guid.CreateVersion7();

        Assert.Equal(new OfferingId(guid), new OfferingId(guid));
    }

    [Fact]
    public void Formats_as_a_plain_guid()
    {
        var guid = Guid.CreateVersion7();

        Assert.Equal(guid.ToString("D"), new OrderId(guid).ToString());
    }
}
