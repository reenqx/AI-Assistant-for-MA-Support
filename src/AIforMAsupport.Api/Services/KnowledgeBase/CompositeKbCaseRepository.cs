using AIforMAsupport.Api.Models;

namespace AIforMAsupport.Api.Services.KnowledgeBase;

// The IKbCaseRepository actually registered in Program.cs: real Mantis cases (CsvKbCaseRepository,
// loaded once at startup, cached) plus whatever the team has added via IKbAdditionStore (queried
// live from SQLite on every call, since that pool can grow while the API is running). Nothing else
// in the search pipeline (KeywordSearchKbContextProvider) needs to know two sources exist - it just
// calls GetAll() like before.
public sealed class CompositeKbCaseRepository : IKbCaseRepository
{
    private readonly CsvKbCaseRepository _csvRepository;
    private readonly IKbAdditionStore _additionStore;

    public CompositeKbCaseRepository(CsvKbCaseRepository csvRepository, IKbAdditionStore additionStore)
    {
        _csvRepository = csvRepository;
        _additionStore = additionStore;
    }

    public IReadOnlyList<KbCase> GetAll() =>
        _csvRepository.GetAll().Concat(_additionStore.GetAll()).ToList();
}
