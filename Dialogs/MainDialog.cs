using VirtualAssistant.Services;
using VirtualAssistant.Models;
using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using System.Text.RegularExpressions;

namespace VirtualAssistant.Dialogs
{
    // MainDialog is needed to tie GreetingDialog and BugReportDialog together (to tie all other dialogs together)
    // goal is - to decide which subsequent dialog we are to call
    public class MainDialog : ComponentDialog
    {
        #region Variables
        private readonly BotStateService botStateService;
        #endregion

        public MainDialog(BotStateService _botStateService) : base(nameof(MainDialog))
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

            // Add Named Dialogs. Our Component dialogs
            AddDialog(new GreetingDialog($"{nameof(MainDialog)}.greeting", botStateService));
            AddDialog(new SurveyDialog($"{nameof(MainDialog)}.survey", botStateService));

            AddDialog(new WaterfallDialog($"{nameof(GreetingDialog)}.mainFlow", waterfallSteps));

            // Set the starting Dialog
            InitialDialogId = $"{nameof(GreetingDialog)}.mainFlow"; ;
        }
        // We created a way to call the dialogs
        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (Regex.Match(stepContext.Context.Activity.Text.ToLower(), "hi").Success)
            {
                return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.greeting", null, cancellationToken);
            }
            else
            {
                return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.survey", null, cancellationToken);
            }
        }
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }
}

