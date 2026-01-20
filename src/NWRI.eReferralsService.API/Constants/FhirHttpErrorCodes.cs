namespace WCCG.eReferralsService.API.Constants;

public static class FhirHttpErrorCodes
{
    public const string SenderBadRequest = "SEND_BAD_REQUEST";
    public const string ReceiverBadRequest = "REC_BAD_REQUEST";
    public const string ReceiverServerError = "REC_SERVER_ERROR";
    public const string ReceiverUnavailable = "REC_UNAVAILABLE";
    public const string TooManyRequests = "TOO_MANY_REQUESTS";
    public const string ReceiverNotFound = "REC_NOT_FOUND";
}
