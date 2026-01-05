using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Test.Databases.App;

namespace Test.WorkoutLogSets;

public class Test : BaseTest
{
    public Test() : base()
    {
    }

    [Fact]

    public async Task GET()
    {
        var dbContext = serviceProvider.GetRequiredService<JournalDbContext>();
        var id = Guid.NewGuid();
        var workoutLogSet = new Databases.App.Tables.WorkoutLogSet.Table()
        {
            Id = id,
            CreatedDate = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            WorkoutLogId = Guid.NewGuid(),
            Value = 1
        };
        dbContext.WorkoutLogSets.Add(workoutLogSet);
        await dbContext.SaveChangesAsync();
        var workoutLogSetsEndpoint = serviceProvider.GetRequiredService<Library.WorkoutLogSets.Interface>();
        var response = await workoutLogSetsEndpoint.GetAsync(new Library.WorkoutLogSets.GET.Parameters()
        {
            PageIndex = 0,
            PageSize = 10
        });
        Assert.NotNull(response);

        dbContext.WorkoutLogSets.Remove(workoutLogSet);
        await dbContext.SaveChangesAsync();
    }

    [Fact]

    public async Task POST()
    {
        var dbContext = serviceProvider.GetRequiredService<JournalDbContext>();
        var id = Guid.NewGuid();
        var workoutLogId = Guid.NewGuid();
        var value = 10;
        var workoutLog = new Databases.App.Tables.WorkoutLog.Table()
        {
            Id = workoutLogId,
            WorkoutId = Guid.NewGuid(),
            WorkoutDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };
        dbContext.WorkoutLogs.Add(workoutLog);
        await dbContext.SaveChangesAsync();

        var workoutLogSetsEndpoint = serviceProvider.GetRequiredService<Library.WorkoutLogSets.Interface>();
        var payLoad = new Library.WorkoutLogSets.POST.Payload()
        {
            WorkoutLogId = workoutLogId,
            Value = value
        };
        await workoutLogSetsEndpoint.PostAsync(payLoad);

        var expected = await dbContext.WorkoutLogSets.FirstOrDefaultAsync(x => x.WorkoutLogId == workoutLogId);
        Assert.NotNull(expected);
        Assert.Equal(expected.WorkoutLogId, workoutLogId);
        Assert.Equal(expected.Value, value);

        dbContext.WorkoutLogs.Remove(workoutLog);
        dbContext.WorkoutLogSets.Remove(expected);
        await dbContext.SaveChangesAsync();
    }

    [Fact]

    public async Task PUT()
    {
        var dbContext = serviceProvider.GetRequiredService<JournalDbContext>();
        var id = Guid.NewGuid();
        var updatedWorkoutLogId = Guid.NewGuid();
        var updatedValue = 10;
        var workoutLog = new Databases.App.Tables.WorkoutLog.Table()
        {
            Id = updatedWorkoutLogId,
            WorkoutId = Guid.NewGuid(),
            WorkoutDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };
        dbContext.WorkoutLogs.Add(workoutLog);

        var workoutLogSet = new Databases.App.Tables.WorkoutLogSet.Table()
        {
            Id = id,
            WorkoutLogId = Guid.NewGuid(),
            Value = 12,
            LastUpdated = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow,
        };
        dbContext.WorkoutLogSets.Add(workoutLogSet);
        await dbContext.SaveChangesAsync();

        var workoutLogSetsEndpoint = serviceProvider.GetRequiredService<Library.WorkoutLogSets.Interface>();
        var payload = new Library.WorkoutLogSets.PUT.Payload()
        {
            Id = id,
            WorkoutLogId = updatedWorkoutLogId,
            Value = updatedValue
        };
        await workoutLogSetsEndpoint.PutAsync(payload);

        await dbContext.Entry(workoutLogSet).ReloadAsync();

        var updatedWorkoutLogSet = workoutLogSet;

        Assert.NotNull(updatedWorkoutLogSet);
        Assert.Equal(updatedWorkoutLogSet.WorkoutLogId, updatedWorkoutLogId);
        Assert.Equal(updatedWorkoutLogSet.Value, updatedValue);

        dbContext.WorkoutLogSets.Remove(updatedWorkoutLogSet);
        dbContext.WorkoutLogs.Remove(workoutLog);
        await dbContext.SaveChangesAsync();

    }

    [Fact]

    public async Task Delete()
    {
        var id = Guid.NewGuid();
        var dbContext = serviceProvider.GetRequiredService<JournalDbContext>();
        var workoutLogSet = new Databases.App.Tables.WorkoutLogSet.Table()
        {
            Id = id,
            WorkoutLogId = Guid.NewGuid(),
            Value = 12,
            LastUpdated = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow,
        };
        dbContext.WorkoutLogSets.Add(workoutLogSet);
        await dbContext.SaveChangesAsync();

        var workoutLogSetsEndpoint = serviceProvider.GetRequiredService<Library.WorkoutLogSets.Interface>();
        var parameters = new Library.WorkoutLogSets.DELETE.Parameters() { Id = id };
        await workoutLogSetsEndpoint.DeleteAsync(parameters);

        await dbContext.Entry(workoutLogSet).ReloadAsync();
        var deletedWorkoutLogSet = await dbContext.WorkoutLogSets.FindAsync(id);
        Assert.Null(deletedWorkoutLogSet);
    }

}
