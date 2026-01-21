using NWRI.eReferralsService.API.Errors;

namespace NWRI.eReferralsService.API.Exceptions;

public abstract class BaseFhirException : Exception
{
    public abstract IEnumerable<BaseFhirHttpError> Errors { get; }
}


