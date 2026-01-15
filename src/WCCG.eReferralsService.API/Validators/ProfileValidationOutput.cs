namespace WCCG.eReferralsService.API.Validators;

public class ProfileValidationOutput
{
    public bool IsSuccessful { get; set; }
    public IList<string>? Errors { get; set; }
}
