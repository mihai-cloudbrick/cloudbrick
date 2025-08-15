# Orleans.Reminders.DataExplorer

## Purpose
Provides an Orleans reminders table implementation backed by DataExplorer storage.

## Public APIs
- `DataExplorerReminderTable` storing reminder state.
- `DataExplorerReminderOptions` to configure database and table identifiers.
- `SiloBuilderExtensions.AddDataExplorerReminders` to register the reminder service.

## Build
```bash
dotnet build src/Orleans.Reminders.DataExplorer/Cloudbrick.Orleans.Reminders.DataExplorer.csproj
```

## Samples
No dedicated sample is available.
