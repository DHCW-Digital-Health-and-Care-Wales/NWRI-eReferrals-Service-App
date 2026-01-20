using WCCG.eReferralsService.API.Constants;

namespace WCCG.eReferralsService.API.Helpers;

public static class FhirHttpErrorHelper
{
    private static readonly Dictionary<string, string> CodeDisplayDictionary = new()
    {
        { FhirHttpErrorCodes.SenderBadRequest, "400: The API was unable to process the request." },
        { FhirHttpErrorCodes.ReceiverBadRequest, "400: The Receiver was unable to process the request." },
        { FhirHttpErrorCodes.ReceiverServerError, "500: The Receiver has encountered an error processing the request." },
        { FhirHttpErrorCodes.ReceiverUnavailable, "503: The Receiver is currently unavailable." },
        { FhirHttpErrorCodes.TooManyRequests, "429: Too many requests have been made by this source in a given amount of time." },
        { FhirHttpErrorCodes.ReceiverNotFound, "404: The Receiver was unable to find the specified resource." }
    };

    public static string GetDisplayMessageByCode(string code)
    {
        return CodeDisplayDictionary.TryGetValue(code, out var displayMessage)
            ? displayMessage
            : string.Empty;
    }
}
