using RateShield.Infrastructure.RateLimiting.Redis;
using StackExchange.Redis;

namespace Rateshield.Gateway.Tests.RateLimiting;

public sealed class RedisTokenBucketResultMapperTests
{
    [Fact]
    public void Map_WhenScriptReturnsExpectedValues_ReturnsTypedResult()
    {
        //arrange
        RedisResult result = RedisResult.Create(
            new RedisValue[] { 1, 42, 0L, 1783000000000L, 1782999999000L }
        );

        //act
        var mapped = RedisTokenBucketResultMapper.Map(result);

        //assert
        Assert.True(mapped.IsAllowed);
        Assert.Equal(42, mapped.RemainingTokens);
        Assert.Equal(TimeSpan.Zero, mapped.RetryAfter);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1783000000000L), mapped.ResetAt);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1782999999000L), mapped.EvaluatedAt);
    }

    [Fact]
    public void Map_WhenScriptReturnsRejectedResult_MapsRetryAfter()
    {
        //arrange
        RedisResult result = RedisResult.Create(
            new RedisValue[] { 0, 0, 1000L, 1783000001000L, 1783000000000L }
        );

        //act
        var mapped = RedisTokenBucketResultMapper.Map(result);

        //assert
        Assert.False(mapped.IsAllowed);
        Assert.Equal(0, mapped.RemainingTokens);
        Assert.Equal(TimeSpan.FromSeconds(1), mapped.RetryAfter);
    }

    [Fact]
    public void Map_WhenScriptReturnsUnexpectedLength_Throws()
    {
        //arrange
        RedisResult result = RedisResult.Create(new RedisValue[] { 1, 42 });

        //act
        var exception = Assert.Throws<InvalidOperationException>(() =>
            RedisTokenBucketResultMapper.Map(result)
        );

        //assert
        Assert.Contains("Redis token bucket script returned", exception.Message);
    }
}
