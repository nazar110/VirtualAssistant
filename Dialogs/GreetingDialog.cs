using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtualAssistant.Services;
using System.Threading;
using VirtualAssistant.Models;
using Microsoft.Bot.Builder;

namespace VirtualAssistant.Dialogs
{
    public class GreetingDialog : ComponentDialog
    {
        #region Variables 
        private readonly BotStateService botStateService;
        #endregion
        public GreetingDialog(string dialogId, BotStateService _botStateService) : base(dialogId)
        {
            botStateService = _botStateService ?? throw new ArgumentNullException(nameof(botStateService));

            InitializeWaterfallDialog();
        }
        private void InitializeWaterfallDialog()
        {
            // Create Waterfall steps
            var waterfallSteps = new WaterfallStep[]
            {
                InitialStepAsync,
                FinalStepAsync
            };

            // Add Named Dialogs
            AddDialog(new WaterfallDialog($"{nameof(GreetingDialog)}.mainFlow", waterfallSteps));
            AddDialog(new TextPrompt($"{nameof(GreetingDialog)}.name"));

            // Set the starting Dialog
            InitialDialogId = $"{nameof(GreetingDialog)}.mainFlow"; ;
        }
        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            UserProfile userProfile = await botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());

            if (string.IsNullOrEmpty(userProfile.Name))
            {
                return await stepContext.PromptAsync($"{nameof(GreetingDialog)}.name",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("What is your name?")
                },
                cancellationToken);
            }
            else
            {
                return await stepContext.NextAsync(null, cancellationToken);
            }
        }
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            UserProfile userProfile = await botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());

            if (string.IsNullOrEmpty(userProfile.Name))
            {
                // Set the name
                userProfile.Name = (string)stepContext.Result;

                // Save any state that might have occured during the turn.
                await botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);
            }
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Hi, {userProfile.Name}. How can I help you?"), cancellationToken);
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

    }
}
