namespace RealtimeChat.Frontend.Pages;

public partial class Home
{
    protected void HandleTypingInput()
    {
        if (string.IsNullOrEmpty(State.Username))
        {
            return;
        }

        // Update mention suggestions
        State.ShowMentionSuggestions = State.MessageText.Contains('@');
        StateHasChanged();

        // Handle typing indicator through presence service
        PresenceService.HandleTypingInput(State.MessageText);
    }
}
