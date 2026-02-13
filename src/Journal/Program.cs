using Journal.Authentication;
using Journal.Databases;
using Journal.Databases.Identity;
using Journal.Databases.Sql;
using Journal.Exercises;
using Journal.Files;
using Journal.Journeys;
using Journal.Wolverine;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddNewtonsoftJson();

var env = builder.Environment;

builder.Services.Configure<DbConfig>(
	builder.Configuration.GetSection("JournalDb"));
builder.Services.Configure<DbConfig>(
	builder.Configuration.GetSection("IdentityDb"));
builder.Services.Configure<OpenSearchConfig>(
	builder.Configuration.GetSection("OpenSearch"));
builder.Services.Configure<Journal.Databases.MongoDb.DbConfig>(
	builder.Configuration.GetSection("MongoDb"));

builder.Services.AddDatabases(builder.Configuration);

builder.Services.AddGrpc();
builder.Services.AddWolverines(builder.Configuration);
builder.Services.AddJourneys(builder.Configuration);
builder.Services.AddSignalR(x => x.EnableDetailedErrors = true);
builder.Services.AddAuthentication(builder.Configuration);
builder.Services.AddFile(builder.Configuration);
builder.Services.AddMemoryCache();

builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowLocalhost5173", policy =>
	{
		policy.WithOrigins("http://localhost:5173")
			  .AllowAnyHeader()
			  .AllowAnyMethod()
			  .AllowCredentials();
	});
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseCors("AllowLocalhost5173");
}

if (app.Environment.IsEnvironment("Docker"))
{
    app.UseCors("AllowLocalhost5173");
}

if (app.Environment.IsEnvironment("k8s"))
{
    app.UseCors("AllowLocalhost5173");
}


app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGrpcService<Journal.Notes.Create.Service>();
app.MapGrpcService<Journal.Notes.Search.Service>();

app.MapHub<Journal.Competitions.Hub>("competitions-hub");
app.MapHub<Journal.Exercises.Hub>("exercises-hub");
app.MapHub<Journal.Workouts.Hub>("workouts-hub");
app.MapHub<Journal.WorkoutLogs.Hub>("workout-logs-hub");
app.MapHub<Journal.WeekPlans.Hub>("week-plans-hub");
app.MapHub<Journal.MeetUps.Hub>("meet-ups-hub");
app.MapHub<Journal.WeekPlanSets.Hub>("week-plan-sets-hub");
app.MapHub<Journal.WorkoutLogSets.Hub>("workout-log-sets-hub");
app.MapHub<Journal.Muscles.Hub>("muscles-hub");
app.MapHub<Journal.ExerciseMuscles.Hub>("exercise-muscles-hub");
app.MapHub<Journal.SoloPools.Hub>("solo-pools-hub");
app.MapHub<Journal.TeamPools.Hub>("team-pools-hub");
app.MapHub<Journal.Profiles.Hub>("users-hub");

//await Journal.Databases.Identity.Initializer.InitDb(app);
//await Journal.Databases.App.Initializer.InitDb(app);

app.UseStaticFiles();	
app.MapFallbackToFile("index.html");

app.Run();