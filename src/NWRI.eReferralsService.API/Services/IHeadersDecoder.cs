namespace NWRI.eReferralsService.API.Services;

public interface IHeadersDecoder
{
    string? GetDecodedSourceSystem(string? requestingSoftwareHeader);
    string? GetDecodedUserRole(string? requestingPractitionerHeader);
}
