
using ToolbarControl_NS;
using UnityEngine;
using KSP_Log;

namespace NavBallTextureChanger
{

    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class RegisterToolbar : MonoBehaviour
    {
        static public Log Log;

        void Start()
        {
            ToolbarControl.RegisterMod(Constants.MODID, Constants.MODNAME);
#if DEBUG
            Log = new Log("NavBallTextureChanger", Log.LEVEL.INFO);
#else
            Log = new Log("NavBallTextureChanger", Log.LEVEL.ERROR);
#endif
        }
    }

    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class Statics : MonoBehaviour
    {
        static public Log Log;

        void Awake()
        {
#if DEBUG
            Log = new Log("NavBallTextureChanger", Log.LEVEL.INFO);
#else
            Log = new Log("NavBallTextureChanger", Log.LEVEL.ERROR);
#endif
        }
    }


}
