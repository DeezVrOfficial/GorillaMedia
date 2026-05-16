using BepInEx;
using Deez.GorillaMedia.Classes;
using Photon.Pun;
using System.Collections;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace Deez.GorillaMedia
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        void Awake()
        {
            ConfigManager.LoadConfig(Config);

            Hashtable props = new()
            {
                    {
                            "Deez's Gorilla Media",
                            $"Made by Deez - Version {PluginInfo.Version}"
                    }
            };

            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        void Start()
        {
            GameObject MediaManager = new GameObject("MediaManager");
            MediaManager.AddComponent<MediaManager>();
            DontDestroyOnLoad(MediaManager);

            GorillaTagger.OnPlayerSpawned(OnPlayerSpawned);
        }

        void OnPlayerSpawned()
        {
            GameObject UI = AssetManager.LoadObject<GameObject>("UI");

            UI.AddComponent<MediaManager>();
        }

# if DEBUG
        void OnGUI()
        {
            if (MediaManager.instance != null)
            {
                GUI.Label(new Rect(70, 10, 250, 50), 
                   $"{MediaManager.Title} - {MediaManager.Artist}\n{Mathf.Floor(MediaManager.ElapsedTime / 60)}:{Mathf.Floor(MediaManager.ElapsedTime % 60):00}");
                GUI.DrawTexture(new Rect(10, 10, 50, 50), MediaManager.Icon);

                if (GUI.Button(new Rect(10, 70, 50, 50), "<"))
                    MediaManager.instance.PreviousTrack();

                if (GUI.Button(new Rect(70, 70, 50, 50), MediaManager.Paused ? "Play" : "Pause"))
                    MediaManager.instance.PauseTrack();

                if (GUI.Button(new Rect(130, 70, 50, 50), ">"))
                    MediaManager.instance.SkipTrack();
            }
        }
#endif
    }
}