# Lesson: Spectre.Console Live display with concurrent host

**Date:** 2025-07-25
**Context:** Adding a Spectre.Console live dashboard to replace scrolling log output

## Problem
When running `AnsiConsole.Live()` alongside `Host.RunAsync()`, both tasks need to be awaited with `Task.WhenAll()`. If the live display runs on the main thread, the host never starts (and vice versa).

## Fix
Run both tasks concurrently:
```csharp
var hostTask = host.RunAsync(cancellationToken);
var displayTask = display.RunAsync(cancellationToken);
await Task.WhenAll(hostTask, displayTask);
```

Both tasks share the same CancellationToken so Ctrl+C stops both.

## Rule
When adding a live UI loop to a hosted service app, always run the display and host in parallel with `Task.WhenAll`. Never `await` one before starting the other.
