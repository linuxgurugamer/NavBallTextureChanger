using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

using UnityEngine;
using ToolbarControl_NS;
using ClickThroughFix;
using KSP.UI.Screens;
using static NavBallTextureChanger.Statics;
using SpaceTuxUtility;


namespace NavBallTextureChanger
{
    public class FileEmissive
    {
        public string file;
        public string emissive;

        public string descr;

        public Color EmissiveColor = new Color(0.376f, 0.376f, 0.376f, 1f);
        public bool Flight = true;
        public bool Iva = true;

        public Texture2D thumb;
        public Texture2D image;
        public Texture2D emissiveImg;

        public FileEmissive(string file, string emissive, string descr, Color emissiveColor, bool Flight, bool Iva)
        {
            this.file = file;
            this.emissive = emissive;
            this.descr = descr;

            this.EmissiveColor = emissiveColor;
            this.Flight = Flight;
            this.Iva = Iva;
        }
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class TextureSelector : MonoBehaviour
    {
        ToolbarControl toolbarControl;
        bool visible = false;
        int baseWindowID;

        const string TEX_NODES = "NavballTextureChanger";
        const string FILE_EMISSIVE = "FILE_EMISSIVE";
        const string SKIN_DATADIR = "GameData/NavBallTextureChanger/Skins";
        static public SortedDictionary<string, FileEmissive> fileEmissiveDict = new SortedDictionary<string, FileEmissive>();

        internal const string MODID = "NavBallTextureChanger";
        internal const string MODNAME = "NavBall Texture Changer";

        const int THUMB_WIDTH = 150;
        const int THUMB_HEIGHT = 75;

        const float WIDTH = 1054;
        const float HEIGHT = 256 + 100;

        private Rect windowPosition = new Rect(Screen.width / 2 - WIDTH / 2, Screen.height / 2 - HEIGHT / 2, WIDTH, HEIGHT);


        internal static String _AssemblyName { get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Name; } }

        private Vector2 scrollOffset = Vector2.zero;
        FileEmissive fe = null;

        bool saved = false;
        bool selected = false;
        bool tested = false;

        void Awake()
        {

        }

        void Start()
        {
            baseWindowID = UnityEngine.Random.Range(1000, 2000000) + _AssemblyName.GetHashCode();
            AddToolbarButton();
            LoadTextureConfigs();
        }

        void AddToolbarButton()
        {
            if (toolbarControl == null)
            {
                GameObject gameObject = new GameObject();
                toolbarControl = gameObject.AddComponent<ToolbarControl>();
                toolbarControl.AddToAllToolbars(windowToggle, windowToggle,
                    ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.TRACKSTATION,
                    MODID,
                    "NavBallTextureChangerPBtn",
                    "NavBallTextureChanger/PluginData/navball-38",
                    "NavBallTextureChanger/PluginData/navball-24",
                    MODNAME
                );
            }

        }

        public void LoadTextureConfigs()
        {
            var n1 = GameDatabase.Instance.GetConfigNodes(TEX_NODES);
            if (n1 != null)
            {
                foreach (var fileNode in n1)
                {
                    var nodes = fileNode.GetNodes(FILE_EMISSIVE);
                    if (nodes != null)
                    {
                        foreach (var n in nodes)
                        {
                            string file = n.SafeLoad("file", "");
                            string emissive = n.SafeLoad("emissive", "");
                            if (file != "")
                            {
                                string descr = n.SafeLoad("descr", "");
                                if (descr == "")
                                    descr = Path.GetFileNameWithoutExtension(file);
                                Color EmissiveColor = n.SafeLoad("EmissiveColor", new Color(0.376f, 0.376f, 0.376f, 1f));
                                bool Flight = n.SafeLoad("Flight", true);
                                bool Iva = n.SafeLoad("Iva", true);

                                string p = "GameData/NavBallTextureChanger/";
                                if (File.Exists(p + file))
                                {
                                    FileEmissive fe = new FileEmissive(file, emissive, descr, EmissiveColor, Flight, Iva);

                                    name = Path.GetFileNameWithoutExtension(file);
                                    Texture2D image;
                                    var thumb = ResizeImage("GameData/NavBallTextureChanger/" + file, out image);
                                    if (thumb != null)
                                    {
                                        fe.thumb = thumb;
                                        fe.image = image;
                                    }

                                    if (emissive != null)
                                    {
                                        Texture2D emImg = new Texture2D(2, 2);
                                        ToolbarControl.LoadImageFromFile(ref emImg, "GameData/NavBallTextureChanger/" + emissive);
                                        fe.emissiveImg = emImg;
                                    }
                                    fileEmissiveDict.Add(file, fe);
                                }
                            }
                        }
                    }
                }
            }
            else
                Log.Error("TEX_NODES not found");
        }

        void windowToggle()
        {
            visible = !visible;
            if (visible)
                NavBallTexture.Instance.SaveTexture();
            else
                NavBallChanger._navballTexture.ResetTexture();
        }

        void OnGUI()
        {
            GUI.skin = HighLogic.Skin;
            if (visible)
                windowPosition = ClickThruBlocker.GUILayoutWindow(baseWindowID, windowPosition, DrawWindow, "NavBallTextureChanger");
        }

        bool lastRectInitted = false;
        Rect lastRect;

        void DrawWindow(int id)
        {
            GUILayout.BeginVertical();
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();

            scrollOffset = GUILayout.BeginScrollView(scrollOffset, GUILayout.Height(256), GUILayout.Width(500));
            GUILayout.BeginVertical(GUILayout.Width(500));

            foreach (var t in fileEmissiveDict)
            {
                GUILayout.BeginHorizontal();

                if (GUILayout.Button(t.Value.descr, GUILayout.Width(250), GUILayout.Height(THUMB_HEIGHT)) || GUILayout.Button(t.Value.thumb, GUI.skin.label))
                {
                    fe = t.Value;
                    saved = false;
                    selected = true;
                    tested = false;
                }

#if false
                if (GUILayout.Button(t.Value.thumb, GUI.skin.label))
                {
                    fe = t.Value;
                    saved = false;
                    selected = true;
                }
#endif
                GUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                if (t.Value.Flight)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Flight");
                    GUILayout.EndHorizontal();
                }
                if (t.Value.Iva)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Iva");
                    GUILayout.EndHorizontal();
                }
                if (t.Value.emissive != "")
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Emissive avail");
                    GUILayout.EndHorizontal();
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();

            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            Event e = Event.current;
            if (!lastRectInitted && e.type == EventType.Repaint)
            {
                lastRect = GUILayoutUtility.GetLastRect();    //button rect   
                lastRectInitted = true;
            }

            GUILayout.BeginVertical();
            if (fe != null)
            {
                GUI.DrawTexture(new Rect(lastRect.x + lastRect.width + 10 /* 520f */, lastRect.y, 512, 256), fe.image, ScaleMode.ScaleToFit, true, 0f);
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.enabled = selected;
            if (GUILayout.Button("Test", GUILayout.Width(90)))
            {
                NavBallChanger._navballTexture.SetTexture(fe, true); // fe.image, fe.emissiveImg, fe.EmissiveColor);
                tested = true;
            }
            GUILayout.FlexibleSpace();
            GUI.enabled = tested;
            if (GUILayout.Button("Save", GUILayout.Width(90)))
            {
                NavBallChanger._navballTexture.SetTexture(fe, true);
                saved = true;
            }
            GUI.enabled = true;
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Reset to Stock"))
            {
                NavBallChanger._navballTexture.ResetToStockTexture();
                saved = true;
                tested = false;
                selected = false;
            }
            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", GUILayout.Width(90)))
            {
                visible = false;
                if (!saved)
                    NavBallChanger._navballTexture.ResetTexture();
                NavBallChanger._navballTexture.SaveConfig();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        void OnDestroy()
        {
            if (toolbarControl != null)
            {
                toolbarControl.OnDestroy();
                GameObject.Destroy(toolbarControl);
                toolbarControl = null;
            }
        }

        // ****************************

        private Texture2D ResizeImage(string filename, out Texture2D image)
        {
            image = new Texture2D(2, 2);
            if (!File.Exists(filename))
                return null;
            var fileData = File.ReadAllBytes(filename);
            var tex = new Texture2D(2, 2);
            tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
            image.LoadImage(fileData);
            TextureScale.Bilinear(tex, THUMB_WIDTH, THUMB_HEIGHT);

            var outFile = Regex.Replace(filename, @"\.png", "-thumb.png");

            var bytes = tex.EncodeToPNG();
            File.WriteAllBytes(outFile, bytes);
            return tex;
        }

    }
}
