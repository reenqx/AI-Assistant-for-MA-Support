using AIforMAsupport.Api.Models;

namespace AIforMAsupport.Api.Services.SavedCases;

public interface ISavedCaseStore
{
    Task<SavedCase> SaveAsync(
        Guid conversationId,
        string summary,
        IReadOnlyList<string> referencedCaseIds,
        IReadOnlyList<string> knownScripts,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SavedCase>> ListAsync(int limit, CancellationToken cancellationToken = default);
}
