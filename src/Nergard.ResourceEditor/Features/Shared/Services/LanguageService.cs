using EPiServer.DataAbstraction;
using Nergard.ResourceEditor.Features.Shared.Models;

namespace Nergard.ResourceEditor.Features.Shared.Services;

public class LanguageService(ILanguageBranchRepository languageBranchRepository) : ILanguageService
{
    public IReadOnlyList<LanguageInfo> GetLanguages()
    {
        var branches = languageBranchRepository.ListEnabled();
        var defaultBranch = FindDefaultBranch(branches);
        var defaultId = defaultBranch?.LanguageID ?? "en";

        return branches
            .Select(b => new LanguageInfo(
                b.LanguageID,
                b.Name,
                b.LanguageID.Equals(defaultId, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(l => l.IsDefault)
            .ThenBy(l => l.Name)
            .ToList();
    }

    public LanguageInfo GetDefaultLanguage()
    {
        var branches = languageBranchRepository.ListEnabled();
        var masterBranch = FindDefaultBranch(branches);

        return masterBranch != null
            ? new LanguageInfo(masterBranch.LanguageID, masterBranch.Name, true)
            : new LanguageInfo("en", "English", true);
    }

    private static LanguageBranch? FindDefaultBranch(IEnumerable<LanguageBranch> branches)
    {
        var branchList = branches as IList<LanguageBranch> ?? branches.ToList();

        return branchList
            .FirstOrDefault(b => b.LanguageID.Equals("en", StringComparison.OrdinalIgnoreCase))
            ?? branchList.FirstOrDefault();
    }
}
