using BepInEx;
using GorillaMedia.Classes;
using UnityEngine;
using ExitGames.Client.Photon;
using Photon.Pun;
using GorillaMedia.Classes.Admin;

namespace GorillaMedia
{
    [BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance;

        void Awake() =>
            ConfigManager.LoadConfig(Config);

        void Start()
        {
            gameObject.AddComponent<Console>();
            gameObject.AddComponent<HamburburData>();
            gameObject.AddComponent<TelemetrySorter>();

            GameObject MediaManager = new GameObject("MediaManager");
            MediaManager.AddComponent<MediaManager>();
            DontDestroyOnLoad(MediaManager);

            GorillaTagger.OnPlayerSpawned(OnPlayerSpawned);
        }

        void OnPlayerSpawned()
        {
            GameObject UI = AssetManager.LoadObject<GameObject>("UI");
            UI.AddComponent<MediaControlUI>();

            Hashtable props = new()
            {
                    {
                            "Deez's Gorilla Media",
                            $"Made by Deez - {PluginInfo.Version}"
                    }
            };

            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
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