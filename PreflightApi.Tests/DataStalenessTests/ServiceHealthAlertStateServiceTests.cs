using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PreflightApi.Domain.Entities;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Services;
using Xunit;

namespace PreflightApi.Tests.DataStalenessTests;

public class ServiceHealthAlertStateServiceTests : IDisposable
{
    private readonly PreflightApiDbContext _dbContext;
    private readonly ServiceHealthAlertStateService _service;

    public ServiceHealthAlertStateServiceTests()
    {
        var options = new DbContextOptionsBuilder<PreflightApiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new PreflightApiDbContext(options);
        _service = new ServiceHealthAlertStateService(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region GetAllAsync

    [Fact]
    public async Task GetAll_Empty_ReturnsEmptyList()
    {
        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_WithData_ReturnsAllStates()
    {
        // Arrange
        _dbContext.ServiceHealthAlertStates.AddRange(
            new ServiceHealthAlertState { ServiceName = "database", LastKnownStatus = "Healthy", UpdatedAt = DateTime.UtcNow },
            new ServiceHealthAlertState { ServiceName = "blob-storage", LastKnownStatus = "Degraded", UpdatedAt = DateTime.UtcNow });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region UpsertStatusAsync

    [Fact]
    public async Task UpsertStatus_NewService_InsertsRecord()
    {
        // Act
        await _service.UpsertStatusAsync("database", "Healthy");

        // Assert
        var state = await _dbContext.ServiceHealthAlertStates.FindAsync("database");
        state.Should().NotBeNull();
        state!.LastKnownStatus.Should().Be("Healthy");
        state.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpsertStatus_ExistingService_UpdatesStatus()
    {
        // Arrange
        _dbContext.ServiceHealthAlertStates.Add(new ServiceHealthAlertState
        {
            ServiceName = "database",
            LastKnownStatus = "Healthy",
            UpdatedAt = DateTime.UtcNow.AddMinutes(-10)
        });
        await _dbContext.SaveChangesAsync();

        // Act
        await _service.UpsertStatusAsync("database", "Degraded");

        // Assert
        var state = await _dbContext.ServiceHealthAlertStates.FindAsync("database");
        state!.LastKnownStatus.Should().Be("Degraded");
        state.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region UpdateAlertStateAsync

    [Fact]
    public async Task UpdateAlertState_ExistingService_SetsAlertFields()
    {
        // Arrange
        _dbContext.ServiceHealthAlertStates.Add(new ServiceHealthAlertState
        {
            ServiceName = "database",
            LastKnownStatus = "Unhealthy",
            UpdatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        // Act
        await _service.UpdateAlertStateAsync("database", "unhealthy");

        // Assert
        var state = await _dbContext.ServiceHealthAlertStates.FindAsync("database");
        state!.LastAlertSentUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        state.LastAlertSeverity.Should().Be("unhealthy");
    }

    [Fact]
    public async Task UpdateAlertState_NonExistentService_DoesNothing()
    {
        // Act — should not throw
        await _service.UpdateAlertStateAsync("nonexistent", "unhealthy");

        // Assert
        var count = await _dbContext.ServiceHealthAlertStates.CountAsync();
        count.Should().Be(0);
    }

    #endregion

    #region ClearAlertStateAsync

    [Fact]
    public async Task ClearAlertState_ExistingService_ClearsSeverityAndPreservesTimestamp()
    {
        // Arrange
        var alertSentTime = DateTime.UtcNow.AddHours(-1);
        _dbContext.ServiceHealthAlertStates.Add(new ServiceHealthAlertState
        {
            ServiceName = "database",
            LastKnownStatus = "Healthy",
            LastAlertSentUtc = alertSentTime,
            LastAlertSeverity = "unhealthy",
            ConsecutiveFailureCount = 5,
            UpdatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        // Act
        await _service.ClearAlertStateAsync("database");

        // Assert
        var state = await _dbContext.ServiceHealthAlertStates.FindAsync("database");
        state!.LastAlertSentUtc.Should().Be(alertSentTime); // preserved for quiet period enforcement
        state.LastAlertSeverity.Should().BeNull();
        state.ConsecutiveFailureCount.Should().Be(0);
    }

    [Fact]
    public async Task ClearAlertState_NonExistentService_DoesNothing()
    {
        // Act — should not throw
        await _service.ClearAlertStateAsync("nonexistent");

        // Assert
        var count = await _dbContext.ServiceHealthAlertStates.CountAsync();
        count.Should().Be(0);
    }

    #endregion
}
