using GorillaLocomotion;
using HarmonyLib;
using UnityEngine;

namespace GorillaMedia.Classes.Patches
{
    [HarmonyPatch(typeof(GorillaQuitBox), nameof(GorillaQuitBox.OnBoxTriggered))]
    public class QuitBoxPatch
    {
        static bool Prefix()
        {
            if (GTPlayer.Instance == null)
                return true;

            ZoneManagement.SetActiveZone(GTZone.forest);
            GTPlayer.Instance.transform.position = new Vector3(-76f, 7f, -80f);

            if (GTPlayer.Instance.playerRigidBody != null)
                GTPlayer.Instance.playerRigidBody.linearVelocity = Vector3.zero;

            return false;
        }
    }
}
