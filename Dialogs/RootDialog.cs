using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Sample.LuisBot;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LuisBot.Dialogs
{
    [Serializable]
    public class RootDialog: IDialog<object>
    {
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;
            await context.Forward(new BasicLuisDialog(), ResumeAftelLuisDialog, activity, CancellationToken.None);
        }

        private async Task ResumeAftelLuisDialog(IDialogContext context, IAwaitable<object> result)
        {
            PromptDialog.Confirm(context, ConfirmEndConversation, "Do you have any other queries?");
        }

        private async Task ConfirmEndConversation(IDialogContext context, IAwaitable<bool> result)
        {
            var confirmation = await result;
            if (!confirmation)
            {
                await context.PostAsync("Okay. Have a wonderful day ahead. :)");
            }
            else
            {
                await context.PostAsync("How can I help you? ");
                context.Wait(MessageReceivedAsync);
            }
        }
    }
}