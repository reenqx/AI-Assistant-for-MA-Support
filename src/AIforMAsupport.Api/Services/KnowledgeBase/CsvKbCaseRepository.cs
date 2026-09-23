using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using AIforMAsupport.Api.Models;
using Microsoft.Extensions.Options;

namespace AIforMAsupport.Api.Services.KnowledgeBase;

// Loads the whole KB CSV into memory once at startup. At 2,180 rows / ~4MB this comfortably
// fits in RAM, so there's no need for a database for this dataset size (see CLAUDE.md scope).
// To pick up new/updated cases, replace the CSV file and restart the API.
//
// This is the real-Mantis-cases-only half of the KB; CompositeKbCaseRepository is what's actually
// registered as IKbCaseRepository - it merges this with IKbAdditionStore's team-added cases. Kept
// as its own class (not folded into the composite) so this file, and the CSV it owns, never has to
// change to support the team-added feature.
public sealed class CsvKbCaseRepository : IKbCaseRepository
{
    private readonly IReadOnlyList<KbCase> _cases;

    public CsvKbCaseRepository(IOptions<KbDataOptions> options, ILogger<CsvKbCaseRepository> logger)
    {
        var path = Path.GetFullPath(options.Value.CsvFilePath);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"KB CSV file not found: {path}", path);
        }

        _cases = Load(path, logger);
        logger.LogInformation("Loaded {Count} KB cases into memory from {Path}", _cases.Count, path);
    }

    public IReadOnlyList<KbCase> GetAll() => _cases;

    private static IReadOnlyList<KbCase> Load(string path, ILogger logger)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            BadDataFound = null,
        };

        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, config);

        var cases = new List<KbCase>();
        var skipped = 0;

        csv.Read();
        csv.ReadHeader();
        while (csv.Read())
        {
            try
            {
                var record = csv.GetRecord<KbCaseCsvRecord>();
                cases.Add(new KbCase(
                    record.Id,
                    record.BsModule,
                    record.SubCategory,
                    record.CaseType,
                    record.Summary,
                    record.Description,
                    record.StepsToReproduce,
                    record.AdditionalInformation,
                    record.Notes,
                    record.KbContent));
            }
            catch (Exception ex)
            {
                skipped++;
                logger.LogWarning(ex, "Skipped malformed KB CSV row {RowNumber}", csv.Context.Parser?.Row);
            }
        }

        if (skipped > 0)
        {
            logger.LogWarning("Skipped {Skipped} malformed KB CSV rows", skipped);
        }

        return cases;
    }
}
