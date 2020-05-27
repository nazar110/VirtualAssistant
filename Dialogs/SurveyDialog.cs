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
    public class SurveyDialog : ComponentDialog
    {
        #region Variables
        private readonly BotStateService botStateService;
        #endregion

        public SurveyDialog(string dialogId, BotStateService _botStateService) : base(dialogId)
        {
            botStateService = _botStateService ?? throw new ArgumentNullException(nameof(botStateService));

            InitializeWaterfallDialog();
        }

        private void InitializeWaterfallDialog()
        {
            // Create Waterfall steps. What kind of information you want to ask for
            var waterfallSteps = new WaterfallStep[]
            {
                MultimediaLaptopStepAsync,
                LaptopForGamesStepAsync,
                WithThinScreenStepAsync,
                WithNvidiaCardStepAsync,
                SummaryStepAsync // our last step
            };

            // Add Named Dialogs. Our subdialogs
            AddDialog(new WaterfallDialog($"{nameof(SurveyDialog)}.mainFlow", waterfallSteps)); // first - our main flow, our Waterfall dialog, holds all questions
            AddDialog(new TextPrompt($"{nameof(SurveyDialog)}.multimediaLaptop", AnswerValidatorAsync));
            AddDialog(new TextPrompt($"{nameof(SurveyDialog)}.laptopForGames", AnswerValidatorAsync)); // method for validation of user's answer
            AddDialog(new TextPrompt($"{nameof(SurveyDialog)}.thinScreen", AnswerValidatorAsync));
            AddDialog(new TextPrompt($"{nameof(SurveyDialog)}.nvidiaCard", AnswerValidatorAsync));

            // Set the starting Dialog
            InitialDialogId = $"{nameof(SurveyDialog)}.mainFlow";
        }
        #region Waterfall Steps
        private async Task<DialogTurnResult> MultimediaLaptopStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync($"{nameof(SurveyDialog)}.multimediaLaptop",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Do you want a multimedia laptop?"),
                    RetryPrompt = MessageFactory.Text("Please reenter your answer, or just type 'Yes'/'No'")
                }, cancellationToken);
        }
        private async Task<DialogTurnResult> LaptopForGamesStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // stepContext is a bag
            stepContext.Values["multimediaLaptop"] = SetAnswer((string)stepContext.Result); // remember the result from the previous waterfall step

            // we are calling our next prompt with id 'laptopForGames' and putting Prompt, RetryPrompt (RP is activated when the user value was invalid)
            return await stepContext.PromptAsync($"{nameof(SurveyDialog)}.laptopForGames",
            new PromptOptions
            {
                Prompt = MessageFactory.Text("Do you want a laptop for games?"),
                RetryPrompt = MessageFactory.Text("Please reenter your answer, or just type 'Yes'/'No'"),
            }, cancellationToken);
        }
        private async Task<DialogTurnResult> WithThinScreenStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["laptopForGames"] = SetAnswer((string)stepContext.Result);

            return await stepContext.PromptAsync($"{nameof(SurveyDialog)}.thinScreen",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Do you want a thin laptop"),
                    RetryPrompt = MessageFactory.Text("Please reenter your answer, or just type 'Yes'/'No'"),
                },
                cancellationToken);
        }
        private async Task<DialogTurnResult> WithNvidiaCardStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["thinScreen"] = SetAnswer((string)stepContext.Result);

            return await stepContext.PromptAsync($"{nameof(SurveyDialog)}.nvidiaCard",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Do you want a laptop with Nvidia video card?"),
                    RetryPrompt = MessageFactory.Text("Please reenter your answer, or just type 'Yes'/'No'"),
                    // Choices = ChoiceFactory.ToChoices(new List<string> { "Security", "Crash", "Power", "Perfomance", "Usability", "Serious Bug", "Other" }),
                }, cancellationToken);
        }
        // our last step
        private async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // stepContext.Values["nvidiaCard"] = ((FoundChoice)stepContext.Result).Value;
            stepContext.Values["nvidiaCard"] = SetAnswer((string)stepContext.Result);

            // Get the current profile object from user state.
            var userProfile = await botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);

            // Save all of the data inside the user profile
            userProfile.Multimedia = (string)stepContext.Values["multimediaLaptop"];
            userProfile.ForGames = (string)stepContext.Values["laptopForGames"];
            userProfile.ThinScreen = (string)stepContext.Values["thinScreen"];
            userProfile.NvidiaCard = (string)stepContext.Values["nvidiaCard"];

            // Show the summary to the user
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Here is a summary of your bug report"), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"multimediaLaptop: {userProfile.Multimedia}"), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"laptopForGames: {userProfile.ForGames}"), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Thin screen: {userProfile.ThinScreen}"), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"nvidiaCard: {userProfile.NvidiaCard}"), cancellationToken);

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
        private Task<bool> AnswerValidatorAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            // promptContext - is what the value is that the user put in to the prompt

            var valid = false;
            string positiveChoices = "Yes|yes|YES|yep|ok|Ok|Okay|okay|Go on|Of course|of course|Agreed|agreed|Certainly|certainly|Fine|fine";
            string negativeChoices = "No|no|NO|Disagreed|disagreed";

            // The Prompt successfuly asked the question and the user successfuly put in some kind of answer
            if (promptContext.Recognized.Succeeded)
            {
                foreach (var item in positiveChoices.Split('|'))
                {
                    if (valid)
                    {
                        break;
                    }
                    valid = promptContext.Recognized.Value.Contains(item);
                }
                if (valid == false)
                {
                    foreach (var item in negativeChoices.Split('|'))
                    {
                        if (valid)
                        {
                            break;
                        }
                        valid = promptContext.Recognized.Value.Contains(item);
                    }
                }
                //valid = Regex.Match(promptContext.Recognized.Value, 
                //    @"(Yes|yes|yep|ok|Ok|Go on|Of course|of course|Agreed|agreed|Certainly|certainly|No|no|)").Success;
            }
            return Task.FromResult(valid);
        }
        #endregion

        #region Functions-helpers
        private string SetAnswer(string pattern)
        {
            string positiveChoices = "Yes|yes|YES|yep|ok|Ok|Okay|okay|Go on|Of course|of course|Agreed|agreed|Certainly|certainly|Fine|fine";
            string negativeChoices = "No|no|NO|Disagreed|disagreed";
            bool valid = false;
            string result = "Error ocurred while geting answer";
            if (pattern != null)
            {
                foreach (var item in positiveChoices.Split('|'))
                {
                    if (valid)
                    {
                        result = "Y";
                        break;
                    }
                    valid = pattern.Contains(item);
                }
                if (valid == false)
                {
                    foreach (var item in negativeChoices.Split('|'))
                    {
                        if (valid)
                        {
                            result = "N";
                            break;
                        }
                        valid = pattern.Contains(item);
                    }
                }
            }
            return result;
        }
        #endregion

    }
}
