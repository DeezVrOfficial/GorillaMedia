using BepInEx;
using GorillaMedia.Classes;
using UnityEngine;
using GorillaMedia.Classes.EzVersionChecking;
using GorillaMedia.Classes.Console;
using ExitGames.Client.Photon;
using Photon.Pun;

namespace GorillaMedia
{
    [BepInPlugin(Constants.Guid, Constants.Name, Constants.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance;

        public static Transform firstPersonCameraTransform;
        public static Transform thirdPersonCameraTransform;

        private void Awake()
        {
            Instance = this;
            ConfigManager.LoadConfig(Config);
        }

        private void Start()
        {
            HarmonyPatches.ApplyHarmonyPatches();

            GameObject MediaManager = new GameObject("MediaManager");
            MediaManager.AddComponent<MediaManager>();
            DontDestroyOnLoad(MediaManager);

            GorillaTagger.OnPlayerSpawned(OnPlayerSpawned);
        }

        private void OnPlayerSpawned()
        {
            gameObject.AddComponent<PromotionManager>();
            gameObject.AddComponent<TrackerManager>();

            GameObject hamburburDataContainer = new("DGMHamburburData");
            hamburburDataContainer.AddComponent<HamburburData>();

            firstPersonCameraTransform = GorillaTagger.Instance.mainCamera.transform;
            thirdPersonCameraTransform = GorillaTagger.Instance.thirdPersonCamera.transform.GetChild(0);

            VersionCheckingInitializer.StartVersionChecking();

            VersionCheckingInitializer.VersionOutdatedDetected += () =>
                                                                  {
                                                                      if (PromotionManager.Instance != null && PromotionManager.Instance.TextObj != null)
                                                                          PromotionManager.Instance.TextObj.text = VersionCheckingInitializer.OutdatedMessage;

                                                                      MediaControlUI mCUI = FindFirstObjectByType<MediaControlUI>();
                                                                      if (mCUI != null)
                                                                          Destroy(mCUI.gameObject);

                                                                      MediaManager mM = FindFirstObjectByType<MediaManager>();
                                                                      if (mM != null)
                                                                          Destroy(mM.gameObject);

                                                                      enabled = false;
                                                                  };
            
            GameObject UI = AssetManager.LoadObject<GameObject>("UI");
            UI.AddComponent<MediaControlUI>();

            Hashtable properties = new()
            {
                    {
                            $"{Constants.HashKey}",
                            $"Made by Deez! Version - {Constants.Version}"
                    }
            };

            PhotonNetwork.LocalPlayer.SetCustomProperties(properties);
        }
    }
}