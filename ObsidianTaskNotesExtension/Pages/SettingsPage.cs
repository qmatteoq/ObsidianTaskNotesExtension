// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Nodes;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using ObsidianTaskNotesExtension.Services;

namespace ObsidianTaskNotesExtension.Pages;

internal sealed partial class SettingsFormContent : FormContent
{
    private readonly SettingsManager _settingsManager;

    public SettingsFormContent(SettingsManager settingsManager, TaskNotesApiClient apiClient)
    {
        _settingsManager = settingsManager;

        var settings = _settingsManager.GetSettings();

        TemplateJson = $$"""
        {
            "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
            "type": "AdaptiveCard",
            "version": "1.5",
            "body": [
                {
                    "type": "TextBlock",
                    "text": "TaskNotes API Configuration",
                    "weight": "bolder",
                    "size": "medium"
                },
                {
                    "type": "TextBlock",
                    "text": "Configure the connection to your TaskNotes HTTP API. Make sure the API is enabled in TaskNotes settings (Settings -> HTTP API -> Enable).",
                    "wrap": true,
                    "spacing": "small"
                },
                {
                    "type": "Input.Text",
                    "id": "apiBaseUrl",
                    "label": "API Base URL",
                    "placeholder": "http://localhost:8080",
                    "value": "{{settings.ApiBaseUrl}}"
                },
                {
                    "type": "Input.Text",
                    "id": "authToken",
                    "label": "Auth Token (optional)",
                    "placeholder": "Leave empty if not using authentication",
                    "value": "{{settings.AuthToken}}"
                },
                {
                    "type": "Input.Text",
                    "id": "vaultName",
                    "label": "Vault Name (for Obsidian links)",
                    "placeholder": "Your vault name",
                    "value": "{{settings.VaultName}}"
                }
            ],
            "actions": [
                {
                    "type": "Action.Submit",
                    "title": "Save Settings",
                    "data": {
                        "action": "save"
                    }
                }
            ]
        }
        """;
    }

    public override CommandResult SubmitForm(string payload)
    {
        var formInput = JsonNode.Parse(payload)?.AsObject();

        if (formInput == null)
        {
            return CommandResult.KeepOpen();
        }

        var apiBaseUrl = formInput["apiBaseUrl"]?.GetValue<string>() ?? "http://localhost:8080";
        var authToken = formInput["authToken"]?.GetValue<string>() ?? "";
        var vaultName = formInput["vaultName"]?.GetValue<string>() ?? "";

        _settingsManager.SaveSettings(new ExtensionSettings
        {
            ApiBaseUrl = apiBaseUrl,
            AuthToken = authToken,
            VaultName = vaultName,
            ShowTaskTagInDecorators = _settingsManager.ShowTaskTagInDecorators,
            ShowCompletedTasksInTodayPage = _settingsManager.ShowCompletedTasksInTodayPage,
            StrikeThroughCompletedTaskTitles = _settingsManager.StrikeThroughCompletedTaskTitles
        });

        return CommandResult.GoBack();
    }
}

internal sealed partial class TaskViewSettingsFormContent : FormContent
{
    private readonly SettingsManager _settingsManager;

    public TaskViewSettingsFormContent(SettingsManager settingsManager)
    {
        _settingsManager = settingsManager;

        var settings = _settingsManager.GetSettings();
        var showTaskTagInDecorators = settings.ShowTaskTagInDecorators ? "true" : "false";
        var showCompletedTasksInTodayPage = settings.ShowCompletedTasksInTodayPage ? "true" : "false";
        var strikeThroughCompletedTaskTitles = settings.StrikeThroughCompletedTaskTitles ? "true" : "false";

        TemplateJson = $$"""
        {
            "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
            "type": "AdaptiveCard",
            "version": "1.5",
            "body": [
                {
                    "type": "TextBlock",
                    "text": "Customize task views",
                    "weight": "bolder",
                    "size": "medium"
                },
                {
                    "type": "TextBlock",
                    "text": "Control how tasks are displayed across the task list pages.",
                    "wrap": true,
                    "spacing": "small"
                },
                {
                    "type": "Input.Toggle",
                    "id": "showTaskTagInDecorators",
                    "title": "Show the default 'task' tag in decorators",
                    "value": "{{showTaskTagInDecorators}}",
                    "valueOn": "true",
                    "valueOff": "false"
                },
                {
                    "type": "Input.Toggle",
                    "id": "showCompletedTasksInTodayPage",
                    "title": "Show completed tasks in Today's page",
                    "value": "{{showCompletedTasksInTodayPage}}",
                    "valueOn": "true",
                    "valueOff": "false"
                },
                {
                    "type": "Input.Toggle",
                    "id": "strikeThroughCompletedTaskTitles",
                    "title": "Strike through completed task titles",
                    "value": "{{strikeThroughCompletedTaskTitles}}",
                    "valueOn": "true",
                    "valueOff": "false"
                }
            ],
            "actions": [
                {
                    "type": "Action.Submit",
                    "title": "Save View Settings",
                    "data": {
                        "action": "save"
                    }
                }
            ]
        }
        """;
    }

    public override CommandResult SubmitForm(string payload)
    {
        var formInput = JsonNode.Parse(payload)?.AsObject();

        if (formInput == null)
        {
            return CommandResult.KeepOpen();
        }

        var currentSettings = _settingsManager.GetSettings();

        _settingsManager.SaveSettings(new ExtensionSettings
        {
            ApiBaseUrl = currentSettings.ApiBaseUrl,
            AuthToken = currentSettings.AuthToken,
            VaultName = currentSettings.VaultName,
            ShowTaskTagInDecorators = GetToggleValue(formInput, "showTaskTagInDecorators", currentSettings.ShowTaskTagInDecorators),
            ShowCompletedTasksInTodayPage = GetToggleValue(formInput, "showCompletedTasksInTodayPage", currentSettings.ShowCompletedTasksInTodayPage),
            StrikeThroughCompletedTaskTitles = GetToggleValue(formInput, "strikeThroughCompletedTaskTitles", currentSettings.StrikeThroughCompletedTaskTitles)
        });

        return CommandResult.GoBack();
    }

    private static bool GetToggleValue(JsonObject formInput, string key, bool defaultValue)
    {
        var valueNode = formInput[key];
        if (valueNode is not JsonValue value)
        {
            return defaultValue;
        }

        if (value.TryGetValue<bool>(out var boolValue))
        {
            return boolValue;
        }

        if (value.TryGetValue<string>(out var stringValue))
        {
            return string.Equals(stringValue, "true", System.StringComparison.OrdinalIgnoreCase);
        }

        return defaultValue;
    }
}

internal sealed partial class TestConnectionCommand : InvokableCommand
{
    private readonly TaskNotesApiClient _apiClient;

    public TestConnectionCommand(TaskNotesApiClient apiClient)
    {
        _apiClient = apiClient;
        Name = "Test Connection";
        Icon = new IconInfo("\uE703"); // Network icon
    }

    public override CommandResult Invoke()
    {
        _ = TestAsync();
        return CommandResult.KeepOpen();
    }

    private async System.Threading.Tasks.Task TestAsync()
    {
        var (success, message) = await _apiClient.TestConnectionAsync();
        // The result will be shown via toast or status
        System.Diagnostics.Debug.WriteLine($"Connection test: {success} - {message}");
    }
}

internal sealed partial class SettingsPage : ListPage
{
    private readonly SettingsManager _settingsManager;
    private readonly TaskNotesApiClient _apiClient;

    public SettingsPage(SettingsManager settingsManager, TaskNotesApiClient apiClient)
    {
        _settingsManager = settingsManager;
        _apiClient = apiClient;

        Icon = new IconInfo("\uE713"); // Settings icon
        Title = "Settings";
        Name = "Settings";
    }

    public override IListItem[] GetItems()
    {
        var settingsForm = new SettingsFormPage(_settingsManager, _apiClient);
        var taskViewSettingsForm = new TaskViewSettingsFormPage(_settingsManager);
        var testCommand = new TestConnectionCommand(_apiClient);
        var taskTagVisibility = _settingsManager.ShowTaskTagInDecorators ? "shown" : "hidden";
        var todayCompletedVisibility = _settingsManager.ShowCompletedTasksInTodayPage ? "shown" : "hidden";
        var completedTitleFormatting = _settingsManager.StrikeThroughCompletedTaskTitles ? "on" : "off";

        return
        [
            new ListItem(settingsForm)
            {
                Title = "Configure API Connection",
                Subtitle = $"Current: {_settingsManager.ApiBaseUrl}",
                Icon = new IconInfo("\uE713")
            },
            new ListItem(taskViewSettingsForm)
            {
                Title = "Customize task views",
                Subtitle = $"Task tag {taskTagVisibility} · Today done tasks {todayCompletedVisibility} · Strike-through {completedTitleFormatting}",
                Icon = new IconInfo("\uE790")
            },
            new ListItem(testCommand)
            {
                Title = "Test Connection",
                Subtitle = "Verify TaskNotes API is reachable",
                Icon = new IconInfo("\uE703")
            }
        ];
    }
}

internal sealed partial class SettingsFormPage : ContentPage
{
    private readonly SettingsManager _settingsManager;
    private readonly TaskNotesApiClient _apiClient;

    public SettingsFormPage(SettingsManager settingsManager, TaskNotesApiClient apiClient)
    {
        _settingsManager = settingsManager;
        _apiClient = apiClient;

        Icon = new IconInfo("\uE713");
        Title = "Configure API";
        Name = "Configure";
    }

    public override IContent[] GetContent()
    {
        return [new SettingsFormContent(_settingsManager, _apiClient)];
    }
}

internal sealed partial class TaskViewSettingsFormPage : ContentPage
{
    private readonly SettingsManager _settingsManager;

    public TaskViewSettingsFormPage(SettingsManager settingsManager)
    {
        _settingsManager = settingsManager;

        Icon = new IconInfo("\uE790");
        Title = "Customize task views";
        Name = "Customize task views";
    }

    public override IContent[] GetContent()
    {
        return [new TaskViewSettingsFormContent(_settingsManager)];
    }
}
