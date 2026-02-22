# Nergard.ResourceEditor

A Blazor-based localization management tool for Optimizely CMS 12. Edit content type names, property captions, tab names, editor hints and view translations — all from a visual editor instead of manually editing XML files.

## Features

- **Content Type Editor** — Manage names and descriptions for pages, blocks and media across all languages
- **Property Editor** — Edit captions and help text, with shared property detection
- **Tab Editor** — Localize property group (tab) names
- **View/Frontend Translations** — Edit multi-language view translation files with a tree-based UI
- **Language Overview** — See translation completeness per language and work through missing translations
- **Runtime Overrides** — Apply translation changes via DDS without redeploying (ideal for DXP)
- **Override Manager** — Import/export CSV, migrate overrides to XML
- **Automated Translation** — Optional DeepL integration at field, item and bulk level
- **XML Migration** — Guided migration from legacy XML format

## Quick Start

```csharp
services.AddServerSideBlazor();
services.AddMudServices();
services.AddResourceEditor(configuration);
```

See the [full blog post](docs/blogpost.md) for detailed setup instructions, configuration options and screenshots.

<!-- TODO: Add link to published blog post -->

## Configuration

Configure via `appsettings.json` under the `ResourceEditor` section:

| Option | Default | Description |
|--------|---------|-------------|
| `TranslationFolder` | `Resources/Translations` | Path to translation XML files |
| `EnableFileSaving` | `true` | Allow saving to XML files (set to `false` on DXP) |
| `EnableOverrides` | `true` | Enable DDS-based runtime overrides |
| `ShowOverridesUI` | `true` | Show inline override buttons in editors |
| `EnableAutomatedTranslation` | `false` | Enable automated translation features |
| `ViewFilePattern` | `views_*.xml` | Glob pattern for view translation files |

## Requirements

- .NET 8.0
- Optimizely CMS 12
- MudBlazor 8.x

## License

Apache 2.0 — see [LICENSE](LICENSE)
