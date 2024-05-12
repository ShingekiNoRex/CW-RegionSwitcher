using UnityEngine;
using System.Collections.Generic;
using HarmonyLib;
using BepInEx;
using Photon.Pun;
using Photon.Realtime;
using Zorro.Settings;
using ContentSettings.API;
using ContentSettings.API.Settings;
using BepInEx.Logging;

namespace RegionSwitcher
{
	[ContentWarningPlugin("Region Switcher", "1.0", true)]
	[BepInPlugin("RegionSwitcher", "Region Switcher", "1.0")]
	public class RegionSwitcher : BaseUnityPlugin
	{
		internal static new ManualLogSource Logger { get; private set; } = null!;
		public static RegionSwitcher Instance { get; private set; } = null!;

		public List<string> AvailableRegions = new List<string>() { "us", "usw", "asia", "sa", "eu" };

		public string SelectedRegion = "";

		void Awake()
		{ 
			Instance = this;

			Logger = base.Logger;

			SettingsLoader.RegisterSetting("Region", new RegionSetting());
			
			var harmony = new Harmony("RegionSwitcher");
			harmony.PatchAll(typeof(RegionSwitcherPatch));
		}
	}


	class RegionSetting : EnumSetting, ICustomSetting
	{

		public string GetDisplayName() => "Server - Will take effect the next time you enter a lobby";


		public override List<string> GetChoices()
		{
			if (PhotonNetwork.NetworkingClient.RegionHandler == null)
			{
				RegionSwitcher.Logger.LogWarning("RegionHandler is null");
				return RegionSwitcher.Instance.AvailableRegions;
			}

			List<string> options = new List<string>(RegionSwitcher.Instance.AvailableRegions);

			foreach (var region in PhotonNetwork.NetworkingClient.RegionHandler.EnabledRegions)
			{
				int index = options.FindIndex(item => item.Equals(region.Code)); 
				if (index > -1 && index < options.Count)
					options[index] = $"{RegionSwitcher.Instance.AvailableRegions[index]}    ({region.Ping}ms)";
			}

			return options;
		}

		public override void ApplyValue()
		{
			if (RegionSwitcher.Instance.AvailableRegions.Count < 1)
				return;

			RegionSwitcher.Logger.LogInfo($"Applied Value: {Value} - {RegionSwitcher.Instance.AvailableRegions[Value]}" );
			RegionSwitcher.Instance.SelectedRegion = RegionSwitcher.Instance.AvailableRegions[Value];
		}

		protected override int GetDefaultValue()
		{
			return RegionSwitcher.Instance.AvailableRegions.FindIndex(region => region.Equals(PhotonNetwork.NetworkingClient.RegionHandler.BestRegion.Code));
		}
	}

	class RegionSwitcherPatch
	{
		[HarmonyPatch(typeof(LoadBalancingClient), "ConnectToRegionMaster")]
		static void Prefix(ref string region)
		{
			if (!string.IsNullOrEmpty(RegionSwitcher.Instance.SelectedRegion) && RegionSwitcher.Instance.AvailableRegions.Contains(RegionSwitcher.Instance.SelectedRegion))
				region = RegionSwitcher.Instance.SelectedRegion;
		}
	}
}
