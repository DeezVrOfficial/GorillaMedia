using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using WebSocketSharp;

namespace GorillaMedia.Classes.Console;

// Player tracking FPs, Sticks, and AAs so tuff yo
public class TrackerManager : MonoBehaviour
{
    public static Action<VRRig>  OnPlayerCosmeticsLoaded;
    public static Action<JToken> OnRoomDataReceived;
    private readonly Queue<string> receivedMessages = new();
    private readonly WebSocket trackerWebSocket = new("wss://deez.uk/tracker");

    private void Start()
    {
        Debug.Log("Starting tracker websocket flow");

        trackerWebSocket.OnMessage += (sender, messageEventArgs) =>
        {
            lock (receivedMessages)
                receivedMessages.Enqueue(messageEventArgs.Data);
            Debug.Log("Received message from tracker websocket: " + messageEventArgs.Data);
        };

        trackerWebSocket.OnClose += (sender, closeEventArgs) =>
        {
            trackerWebSocket.ConnectAsync();
            Debug.Log("Tracker websocket closed... reconnecting");
        };

        trackerWebSocket.OnOpen += (sender, args) => Debug.Log("Tracker websocket Connected");

        trackerWebSocket.ConnectAsync();

            OnPlayerCosmeticsLoaded += OnRigCosmeticsLoaded;
    }

    private void OnRigCosmeticsLoaded(VRRig rig)
    {
        if (rig == null)
            return;

        NetPlayer player = rig.creator;

        if (player.GetPlayerRef() == PhotonNetwork.LocalPlayer)
            return;

        string specialCosmetics = HamburburData.Data["specialCosmetics"]
                                         ?.ToObject<Dictionary<string, string>>()
                                          .Where(c => rig.HasCosmetic(c.Key))
                                          .Aggregate("",
                                              (current, c) => current + c.Value + ", ");

        specialCosmetics = specialCosmetics.TrimEnd(',', ' ').Trim();

        string userName = HamburburData.Data["knownPeople"]
                         ?.ToObject<Dictionary<string, string>>()
                          .GetValueOrDefault(player.UserId, "");

        if (string.IsNullOrEmpty(specialCosmetics) && string.IsNullOrWhiteSpace(userName))
            return;

        JObject trackingData = new()
        {
            { "isUserKnown", !string.IsNullOrWhiteSpace(userName) },
            { "username", userName },
            { "hasSpecialCosmetic", !string.IsNullOrWhiteSpace(specialCosmetics) },
            { "specialCosmetic", specialCosmetics },
            { "roomCode", PhotonNetwork.CurrentRoom.Name },
            { "playersInRoom", PhotonNetwork.CurrentRoom.PlayerCount },
            { "inGameName", rig.creator.NickName },
            { "gameModeString", NetworkSystem.Instance.GameModeString },
            { "userId", player.UserId },
        };

        StartCoroutine(UploadTrackerData(trackingData));
    }

    private void Update()
    {
        lock (receivedMessages)
        {
            while (receivedMessages.Count > 0)
                ParseAndReceiveMessage(receivedMessages.Dequeue());
        }
    }

    private void ParseAndReceiveMessage(string data)
    {
        JObject trackingData;
        try
        {
            trackingData = JObject.Parse(data);
        }
        catch
        {
            return;
        }

        if (trackingData["type"]?.ToString() == "message")
            return;
        OnRoomDataReceived?.Invoke(trackingData);

        bool isUserKnown = trackingData["isUserKnown"]?.ToObject<bool>() ?? false;
        string username = trackingData["username"]?.ToString() ?? "Someone";
        bool hasCosmetic = !string.IsNullOrEmpty(trackingData["specialCosmetic"]?.ToString());
        string cosmetic = trackingData["specialCosmetic"]?.ToString() ?? "";
        string room = trackingData["roomCode"]?.ToString() ?? "unknown";
        int players = trackingData["playersInRoom"]?.ToObject<int>() ?? 0;
        string inGameName = trackingData["inGameName"]?.ToString() ?? "unknown";
        string gameMode = trackingData["gameModeString"]?.ToString() ?? "unknown";
    }

    public static IEnumerator SendPlayerDataSync(Dictionary<string, Dictionary<string, string>> data, string directory, string region)
    {
        string json = JsonConvert.SerializeObject(new
        {
            directory = TelemetryManagement.CleanString(directory, 12, ['@']),
            region = TelemetryManagement.CleanString(region, 3),
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

    private static IEnumerator UploadTrackerData(JObject data)
    {
        byte[] raw = Encoding.UTF8.GetBytes(data.ToString(Formatting.None));

        UnityWebRequest request = new("https://api.deez.uk/tracker/upload", "POST");
        request.uploadHandler = new UploadHandlerRaw(raw);
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("auth-key", "deez-tracker-secret");
        request.downloadHandler = new DownloadHandlerBuffer();

        yield return request.SendWebRequest();
    }
}