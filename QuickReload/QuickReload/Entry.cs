using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace QuickReload;

[ModInitializer("Init")]
public class Entry
{
	public static void Init()
	{
		new Harmony("QuickReload").PatchAll();
		ScriptManagerBridge.LookupScriptsInAssembly(typeof(Entry).Assembly);
		Log.Info("[QUICKRELOAD]: QuickReload mod initialized!");
	}
}
