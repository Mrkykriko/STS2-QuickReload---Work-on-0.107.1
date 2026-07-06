using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using MegaCrit.Sts2.Core.Nodes.Screens.DailyRun;

namespace QuickReload;

[HarmonyPatch]
internal static class RunStartupNetGuardProcessPatch
{
	private static IEnumerable<MethodBase> TargetMethods()
	{
		Type[] array = new Type[3]
		{
			typeof(NMultiplayerLoadGameScreen),
			typeof(NDailyRunLoadScreen),
			typeof(NCustomRunLoadScreen)
		};
		Type[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			MethodInfo methodInfo = AccessTools.Method(array2[i], "_Process");
			if (methodInfo != null)
			{
				yield return methodInfo;
			}
		}
	}

	[HarmonyPrefix]
	private static bool Prefix()
	{
		return !QuickReloadState.IsRunStartupNetGuardActive();
	}
}
