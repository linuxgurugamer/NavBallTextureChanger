using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using UnityEngine;
using ToolbarControl_NS;
using ClickThroughFix;
using KSP.UI.Screens;
using static NavBallTextureChanger.Statics;
using SpaceTuxUtility;


namespace NavBallTextureChanger
{
    public class Constants
    {
        internal const string TEX_NODES = "NavballTextureChanger";
        internal const string FILE_EMISSIVE = "FILE_EMISSIVE";
        internal const string MOD_DIR = "GameData/NavBallTextureChanger/";
        internal const string SKIN_DATADIR = MOD_DIR + "PluginData/Skins";

        internal const string MODID = "NavBallTextureChanger";
        internal const string MODNAME = "NavBall Texture Changer";

    }
    public class FileEmissive
    {
        public string file;
        public string emissive;
        public string descr;

        public Color EmissiveColor = new Color(0.376f, 0.376f, 0.376f, 1f);

        public Texture2D thumb;
        public Texture2D image;
        public Texture2D emissiveImg;

        public FileEmissive(string file, string emissive, string descr, Color emissiveColor) //, bool Flight, bool Iva)
        {
            this.file = file;
            this.emissive = emissive;
            this.descr = descr;

            this.EmissiveColor = emissiveColor;
        }
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class TextureSelector : MonoBehaviour
    {
        ToolbarControl toolbarControl;
        bool visible = false;
        int baseWindowID;

        static public SortedDictionary<string, FileEmissive> fileEmissiveDict = new SortedDictionary<string, FileEmissive>();

        const int THUMB_WIDTH = 150;
        const int THUMB_HEIGHT = 75;

        const float WIDTH = 1054;
        const float HEIGHT = 372;

        private Rect windowPosition = new Rect(Screen.width / 2 - WIDTH / 2, Screen.height / 2 - HEIGHT / 2, WIDTH, HEIGHT);


        internal static String _AssemblyName { get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Name; } }

        private Vector2 scrollOffset = Vector2.zero;
        FileEmissive fe = null;

        bool saved = false;
        bool selected = false;
        bool tested = false;

        bool onlyShowWithEmissives = false;
        bool advanced = false;
        bool emissiveApplied = false;

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
                    Constants.MODID,
                    "NavBallTextureChangerPBtn",
                    "NavBallTextureChanger/PluginData/navball-38",
                    "NavBallTextureChanger/PluginData/navball-24",
                    Constants.MODNAME
                );
            }

        }

        public void LoadTextureConfigs()
        {
            var files = Directory.GetFiles(Constants.SKIN_DATADIR, "*.cfg");
            foreach (var f in files)
            {
                var fnode = ConfigNode.Load(f);
                if (fnode != null)
                {
                    var n1 = fnode.GetNodes(Constants.TEX_NODES);
                    if (n1 != null)
                    {
                        foreach (var fileNode in n1)
                        {
                            var nodes = fileNode.GetNodes(Constants.FILE_EMISSIVE);
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
                                        if (File.Exists(Constants.MOD_DIR + file))
                                        {
                                            FileEmissive fe = new FileEmissive(file, emissive, descr, EmissiveColor); //, Flight, Iva);

                                            name = Path.GetFileNameWithoutExtension(file);
                                            Texture2D image;
                                            var thumb = ResizeImage(Constants.MOD_DIR + file, out image);
                                            if (thumb != null)
                                            {
                                                fe.thumb = thumb;
                                                fe.image = image;
                                            }

                                            if (emissive != null && emissive != "")
                                            {
                                                Texture2D emImg = new Texture2D(2, 2);
                                                ToolbarControl.LoadImageFromFile(ref emImg, Constants.MOD_DIR + emissive);
                                                fe.emissiveImg = emImg;
                                            }
                                            if (!fileEmissiveDict.ContainsKey(file))
                                                fileEmissiveDict.Add(file, fe);
                                            else
                                                Log.Error("Duplicate key found: " + file);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            Log.Info("Total skins loaded: " + fileEmissiveDict.Count);
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
                windowPosition = ClickThruBlocker.GUILayoutWindow(baseWindowID, windowPosition, DrawWindow, "NavBall Texture Changer");
        }

        bool lastRectInitted = false;
        Rect lastRect;
        Color emc;
        void DrawWindow(int id)
        {
            GUILayout.BeginVertical();
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();

            scrollOffset = GUILayout.BeginScrollView(scrollOffset, GUILayout.Height(256), GUILayout.Width(500));
            GUILayout.BeginVertical(GUILayout.Width(500));

            foreach (var t in fileEmissiveDict)
            {
                if (t.Value.emissive != "" || !onlyShowWithEmissives)
                {
                    GUILayout.BeginHorizontal();

                    if (GUILayout.Button(t.Value.descr, GUILayout.Width(250), GUILayout.Height(THUMB_HEIGHT)) || GUILayout.Button(t.Value.thumb, GUI.skin.label))
                    {
                        fe = t.Value;
                        saved = false;
                        selected = true;
                        tested = false;
                        emc = fe.EmissiveColor;
                    }

                    GUILayout.BeginVertical();
                    GUILayout.FlexibleSpace();
                    if (t.Value.emissive != "" && !onlyShowWithEmissives)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Emissive\navailable");
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndVertical();
                    GUILayout.EndHorizontal();
                }
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
            //if (NavBallChanger.IVAactive)
            //    onlyShowWithEmissives = true;
            //else
            onlyShowWithEmissives = GUILayout.Toggle(onlyShowWithEmissives, "Only w/ emissives", GUILayout.Width(90));

            GUILayout.FlexibleSpace();
            GUI.enabled = selected;
            if (GUILayout.Button("Test", GUILayout.Width(90)))
            {
                NavBallChanger._navballTexture.SetTexture(fe);
                tested = true;
            }
            GUILayout.FlexibleSpace();
            GUI.enabled = tested;

            if (GUILayout.Button("Save", GUILayout.Width(90)))
            {
                NavBallChanger._navballTexture.SetTexture(fe, true);
                saved = true;
            }
            if (GUILayout.Button("Save both", GUILayout.Width(90)))
            {
                NavBallChanger._navballTexture.SetTexture(fe, true, true);
                saved = true;
            }
            GUILayout.FlexibleSpace();
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
            GUILayout.BeginHorizontal();
            GUI.enabled = (tested || saved) && (fe != null && fe.emissive != "");
            if (fe == null || fe.emissive == "")
                advanced = false;
            advanced = GUILayout.Toggle(advanced, "Advanced (only visible in IVA)", GUILayout.Width(90));
            if (!advanced)
                windowPosition.height = HEIGHT;

            GUILayout.EndHorizontal();
            if (advanced && NavBallChanger.IVAactive)
            {
                var tmp = emc;
                GUILayout.BeginHorizontal();
                GUILayout.Label("Changes won't take effect until applied\nChanges won't be saved until saved ");
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Red:", GUILayout.Width(60));
                emc.r = GUILayout.HorizontalSlider(emc.r, 0, 1, GUILayout.Width(600));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Green:", GUILayout.Width(60));
                emc.g = GUILayout.HorizontalSlider(emc.g, 0, 1, GUILayout.Width(600));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Blue:", GUILayout.Width(60));
                emc.b = GUILayout.HorizontalSlider(emc.b, 0, 1, GUILayout.Width(600));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Alpha:", GUILayout.Width(60));
                emc.a = GUILayout.HorizontalSlider(emc.a, 0, 1, GUILayout.Width(600));
                GUILayout.EndHorizontal();


                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUI.enabled = (emc != fe.EmissiveColor);
                if (tmp != emc)
                    emissiveApplied = false;
                if (GUILayout.Button("Apply Emissive Changes", GUILayout.Width(180)))
                {
                    NavBallChanger._navballTexture.SetEmissiveColor(emc);
                    emissiveApplied = true;
                }
                GUILayout.FlexibleSpace();
                GUI.enabled = emissiveApplied;
                if (GUILayout.Button("Save Emissive Changes", GUILayout.Width(180)))
                {
                    fe.EmissiveColor = emc;
                }
                GUILayout.FlexibleSpace();
                GUI.enabled = true;
                if (GUILayout.Button("Reset", GUILayout.Width(180)))
                {
                    emc = fe.EmissiveColor;
                    NavBallChanger._navballTexture.SetEmissiveColor(emc);
                }
                GUI.enabled = true;
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            GUI.enabled = true;
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
