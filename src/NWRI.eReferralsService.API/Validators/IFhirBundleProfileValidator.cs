using Hl7.Fhir.Model;
using Task = System.Threading.Tasks.Task;

namespace NWRI.eReferralsService.API.Validators;

public interface IFhirBundleProfileValidator
{
    Task<ProfileValidationOutput> ValidateAsync(Bundle bundle, CancellationToken cancellationToken = default);
    Task InitializeAsync(CancellationToken cancellationToken = default);

    bool IsInitialized { get; }
    bool IsReady { get; }
}
