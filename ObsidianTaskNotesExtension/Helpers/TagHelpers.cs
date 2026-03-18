// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using ObsidianTaskNotesExtension.Models;

namespace ObsidianTaskNotesExtension.Helpers;

/// <summary>
/// Helper methods for creating consistent, beautiful tags for Command Palette list items.
/// </summary>
internal static class TagHelpers
{
  // Color palette for consistent styling
  private static class Colors
  {
    // Priority colors
    public static readonly OptionalColor UrgentBg = ColorHelpers.FromRgb(220, 53, 69);      // Red
    public static readonly OptionalColor HighBg = ColorHelpers.FromRgb(255, 140, 0);        // Orange
    public static readonly OptionalColor MediumBg = ColorHelpers.FromRgb(255, 193, 7);      // Yellow
    public static readonly OptionalColor NormalBg = ColorHelpers.FromRgb(23, 162, 184);     // Cyan
    public static readonly OptionalColor LowBg = ColorHelpers.FromRgb(108, 117, 125);       // Gray

    // Status colors
    public static readonly OptionalColor OverdueBg = ColorHelpers.FromRgb(220, 53, 69);     // Red
    public static readonly OptionalColor DueTodayBg = ColorHelpers.FromRgb(255, 193, 7);    // Yellow
    public static readonly OptionalColor DueTomorrowBg = ColorHelpers.FromRgb(40, 167, 69); // Green
    public static readonly OptionalColor CompletedBg = ColorHelpers.FromRgb(40, 167, 69);   // Green
    public static readonly OptionalColor ArchivedBg = ColorHelpers.FromRgb(108, 117, 125);  // Gray

    // User content colors
    public static readonly OptionalColor UserTagBg = ColorHelpers.FromRgb(100, 149, 237);   // Cornflower blue
    public static readonly OptionalColor ProjectBg = ColorHelpers.FromRgb(106, 90, 205);    // Slate blue

    // Text colors
    public static readonly OptionalColor LightText = ColorHelpers.FromRgb(255, 255, 255);   // White
    public static readonly OptionalColor DarkText = ColorHelpers.FromRgb(33, 37, 41);       // Dark gray

    // Stat/badge colors
    public static readonly OptionalColor StreakBg = ColorHelpers.FromRgb(255, 69, 0);       // Orange-red
    public static readonly OptionalColor ActiveBg = ColorHelpers.FromRgb(0, 123, 255);      // Blue
    public static readonly OptionalColor TimeBg = ColorHelpers.FromRgb(102, 16, 242);       // Purple
    public static readonly OptionalColor CountBg = ColorHelpers.FromRgb(32, 201, 151);      // Teal
  }

  /// <summary>
  /// Creates all relevant tags for a task item (priority, due status, user tags, projects).
  /// </summary>
  public static ITag[] CreateTaskTags(TaskItem task, bool includeDefaultTaskTag = true, int maxUserTags = 3)
  {
    var tags = new List<ITag>();

    // Priority tag (always first for visibility)
    var priorityTag = CreatePriorityTag(task.Priority);
    if (priorityTag != null)
    {
      tags.Add(priorityTag);
    }

    // Due status tag
    var dueTag = CreateDueStatusTag(task);
    if (dueTag != null)
    {
      tags.Add(dueTag);
    }

    // Status tag for completed/archived
    var statusTag = CreateStatusTag(task);
    if (statusTag != null)
    {
      tags.Add(statusTag);
    }

    // User-defined tags (limited)
    if (task.Tags is { Length: > 0 })
    {
      var visibleTags = includeDefaultTaskTag
        ? task.Tags
        : task.Tags.Where(tag => !tag.Equals("task", StringComparison.OrdinalIgnoreCase)).ToArray();

      var userTags = visibleTags.Take(maxUserTags).Select(CreateUserTag);
      tags.AddRange(userTags);

      if (visibleTags.Length > maxUserTags)
      {
        tags.Add(new Tag($"+{visibleTags.Length - maxUserTags}")
        {
          Icon = new IconInfo("\uE8EC"), // Tag icon
          ToolTip = string.Join(", ", visibleTags.Skip(maxUserTags))
        });
      }
    }

    // Projects (limited to 2)
    if (task.Projects is { Length: > 0 })
    {
      var projectTags = task.Projects.Take(2).Select(CreateProjectTag);
      tags.AddRange(projectTags);

      if (task.Projects.Length > 2)
      {
        tags.Add(new Tag($"+{task.Projects.Length - 2}")
        {
          Icon = new IconInfo("\uE821"), // Folder icon
          ToolTip = string.Join(", ", task.Projects.Skip(2))
        });
      }
    }

    return tags.ToArray();
  }

  /// <summary>
  /// Formats a task title with an optional strike-through effect for completed tasks.
  /// </summary>
  public static string FormatTaskTitle(TaskItem task, bool strikeThroughCompletedTaskTitles)
  {
    if (!strikeThroughCompletedTaskTitles || !task.Completed || string.IsNullOrEmpty(task.Title))
    {
      return task.Title;
    }

    var builder = new StringBuilder(task.Title.Length * 2);

    foreach (var character in task.Title)
    {
      builder.Append(character);

      if (!char.IsWhiteSpace(character))
      {
        builder.Append('\u0336');
      }
    }

    return builder.ToString();
  }

  /// <summary>
  /// Creates a priority tag with appropriate color and icon.
  /// </summary>
  public static Tag? CreatePriorityTag(string? priority)
  {
    if (string.IsNullOrEmpty(priority))
    {
      return null;
    }

    var p = priority.ToLowerInvariant();
    return p switch
    {
      "1-urgent" or "urgent" or "1" => new Tag("Urgent")
      {
        Icon = new IconInfo("\uE7C1"), // Important icon
        Background = Colors.UrgentBg,
        Foreground = Colors.LightText,
        ToolTip = "Priority: Urgent"
      },
      "2-high" or "high" or "2" => new Tag("High")
      {
        Icon = new IconInfo("\uE8CB"), // Flag icon
        Background = Colors.HighBg,
        Foreground = Colors.LightText,
        ToolTip = "Priority: High"
      },
      "3-medium" or "medium" or "3" => new Tag("Medium")
      {
        Icon = new IconInfo("\uE8CB"),
        Background = Colors.MediumBg,
        Foreground = Colors.DarkText,
        ToolTip = "Priority: Medium"
      },
      "4-normal" or "normal" or "4" => new Tag("Normal")
      {
        Icon = new IconInfo("\uE8CB"),
        Background = Colors.NormalBg,
        Foreground = Colors.LightText,
        ToolTip = "Priority: Normal"
      },
      "5-low" or "low" or "5" => new Tag("Low")
      {
        Icon = new IconInfo("\uE8CB"),
        Background = Colors.LowBg,
        Foreground = Colors.LightText,
        ToolTip = "Priority: Low"
      },
      _ => null
    };
  }

  /// <summary>
  /// Creates a due status tag (Overdue, Today, Tomorrow) if applicable.
  /// </summary>
  public static Tag? CreateDueStatusTag(TaskItem task)
  {
    if (task.Completed || task.Archived)
    {
      return null;
    }

    if (task.IsOverdue)
    {
      var daysOverdue = (DateTime.Today - task.Due!.Value.Date).Days;
      return new Tag("Overdue")
      {
        Icon = new IconInfo("\uE7BA"), // Warning icon
        Background = Colors.OverdueBg,
        Foreground = Colors.LightText,
        ToolTip = daysOverdue == 1 ? "Overdue by 1 day" : $"Overdue by {daysOverdue} days"
      };
    }

    if (task.IsDueToday)
    {
      return new Tag("Today")
      {
        Icon = new IconInfo("\uE787"), // Calendar icon
        Background = Colors.DueTodayBg,
        Foreground = Colors.DarkText,
        ToolTip = "Due today"
      };
    }

    if (task.IsDueTomorrow)
    {
      return new Tag("Tomorrow")
      {
        Icon = new IconInfo("\uE787"),
        Background = Colors.DueTomorrowBg,
        Foreground = Colors.LightText,
        ToolTip = "Due tomorrow"
      };
    }

    return null;
  }

  /// <summary>
  /// Creates a status tag for completed or archived tasks.
  /// </summary>
  public static Tag? CreateStatusTag(TaskItem task)
  {
    if (task.Archived)
    {
      return new Tag("Archived")
      {
        Icon = new IconInfo("\uE7B8"), // Archive icon
        Background = Colors.ArchivedBg,
        Foreground = Colors.LightText
      };
    }

    if (task.CompletedToday)
    {
      return new Tag("Done Today")
      {
        Icon = new IconInfo("\uE73E"), // Check mark icon
        Background = Colors.CompletedBg,
        Foreground = Colors.LightText
      };
    }

    if (task.Completed)
    {
      return new Tag("Done")
      {
        Icon = new IconInfo("\uE73E"),
        Background = Colors.CompletedBg,
        Foreground = Colors.LightText
      };
    }

    return null;
  }

  /// <summary>
  /// Creates a user-defined tag badge.
  /// </summary>
  public static Tag CreateUserTag(string tagName)
  {
    return new Tag(tagName)
    {
      Icon = new IconInfo("\uE8EC"), // Tag icon
      Background = Colors.UserTagBg,
      Foreground = Colors.LightText,
      ToolTip = $"Tag: {tagName}"
    };
  }

  /// <summary>
  /// Creates a project badge.
  /// </summary>
  public static Tag CreateProjectTag(string projectName)
  {
    return new Tag(projectName)
    {
      Icon = new IconInfo("\uE821"), // Folder icon
      Background = Colors.ProjectBg,
      Foreground = Colors.LightText,
      ToolTip = $"Project: {projectName}"
    };
  }

  /// <summary>
  /// Creates a streak badge for Pomodoro.
  /// </summary>
  public static Tag CreateStreakTag(int streak)
  {
    return new Tag($"🔥 {streak}")
    {
      Background = Colors.StreakBg,
      Foreground = Colors.LightText,
      ToolTip = $"Current streak: {streak} days"
    };
  }

  /// <summary>
  /// Creates a session count badge.
  /// </summary>
  public static Tag CreateSessionCountTag(int count)
  {
    return new Tag($"{count} sessions")
    {
      Icon = new IconInfo("\uE73E"), // Check icon
      Background = Colors.CountBg,
      Foreground = Colors.LightText
    };
  }

  /// <summary>
  /// Creates a time duration badge.
  /// </summary>
  public static Tag CreateTimeTag(double minutes, string? label = null)
  {
    var timeStr = minutes >= 60 ? $"{minutes / 60:F0}h {minutes % 60:F0}m" : $"{minutes:F0}m";
    var displayText = label != null ? $"{label}: {timeStr}" : timeStr;

    return new Tag(displayText)
    {
      Icon = new IconInfo("\uE823"), // Clock icon
      Background = Colors.TimeBg,
      Foreground = Colors.LightText
    };
  }

  /// <summary>
  /// Creates an "Active" state badge.
  /// </summary>
  public static Tag CreateActiveTag(string? state = null)
  {
    return new Tag(state ?? "Active")
    {
      Icon = new IconInfo("\uE916"), // Play icon
      Background = Colors.ActiveBg,
      Foreground = Colors.LightText
    };
  }

  /// <summary>
  /// Creates a numeric count badge with optional warning coloring.
  /// </summary>
  public static Tag CreateCountTag(int count, string label, bool isWarning = false)
  {
    return new Tag($"{count}")
    {
      Background = isWarning ? Colors.OverdueBg : Colors.CountBg,
      Foreground = Colors.LightText,
      ToolTip = $"{count} {label}"
    };
  }

  /// <summary>
  /// Creates details metadata for a task.
  /// </summary>
  public static IDetails CreateTaskDetails(TaskItem task, bool strikeThroughCompletedTaskTitles = false)
  {
    var metadata = new List<IDetailsElement>();

    // Status
    var status = task.Archived ? "Archived" : task.Completed ? "Completed" : "Active";
    metadata.Add(new DetailsElement()
    {
      Key = "Status",
      Data = new DetailsLink() { Text = status }
    });

    // Due date
    if (task.Due.HasValue)
    {
      metadata.Add(new DetailsElement()
      {
        Key = "Due",
        Data = new DetailsLink() { Text = task.Due.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) }
      });
    }

    // Priority
    if (!string.IsNullOrEmpty(task.Priority))
    {
      metadata.Add(new DetailsElement()
      {
        Key = "Priority",
        Data = new DetailsLink() { Text = FormatPriorityLabel(task.Priority) }
      });
    }

    // Tags as visual badges
    if (task.Tags is { Length: > 0 })
    {
      metadata.Add(new DetailsElement()
      {
        Key = "Tags",
        Data = new DetailsTags() { Tags = task.Tags.Select(CreateUserTag).ToArray() }
      });
    }

    // Projects as visual badges
    if (task.Projects is { Length: > 0 })
    {
      metadata.Add(new DetailsElement()
      {
        Key = "Projects",
        Data = new DetailsTags() { Tags = task.Projects.Select(CreateProjectTag).ToArray() }
      });
    }

    return new Details()
    {
      Title = FormatTaskTitle(task, strikeThroughCompletedTaskTitles),
      Body = FormatTaskBody(task),
      Metadata = metadata.ToArray()
    };
  }

  private static string FormatPriorityLabel(string priority)
  {
    var p = priority.ToLowerInvariant();
    return p switch
    {
      "1-urgent" or "urgent" or "1" => "🔴 Urgent",
      "2-high" or "high" or "2" => "🟠 High",
      "3-medium" or "medium" or "3" => "🟡 Medium",
      "4-normal" or "normal" or "4" => "🔵 Normal",
      "5-low" or "low" or "5" => "⚪ Low",
      _ => priority
    };
  }

  private static string FormatTaskBody(TaskItem task)
  {
    var parts = new List<string>();

    if (task.IsOverdue)
    {
      var daysOverdue = (DateTime.Today - task.Due!.Value.Date).Days;
      parts.Add($"⚠️ **Overdue** by {daysOverdue} day{(daysOverdue == 1 ? "" : "s")}");
    }
    else if (task.IsDueToday)
    {
      parts.Add("📅 **Due today**");
    }
    else if (task.IsDueTomorrow)
    {
      parts.Add("📅 Due tomorrow");
    }

    if (task.Scheduled != null)
    {
      parts.Add($"📆 Scheduled: {task.Scheduled}");
    }

    if (parts.Count == 0)
    {
      parts.Add("Use the context menu for more actions.");
    }

    return string.Join("\n\n", parts);
  }
}
