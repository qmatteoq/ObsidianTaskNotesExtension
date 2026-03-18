// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using ObsidianTaskNotesExtension.Commands;
using ObsidianTaskNotesExtension.Helpers;
using ObsidianTaskNotesExtension.Models;
using ObsidianTaskNotesExtension.Services;

namespace ObsidianTaskNotesExtension.Pages;

internal sealed partial class TodayTasksPage : DynamicListPage
{
    private readonly TaskNotesApiClient _apiClient;
    private List<TaskItem> _tasks = new();
    private string? _errorMessage;
    private string _searchText = string.Empty;

    public TodayTasksPage(TaskNotesApiClient apiClient)
    {
        _apiClient = apiClient;

        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Title = "Today's Obsidian Tasks";
        Name = "Today Tasks";
        ShowDetails = true;

        FetchTasksAsync();
    }

    public override IListItem[] GetItems()
    {
        var items = new List<IListItem>();

        if (_errorMessage != null)
        {
            items.Add(new ListItem(new NoOpCommand())
            {
                Title = "Connection Error",
                Subtitle = _errorMessage,
                Icon = new IconInfo("\uE783")
            });
        }
        else if (_tasks.Count == 0)
        {
            items.Add(new ListItem(new NoOpCommand())
            {
                Title = "No tasks found",
                Subtitle = "No tasks are due, scheduled, or overdue for today",
                Icon = new IconInfo("\uE8E5")
            });
        }
        else
        {
            var filteredTasks = string.IsNullOrWhiteSpace(_searchText)
                ? _tasks
                : _tasks.Where(t => t.Title.Contains(_searchText, StringComparison.OrdinalIgnoreCase)).ToList();

            if (filteredTasks.Count == 0 && !string.IsNullOrWhiteSpace(_searchText))
            {
                items.Add(new ListItem(new NoOpCommand())
                {
                    Title = "No matching tasks",
                    Subtitle = $"No tasks found matching '{_searchText}'",
                    Icon = new IconInfo("\uE721")
                });
            }
            else
            {
                var taskItems = filteredTasks
                    .OrderBy(t => t.Completed ? 1 : 0)
                    .ThenBy(t => t.Archived ? 1 : 0)
                    .ThenBy(t => t.IsOverdue ? 0 : 1)
                    .ThenBy(t => t.Due ?? DateTime.MaxValue)
                    .ThenBy(t => GetPrioritySortOrder(t.Priority))
                    .Select(task => CreateTaskListItem(task));

                items.AddRange(taskItems);
            }
        }

        var refreshCommand = new RefreshListCommand(RefreshTasks);
        items.Add(new ListItem(refreshCommand)
        {
            Title = "Refresh Tasks",
            Subtitle = "Reload tasks from TaskNotes API",
            Icon = new IconInfo("\uE72C")
        });

        return items.ToArray();
    }

    private ListItem CreateTaskListItem(TaskItem task)
    {
        var toggleCommand = new ToggleTaskStatusCommand(task, _apiClient, RefreshTasks);
        var openCommand = new OpenInObsidianCommand(task, _apiClient);
        var archiveCommand = new ArchiveTaskCommand(task, _apiClient, RefreshTasks);
        var copyLinkCommand = new CopyTaskLinkCommand(task, _apiClient);
        var deleteCommand = new DeleteTaskCommand(task, _apiClient, RefreshTasks);
        var startTimeCommand = new StartTimeTrackingCommand(task, _apiClient, RefreshTasks);
        var stopTimeCommand = new StopTimeTrackingCommand(task, _apiClient, RefreshTasks);
        var startPomodoroCommand = new StartPomodoroCommand(_apiClient, RefreshTasks, task);

        return new ListItem(toggleCommand)
        {
            Title = task.Title,
            Subtitle = FormatSubtitle(task),
            Icon = GetIcon(task),
            Tags = TagHelpers.CreateTaskTags(task),
            Details = TagHelpers.CreateTaskDetails(task),
            MoreCommands = [
                new CommandContextItem(new EditTaskPage(task, _apiClient)),
                new CommandContextItem(openCommand),
                new CommandContextItem(startTimeCommand),
                new CommandContextItem(stopTimeCommand),
                new CommandContextItem(startPomodoroCommand),
                new CommandContextItem(archiveCommand),
                new CommandContextItem(deleteCommand),
                new CommandContextItem(copyLinkCommand)
            ]
        };
    }

    private static string FormatSubtitle(TaskItem task)
    {
        var status = task.Archived ? "Archived" : task.Completed ? "Completed" : "Active";

        if (task.Due.HasValue)
        {
            var due = task.Due.Value;
            if (!task.Completed && !task.Archived && task.IsOverdue)
            {
                var daysOverdue = (DateTime.Today - due.Date).Days;
                return $"{status} · Overdue by {daysOverdue} day{(daysOverdue == 1 ? "" : "s")}";
            }

            return $"{status} · Due: {due:MMM d}";
        }

        return status;
    }

    private static IconInfo GetIcon(TaskItem task)
    {
        if (task.Archived)
        {
            return new IconInfo("\uE7B8");
        }

        if (task.Completed)
        {
            return new IconInfo("\uE73E");
        }

        if (task.IsOverdue)
        {
            return new IconInfo("\uE7BA");
        }

        return new IconInfo("\uE73A");
    }

    private static int GetPrioritySortOrder(string? priority)
    {
        var p = priority?.ToLowerInvariant() ?? "";

        return p switch
        {
            "1-urgent" or "urgent" or "1" => 1,
            "2-high" or "high" or "2" => 2,
            "3-medium" or "medium" or "3" => 3,
            "4-normal" or "normal" or "4" => 4,
            "5-low" or "low" or "5" => 5,
            _ => 4
        };
    }

    private void RefreshTasks()
    {
        // Set loading state immediately and notify UI to show spinner
        IsLoading = true;
        RaiseItemsChanged();

        // Then start the async fetch
        FetchTasksAsync();
    }

    private async void FetchTasksAsync()
    {
        _errorMessage = null;

        try
        {
            var (success, message) = await _apiClient.TestConnectionAsync();

            if (!success)
            {
                _errorMessage = message;
                _tasks = new List<TaskItem>();
            }
            else
            {
                _tasks = (await _apiClient.GetAllTasksAsync())
                    .Where(task => task.IsOverdue || task.IsDueToday || task.IsScheduledToday)
                    .ToList();
                Debug.WriteLine($"[TodayTasksPage] FetchTasksAsync - Got {_tasks.Count} tasks");
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error: {ex.Message}";
            _tasks = new List<TaskItem>();
        }
        finally
        {
            IsLoading = false;
            RaiseItemsChanged();
        }
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        _searchText = newSearch ?? string.Empty;
        RaiseItemsChanged();
    }
}