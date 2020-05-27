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
using VirtualAssistant.Helpers;

namespace VirtualAssistant.Dialogs
{
    public class SurveyDialog : ComponentDialog
    {
        #region Variables
        private readonly BotStateService botStateService;
        private Answers answers = new Answers();
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
                WithWaterCoolingStepAsync,
                ForWorkWithImagesStepAsync,
                SummaryStepAsync // our last step
            };

            // Add Named Dialogs. Our subdialogs
            AddDialog(new WaterfallDialog($"{nameof(SurveyDialog)}.mainFlow", waterfallSteps)); // first - our main flow, our Waterfall dialog, holds all questions
            AddDialog(new TextPrompt($"{nameof(SurveyDialog)}.multimediaLaptop", AnswerValidatorAsync));
            AddDialog(new TextPrompt($"{nameof(SurveyDialog)}.laptopForGames", AnswerValidatorAsync)); // method for validation of user's answer
            AddDialog(new TextPrompt($"{nameof(SurveyDialog)}.thinScreen", AnswerValidatorAsync));
            AddDialog(new TextPrompt($"{nameof(SurveyDialog)}.nvidiaCard", AnswerValidatorAsync));
            AddDialog(new TextPrompt($"{nameof(SurveyDialog)}.waterCooling", AnswerValidatorAsync));
            AddDialog(new TextPrompt($"{nameof(SurveyDialog)}.workWithImages", AnswerValidatorAsync));

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
            string[] check = {
                (string)stepContext.Values["multimediaLaptop"],
                (string)stepContext.Values["laptopForGames"],
                (string)stepContext.Values["thinScreen"],
            };
            string answer = answers.GetRecommendation(check);
            if (answer != null)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I highly recommend you this model - {answer}."), cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);

            }

            return await stepContext.PromptAsync($"{nameof(SurveyDialog)}.nvidiaCard",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Do you want a laptop with Nvidia video card?"),
                    RetryPrompt = MessageFactory.Text("Please reenter your answer, or just type 'Yes'/'No'"),
                    // Choices = ChoiceFactory.ToChoices(new List<string> { "Security", "Crash", "Power", "Perfomance", "Usability", "Serious Bug", "Other" }),
                }, cancellationToken);
        }

        private async Task<DialogTurnResult> WithWaterCoolingStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["nvidiaCard"] = SetAnswer((string)stepContext.Result);
            string[] check = {
                (string)stepContext.Values["multimediaLaptop"],
                (string)stepContext.Values["laptopForGames"],
                (string)stepContext.Values["thinScreen"],
                (string)stepContext.Values["nvidiaCard"]
            };
            string answer = answers.GetRecommendation(check);
            if (answer != null)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I highly recommend you this model - {answer}."), cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }

            return await stepContext.PromptAsync($"{nameof(SurveyDialog)}.waterCooling",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Do you want a laptop with water cooling?"),
                    RetryPrompt = MessageFactory.Text("Please reenter your answer, or just type 'Yes'/'No'"),
                }, cancellationToken);
        }

        private async Task<DialogTurnResult> ForWorkWithImagesStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["waterCooling"] = SetAnswer((string)stepContext.Result);
            string[] check = {
                (string)stepContext.Values["multimediaLaptop"],
                (string)stepContext.Values["laptopForGames"],
                (string)stepContext.Values["thinScreen"],
                (string)stepContext.Values["nvidiaCard"],
                (string)stepContext.Values["waterCooling"]
            };
            string answer = answers.GetRecommendation(check);
            if (answer != null)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I highly recommend you this model - {answer}."), cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }

            return await stepContext.PromptAsync($"{nameof(SurveyDialog)}.workWithImages",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Do you want a laptop for work with images?"),
                    RetryPrompt = MessageFactory.Text("Please reenter your answer, or just type 'Yes'/'No'"),
                }, cancellationToken);
        }

        // our last step
        private async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["workWithImages"] = SetAnswer((string)stepContext.Result);

            // Get the current profile object from user state.
            var userProfile = await botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);

            // Save all of the data inside the user profile
            userProfile.Multimedia = (string)stepContext.Values["multimediaLaptop"];
            userProfile.ForGames = (string)stepContext.Values["laptopForGames"];
            userProfile.ThinScreen = (string)stepContext.Values["thinScreen"];
            userProfile.NvidiaCard = (string)stepContext.Values["nvidiaCard"];
            userProfile.WithWaterCooling = (string)stepContext.Values["waterCooling"];
            userProfile.ForWorkWithImages = (string)stepContext.Values["workWithImages"];


            // Show the summary to the user
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(
                $"Here is a summary of what you told me."), cancellationToken);
            string answer = $"Multimedia Laptop: {ConvertAnswer(userProfile.Multimedia)}\n\n" +
                $"Laptop for games: {ConvertAnswer(userProfile.ForGames)}\n\n" +
                $"Laptop with thin screen: {ConvertAnswer(userProfile.ThinScreen)}\n\n" +
                $"Laptop with Nvidia video card: {ConvertAnswer(userProfile.NvidiaCard)}\n\n" +
                $"Laptop with water cooling: {ConvertAnswer(userProfile.WithWaterCooling)}\n\n" +
                $"Laptop for work with images: {ConvertAnswer(userProfile.ForWorkWithImages)}\n\n";

            await stepContext.Context.SendActivityAsync(MessageFactory.Text(answer), cancellationToken);

            await stepContext.Context.SendActivityAsync(MessageFactory.Text(
                $"I don't have a suggestion for you. Sorry"), cancellationToken);

            // Save data in userstate
            await botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);

            // WaterfallStep always finishes with the end of the Waterfall or with another dialog, here it is the end.
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
        #endregion

        #region Validators
        private Task<bool> AnswerValidatorAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            // promptContext - is what the value is that the user put in to the prompt

            var valid = false;
            string positiveChoices = "Yes|yes|YES|Yep|yep|Ok|ok|Okay|okay|Go on|go on|Of course|of course|Agreed|agreed|Certainly|certainly|Fine|fine";
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

        private string ConvertAnswer(string pattern)
        {
            if (pattern != null)
            {
                return (pattern == "Y" ? "Yes" : "No");
            }
            return pattern;
        }
        #endregion

    }
}
