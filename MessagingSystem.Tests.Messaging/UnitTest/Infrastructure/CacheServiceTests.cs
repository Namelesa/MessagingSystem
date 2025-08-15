using System.Text;
using System.Text.Json;
using MessagingSystem.Services.Messaging.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.Infrastructure;

public class CacheServiceTests
{
    private readonly Mock<IDistributedCache> _mockCache;
    private readonly Mock<ILogger<CacheService>> _mockLogger;
    private readonly CacheService _cacheService;

    public CacheServiceTests()
    {
        _mockCache = new Mock<IDistributedCache>();
        _mockLogger = new Mock<ILogger<CacheService>>();
        
        _cacheService = new CacheService(_mockCache.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetAsync_KeyNotInCache_ReturnsDefaultAndLogsMiss()
    {
        // Arrange
        string key = "key1";
        _mockCache.Setup(c => c.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        // Act
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        Assert.Null(result);
        _mockLogger.VerifyLog(LogLevel.Information, $"Trying to GET from cache with key: {key}", Times.Once());
        _mockLogger.VerifyLog(LogLevel.Information, $"Cache MISS for key: {key}", Times.Once());
    }
    
    [Fact]
    public async Task GetAsync_KeyInCache_ReturnsDeserializedValueAndLogsHit()
    {
        // Arrange
        string key = "key2";
        var originalValue = "test string";
        string json = JsonSerializer.Serialize(originalValue);
        var encoded = Encoding.UTF8.GetBytes(json);

        _mockCache.Setup(c => c.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(encoded);

        // Act
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        Assert.Equal(originalValue, result);
        _mockLogger.VerifyLog(LogLevel.Information, $"Trying to GET from cache with key: {key}", Times.Once());
        _mockLogger.VerifyLog(LogLevel.Information, $"Cache HIT for key: {key}", Times.Once());
    }
    
    [Fact]
    public async Task SetAsync_SetsCacheWithCorrectParametersAndLogs()
    {
        // Arrange
        string key = "key3";
        var value = new { Name = "John", Age = 30 };
        string json = JsonSerializer.Serialize(value);
        byte[] encoded = Encoding.UTF8.GetBytes(json);
        TimeSpan ttl = TimeSpan.FromMinutes(5);

        _mockCache.Setup(c => c.SetAsync(
                key,
                It.Is<byte[]>(b => Encoding.UTF8.GetString(b) == json),
                It.Is<DistributedCacheEntryOptions>(opt => opt.AbsoluteExpirationRelativeToNow == ttl),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _cacheService.SetAsync(key, value, ttl);

        // Assert
        _mockLogger.VerifyLog(LogLevel.Information, $"SET cache key: {key} with TTL: {ttl}", Times.Once());
    }
    
    [Fact]
    public async Task SetAsync_SetsCacheWithoutTTLAndLogs()
    {
        // Arrange
        string key = "key4";
        var value = 42;
        string json = JsonSerializer.Serialize(value);
        Encoding.UTF8.GetBytes(json);

        _mockCache.Setup(c => c.SetAsync(
                key,
                It.Is<byte[]>(b => Encoding.UTF8.GetString(b) == json),
                It.Is<DistributedCacheEntryOptions>(opt => opt.AbsoluteExpirationRelativeToNow == null),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _cacheService.SetAsync(key, value);

        // Assert
        _mockCache.Verify(c => c.SetAsync(
            key,
            It.Is<byte[]>(b => Encoding.UTF8.GetString(b) == json),
            It.Is<DistributedCacheEntryOptions>(opt => opt.AbsoluteExpirationRelativeToNow == null),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockLogger.VerifyLog(LogLevel.Information, $"SET cache key: {key} with TTL: ", Times.Once());
    }

    [Fact]
    public async Task RemoveAsync_RemovesCacheKeyAndLogs()
    {
        // Arrange
        string key = "key5";

        _mockCache.Setup(c => c.RemoveAsync(key, default))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        await _cacheService.RemoveAsync(key);

        // Assert
        _mockCache.Verify(c => c.RemoveAsync(key, default), Times.Once);
        _mockLogger.VerifyLog(LogLevel.Information, $"REMOVED cache key: {key}", Times.Once());
    }
}

public static class MoqExtensions
{
    public static void VerifyLog(this Mock<ILogger<CacheService>> loggerMock, LogLevel level, string message, Times times)
    {
        loggerMock.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            times);
    }
}