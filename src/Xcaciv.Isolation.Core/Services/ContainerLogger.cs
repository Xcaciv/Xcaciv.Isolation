using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Xcaciv.Isolation.Core.Interfaces;
using Xcaciv.Isolation.Core.Models;

namespace Xcaciv.Isolation.Core.Services;

/// <summary>
/// Container logging service
/// </summary>
public sealed class ContainerLogger : IContainerLogger
{
    private readonly ConcurrentDictionary<string, List<ContainerLogEntry>> containerLogs = new();
    private readonly int maxLogsPerContainer = 10000;

    /// <summary>
    /// Adds a log entry for a container
    /// </summary>
    /// <param name="containerId">Container identifier</param>
    /// <param name="stream">Log stream type</param>
    /// <param name="message">Log message</param>
    public void AddLog(string containerId, LogStream stream, string message)
    {
        var logEntry = new ContainerLogEntry
        {
            Timestamp = DateTime.UtcNow,
            Stream = stream,
            Message = message
        };

        var logs = containerLogs.GetOrAdd(containerId, _ => new List<ContainerLogEntry>());

        lock (logs)
        {
            logs.Add(logEntry);

            // Keep only the most recent logs
            if (logs.Count > maxLogsPerContainer)
            {
                logs.RemoveRange(0, logs.Count - maxLogsPerContainer);
            }
        }
    }

    public async IAsyncEnumerable<ContainerLogEntry> GetLogsAsync(
        string containerId,
        bool follow = false,
        int? tail = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!containerLogs.TryGetValue(containerId, out var logs))
        {
            yield break;
        }

        List<ContainerLogEntry> logsCopy;
        lock (logs)
        {
            var startIndex = tail.HasValue && tail.Value < logs.Count
                ? logs.Count - tail.Value
                : 0;

            logsCopy = logs.Skip(startIndex).ToList();
        }

        foreach (var log in logsCopy)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            yield return log;
        }

        if (follow)
        {
            var lastCount = logsCopy.Count;

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(500, cancellationToken);

                if (!containerLogs.TryGetValue(containerId, out logs))
                {
                    yield break;
                }

                List<ContainerLogEntry> newLogs;
                lock (logs)
                {
                    if (logs.Count > lastCount)
                    {
                        newLogs = logs.Skip(lastCount).ToList();
                        lastCount = logs.Count;
                    }
                    else
                    {
                        continue;
                    }
                }

                foreach (var log in newLogs)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        yield break;
                    }

                    yield return log;
                }
            }
        }
    }

    public Task ClearLogsAsync(string containerId, CancellationToken cancellationToken = default)
    {
        if (containerLogs.TryGetValue(containerId, out var logs))
        {
            lock (logs)
            {
                logs.Clear();
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes all logs for a container
    /// </summary>
    /// <param name="containerId">Container identifier</param>
    public void RemoveContainer(string containerId)
    {
        containerLogs.TryRemove(containerId, out _);
    }
}
