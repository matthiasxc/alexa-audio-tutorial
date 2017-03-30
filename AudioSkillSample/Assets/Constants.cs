using AudioSkillSample.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AudioSkillSample.Assets
{
    public static class Constants
    {
        public static string AppId = "";
        private static string dynamoDBTableName = "AudioStates";

        public static StateMap SetDefaultState()
        {
            StateMap map = new StateMap()
            {
                EnqueuedToken = null,
                Index = 0,
                Loop = true,
                OffsetInMS = 0,
                PlaybackFinished = false,
                PlaybackIndexChanged = false,
                playOrder = new List<int>(),
                Shuffle = false,
                State = "",
                Token = null
            };
            return map;
        }
        public static Dictionary<string, string> States
        {
            get
            {
                var stateReturn = new Dictionary<string, string>();
                stateReturn.Add("StartMode", "");
                stateReturn.Add("PlayMode", "PLAY_MODE");
                stateReturn.Add("ResumeDecisionMode", "RESUME_DECISION_MODE");
                return stateReturn;
            }
        }
    }
}
