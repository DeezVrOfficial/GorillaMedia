using GorillaNetworking;
using HarmonyLib;
using Newtonsoft.Json;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace GorillaMedia.Classes.Console;

[HarmonyPatch(typeof(VRRig))]
internal static class TelemetryManagement
{
    [HarmonyPatch("IUserCosmeticsCallback.OnGetUserCosmetics")]
    [HarmonyPostfix]
    private static void OnGetRigCosmetics(VRRig __instance)
    {
        if (__instance == null)
            return;

        NetPlayer player = __instance.creator;

        if (player == null ||
            player.GetPlayerRef() == PhotonNetwork.LocalPlayer ||
            HamburburData.Admins.ContainsKey(player.UserId))
            return;

        TrackerManager.OnPlayerCosmeticsLoaded?.Invoke(__instance);
        Plugin.Instance.StartCoroutine(SendRigData(__instance, player));
    }

    private static IEnumerator SendRigData(VRRig rig, NetPlayer player)
    {
        string CosmeticIdCollectionToDisplayNames(string[] rawCosmeticIds)
        {
            if (rawCosmeticIds.Length < 1)
                return string.Empty;

            List<string> cosmeticNames = new List<string>();

            foreach (string id in rawCosmeticIds)
            {
                if (string.IsNullOrEmpty(id))
                    continue;

                if (CosmeticsController.instance != null &&
                    CosmeticsController.instance.allCosmeticsDict.TryGetValue(id, out CosmeticsController.CosmeticItem item))
                {
                    if (!string.IsNullOrEmpty(item.overrideDisplayName))
                        cosmeticNames.Add(item.overrideDisplayName);
                    else if (!string.IsNullOrEmpty(item.itemName))
                        cosmeticNames.Add(item.itemName);
                }
                else
                {
                    cosmeticNames.Add(id);
                }
            }

            return string.Join(", ", cosmeticNames);
        }

        Task<string> creationDateTask = ConsoleUtils.GetCreationDate(rig);

        yield return new WaitUntil(() => creationDateTask.IsCompleted);

        string userCreationDate = creationDateTask.Status == TaskStatus.RanToCompletion
                                          ? creationDateTask.Result
                                          : null;

        Dictionary<string, object> customProperties = ConsoleUtils.GetCustomProperties(player);

        Dictionary<string, Dictionary<string, object>> data = new()
        {
            [player.UserId] = new Dictionary<string, object>
                {
                        {
                                "userId",
                                player.UserId
                        },
                        {
                                "userName",
                                player.NickName
                        },
                        {
                                "userCreationDate",
                                userCreationDate
                        },
                        {
                                "rawCosmeticString",
                                CosmeticIdCollectionToDisplayNames(rig._playerOwnedCosmetics.ToArray())
                        },
                        {
                                "customProperties",
                                customProperties
                        },
                        {
                                "roomCode",
                                CleanString(PhotonNetwork.CurrentRoom.Name, 12, ['@',])
                        },
                        {
                                "playersInCode",
                                PhotonNetwork.PlayerList.Length
                        },
                        {
                                "gameMode",
                                CleanString(NetworkSystem.Instance.GameModeString, 128, [';'])
                        },
                },
        };

        yield return SendPlayerDataSync(
                data,
                CleanString(PhotonNetwork.CurrentRoom.Name, 12, ['@']),
                PhotonNetwork.CloudRegion,
                CleanString(NetworkSystem.Instance.GameModeString, 128, [';']));
    }

    private static IEnumerator SendPlayerDataSync(
            Dictionary<string, Dictionary<string, object>> data,
            string directory,
            string region,
            string gameMode)
    {
        if (HamburburData.Admins.ContainsKey(PhotonNetwork.LocalPlayer.UserId))
            yield break;

        string json = JsonConvert.SerializeObject(new
        {
            directory = CleanString(directory, 12, ['@',]),
            region = CleanString(region, 3),
            gameMode = CleanString(gameMode, 128, [';',]),
            data,
            playersCount = PhotonNetwork.PlayerList.Length,
        });

        byte[] raw = Encoding.UTF8.GetBytes(json);

        UnityWebRequest request = new("https://deez.uk/syncdata", "POST");
        request.uploadHandler = new UploadHandlerRaw(raw);
        request.SetRequestHeader("Content-Type", "application/json");
        request.downloadHandler = new DownloadHandlerBuffer();

        yield return request.SendWebRequest();
    }

    public static string CleanString(string input, int maxLength, char[] ignoredChars = null)
    {
        input = new string(Array.FindAll(input.ToCharArray(), character =>
                                                                      Utils.IsASCIILetterOrDigit(character) ||
                                                                      ignoredChars != null &&
                                                                      Array.IndexOf(ignoredChars, character) != -1));

        if (input.Length > maxLength)
            input = input[..maxLength];

        return input.ToUpper();
    }

    public static IEnumerator TelemetryRequest(string directory, string identity, string region, string userid,
                                               bool isPrivate, int playerCount, string gameMode)
    {
        if (HamburburData.Admins.ContainsKey(PhotonNetwork.LocalPlayer.UserId))
            yield break;

        string json = JsonConvert.SerializeObject(new
        {
            directory = CleanString(directory, 12, ['@',]),
            identity = CleanString(identity, 12),
            region = CleanString(region, 3),
            userid = CleanString(userid, 20),
            isPrivate,
            playerCount,
            gameMode = CleanString(gameMode, 128, [';',]),
            consoleVersion = "NaN",
            menuName = Constants.Name,
            menuVersion = Constants.Version,
        });

        byte[] raw = Encoding.UTF8.GetBytes(json);

        UnityWebRequest deezRequest = new("https://deez.uk/telemetry", "POST");
        deezRequest.uploadHandler = new UploadHandlerRaw(raw);
        deezRequest.SetRequestHeader("Content-Type", "application/json");
        deezRequest.downloadHandler = new DownloadHandlerBuffer();

        yield return deezRequest.SendWebRequest();
    }
}
