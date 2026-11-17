using GleisGrain.Alerts.Contracts;
using GleisGrain.Alerts.Domain;

namespace GleisGrain.Alerts.Tests;

public class AlertIdsTests
{
    [Theory]
    [InlineData("11469_LL_ICSLAG")]
    [InlineData("a:b-c.d")]
    public void From_ValidNoticeId_IsKeptAsItIs(string noticeId) =>
        Assert.Equal(noticeId, AlertIds.From(noticeId).Value);

    [Theory]
    [InlineData("has spaces")]
    [InlineData("Ünïcode/slash")]
    [InlineData("")]
    [InlineData("   ")]
    public void From_NoticeIdOutsideTheRules_BecomesAValidDerivedId(string noticeId)
    {
        var id = AlertIds.From(noticeId);

        Assert.True(AlertId.TryParse(id.Value, null, out _));
        Assert.StartsWith("h-", id.Value, StringComparison.Ordinal);
    }

    [Fact]
    public void From_NoticeIdTooLong_BecomesAValidDerivedId()
    {
        var id = AlertIds.From(new string('x', AlertId.MaxLength + 1));

        Assert.True(AlertId.TryParse(id.Value, null, out _));
    }

    [Fact]
    public void From_SameNoticeId_GivesTheSameIdEveryTime() =>
        Assert.Equal(AlertIds.From("has spaces"), AlertIds.From("has spaces"));

    [Fact]
    public void From_DifferentNoticeIds_GiveDifferentIds() =>
        Assert.NotEqual(AlertIds.From("has spaces"), AlertIds.From("has  spaces"));
}
