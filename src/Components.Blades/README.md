# Cloudbrick Blades (Azure-Portal-like blades)

**.NET 9.0** Razor Class Library + Blazor Server demo.

## Build
```bash
# from this folder
dotnet restore
dotnet build
dotnet run --project demos/Cloudbrick.Blades.Demo
```

Open a browser to one of:
- `/subscriptions/00000000-0000-0000-0000-000000000000`
- `/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-demo`
- `/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-demo/providers/Microsoft.Storage/storageAccounts`
- `/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-demo/providers/Microsoft.Storage/storageAccounts/acc-001`
```

## Notes
- The demo registers four blades: Subscription, ResourceGroup, Provider Type List, Resource Details.
- URL ↔ blade stack sync is handled by `<ArmUrlSync />`.
- Fluent UI (Microsoft.FluentUI.AspNetCore.Components) is used for visuals.
- Include the library CSS in host via: `<link href="_content/Cloudbrick.Components.Blades/blades.css" rel="stylesheet" />`


### New in this build
- **BladeLink** component to generate ARM-style `<a href>` links.
- **Splitter** between blades (drag to resize).
- **Dirty-state guard**: components can mark themselves dirty via `IBladeDirtyRegistry` using the cascaded `BladeDescriptor`. Manager will block close and call your confirm delegate.


- **BladeCommandBar** with simple command model.
- **ARM navigation pre-check**: if closing blades would lose unsaved changes, URL navigation is cancelled and you remain on the current stack until you confirm via the dialog.
- **Width persistence** via `localStorage` keyed by blade key.
