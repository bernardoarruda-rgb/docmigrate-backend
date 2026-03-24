using DocMigrate.Application.DTOs.Reference;
using DocMigrate.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocMigrate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReferencesController(IReferenceService referenceService) : ControllerBase
{
    [HttpPost("check")]
    public async Task<ActionResult<CheckReferencesResponse>> Check(CheckReferencesRequest request)
    {
        var result = await referenceService.CheckAsync(request);
        return Ok(result);
    }
}
