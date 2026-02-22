namespace Nergard.ResourceEditor.Features.Shared.Services;

public interface IXmlMigrationService
{
    bool NeedsMigration();
    Task<MigrationResult> MigrateAsync(IProgress<MigrationProgress>? progress = null);
}

public record MigrationResult(bool Success, int FilesCreated, List<string> Errors);
public record MigrationProgress(string CurrentStep, int Completed, int Total);
