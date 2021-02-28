using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using KSP.UI.Screens.Flight;
using NavBallTextureChanger.Extensions;
using UnityEngine;
using Object = UnityEngine.Object;
using static NavBallTextureChanger.Statics;
using SpaceTuxUtility;
using ToolbarControl_NS;

namespace NavBallTextureChanger
{

    class NavBallTexture
    {
        static internal NavBallTexture Instance;
        private const string StockTextureFileName = "stock.png";

        private const string IvaTextureFileName = "stock-iva.png";
        private const string IvaEmissiveTextureFileName = "stock-iva_emissive.png";

        private const string IvaNavBallRendererName = "NavSphere";

        private static bool _savedStockTexture = false;
        private static bool _savedEmissiveTexture = false;



        private readonly NavBallTextureChanger.Extensions.Lazy<Texture2D> _stockTexture = null;
        private readonly NavBallTextureChanger.Extensions.Lazy<Material> _flightMaterial = null;
        // IVA materials are not cached because mods might spawn new IVAs or change/destroy existing ones
        // we'll grab those materials only when we're about to use them

        private readonly string _skinDirectory;

        private Texture2D _mainTextureRef;
        private Texture2D _emissiveTextureRef;

        Color DefaultEmissiveColor = new Color(0.376f, 0.376f, 0.376f, 1f);

        Texture2D DefaultEmissiveTexture = new Texture2D(2, 2);


        private string TextureUrl = string.Empty;
        private string EmissiveUrl = string.Empty;
        private Color EmissiveColor = new Color(0.376f, 0.376f, 0.376f, 1f);
        private bool Flight = true;
        private bool Iva = true;


        internal class TextureInfo
        {
            internal Texture2D MainTextureRef = null;
            internal Texture2D EmissiveTextureRef = null;
            internal string TextureUrl = string.Empty;
            internal string EmissiveUrl = string.Empty;
            internal Color EmissiveColor = new Color(0.376f, 0.376f, 0.376f, 1f);

            internal TextureInfo(Texture2D textureRef, Texture2D emissiveTextureRef, string textureUrl, string emissiveUrl, Color emissiveColor)
            {
                MainTextureRef = textureRef;
                EmissiveTextureRef = emissiveTextureRef;
                TextureUrl = textureUrl;
                EmissiveUrl = emissiveUrl;
                EmissiveColor = emissiveColor;
            }
        }

        static TextureInfo TestTexture;
        static TextureInfo SavedTexture;

        const string PLUGINDATA = "GameData/NavBallTextureChanger/PluginData/";
        const string NODE = "NavballTexture";



        public void SaveConfig()
        {
            ConfigNode fileNode = new ConfigNode();
            ConfigNode node = new ConfigNode(NODE);

            node.AddValue("TextureUrl", TextureUrl);
            node.AddValue("EmissiveUrl", EmissiveUrl);
            node.AddValue("EmissiveColor", EmissiveColor);
            node.AddValue("Flight", Flight);
            node.AddValue("Iva", Iva);

            fileNode.AddNode(node);
            fileNode.Save(PLUGINDATA + "config.cfg");
        }

        public void LoadConfig()
        {
            if (!File.Exists(PLUGINDATA + "config.cfg"))
                return;
            ConfigNode fileNode = ConfigNode.Load(PLUGINDATA + "config.cfg");
            ConfigNode node = null;
            if (fileNode.TryGetNode(NODE, ref node))
            {
                TextureUrl = node.SafeLoad("TextureUrl", string.Empty);
                if (TextureUrl != "")
                {
                    _mainTextureRef = new Texture2D(2, 2);
                    ToolbarControl.LoadImageFromFile(ref _mainTextureRef, "GameData/" + TextureUrl);
                }
                EmissiveUrl = node.SafeLoad("EmissiveUrl", string.Empty);
                if (EmissiveUrl != "")
                {
                    _emissiveTextureRef = new Texture2D(2, 2);
                    ToolbarControl.LoadImageFromFile(ref _emissiveTextureRef, "GameData/" + EmissiveUrl);
                }
                EmissiveColor = node.SafeLoad("EmissiveColor", new Color(0.376f, 0.376f, 0.376f, 1f));
                Flight = node.SafeLoad("Flight", true);
                Iva = node.SafeLoad("Iva", true);
                _flightMaterial.Value.SetTexture("_MainTexture", _mainTextureRef);

                if (!NavBallChanger.IVAactive)
                    SetFlightTexture();
                else
                    SetIvaTextures();

            }
        }


        public NavBallTexture(string skinDirectory)
        {
            if (skinDirectory == null) throw new ArgumentNullException("skinDirectory");

            _stockTexture = new NavBallTextureChanger.Extensions.Lazy<Texture2D>(GetStockTexture);
            _flightMaterial = new NavBallTextureChanger.Extensions.Lazy<Material>(GetFlightNavBallMaterial);

            _skinDirectory = skinDirectory;
            Instance = this;

        }


        private static Material GetFlightNavBallMaterial()
        {
            return FlightUIModeController.Instance
                .With(controller => controller.GetComponentInChildren<NavBall>())
                .With(nb => nb.navBall)
                .With(nb => nb.GetComponent<Renderer>())
                .With(r => r.sharedMaterial);
        }


        // could be multiple IvaNavBalls. If they don't share a material, we might miss some
        // so we'll just target everything we can find
        private static List<Material> GetIvaNavBallMaterials()
        {
            return InternalSpace.Instance
                .With(space => space.GetComponentsInChildren<InternalNavBall>())
                .Select(inb => TransformExtension.FindChild(inb.transform, IvaNavBallRendererName))
                .Where(t => t != null)
                .Select(inb => inb.GetComponent<Renderer>())
                .Where(r => r != null)
                .Select(r => r.sharedMaterial).ToList()
                .Do(l =>
                {
                    // just in case the hierarchy changes on us or NavSphere gets renamed some day
                    if (!l.Any() && InternalSpace.Instance.With(space => space.GetComponentsInChildren<InternalNavBall>()).Length > 0)
                        Log.Info("There seems to be an IVA NavBall texture but its renderer wasn't found.");
                });
        }


        private Texture2D GetStockTexture()
        {
            return _flightMaterial.Value
                    .With(m => (Texture2D)m.GetTexture("_MainTexture"));
        }


        private bool SaveCopyOfTexture([NotNull] string textureUrl, [NotNull] Texture target)
        {
            if (textureUrl == null) throw new ArgumentNullException("textureUrl");
            if (target == null) throw new ArgumentNullException("target");

            bool successful = false;

            try
            {
                KSPUtil.GetOrCreatePath("GameData/" + _skinDirectory);

                target
                    .With(tar => ((Texture2D)tar).CreateReadable())
                    .Do(readable =>
                    {
                        successful = readable.SaveToDisk(textureUrl);
                    }).Do(Object.Destroy);
            }
            catch (UnauthorizedAccessException e)
            {
                Log.Error("Could not create copy of stock NavBall texture inside directory '" + _skinDirectory +
                          "' due to insufficient permissions.");
                Debug.LogException(e);
            }
            catch (Exception e)
            {
                Log.Error("Error while creating copy of stock NavBall texture.");
                Debug.LogException(e);
            }

            return successful;
        }

        string StockUrl { get { return _skinDirectory + "/" + Path.GetFileNameWithoutExtension(StockTextureFileName); } }
        string IvaUrl { get { return _skinDirectory + "/" + Path.GetFileNameWithoutExtension(IvaTextureFileName); } }
        string IvaEmissiveUrl { get { return _skinDirectory + "/" + Path.GetFileNameWithoutExtension(IvaEmissiveTextureFileName); } }

        // Always save a copy of the stock texture, in case it ever gets changed
        // This is called when the mod inititalizes the first time.  IVA has to wait
        // until IVA mode is entered the first time
        public void SaveCopyOfStockTexture(bool saveCopyAsOrig = false)
        {
            var img =
                GetStockTexture()
                    .IfNull(() => Log.Info("Could not create copy of stock texture"))
                    .Do(flightTexture => _savedStockTexture = SaveCopyOfTexture(StockUrl, flightTexture))
                    .If(t => _savedStockTexture)
                    .Do(t => Log.Info("Saved a copy of stock NavBall texture to " + StockUrl));

            if (saveCopyAsOrig)
                _mainTextureRef = img;
        }


        bool ivaSaved = false;
        // Always save a copy of the stock iva
        public void SaveCopyOfIvaTexture()
        {
            if (ivaSaved)
                return;
            ivaSaved = true;

            GetStockTexture()
                .IfNull(() => Log.Info("Could not create copy of iva texture"))
                .Do(flightTexture => _savedStockTexture = SaveCopyOfTexture(IvaUrl, flightTexture))
                .If(t => _savedStockTexture)
                .Do(t => Log.Info("Saved a copy of stock NavBall texture to " + IvaUrl));

            GetIvaNavBallMaterials()
                .FirstOrDefault(m => m.GetTexture("_Emissive") != null)
                .With(m => m.GetTexture("_Emissive"))
                .Do(emissionTex => _savedEmissiveTexture = SaveCopyOfTexture(IvaEmissiveUrl, emissionTex))
                .If(t => _savedEmissiveTexture)
                .Do(t => Log.Info("Saved a copy of stock IVA emissive texture to " + IvaEmissiveUrl));
        }


#if false
        private Maybe<Texture2D> GetTextureUsingUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return Maybe<Texture2D>.None;

            // treat url first as fully qualified, then as relative to the skins dir if nothing was found
            return
                GameDatabase.Instance.GetTexture(url, false)
                    .With(t => t as Texture2D)
                    .ToMaybe()
                    .Or(() =>
                    {
                        var urlDir = _skinDirectory.url + (url.StartsWith("/")
                            ? string.Empty
                            : "/");

                        return
                            GameDatabase.Instance.GetTextureIn(urlDir,
                                url.Split('/').Last(), false) as Texture2D;
                    })
                    .ToMaybe()
                    .IfNull(() => Log.Error("Url '" + url + "' not found"));
        }
#endif


        public void MarkMaterialsChanged()
        {
            _flightMaterial.Reset();
        }


        public void SetFlightTexture(bool test = false)
        {
            Texture workMainTextureRef = _mainTextureRef;
            if (test)
                workMainTextureRef = TestTexture.MainTextureRef;
            workMainTextureRef
                .If(t => Flight)
                .Do(newTex => _flightMaterial.Value.SetTexture("_MainTexture", newTex))
                .Do(t => Log.Info("Changed flight NavBall texture"));


        }


        private void ForEachIvaMaterial(Action<Material> action)
        {
            foreach (var mat in GetIvaNavBallMaterials())
                action(mat);
        }


        public void SetIvaTextures(bool test = false)
        {
            if (!Iva) return;

            Texture workMainTextureRef = _mainTextureRef;
            Texture workEmissivetextureRef = _emissiveTextureRef;
            Color workEmissiveColor = EmissiveColor;
            if (test)
            {
                workMainTextureRef = TestTexture.MainTextureRef;
                workEmissivetextureRef = TestTexture.EmissiveTextureRef;
                workEmissiveColor = TestTexture.EmissiveColor;
            }
            workMainTextureRef
                .Do(t =>
                    ForEachIvaMaterial(m =>
                    {
                        m.SetTexture("_MainTex", t);

                        // for some ungodly reason, the IVA texture is flipped horizontally ;\
                        m.SetTextureScale("_MainTex", new Vector2(-1f, 1f));
                        m.SetTextureOffset("_MainTex", new Vector2(1f, 0f));
                    }))
                    .With(t => GetIvaNavBallMaterials());

            if (workEmissivetextureRef != null)
            {
                workEmissivetextureRef
                    .Do(t =>
                        ForEachIvaMaterial(m =>
                        {
                            m.SetTexture("_Emissive", t);
                            m.SetTextureScale("_Emissive", new Vector2(-1f, 1f));
                            m.SetTextureOffset("_Emissive", new Vector2(1f, 0f));
                        }))
                        .With(t => GetIvaNavBallMaterials());


                ForEachIvaMaterial(m =>
                {
                    m.SetColor("_EmissiveColor", workEmissiveColor);
                });
            }
        }

#if false

        public void PersistenceLoad()
        {
            _mainTextureRef = GetTextureUsingUrl(TextureUrl).Or(_stockTexture.Value);
            _emissiveTextureRef = GetTextureUsingUrl(EmissiveUrl).Or((Texture2D)null);
        }

        public void SetTexture(string textureUrl, string emissiveUrl)
        {
            TextureUrl = textureUrl;
            EmissiveUrl = emissiveUrl;
            _mainTextureRef = GetTextureUsingUrl(TextureUrl).Or(_stockTexture.Value);
            _emissiveTextureRef = GetTextureUsingUrl(EmissiveUrl).Or((Texture)null);
            SetTexture((Texture2D)_mainTextureRef, (Texture2D)_emissiveTextureRef, EmissiveColor);
        }
#endif
        public void SetTexture(FileEmissive fe, bool test = false)
        {
            if (!test)
                TestTexture = new TextureInfo(fe.image, fe.emissiveImg, fe.file, fe.emissive, fe.EmissiveColor);
            else
                TestTexture = new TextureInfo(fe.image, fe.emissiveImg, fe.file, fe.emissive, fe.EmissiveColor);

            if (HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                if (!NavBallChanger.IVAactive)
                    SetFlightTexture(test);
                else
                    SetIvaTextures(test);

                TextureUrl = "NavBallTextureChanger/" + TestTexture.TextureUrl;
                EmissiveUrl = "NavBallTextureChanger/" +TestTexture.EmissiveUrl;
                EmissiveColor = TestTexture.EmissiveColor;

                _mainTextureRef = fe.image;
                _emissiveTextureRef = fe.emissiveImg;
                EmissiveColor = fe.EmissiveColor;

                SaveConfig();
            }
        }

        public void SetEmissiveColor(Color ec)
        {
            EmissiveColor = ec;
            SetIvaTextures(false);
        }

        public void ResetTexture()
        {
            if (SavedTexture != null)
            {
                _mainTextureRef = SavedTexture.MainTextureRef;
                _emissiveTextureRef = SavedTexture.EmissiveTextureRef;
                TextureUrl = SavedTexture.TextureUrl;
                EmissiveUrl = SavedTexture.EmissiveUrl;
                EmissiveColor = SavedTexture.EmissiveColor;

                var stockUrl = _skinDirectory + "/" + Path.GetFileNameWithoutExtension("SavedTexture-" + StockTextureFileName);
                SaveCopyOfTexture(stockUrl, SavedTexture.MainTextureRef);
                // Maybe use SetTexture below???
                if (HighLogic.LoadedScene == GameScenes.FLIGHT)
                {
                    if (!NavBallChanger.IVAactive)
                        SetFlightTexture();
                    else
                        SetIvaTextures();
                }
            }
            else
                Log.Error("SavedTexture is null, unable to restore original");
        }

        public void ResetToStockTexture()
        {
            TextureUrl = _skinDirectory + "/" + StockTextureFileName;
            EmissiveUrl = _skinDirectory + "/" + IvaTextureFileName;
            EmissiveColor = DefaultEmissiveColor;
            Flight = true;
            Iva = true;
            SaveConfig();
            LoadConfig();
        }
        public void SaveTexture()
        {
            SavedTexture = new TextureInfo((Texture2D)_mainTextureRef, (Texture2D)_emissiveTextureRef, TextureUrl, EmissiveUrl, EmissiveColor);
            if (_mainTextureRef == null)
                Log.Info("_mainTextureRef is null");
            if (SavedTexture.MainTextureRef == null)
                Log.Info("SaveTexture, MainTexRef is null");
        }

        public void SetTexture(Texture2D texture, Texture2D emissive, Color EmissiveColor, bool test = false)
        {
            _mainTextureRef = texture;
            if (emissive != null)
            {
                _emissiveTextureRef = emissive;
                this.EmissiveColor = EmissiveColor;
            }
            else
            {
                _emissiveTextureRef = DefaultEmissiveTexture;
                EmissiveColor = DefaultEmissiveColor;
            }

            if (HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                if (!NavBallChanger.IVAactive)
                    SetFlightTexture();
                else
                    SetIvaTextures();
            }
        }
    }
}