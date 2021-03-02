using System;
using System.IO;
using System.Linq;
using System.Reflection;
using KSP.IO;
using NavBallTextureChanger.Extensions;
using UnityEngine;
using File = System.IO.File;
using static NavBallTextureChanger.Statics;


namespace NavBallTextureChanger
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class NavBallChanger : MonoBehaviour
	{
		private const string ConfigFileName = "settings.cfg";

		internal static  NavBallTexture _navballTexture; //= new NavBallTexture(GetSkinDirectory());

		private void Awake()
		{
			Log.Info("NavBallChanger.Awake");
			_navballTexture = new NavBallTexture(GetSkinDirectory());

			// Save the original textures first
			_navballTexture.SaveCopyOfStockTexture(true);

			// Then load the config
			_navballTexture.LoadConfig();

			GameEvents.onVesselChange.Add(OnVesselChanged);
			GameEvents.OnCameraChange.Add(OnCameraChanged);
			GameEvents.onGameSceneSwitchRequested.Add(OnGameSceneSwitchRequested);

			//UpdateFlightTexture(); // Now done in LoadConfig()
		}


		private void OnDestroy()
		{
			GameEvents.onVesselChange.Remove(OnVesselChanged);
			GameEvents.OnCameraChange.Remove(OnCameraChanged);
			GameEvents.onGameSceneSwitchRequested.Remove(OnGameSceneSwitchRequested);
		}


		private void OnVesselChanged(Vessel data)
		{
			_navballTexture.Do(nbt => nbt.MarkMaterialsChanged());
			UpdateFlightTexture();
		}
		private void OnGameSceneSwitchRequested(GameEvents.FromToAction<GameScenes, GameScenes> fta)
        {
			IVAactive = false;
        }

		public static bool IVAactive = false;
		// Reset textures when entering IVA. Parts might have loaded or changed their internal spaces
		// in the meantime
		private void OnCameraChanged(CameraManager.CameraMode mode)
		{
			Log.Info("OnCameraChanged");
			IVAactive = (mode == CameraManager.CameraMode.IVA);
			if (!IVAactive)
			{			
				return;
			}

			UpdateIvaTextures();
		}


		private void UpdateFlightTexture()
		{
			_navballTexture
				.Do(nbt => nbt.SetFlightTexture());
		}


		private void UpdateIvaTextures()
		{
			Log.Info("UpdateIvaTextures");
			_navballTexture.SaveCopyOfIvaTexture();
			_navballTexture.Do(nbt => nbt.SetIvaTextures());
		}


		internal static string GetSkinDirectory()
		{
			var skinUrl =  "NavBallTextureChanger/PluginData/Skins";
			return skinUrl;
		}
	}
}