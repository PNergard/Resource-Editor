using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nergard.ResourceEditor.Features.ContentTypes.Services;
using Nergard.ResourceEditor.Features.DisplayChannels.Services;
using Nergard.ResourceEditor.Features.EditorHints.Services;
using Nergard.ResourceEditor.Features.Overrides.Services;
using Nergard.ResourceEditor.Features.Views.Services;
using Nergard.ResourceEditor.Features.Shared.Models;
using Nergard.ResourceEditor.Features.Shared.Services;
using Nergard.ResourceEditor.Features.Tabs.Services;

namespace Nergard.ResourceEditor;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Resource Editor services with configuration from appsettings.json.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration to bind ResourceEditor section from.</param>
    /// <param name="configureOptions">Optional additional configuration action.</param>
    public static IServiceCollection AddResourceEditor(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<ResourceEditorOptions>? configureOptions = null)
    {
        // Bind from configuration section "ResourceEditor"
        services.Configure<ResourceEditorOptions>(configuration.GetSection("ResourceEditor"));

        // Apply additional configuration if provided
        if (configureOptions != null)
        {
            services.PostConfigure(configureOptions);
        }

        return AddResourceEditorServices(services);
    }

    /// <summary>
    /// Adds Resource Editor services with programmatic configuration only.
    /// Uses default values for all options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional configuration action.</param>
    public static IServiceCollection AddResourceEditor(
        this IServiceCollection services,
        Action<ResourceEditorOptions>? configureOptions = null)
    {
        services.Configure<ResourceEditorOptions>(options =>
        {
            configureOptions?.Invoke(options);
        });

        return AddResourceEditorServices(services);
    }

    /// <summary>
    /// Registers the built-in DeepL translation service.
    /// Call before AddResourceEditor() so TryAddScoped won't overwrite it.
    /// Binds DeepL options from the "ResourceEditor:DeepL" configuration section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration root.</param>
    public static IServiceCollection AddDeepLTranslation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DeepLOptions>(configuration.GetSection("ResourceEditor:DeepL"));
        services.AddScoped<ITranslationService, DeepLTranslationService>();
        services.AddHttpClient("DeepL");
        return services;
    }

    private static IServiceCollection AddResourceEditorServices(IServiceCollection services)
    {
        services.AddScoped<ILanguageService, LanguageService>();
        services.AddScoped<IContentTypeLocalizationService, ContentTypeLocalizationService>();
        services.AddScoped<ISharedPropertyService, SharedPropertyService>();
        services.AddScoped<ITabLocalizationService, TabLocalizationService>();
        services.AddScoped<IDisplayLocalizationService, DisplayLocalizationService>();
        services.AddScoped<IEditorHintLocalizationService, EditorHintLocalizationService>();
        services.AddScoped<IViewLocalizationService, ViewLocalizationService>();
        services.AddScoped<IXmlMigrationService, XmlMigrationService>();

        // Override service for DDS-stored localization overrides
        services.AddSingleton<IOverrideService, OverrideService>();

        // TryAdd so consumers can register their own evaluator before calling AddResourceEditor()
        services.TryAddSingleton<ITranslationStatusEvaluator, DefaultTranslationStatusEvaluator>();
        services.AddScoped<ITranslationStatusService, TranslationStatusService>();
        services.Configure<TranslationStatusOptions>(_ => { });

        // Automated translation service - TryAddScoped so consumers can provide their own implementation
        // by registering before calling AddResourceEditor()
        services.TryAddScoped<ITranslationService, NoOpTranslationService>();

        return services;
    }
}
