using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Enums;
using FlowBoard.Infrastructure.Services;
using FlowBoard.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlowBoard.Tests;

public class NotificationServiceTests : IDisposable
{
    private readonly FlowBoard.Infrastructure.Data.FlowBoardDbContext _db;
    private readonly NotificationService _sut;

    public NotificationServiceTests()
    {
        _db = TestDbFactory.Create();
        _sut = new NotificationService(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task CreateNotification_PersistsToDb()
    {
        var id = Guid.NewGuid();
        await _sut.NotifyAsync("test-user-1", null, NotificationType.TaskAssigned, "Test Notification", "This is a test", null);

        var saved = await _db.Notifications.FirstOrDefaultAsync(n => n.UserId == "test-user-1");
        Assert.NotNull(saved);
        Assert.Equal("Test Notification", saved!.Title);
        Assert.Equal("This is a test", saved.Body);
    }

    [Fact]
    public async Task GetUnreadCount_ReturnsCorrectCount()
    {
        await _sut.NotifyAsync("test-user-1", null, NotificationType.TaskAssigned, "1", "1", null);
        await _sut.NotifyAsync("test-user-1", null, NotificationType.TaskAssigned, "2", "2", null);

        var count = await _sut.GetUnreadCountAsync("test-user-1");
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task MarkAsRead_UpdatesIsRead()
    {
        await _sut.NotifyAsync("test-user-1", null, NotificationType.TaskAssigned, "1", "1", null);
        var saved = await _db.Notifications.FirstAsync();
        
        await _sut.MarkReadAsync(saved.Id, "test-user-1");

        var updated = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == saved.Id);
        Assert.True(updated!.IsRead);
    }
}