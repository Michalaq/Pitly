using Pitly.Core.Models;

namespace Pitly.Api.Services;

public interface IImportService
{
    Task<ImportResult> ImportStatementsAsync(IReadOnlyList<Stream> fileStreams);
}

public record ImportResult(Guid SessionId, TaxSummary Summary);
