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
			_navballTexture = new NavBallTexture(GetSkinDirectory());

			//LoadConfig();

			// Save the original textures first
			_navballTexture.SaveCopyOfStockTexture(true);
			//_navballTexture.SaveCopyOfIvaTexture();
			// Then load the config
			_navballTexture.LoadConfig();

			GameEvents.onVesselChange.Add(OnVesselChanged);
			GameEvents.OnCameraChange.Add(OnCameraChanged);
			GameEvents.onGameSceneSwitchRequested.Add(OnGameSceneSwitchRequested);

			//UpdateFlightTexture();
		}


		private void OnDestroy()
		{
			GameEvents.onVesselChange.Remove(OnVesselChanged);
			GameEvents.OnCameraChange.Remove(OnCameraChanged);
			GameEvents.onGameSceneSwitchRequested.Remove(OnGameSceneSwitchRequested);
		}

#if false
		private void LoadConfig()
		{
			var configPath = IOUtils.GetFilePathFor(typeof(NavBallChanger), ConfigFileName);
			var haveConfig = File.Exists(configPath);

			if (!haveConfig)
			{
				Log.Info("Config file not found. Creating default");

				if (!Directory.Exists(Path.GetDirectoryName(configPath)))
					Directory.CreateDirectory(Path.GetDirectoryName(configPath));

				ConfigNode.CreateConfigFromObject(this).Save(configPath);
				Log.Info("Default config file saved: " + configPath);
			}

			configPath
				.With(ConfigNode.Load)
				.Do(config => ConfigNode.LoadObjectFromConfig(this, config));
		}
#endif

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
			_navballTexture.SaveCopyOfIvaTexture();
			_navballTexture.Do(nbt => nbt.SetIvaTextures());
		}


		internal static UrlDir GetSkinDirectory()
		{
			var skinUrl =  "NavBallTextureChanger/Skins";

			var directory =
				GameDatabase.Instance.root.AllDirectories.FirstOrDefault(d => d.url == skinUrl);

			if (directory == null)
				throw new InvalidOperationException("Failed to find skin directory inside GameDatabase");

			return directory;
		}
	}
}