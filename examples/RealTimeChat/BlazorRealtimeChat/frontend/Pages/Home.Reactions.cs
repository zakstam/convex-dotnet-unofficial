using RealtimeChat.Frontend.Components;

namespace RealtimeChat.Frontend.Pages;

public partial class Home
{
    protected void ShowReactionPicker(string messageId)
    {
        State.ShowReactionPickerForMessageId = messageId;
        StateHasChanged();
    }

    protected void HideReactionPicker()
    {
        State.ShowReactionPickerForMessageId = null;
        StateHasChanged();
    }

    protected async Task AddReaction((string messageId, string emoji) args) => await AddReaction(args.messageId, args.emoji);

    protected async Task AddReaction(string messageId, string emoji)
    {
        HideReactionPicker();

        if (string.IsNullOrEmpty(State.Username))
        {
            return;
        }

        await ReactionService.AddReactionAsync(messageId, emoji, State.Username);
    }

    protected async Task ToggleReaction((string messageId, string emoji) args) => await ToggleReaction(args.messageId, args.emoji);

    protected async Task ToggleReaction(string messageId, string emoji)
    {
        if (string.IsNullOrEmpty(State.Username))
        {
            return;
        }

        await ReactionService.ToggleReactionAsync(messageId, emoji, State.Username);
    }

    protected async Task LoadReactions()
    {
        if (State.Messages.Count == 0)
        {
            return;
        }

        var messageIds = State.Messages.Select(m => m.Id).ToList();
        await ReactionService.LoadReactionsAsync(messageIds);
        State.MessageReactions = CloneReactions(ReactionService.MessageReactions);
        StateHasChanged();
    }
}
