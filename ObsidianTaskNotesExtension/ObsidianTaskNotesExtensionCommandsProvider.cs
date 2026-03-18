// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using ObsidianTaskNotesExtension.Pages;
using ObsidianTaskNotesExtension.Services;

namespace ObsidianTaskNotesExtension;

public partial class ObsidianTaskNotesExtensionCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;
    private readonly SettingsManager _settingsManager;
    private readonly TaskNotesApiClient _apiClient;

    public ObsidianTaskNotesExtensionCommandsProvider()
    {
        DisplayName = "Task Notes Command Palette for Obsidian";
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");

        // Initialize shared services
        _settingsManager = new SettingsManager();
        _apiClient = new TaskNotesApiClient(_settingsManager);

        // Create pages
        var tasksPage = new ObsidianTaskNotesExtensionPage(_apiClient, _settingsManager);
        var todayTasksPage = new TodayTasksPage(_apiClient, _settingsManager);
        var allTasksPage = new AllTasksPage(_apiClient, _settingsManager);
        var createTaskPage = new CreateTaskPage(_apiClient);
        var statsPage = new StatsPage(_apiClient);
        var pomodoroPage = new PomodoroPage(_apiClient);
        var timeTrackingPage = new TimeTrackingPage(_apiClient);
        var settingsPage = new SettingsPage(_settingsManager, _apiClient);

        _commands =
        [
            new CommandItem(tasksPage)
            {
                Title = "Obsidian Tasks",
                Subtitle = "View and manage your TaskNotes tasks"
            },
            new CommandItem(todayTasksPage)
            {
                Title = "Obsidian Today Tasks",
                Subtitle = "View open tasks due or scheduled for today"
            },
            new CommandItem(allTasksPage)
            {
                Title = "Obsidian All Tasks",
                Subtitle = "View all tasks including completed and archived"
            },
            new CommandItem(createTaskPage)
            {
                Title = "Obsidian Create Task",
                Subtitle = "Create a new task"
            },
            new CommandItem(statsPage)
            {
                Title = "Obsidian Task Stats",
                Subtitle = "View task and time statistics"
            },
            new CommandItem(pomodoroPage)
            {
                Title = "Obsidian Pomodoro",
                Subtitle = "Pomodoro timer and focus sessions"
            },
            new CommandItem(timeTrackingPage)
            {
                Title = "Obsidian Time Tracking",
                Subtitle = "View active timers and time summaries"
            },
            new CommandItem(settingsPage)
            {
                Title = "Obsidian Tasks Settings",
                Subtitle = "Configure API connection"
            }
        ];
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return _commands;
    }
}
