using System.Reflection;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Framework.Localization;
using EPiServer.ServiceLocation;
using Nergard.ResourceEditor.Features.Overrides.Providers;
using Nergard.ResourceEditor.Features.Overrides.Services;

namespace Nergard.ResourceEditor.Features.Overrides.Initialization;

/// <summary>
/// Initialization module that registers the override localization provider.
/// This module adds the OverrideLocalizationProvider to the localization service
/// so that DDS-stored overrides take precedence over XML translations.
/// </summary>
[InitializableModule]
[ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
public class LocalizationOverrideInitialization : IConfigurableModule
{
    private OverrideLocalizationProvider? _provider;

    public void ConfigureContainer(ServiceConfigurationContext context)
    {
        // Services are registered via ServiceCollectionExtensions.AddResourceEditor()
    }

    public void Initialize(InitializationEngine context)
    {
        var overrideService = context.Locate.Advanced.GetService(typeof(IOverrideService)) as IOverrideService;
        if (overrideService == null)
        {
            // ResourceEditor services not registered — skip override provider
            return;
        }

        var localizationService = context.Locate.Advanced.GetInstance<LocalizationService>();

        // Create the override provider
        _provider = new OverrideLocalizationProvider(overrideService);

        // Add provider using reflection since Providers property might not be public
        TryAddProvider(localizationService, _provider);
    }

    public void Uninitialize(InitializationEngine context)
    {
        if (_provider != null)
        {
            var localizationService = context.Locate.Advanced.GetInstance<LocalizationService>();
            TryRemoveProvider(localizationService, _provider);
            _provider = null;
        }
    }

    private static void TryAddProvider(LocalizationService service, LocalizationProvider provider)
    {
        // Try to get the _providers field via reflection
        var providersField = service.GetType()
            .GetField("_providers", BindingFlags.Instance | BindingFlags.NonPublic);

        if (providersField?.GetValue(service) is IList<LocalizationProvider> providers)
        {
            providers.Insert(0, provider);
            return;
        }

        // Try Providers property (might be internal)
        var providersProperty = service.GetType()
            .GetProperty("Providers", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (providersProperty?.GetValue(service) is IList<LocalizationProvider> providersList)
        {
            providersList.Insert(0, provider);
        }
    }

    private static void TryRemoveProvider(LocalizationService service, LocalizationProvider provider)
    {
        var providersField = service.GetType()
            .GetField("_providers", BindingFlags.Instance | BindingFlags.NonPublic);

        if (providersField?.GetValue(service) is IList<LocalizationProvider> providers)
        {
            providers.Remove(provider);
            return;
        }

        var providersProperty = service.GetType()
            .GetProperty("Providers", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (providersProperty?.GetValue(service) is IList<LocalizationProvider> providersList)
        {
            providersList.Remove(provider);
        }
    }
}
