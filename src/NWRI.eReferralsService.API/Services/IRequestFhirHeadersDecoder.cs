namespace NWRI.eReferralsService.API.Services;

public interface IRequestFhirHeadersDecoder
{
    string? GetDecodedSourceSystem(string? requestingSoftwareHeader);
    string? GetDecodedUserRole(string? requestingPractitionerHeader);
}
