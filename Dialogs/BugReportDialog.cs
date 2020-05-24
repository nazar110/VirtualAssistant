using VirtualAssistant.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtualAssistant.Models;
using System.Text.RegularExpressions;

namespace VirtualAssistant.Dialogs
{
    public class BugReportDialog : ComponentDialog
    {
        #region Variables
        private readonly BotStateService botStateService;
        #endregion

        public BugReportDialog(string dialogId, BotStateService _botStateService) : base(dialogId)
        {
            botStateService = _botStateService ?? throw new ArgumentNullException(nameof(botStateService));

            InitializeWaterfallDialog();
        }

        private void InitializeWaterfallDialog()
        {
            // Create Waterfall steps. What kind of information you want to ask for
            var waterfallSteps = new WaterfallStep[]
            {
                DescriptionStepAsync,
                CallbackTimeStepAsync,
                PhoneNumberStepAsync,
                BugStepAsync,
                SummaryStepAsync // our last step
            };

            // Add Named Dialogs. Our subdialogs
            AddDialog(new WaterfallDialog($"{nameof(BugReportDialog)}.mainFlow", waterfallSteps)); // first - our main flow, our Waterfall dialog, holds all questions
            AddDialog(new TextPrompt($"{nameof(BugReportDialog)}.description"));
            AddDialog(new DateTimePrompt($"{nameof(BugReportDialog)}.callbackTime", CallBackTimeValidatorAsync)); // method for validation of user's answer
            AddDialog(new TextPrompt($"{nameof(BugReportDialog)}.phoneNumber", PhoneNumberValidatorAsync));
            AddDialog(new TextPrompt($"{nameof(BugReportDialog)}.bug"));

            // Set the starting Dialog
            InitialDialogId = $"{nameof(BugReportDialog)}.mainFlow";
        }
        #region Waterfall Steps
        private async Task<DialogTurnResult> DescriptionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync($"{nameof(BugReportDialog)}.description",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Enter a description for your report")
                }, cancellationToken);
        }
        private async Task<DialogTurnResult> CallbackTimeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // stepContext is a bag
            stepContext.Values["description"] = (string)stepContext.Result; // remember the result from the previous waterfall step

            // we are calling our next prompt with id 'callbackTime' and putting Prompt, RetryPrompt (RP is activated when the user value was invalid)
            return await stepContext.PromptAsync($"{nameof(BugReportDialog)}.callbackTime",
            new PromptOptions
            {
                Prompt = MessageFactory.Text("Please enter in a callback time"),
                RetryPrompt = MessageFactory.Text("The value entered must be between the hours of 9 am and 5 pm."),
            }, cancellationToken);
        }
        private async Task<DialogTurnResult> PhoneNumberStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["callbackTime"] = Convert.ToDateTime(((List<DateTimeResolution>)stepContext.Result).FirstOrDefault().Value); // remember the result from the previous waterfall step - our callback time, converted in need type

            return await stepContext.PromptAsync($"{nameof(BugReportDialog)}.phoneNumber",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Please enter in a phone number that we can call you back at"),
                    RetryPrompt = MessageFactory.Text("Please enter a valid phone number"),
                },
                cancellationToken);
        }
        private async Task<DialogTurnResult> BugStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["phoneNumber"] = (string)stepContext.Result; // remember typed in phoneNumber

            return await stepContext.PromptAsync($"{nameof(BugReportDialog)}.bug",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Please enter the type of the bug."),
                    // RetryPrompt = MessageFactory.Text("Sorry, you entered incorrect variant. Please, enter again"),
                    Choices = ChoiceFactory.ToChoices(new List<string> { "Security", "Crash", "Power", "Perfomance", "Usability", "Serious Bug", "Other" }),
                }, cancellationToken);
        }
        // our last step
        private async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // var temp = ((FoundChoice)stepContext.Result).Value;
            // stepContext.Values["bug"] = ((FoundChoice)stepContext.Result).Value;
            stepContext.Values["bug"] = (string)stepContext.Result; // (stepContext.Result as FoundChoice).Value;

            // Get the current profile object from user state.
            var userProfile = await botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);

            // Save all of the data inside the user profile
            userProfile.Description = (string)stepContext.Values["description"];
            userProfile.CallbackTime = (DateTime)stepContext.Values["callbackTime"];
            userProfile.PhoneNumber = (string)stepContext.Values["phoneNumber"];
            userProfile.Bug = (string)stepContext.Values["bug"];

            // Show the summary to the user
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Here is a summary of your bug report"), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Description: {userProfile.Description}"), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Callback Time: {userProfile.CallbackTime}"), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Phone Number: {userProfile.PhoneNumber}"), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Bug: {userProfile.Bug}"), cancellationToken);

            // Save data in userstate
            await botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);

            // WaterfallStep always finishes with the end of the Waterfall or with another dialog, here it is the end.
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
        #endregion

        #region Validators
        private Task<bool> CallBackTimeValidatorAsync(PromptValidatorContext<IList<DateTimeResolution>> promptContext, CancellationToken cancellationToken)
        {
            var valid = false;

            if (promptContext.Recognized.Succeeded)
            {
                var resolution = promptContext.Recognized.Value.First();
                DateTime selectedDate = Convert.ToDateTime(resolution.Value);
                TimeSpan start = new TimeSpan(9, 0, 0); // 10 o'clock
                TimeSpan end = new TimeSpan(17, 0, 0); // 12 o'clock
                if ((selectedDate.TimeOfDay >= start) && (selectedDate.TimeOfDay <= end))
                {
                    valid = true;
                }
            }
            return Task.FromResult(valid);
        }
        private Task<bool> PhoneNumberValidatorAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            // promptContext - is what the value is that the user put in to the prompt

            var valid = false;

            // The Prompt successfuly asked the question and the user successfuly put in some kind of answer
            if (promptContext.Recognized.Succeeded)
            {
                valid = Regex.Match(promptContext.Recognized.Value, @"^(\+\d{1,2}\s)?\(?\d{3}\)?[\s.-]?\d{3}[\s.-]?\d{4}$").Success;
            }
            return Task.FromResult(valid);
        }
        #endregion
    }
}
