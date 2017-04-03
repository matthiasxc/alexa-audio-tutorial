using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization;
using Alexa.NET.Request;
using Alexa.NET.Response;
using Newtonsoft.Json;
using Alexa.NET.Request.Type;
using Alexa.NET;
using Alexa.NET.Response.Directive;
using AudioSkillSample.Assets;
using AudioSkillSample.Helpers;
using AudioSkillSample.Models;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializerAttribute(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace AudioSkillSample
{
    public class Function
    {
        
        public async Task<SkillResponse> FunctionHandler(SkillRequest input, ILambdaContext context)
        {
            var log = context.Logger;
            // I use the following lines to log and validate my input
            //      but this isn't a requirement for the skill
            //log.LogLine($"Skill Request Object...");
            //log.LogLine(JsonConvert.SerializeObject(input));

            SkillResponse returnResponse = new SkillResponse();
            var audioItems = AudioAssets.GetSampleAudioFiles();

            // initialize a connection to the database
            //  this also initialized the context for the DynamoDB helper
            var audioStateHelper = new AudioStateHelper();
            await audioStateHelper.VerifyTable();
            string userId = "";
            if (input.Session != null)
                userId = input.Session.User.UserId;
            else
                userId = input.Context.System.User.UserId;
                        
            var lastState = await audioStateHelper.GetAudioState(userId);

            var currentState = new AudioState() { UserId = userId };
            currentState.State = lastState.State;

            // For an intent 
            if (input.GetRequestType() == typeof(LaunchRequest))
            {
                log.LogLine($"Default LaunchRequest made");
                var output = new PlainTextOutputSpeech()
                    {
                        Text = "Welcome to the Alexa audio sample. " 
                        + "You can say, play the audio, to begin."
                    };
                var reprompt = new Reprompt()
                    {
                        OutputSpeech = new PlainTextOutputSpeech()
                            {
                                Text = "You can say, play the audio, to begin."
                            }
                    };
                returnResponse = ResponseBuilder.Ask(output, reprompt);

                await audioStateHelper.SaveAudioState(currentState);
            }
            else if (input.GetRequestType() == typeof(IntentRequest))
            {
                var intentRequest = (IntentRequest)input.Request;
                var output = new PlainTextOutputSpeech();
                var reprompt = new Reprompt();
                log.LogLine($"Triggered " + intentRequest.Intent.Name);
                switch (intentRequest.Intent.Name)
                {
                    case "PlayAudio":
                        currentState.State.Token = audioItems.FirstOrDefault().Title;
                        currentState.State.State = "PLAY_MODE";
                        currentState.State.Index = 0;
                        currentState.State.playOrder = new List<int> { 0, 1, 2, 3, 4 };
                        returnResponse = ResponseBuilder.AudioPlayerPlay(
                            PlayBehavior.ReplaceAll, 
                            audioItems[currentState.State.Index].Url, 
                            currentState.State.Token);                        
                        
                        break;

                    case BuiltInIntent.Help:
                        output.Text = "You can say, play the audio, to begin.";
                        reprompt.OutputSpeech = new PlainTextOutputSpeech() { Text = "You can say, play the audio, to begin." };
                        returnResponse = ResponseBuilder.Ask(output, reprompt);
                        break;

                    case BuiltInIntent.Cancel:
                        currentState.State.OffsetInMS = Convert.ToInt32(input.Context.AudioPlayer.OffsetInMilliseconds);
                        currentState.State.Token = input.Context.AudioPlayer.Token;
                        currentState.State.State = "PAUSE_MODE";
                        returnResponse = ResponseBuilder.AudioPlayerStop();
                        break;

                    case BuiltInIntent.Next:
                        var thisFile = lastState.State.Token;
                        // get the last state, get the index, add 1 
                        // or start from the beginning if you're doing a loop
                        currentState.State.Index++;
                        if (currentState.State.Index >= audioItems.Count)
                            currentState.State.Index = 0;
                        currentState.State.Token = audioItems[currentState.State.Index].Title;
                        currentState.State.OffsetInMS = 0;
                        currentState.State.State = "PLAY_MODE";
                        returnResponse = ResponseBuilder.AudioPlayerPlay(PlayBehavior.ReplaceAll,
                                                                         audioItems[currentState.State.Index].Url,
                                                                         currentState.State.Token);
                        break;

                    case BuiltInIntent.Previous:
                        // get the last state, get the index, subtract 1
                        currentState.State.Index = currentState.State.Index - 1;
                        if (currentState.State.Index < 0)
                            currentState.State.Index = 0;

                        currentState.State.Token = audioItems[currentState.State.Index].Title;
                        currentState.State.OffsetInMS = 0;
                        currentState.State.State = "PLAY_MODE";
                        returnResponse = ResponseBuilder.AudioPlayerPlay(PlayBehavior.ReplaceAll,
                                                                             audioItems[currentState.State.Index].Url,
                                                                             currentState.State.Token);
                        break;
                    case BuiltInIntent.Repeat:
                        // get the last state, get the index, start over at offset = 0
                        currentState.State.Token = audioItems[currentState.State.Index].Title;
                        currentState.State.OffsetInMS = 0;
                        currentState.State.State = "PLAY_MODE";
                        returnResponse = ResponseBuilder.AudioPlayerPlay(PlayBehavior.ReplaceAll,
                                                                             audioItems[currentState.State.Index].Url,
                                                                             currentState.State.Token, 
                                                                             0);
                        break;

                    case BuiltInIntent.StartOver:
                        // start everything from the beginning
                        currentState.State.Token = audioItems[0].Title;
                        currentState.State.OffsetInMS = 0;
                        currentState.State.State = "PLAY_MODE";
                        returnResponse = ResponseBuilder.AudioPlayerPlay(PlayBehavior.ReplaceAll,
                                                                             audioItems[0].Url,
                                                                             currentState.State.Token,
                                                                             0);
                        break;

                    case BuiltInIntent.Stop:
                        currentState.State.OffsetInMS = Convert.ToInt32(input.Context.AudioPlayer.OffsetInMilliseconds);
                        currentState.State.Token = input.Context.AudioPlayer.Token;
                        currentState.State.State = "PAUSE_MODE";
                        returnResponse = ResponseBuilder.AudioPlayerStop();
                        break;

                    case BuiltInIntent.Resume:
                        // Get the last state, start from the offest in milliseconds

                        returnResponse = ResponseBuilder.AudioPlayerPlay(PlayBehavior.ReplaceAll,
                                                                            audioItems[currentState.State.Index].Url,
                                                                            currentState.State.Token,
                                                                            currentState.State.OffsetInMS);
                        // If there was an enqueued item...
                        if (currentState.State.EnqueuedToken != null)
                        {
                            returnResponse.Response.Directives.Add(new AudioPlayerPlayDirective()
                            {
                                PlayBehavior = PlayBehavior.Enqueue,
                                AudioItem = new Alexa.NET.Response.Directive.AudioItem()
                                {
                                    Stream = new AudioItemStream()
                                    {
                                        Url = audioItems[currentState.State.Index + 1].Url,
                                        Token = audioItems[currentState.State.Index + 1].Title,
                                        ExpectedPreviousToken = currentState.State.Token,
                                        OffsetInMilliseconds = 0
                                    }
                                }
                            });
                        }

                        currentState.State.EnqueuedToken = audioItems[currentState.State.Index + 1].Title;
                        currentState.State.State = "PLAY_MODE";
                        break;

                    case BuiltInIntent.Pause:
                        currentState.State.OffsetInMS = Convert.ToInt32(input.Context.AudioPlayer.OffsetInMilliseconds);
                        currentState.State.Token = input.Context.AudioPlayer.Token;
                        currentState.State.State = "PAUSE_MODE";
                        returnResponse = ResponseBuilder.AudioPlayerStop();
                        break;

                    default:
                        log.LogLine($"Unknown intent: " + intentRequest.Intent.Name);
                        output.Text = "Welcome to Pocast Player";
                        reprompt.OutputSpeech = new PlainTextOutputSpeech() { Text = "This is your reprompt. Please do something." };
                        returnResponse = ResponseBuilder.TellWithReprompt(output, reprompt);
                        break;
                }
            }
            else if (input.GetRequestType() == typeof(AudioPlayerRequest))
            {
                var audioRequest = input.Request as AudioPlayerRequest;

                if (audioRequest.AudioRequestType == AudioRequestType.PlaybackStarted)
                {
                    log.LogLine($"PlaybackStarted Triggered ");
                    // respond with Stop or ClearQueue
                    returnResponse = ResponseBuilder.AudioPlayerClearQueue(ClearBehavior.ClearEnqueued);

                }
                else if (audioRequest.AudioRequestType == AudioRequestType.PlaybackFinished)
                {
                    // Audio comes to an end on its own 
                    log.LogLine($"PlaybackFinished Triggered ");
                    if (currentState.State.EnqueuedToken != null)
                    {
                        int itemIndex = audioItems.IndexOf(audioItems.Where(i => i.Title == currentState.State.EnqueuedToken).FirstOrDefault());
                        currentState.State.Token = audioItems[itemIndex].Title;
                        currentState.State.Index = itemIndex;
                        returnResponse = ResponseBuilder.AudioPlayerPlay(PlayBehavior.ReplaceAll,
                            audioItems[itemIndex].Url,
                            currentState.State.Token);                         
                    }
                    else
                    {
                        // respond with Stop or ClearQueue
                        returnResponse = ResponseBuilder.AudioPlayerClearQueue(ClearBehavior.ClearEnqueued);

                    }
                }
                else if (audioRequest.AudioRequestType == AudioRequestType.PlaybackStopped)
                {
                    // This is when your audio is explicitly stopped
                    log.LogLine($"PlaybackStopped Triggered ");
                    currentState.State.State = "PAUSE_MODE";
                    currentState.State.Token = audioRequest.Token;
                    currentState.State.EnqueuedToken = audioRequest.EnqueuedToken;
                    currentState.State.OffsetInMS = Convert.ToInt32(audioRequest.OffsetInMilliseconds);
                    log.LogLine($"Saving AudioState: " + currentState.State.Token + " at " + currentState.State.OffsetInMS.ToString() + "ms");
                    returnResponse = null;
                }
                else if (audioRequest.AudioRequestType == AudioRequestType.PlaybackNearlyFinished)
                {
                    log.LogLine($"PlaybackNearlyFinished Triggered ");

                    // we'll want to hand back the "next" item in the queue 
                    //  First we check to see if there is an enqueued item and, if there is
                    //  we can respond with nothing 
                    if (audioRequest.HasEnqueuedItem)
                        return null;

                    // let's get the current token
                    var currentPlay = audioRequest.Token;
                    // get the index of that current item
                    int itemIndex = audioItems.IndexOf(audioItems.Where(i => i.Title == audioRequest.Token).FirstOrDefault());
                    if (itemIndex == -1)
                        log.LogLine($"Could not get the index of: " + audioRequest.Token);
                    itemIndex++;
                    if (itemIndex == audioItems.Count)
                        itemIndex = 0;

                    currentState.State.EnqueuedToken = audioItems[itemIndex].Title;
                    currentState.State.Token = audioRequest.Token;
                    // if there is not, we send a play intent with "ENQUEUE"
                    returnResponse = ResponseBuilder.AudioPlayerPlay(
                                            PlayBehavior.Enqueue, 
                                            audioItems[itemIndex].Url, 
                                            currentState.State.EnqueuedToken, 
                                            currentState.State.Token, 
                                            0);
                 
                }
                else if (audioRequest.AudioRequestType == AudioRequestType.PlaybackFailed)
                {
                    log.LogLine($"PlaybackFailed Triggered");
                    // atm, we basically pretend nothing happened and play the first
                    //  file again on a failure
                    //  THIS IS A TERRIBLE SOLUTION
                    //  Figure out a better one for your skill
                    currentState.State.Token = audioItems.FirstOrDefault().Title;
                    currentState.State.Index = 0;
                    currentState.State.State = "PLAY_MODE";
                    returnResponse = ResponseBuilder.AudioPlayerPlay(PlayBehavior.ReplaceAll, audioItems.FirstOrDefault().Url, currentState.State.Token);
                }
            }

            // I use the following code to validate and log my outputs for
            //      later investigation
            //log.LogLine($"Skill Response Object...");
            //string responseJson = "no response is given";
            //try
            //{
            //    responseJson = JsonConvert.SerializeObject(returnResponse);
            //}
            //catch
            //{
            //    log.LogLine(responseJson);
            //    return null;
            //}
            //log.LogLine(responseJson);

            // Save our state
            await audioStateHelper.SaveAudioState(currentState);
            // return our response
            return returnResponse;
        }
    }
}
