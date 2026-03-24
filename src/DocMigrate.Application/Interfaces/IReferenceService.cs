using DocMigrate.Application.DTOs.Reference;

namespace DocMigrate.Application.Interfaces;

public interface IReferenceService
{
    Task<CheckReferencesResponse> CheckAsync(CheckReferencesRequest request);
}
