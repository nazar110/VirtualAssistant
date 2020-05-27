using System;
using System.Threading.Tasks;
using VirtualAssistant.Services;
using VirtualAssistant.Helpers;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Logging;
using System.Threading;
using Microsoft.Bot.Schema;

namespace VirtualAssistant.Bots
{
    public class DialogBot<T> : ActivityHandler where T : Dialog
    {
        #region Variables
        protected readonly Dialog dialog;
        protected readonly BotStateService botStateService;
        protected readonly ILogger logger;
        #endregion

        // T dialog - we need to instanciate it in Startup.cs (method ConfigureDialogs)
        public DialogBot(BotStateService _botStateService, T _dialog, ILogger<DialogBot<T>> _logger)
        {
            botStateService = _botStateService ?? throw new ArgumentNullException(nameof(_botStateService));
            dialog = _dialog ?? throw new ArgumentNullException(nameof(_dialog));
            logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes that might have occured during the turn.
            await botStateService.UserState.SaveChangesAsync(turnContext, false, cancellationToken);
            await botStateService.ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            logger.LogInformation("Running dialog with Message Activity.");

            // Run the Dialog with the new message Activity.
            await dialog.Run(turnContext, botStateService.DialogStateAccessor, cancellationToken);
        }
    }
}
