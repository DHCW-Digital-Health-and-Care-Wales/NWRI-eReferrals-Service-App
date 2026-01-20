namespace WCCG.eReferralsService.API.Validators;

public class ProfileValidationOutput
{
    public bool IsSuccessful { get; set; }
    public List<string>? Errors { get; set; }
}
