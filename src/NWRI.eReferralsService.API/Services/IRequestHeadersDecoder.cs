namespace NWRI.eReferralsService.API.Services;

public interface IRequestHeadersDecoder
{
    string? GetDecodedSourceSystem(string? requestingSoftwareHeader);
    string? GetDecodedUserRole(string? requestingPractitionerHeader);
}
