using AIforMAsupport.Api.Models;

namespace AIforMAsupport.Api.Services.KnowledgeBase;

public interface IKbCaseRepository
{
    IReadOnlyList<KbCase> GetAll();
}
