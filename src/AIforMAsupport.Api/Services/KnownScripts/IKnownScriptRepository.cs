namespace AIforMAsupport.Api.Services.KnownScripts;

public interface IKnownScriptRepository
{
    IReadOnlyList<KnownScript> GetAll();
}
