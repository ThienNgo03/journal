using BFF.Authentication;
using BFF.Databases;
using BFF.Exercises;
using BFF.Exercises.Configurations;
using BFF.Subscriptions;
using BFF.Users;
using BFF.Wolverine;
using Library;
using Provider;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddExercises();
builder.Services.AddExerciseConfigurations();
builder.Services.AddSubcriptions();
builder.Services.AddUsers();
//builder.Services.AddChat();
builder.Services.AddSignalR(x => x.EnableDetailedErrors = true);
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddWolverine(builder.Configuration);
builder.Services.AddAuthentication(builder.Configuration);

var libraryConfig = new Library.Config(
    url: builder.Configuration["LibraryConfig:Url"] ?? "https://localhost:7011",
    secretKey: builder.Configuration["LibraryConfig:SecretKey"]
);
builder.Services.AddEndpoints(libraryConfig);

var providerConfig = new Provider.Config(
    url: builder.Configuration["ProviderConfig:Url"] ?? "https://localhost:7063",
    secretKey: builder.Configuration["ProviderConfig:SecretKey"] ?? "secretKey"
);
builder.Services.AddProviders(providerConfig);

var app = builder.Build();

app.MapHub<BFF.Chat.Hub>("messages-hub");
app.MapHub<BFF.WorkoutLogTracking.Hub>("workout-log-tracking-hub");
app.MapHub<BFF.Users.Hub>("users-hub");
app.MapHub<BFF.Exercises.Configurations.Hub>("exercise-configurations-hub");

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
