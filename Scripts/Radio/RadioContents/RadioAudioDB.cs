using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using AdBlocker.FMOD.Radio.RadioStations;
using AdBlocker.FMOD.LLMCommands;
using AdBlocker.Utils;

namespace AdBlocker.FMOD.Radio.RadioContents {
    public static class RadioAudioDB {
        public const string STREAMING_ASSETS_TO_DB_PATH = "99_FMOD/RadioAudioDB";
        public const string AUDIO_FILE_EXTENSION = ".mp3";

        public static async Task<RadioContent> Query(RadioStation radioStation, LLMCommand llmCommand, System.Action<string> onDBRouteResult = null) {
            if (radioStation == null) {
                string error = "RadioAudioDB: radio station is null";
                Debug.LogWarning(error);
                return new RadioContent(new RadioContent.RCException(error));
            }
            if (llmCommand == null) {
                string error = "RadioAudioDB: llm command is null";
                Debug.LogWarning(error);
                return new RadioContent(new RadioContent.RCException(error));
            }

            // Find out llm command weight for set radio station
            List<WeightedValue<LLMCommand>> radioStationCmds = radioStation.GetLLMCommands(llmCommand.CmdType);
            if (radioStationCmds.Count == 0) {
                string error = $"RadioAudioDB: radio radio station <{radioStation.Name}> does not support command type <{llmCommand.CmdType}>";
                Debug.LogWarning(error);
                return new RadioContent(new RadioContent.RCException(error));
            }
            float cmdWeight = 100f;
            foreach (var cmdWV in radioStationCmds) {
                if (cmdWV.value == llmCommand) {
                    cmdWeight = cmdWV.weight;
                    break;
                }
            }

            string gameStateRoute = "";
            if (llmCommand.CtxGameStateValueTypes.Count > 0) gameStateRoute = FMODManagers.GameStateExtractor.GetDBRoute(0);

            string dbRoute = GetDBRoute(radioStation, llmCommand.CmdType, cmdWeight, gameStateRoute);
            onDBRouteResult?.Invoke(dbRoute);

            return await Query(dbRoute, llmCommand);
        }

        public static async Task<RadioContent> Query(string dbRoute, LLMCommand llmCommand) {
            string folderPath = DBRouteToFolderPath(dbRoute);
            if (!Directory.Exists(folderPath)) {
                string error = $"RadioAudioDB: folder path does not exist: {folderPath}";
                Debug.LogWarning(error);
                return new RadioContent(new RadioContent.RCException(error));
            }
            int fileCount = Directory.GetFiles(folderPath, $"*{AUDIO_FILE_EXTENSION}").Length;

            int numSections = llmCommand.ContentType == RadioContentType.Ads ? 2 : 1;
            List<Task<byte[]>> mp3DataTasks = new();
            while (numSections-- > 0) {
                mp3DataTasks.Add(File.ReadAllBytesAsync(DBRouteToFilePath(dbRoute, Random.Range(0, fileCount))));
            }
            List<byte[]> audioContentSections = new(await Task.WhenAll(mp3DataTasks));

            return new RadioContent(llmCommand.ContentType, (byte)audioContentSections.Count, audioContentSections);
        }

        public static string GetDBRoute(RadioStation radioStation, LLMCommandType cmdType, float cmdWeight, string gameStateRoute) {
            return $"{radioStation.Name.ToLower().Replace(' ', '_')}/{cmdType.ToString().ToLower()}${cmdWeight}{gameStateRoute}";
        }

        public static string DBRouteToFolderPath(string dbRoute) {
            return Path.Combine(Application.streamingAssetsPath, STREAMING_ASSETS_TO_DB_PATH, dbRoute);
        }

        public static string DBRouteToFilePath(string dbRoute, int fileIndex) {
            return Path.Combine(DBRouteToFolderPath(dbRoute), $"{dbRoute.Replace('/', '-')}-{fileIndex}{AUDIO_FILE_EXTENSION}");
        }

        public static void Write(string dbRoute, RadioContent content, bool splitAudioSections = false) {
            string folderPath = DBRouteToFolderPath(dbRoute);
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            int fileIndex = 0;
            string filePathNumbered = DBRouteToFilePath(dbRoute, fileIndex);

            if (splitAudioSections) {
                foreach (byte[] audioSection in content.AudioContentSections) {
                    while (File.Exists(filePathNumbered)) {
                        filePathNumbered = DBRouteToFilePath(dbRoute, ++fileIndex);
                    }
                    File.WriteAllBytes(filePathNumbered, audioSection);
                }
            } else {
                while (File.Exists(filePathNumbered)) {
                    filePathNumbered = DBRouteToFilePath(dbRoute, ++fileIndex);
                }
                File.WriteAllBytes(filePathNumbered, content.ConcatAudioContentSections);
            }
        }
    }
}