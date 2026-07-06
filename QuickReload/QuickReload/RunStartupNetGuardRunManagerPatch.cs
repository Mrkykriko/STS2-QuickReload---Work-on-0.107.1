using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Runs;

namespace QuickReload;

[HarmonyPatch]
internal static class RunStartupNetGuardRunManagerPatch
{
	[HarmonyPatch(typeof(RunManager), "SetUpSavedMultiPlayer")]
	[HarmonyPostfix]
	private static void OnSetUpSavedMultiPlayerPostfix()
	{
		TryClearGuard("SetUpSavedMultiPlayer");
	}

	[HarmonyPatch(typeof(RunManager), "SetUpNewMultiPlayer")]
	[HarmonyPostfix]
	private static void OnSetUpNewMultiPlayerPostfix()
	{
		TryClearGuard("SetUpNewMultiPlayer");
	}

	private static void TryClearGuard(string source)
	{
		if (QuickReloadState.IsRunStartupNetGuardActive())
		{
			QuickReloadState.ClearRunStartupNetGuard();
			Log.Info("[QUICKRELOAD]: Cleared load-screen net update guard after " + source + ".");
		}
	}
}
