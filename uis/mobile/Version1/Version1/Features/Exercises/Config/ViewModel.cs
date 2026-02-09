using Mvvm;
using Navigation;
using System.Collections.Specialized;

namespace Version1.Features.Exercises.Config;

public partial class ViewModel(
    Core.Exercises.Configurations.Interface workoutsBiz,
    IAppNavigator appNavigator) : NavigationAwareBaseViewModel(appNavigator)
{
    #region [ Fields ]

    private readonly Core.Exercises.Configurations.Interface workoutsBiz = workoutsBiz;
    #endregion

    #region [ UI ]

    [ObservableProperty]
    string id;

    protected override void OnInit(IDictionary<string, object> query)
    {
        base.OnInit(query);


        if (query.GetData<object>() is IDictionary<string, object> data &&
            data.TryGetValue("Id", out var idObj) &&
            idObj is string idStr)
        {
            Id = idStr;
        }
        SelectedWeeklyItems.CollectionChanged += OnSelectedWeeklyItems_CollectionChanged;
        TotalSets.CollectionChanged += TotalSets_CollectionChanged;
    }

    public override async Task OnAppearingAsync()
    {
        await base.OnAppearingAsync();
        IsFuckingBusy = true;

        var response = await workoutsBiz
            .DetailAsync(new()
            {
                UserId = MyApp?.CurrentUser?.Id,
                ExerciseId = Guid.Parse(Id)
            });
        if (response == null)
        {
            IsFuckingBusy = false;
            return;
        }
        // Get the days that are already configured on the server
        //var serverWeekDays = response.WeekPlans ?? new List<Library.Workouts.WeekPlan>())
        //    .Select(wp => wp.DateOfWeek)
        //    .ToHashSet(StringComparer.OrdinalIgnoreCase);
        //var serverWeekDays = response.WeekPlans
        //    .SelectMany(x => x.WeekPlans ?? new List<Library.Workouts.WeekPlan>())
        //    .Select(wp => wp.DateOfWeek)
        //    .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var serverWeekDays = (response.WeekPlans ?? new List<Core.Exercises.Configurations.Detail.WeekPlan>())
            .Select(wp => wp.DateOfWeek)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Find matching WeeklyItems and mark them as selected
        var selectedWeeklyItems = WeeklyItems
            .Where(item => serverWeekDays.Contains(item.Content))
            .ToList();

        // Mark server data and add to selected items
        foreach (var item in selectedWeeklyItems)
        {
            item.IsServerData = true;
            SelectedWeeklyItems.Add(item);
            SelectedDays.Add(item);
        }

        //Now add the WorkoutTimeItems from the server
        var serverWorkoutTimes = (response.WeekPlans ?? new List<Core.Exercises.Configurations.Detail.WeekPlan>())
                .Select(wp => {
                    var correspondingWeeklyItem = WeeklyItems
                        .FirstOrDefault(wi => string.Equals(wi.Content, wp.DateOfWeek, StringComparison.OrdinalIgnoreCase));

                    return new WorkoutTimeItem
                    {
                        Id = correspondingWeeklyItem?.Id ?? Guid.NewGuid().ToString(), // Use WeeklyItem's ID if found
                        Content = wp.DateOfWeek ?? string.Empty,
                        Time = wp.Time
                    };
                })
                .ToList();

        foreach (var workoutTime in serverWorkoutTimes)
        {
            WorkoutTimeItems?.Add(workoutTime);
        }

        //Now for the TotalSets 
        var serverTotalSets = (response.WeekPlans ?? new List<Core.Exercises.Configurations.Detail.WeekPlan>())
            .SelectMany(wp => (wp.WeekPlanSets ?? new List<Core.Exercises.Configurations.Detail.WeekPlanSet>()).Select(wps => new SetConfigItem
            {
                Id = wps.Id.ToString(),
                Content = $"Set {wps.Value}",
                Day = wp.DateOfWeek,
                Reps = wps.Value
            }))
            .ToList();

        foreach (var totalSet in serverTotalSets)
        {
            TotalSets.Add(totalSet);
        }

        IsFuckingBusy = false;
    }

    [RelayCommand]
    async Task SaveAsync()
    {
        if (Id is null)
        {
            await ShowSnackBarAsync("Exercise Id is missing.");
            return;
        }
        if (WeeklyItems is null || !WeeklyItems.Any())
        {
            await ShowSnackBarAsync("Please select at least one day for the workout.");
            return;
        }
        if (WorkoutTimeItems is null || !WorkoutTimeItems.Any())
        {
            await ShowSnackBarAsync("Workout times are not configured.");
            return;
        }
        if (SetConfigItems is null || !SetConfigItems.Any())
        {
            await ShowSnackBarAsync("Set configurations are missing.");
            return;
        }

        List<string> daysWithoutReps = new();
        var daysWithSets = SelectedDays.Select(sd => sd.Content).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var selectedDay in SelectedDays)
        {
            var hasSets = TotalSets.Any(ts => string.Equals(ts.Day, selectedDay.Content, StringComparison.OrdinalIgnoreCase) && ts.Reps > 0);
            if (!hasSets)
            {
                daysWithoutReps.Add(selectedDay.Content + " ");
            }
        }

        if (daysWithoutReps.Any())
        {
            string message = "Reps in " + string.Join(", ", daysWithoutReps) + "are missing";
            await ShowSnackBarAsync(message);
            return;
        }
        if (MyApp is null || MyApp.CurrentUser is null)
        {
            await ShowSnackBarAsync("User credentials are missing");
            return;
        }

        await workoutsBiz.SaveAsync(new Core.Exercises.Configurations.Save.Payload
        {
            ExerciseId = Guid.Parse(Id),
            UserId = MyApp.CurrentUser.Id,
            WeekPlans = WorkoutTimeItems?.Select(wti => new Core.Exercises.Configurations.Save.WeekPlan
            {
                DateOfWeek = wti.Content,
                Time = wti.Time,
                WeekPlanSets = TotalSets?
                    .Where(ts => string.Equals(ts.Day, wti.Content, StringComparison.OrdinalIgnoreCase))
                    .Select(sci => new Core.Exercises.Configurations.Save.WeekPlanSet
                    {
                        Value = sci.Reps
                    }).ToList()
            }).ToList()
        });

        await AppNavigator.GoBackAsync(true);
    }

    public async Task ShowSnackBarAsync(string message)
    {
        await AppNavigator.ShowSnackbarAsync(message);
    }
    #endregion

    #region [ Weekly Schedule ]

    [ObservableProperty]
    ObservableCollection<WeeklyItem> weeklyItems = new ObservableCollection<WeeklyItem>(
    Enumerable.Range(0, 7)
        .Select(i => new WeeklyItem
        {
            Id = Guid.NewGuid().ToString(),
            Content = CultureInfo.CurrentCulture.DateTimeFormat.DayNames[(i + 1) % 7]
        }));

    [ObservableProperty]
    ObservableCollection<object> selectedWeeklyItems = new();

    private void OnSelectedWeeklyItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            var addedWeeklyItem = e.NewItems[0] as WeeklyItem;
            if (addedWeeklyItem is null) return;
            if (addedWeeklyItem.IsServerData)
            {
                addedWeeklyItem.IsServerData = false;
                return;
            }
            WorkoutTimeItems?.Add(new()
            {
                Id = addedWeeklyItem.Id,
                Content = addedWeeklyItem.Content,
                Time = new(6, 0, 0)
            });
            SelectedDays.Add(addedWeeklyItem);
        }
        if (e.OldItems != null)
        {
            var removedWeeklyItem = e.OldItems[0] as WeeklyItem;
            if (removedWeeklyItem is null)
                return;

            if (WorkoutTimeItems is null)
                return;

            var existingItem = WorkoutTimeItems.FirstOrDefault(x => x.Id == removedWeeklyItem.Id);
            if (existingItem is null)
                return;
            WorkoutTimeItems.Remove(existingItem);
            SelectedDays.Remove(removedWeeklyItem);
            var setsToRemove = TotalSets.Where(x => string.Equals(x.Day, removedWeeklyItem.Content, StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var set in setsToRemove)
            {
                TotalSets.Remove(set);
            }
            if (SelectedDayForSet?.Content == removedWeeklyItem.Content)
            {
                SelectedDayForSet = null;
            }
        }
    }
    #endregion

    #region [ Workout Times ]

    [ObservableProperty]
    ObservableCollection<WorkoutTimeItem>? workoutTimeItems = new();

    #endregion

    #region [ Sets For Days ]

    [ObservableProperty]
    string setConfigItemHeader = "Set For Days";

    [ObservableProperty]
    WeeklyItem? selectedDayForSet;

    partial void OnSelectedDayForSetChanged(WeeklyItem? value)
    {
        UpdateSetConfigItems();
    }

    [ObservableProperty]
    ObservableCollection<WeeklyItem> selectedDays = new();

    public ObservableCollection<SetConfigItem> TotalSets { get; set; } = new();

    [ObservableProperty]
    ObservableCollection<SetConfigItem>? setConfigItems = new();

    private void TotalSets_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SummaryTotalReps = TotalSets.Sum(x => x.Reps);
        UpdateSetConfigItems();
    }

    private void UpdateSetConfigItems()
    {
        if (SetConfigItems == null) return;
        SetConfigItems.Clear();

        if (SelectedDayForSet != null)
        {
            var filteredItems = TotalSets
                .Where(item => string.Equals(item.Day, SelectedDayForSet.Content, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var item in filteredItems)
            {
                SetConfigItems.Add(item);
            }
        }
    }
    #endregion

    #region [ Summary ]

    [ObservableProperty]
    int summaryTotalReps;
    #endregion

    #region [ Test ]
    #endregion
}

public partial class WeeklyItem : BaseModel
{
    [ObservableProperty]
    string id;

    [ObservableProperty]
    string content;

    [ObservableProperty]
    bool isServerData = false;
}

public partial class WorkoutTimeItem : BaseModel
{
    [ObservableProperty]
    string id;
    [ObservableProperty]
    string content;
    [ObservableProperty]
    TimeSpan time;
}

public partial class SetConfigItem : BaseModel
{
    [ObservableProperty]
    string id;
    [ObservableProperty] 
    string content;
    [ObservableProperty]
    string day;
    [ObservableProperty]
    int reps;
}
