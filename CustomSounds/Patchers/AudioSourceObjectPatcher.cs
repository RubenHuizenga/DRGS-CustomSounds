using FundayFactory.Audio;
using HarmonyLib;
using System.Linq;
using BepInEx.Logging;
using FundayFactory.Audio.Data;
using System.Collections.Generic;

namespace CustomSounds.Patchers
{
    public class AudioSourceObjectPatcher
    {
        [HarmonyPatch(typeof(AudioSourceObject), "Play", typeof(AudioSampleGroup), typeof(bool))]
        [HarmonyPrefix]
        public static bool Play_Patch(ref AudioSampleGroup sampleGroup)
        {
            var key = CustomSounds.GetSoundKey(sampleGroup.id);

            var replacementSounds = FindReplacementSounds(key);

            if(!replacementSounds.Any())
                return true;

            if (replacementSounds.Any())
                sampleGroup.clips = replacementSounds.ToArray();


            CustomSounds.Log.Log(LogLevel.Message, $"Now playing '{sampleGroup.id}'. Count: {sampleGroup.clips.Count}");

            // Reset the pitch from Random to One
            // Voicelines are still pitchshifted, even with this line
            // sampleGroup.pitch = new IAudioPitch(new AudioOnePitch().Pointer);

            CustomSounds.Log.Log(LogLevel.Debug, $"Now playing '{sampleGroup.id}' using key '{key}'");

            return true;
        }

        private static List<AudioSampleClip> FindReplacementSounds(string id)
        {
            var keys = CustomSounds.CustomSoundsReplace.Keys.Where(id.Equals).ToList();

            if (!keys.Any())
                return new List<AudioSampleClip>();

            if (keys.Count > 1)
            {
                CustomSounds.Log.Log(LogLevel.Warning, $"Found multiple keys for samplegroup '{id}' in Replace group. Using the first one");
                foreach (var key in keys)
                    CustomSounds.Log.Log(LogLevel.Warning, $"    - {key}");
            }

            return CustomSounds.CustomSoundsReplace[keys.First()];
        }

    }
}
