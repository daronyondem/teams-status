# teams-status
An app to trigger http endpoints based on local Teams status.

The original Teams implementation code comes from the [OnlineStatusLight](https://github.com/miscalencu/OnlineStatusLight/blob/main/OnlineStatusLight.Application/MicrosoftTeamsService.cs) project. The implementation here is simplified into a single script that can be used to trigger http endpoints based on local Teams status. Currently it is configured to trigger an IFTTT WekHook endpoint when the user is available and another when the user is not available.