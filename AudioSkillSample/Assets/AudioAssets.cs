using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AudioSkillSample.Assets
{
    public class AudioAssets
    {
        public static List<AudioItem> GetSampleAudioFiles()
        {
            var returnAudio = new List<AudioItem>();

            returnAudio.Add(new AudioItem()
            {
                Title = "First Audio File",
                Url = "https://matthiasshapiro.com/alexasamples/First.mp3"
            }
            );
            returnAudio.Add(new AudioItem()
            {
                Title = "Second Audio File",
                Url = "https://matthiasshapiro.com/alexasamples/Second.mp3"
            });
            returnAudio.Add(new AudioItem()
            {
                Title = "Third Audio File",
                Url = "https://matthiasshapiro.com/alexasamples/Third.mp3"
            }
            );
            returnAudio.Add(new AudioItem()
            {
                Title = "Fourth Audio File",
                Url = "https://matthiasshapiro.com/alexasamples/Fourth.mp3"
            });
            returnAudio.Add(new AudioItem()
            {
                Title = "Fifth Audio File",
                Url = "https://matthiasshapiro.com/alexasamples/Fifth.mp3"
            }
            );

            return returnAudio;
        }
    }

    public class AudioItem
    {
        public string Title { get; set; }
        public string Url { get; set; }
    }
}
