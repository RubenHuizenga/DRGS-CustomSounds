using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using CustomSounds.Patchers;
using HarmonyLib;
using Il2CppSystem.Text.RegularExpressions;
using UnityEngine;
using FundayFactory.Audio.Data;
using NAudio.Wave;

namespace CustomSounds
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class CustomSounds : BasePlugin
    {
        public new static ManualLogSource Log;
        public static Dictionary<string, List<AudioSampleClip>> CustomSoundsReplace;

        public override void Load()
        {
            Log = base.Log;

            Harmony.CreateAndPatchAll(typeof(AudioSourceObjectPatcher));

            Log.Log(LogLevel.Message, $"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            CustomSoundsReplace = GetCustomAudioClips("Replace");
        }

        private static Dictionary<string, List<AudioSampleClip>> GetCustomAudioClips(string subPath)
        {
            var customSoundsPath = $@"{Paths.PluginPath}\CustomSounds\{subPath}";
            var result = new Dictionary<string, List<AudioSampleClip>>();

            if (Directory.Exists(customSoundsPath))
            {
                Log.Log(LogLevel.Info, $"Found folder {customSoundsPath}");

                var wavFiles = Directory.GetFiles(customSoundsPath, "*.wav");

                if (wavFiles.Length == 0)
                {
                    Log.Log(LogLevel.Warning, $"Could not find any .wav files in {customSoundsPath}");
                    return result;
                }

                Log.Log(LogLevel.Info, $"Found {wavFiles.Length} .wav files in {customSoundsPath}");

                foreach (var filePath in wavFiles)
                {
                    Log.Log(LogLevel.Debug, $"Processing file {filePath}");

                    var fileKey = GetSoundKey(Path.GetFileNameWithoutExtension(filePath));

                    var audioClip = LoadAudioClipFromWav(filePath, fileKey);

                    if (!result.ContainsKey(fileKey))
                    {
                        Log.Log(LogLevel.Message, $"Added new key '{fileKey}'");
                        result.Add(fileKey, new List<AudioSampleClip>());
                    }

                    Log.Log(LogLevel.Info, $"Linked file '{filePath}' to key '{fileKey}'");
                    result[fileKey].Add(audioClip);
                }

                return result;
            }

            Log.Log(LogLevel.Fatal, $"Directory '{customSoundsPath}' does not exist");
            return result;
        }

        /// <summary>
        /// Gets the Key from an input string. This can either be a <see cref="AudioSampleGroup.id"/> or a filepath (without extention)
        /// Will trim any hierarchy and index numbers. e.g. "SFX/ui_button_hover_01" will turn into "ui_button_hover"
        /// </summary>
        public static string GetSoundKey(string input)
        {
            input = input.Split('/').Last();
            return Regex.Replace(input, @"_\d*$", "");
        }

        public static AudioSampleClip LoadAudioClipFromWav(string path, string key)
        {
            using var audioFile = new AudioFileReader(path);

            var audioData = new float[audioFile.Length / 4]; // Assuming 16-bit PCM
            var bytesRead = audioFile.Read(audioData, 0, audioData.Length);

            var audioClip = AudioClip.Create(key, bytesRead, audioFile.WaveFormat.Channels, audioFile.WaveFormat.SampleRate, false);
            audioClip.SetData(audioData, 0);

            return new AudioSampleClip() { clip = audioClip };
        }
    }
}