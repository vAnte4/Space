// ============================================================
//  GameUI.cs  -  TODAS LAS PANTALLAS DEL JUEGO
//  Título -> Elegir destino -> Instrucciones -> Base (juego)
//  -> Evento / Informe de la semana -> Resultado final
//  Todo se dibuja por código (ver UIKit.cs). No hay que armar nada
//  en el editor: GameBootstrap.cs crea este objeto solo.
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace AstroJunior
{
    public class GameUI : MonoBehaviour
    {
        class Bob
        {
            public RectTransform rt;
            public float x, y, amp, speed, phase, walk;
        }

        // ---- capas ----
        RectTransform root, starLayer, layer, modal;
        List<Image> starImgs = new List<Image>();
        List<float> starPhase = new List<float>();
        List<float> starSpeed = new List<float>();
        List<Bob> bobs = new List<Bob>();
        List<BarRef> bars = new List<BarRef>();

        // ---- estado del juego ----
        Mission m;
        SpaceWeatherInfo wx;
        SiteDef site;
        System.Random rnd = new System.Random();
        HashSet<int> usedFacts = new HashSet<int>();
        WeekReport currentReport;
        bool inMain = false;
        bool stormFlash = false;

        // ---- elementos que se actualizan ----
        Text titleStatus;
        RectTransform extraHolder;   // recuadros con datos extra de NASA (se redibujan al llegar los datos)
        int screen = 0;              // 1 = título, 2 = instrucciones, 3 = final
        Text tTitle, tBadge, tTip, tWeather, tEnergyInfo, tShieldVal, tScienceVal;
        Image badge;
        Text[] tMeterVal = new Text[4];
        BarRef[] meter = new BarRef[4];
        BarRef energyBar, scienceBar;
        Image[] shieldSq = new Image[10];
        Image[][] pips = new Image[4][];
        Text[] tFx = new Text[4];
        Button[] btnPlus = new Button[4];
        Button[] btnMinus = new Button[4];
        Image sunImg, nightOverlay, dustOverlay, stormOverlay;
        Image[] mounds = new Image[10];
        List<Image> panelImgs = new List<Image>();

        static readonly string[] SysName = { "Soporte vital", "Invernadero", "Escudo de regolito", "Ciencia" };
        static readonly string[] SysDesc =
        {
            "Fabrica aire para respirar.",
            "Luces para cultivar comida.",
            "Cubre la base con regolito (polvo y piedras): menos radiación.",
            "Experimentos que dan puntos."
        };
        static readonly Color[] SysColor = { UIKit.Cyan, UIKit.Green, UIKit.Orange, UIKit.Purple };

        // ======================================================
        //  ARRANQUE
        // ======================================================
        void Start()
        {
            Application.targetFrameRate = 60;
            UIKit.Init();
            AudioFx.Init(gameObject);
            SetupCamera();
            BuildCanvas();
            BuildStars();

            wx = SpaceWeatherInfo.Loading();
            StartCoroutine(SpaceWeatherService.Fetch(delegate (SpaceWeatherInfo info)
            {
                wx = info;
                if (m != null && m.wx.loading) m.wx = info;
                if (titleStatus != null) titleStatus.text = wx.Headline();
                if (inMain) RefreshMain();
            }));
            NasaExtras.Changed = OnExtrasChanged;
            NasaExtras.StartAll(this);
            ShowTitle();
        }

        void SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam == null) cam = FindObjectOfType<Camera>();
            if (cam == null)
            {
                GameObject c = new GameObject("Camera");
                cam = c.AddComponent<Camera>();
                c.tag = "MainCamera";
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = UIKit.Navy;
        }

        void BuildCanvas()
        {
            GameObject cg = new GameObject("Canvas");
            Canvas canvas = cg.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler cs = cg.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920f, 1080f);
            cs.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            cg.AddComponent<GraphicRaycaster>();

            if (FindObjectOfType<EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            RectTransform bg = UIKit.Stretch("Fondo", cg.transform);
            Image bi = bg.gameObject.AddComponent<Image>();
            bi.color = UIKit.Navy;
            bi.raycastTarget = false;

            starLayer = UIKit.Stretch("Estrellas", cg.transform);

            GameObject rg = new GameObject("Contenido", typeof(RectTransform));
            root = rg.GetComponent<RectTransform>();
            root.SetParent(cg.transform, false);
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(1920f, 1080f);
            root.anchoredPosition = Vector2.zero;

            layer = UIKit.Stretch("Pantalla", root);
            modal = UIKit.Stretch("Ventana", root);
        }

        void BuildStars()
        {
            for (int i = 0; i < 110; i++)
            {
                float s = 2f + (float)rnd.NextDouble() * 4f;
                GameObject go = new GameObject("e", typeof(RectTransform));
                RectTransform rt = go.GetComponent<RectTransform>();
                rt.SetParent(starLayer, false);
                Vector2 a = new Vector2((float)rnd.NextDouble(), (float)rnd.NextDouble());
                rt.anchorMin = a;
                rt.anchorMax = a;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(s, s);
                Image img = go.AddComponent<Image>();
                img.sprite = UIKit.circle;
                img.color = new Color(1f, 1f, 1f, 0.6f);
                img.raycastTarget = false;
                starImgs.Add(img);
                starPhase.Add((float)rnd.NextDouble() * 6.28f);
                starSpeed.Add(0.6f + (float)rnd.NextDouble() * 2f);
            }
        }

        // ======================================================
        //  ANIMACIONES (cada fotograma)
        // ======================================================
        void Update()
        {
            float t = Time.time;
            for (int i = 0; i < starImgs.Count; i++)
            {
                Color c = starImgs[i].color;
                c.a = 0.3f + 0.7f * (0.5f + 0.5f * Mathf.Sin(t * starSpeed[i] + starPhase[i]));
                starImgs[i].color = c;
            }

            float k = 1f - Mathf.Exp(-9f * Time.deltaTime);
            for (int i = 0; i < bars.Count; i++)
            {
                BarRef b = bars[i];
                b.cur = Mathf.Lerp(b.cur, b.target, k);
                b.Apply();
            }

            for (int i = 0; i < bobs.Count; i++)
            {
                Bob b = bobs[i];
                if (b.rt == null) continue;
                float sn = Mathf.Sin(t * b.speed + b.phase);
                b.rt.anchoredPosition = new Vector2(b.x + Mathf.Sin(t * 0.5f + b.phase) * b.walk, -(b.y + sn * b.amp));
            }

            if (stormOverlay != null)
            {
                Color c = stormOverlay.color;
                c.a = stormFlash ? 0.12f + 0.12f * Mathf.Sin(t * 9f) : 0f;
                stormOverlay.color = c;
            }
        }

        // ======================================================
        //  AYUDANTES
        // ======================================================
        static Text T(Transform p, string s, float x, float y, float w, float h, int size, Color c)
        {
            return UIKit.Label(p, s, x, y, w, h, size, c, TextAnchor.UpperLeft, FontStyle.Normal);
        }
        static Text TC(Transform p, string s, float x, float y, float w, float h, int size, Color c)
        {
            return UIKit.Label(p, s, x, y, w, h, size, c, TextAnchor.MiddleCenter, FontStyle.Bold);
        }
        static Text TB(Transform p, string s, float x, float y, float w, float h, int size, Color c)
        {
            return UIKit.Label(p, s, x, y, w, h, size, c, TextAnchor.MiddleLeft, FontStyle.Bold);
        }
        static Text TR(Transform p, string s, float x, float y, float w, float h, int size, Color c)
        {
            return UIKit.Label(p, s, x, y, w, h, size, c, TextAnchor.MiddleRight, FontStyle.Bold);
        }

        void ClearScreen()
        {
            UIKit.Clear(layer);
            UIKit.Clear(modal);
            bars.Clear();
            bobs.Clear();
            inMain = false;
            stormFlash = false;
            titleStatus = null;
            extraHolder = null;
            screen = 0;
            sunImg = null; nightOverlay = null; dustOverlay = null; stormOverlay = null;
            panelImgs.Clear();
        }

        void AddBob(RectTransform rt, float x, float y, float amp, float speed, float walk)
        {
            Bob b = new Bob();
            b.rt = rt; b.x = x; b.y = y; b.amp = amp; b.speed = speed; b.walk = walk;
            b.phase = (float)rnd.NextDouble() * 6.28f;
            bobs.Add(b);
        }

        BarRef NewBar(Transform p, float x, float y, float w, float h, Color c)
        {
            BarRef b = UIKit.Bar(p, x, y, w, h, c);
            bars.Add(b);
            return b;
        }

        // Polar, el oso astronauta (mascota del equipo Polarsten)
        RectTransform DrawPolar(Transform parent, float cx, float cy, float s, bool bob)
        {
            float S = 300f * s;
            float px = cx - S * 0.5f, py = cy - S * 0.5f;
            RectTransform c = UIKit.NewRect("Polar", parent, px, py, S, S);
            float o = S * 0.5f;
            Color white = new Color(0.96f, 0.97f, 1f, 1f);
            Color dark = new Color(0.08f, 0.1f, 0.2f, 1f);
            UIKit.Box(c, o - 70f * s, o + 55f * s, 140f * s, 120f * s, white, true);
            UIKit.Box(c, o - 32f * s, o + 88f * s, 64f * s, 40f * s, UIKit.Orange, true);
            UIKit.Circle(c, o - 62f * s, o - 70f * s, 56f * s, white);
            UIKit.Circle(c, o + 62f * s, o - 70f * s, 56f * s, white);
            UIKit.Circle(c, o - 62f * s, o - 70f * s, 28f * s, new Color(0.9f, 0.7f, 0.75f, 1f));
            UIKit.Circle(c, o + 62f * s, o - 70f * s, 28f * s, new Color(0.9f, 0.7f, 0.75f, 1f));
            UIKit.Circle(c, o, o - 10f * s, 170f * s, white);
            UIKit.Circle(c, o, o + 20f * s, 74f * s, new Color(0.86f, 0.89f, 0.96f, 1f));
            UIKit.Circle(c, o, o + 6f * s, 24f * s, dark);
            UIKit.Circle(c, o - 34f * s, o - 26f * s, 18f * s, dark);
            UIKit.Circle(c, o + 34f * s, o - 26f * s, 18f * s, dark);
            UIKit.Circle(c, o, o - 10f * s, 220f * s, new Color(0.5f, 0.85f, 1f, 0.26f));
            if (bob) AddBob(c, px, py, 7f * s, 2.2f, 0f);
            return c;
        }

        RectTransform DrawAstronaut(Transform parent, float x, float y, float s)
        {
            RectTransform c = UIKit.NewRect("Astronauta", parent, x, y, 30f * s, 50f * s);
            Color white = new Color(0.97f, 0.97f, 1f, 1f);
            UIKit.Box(c, 4f * s, 20f * s, 22f * s, 28f * s, white, true);
            UIKit.Circle(c, 15f * s, 14f * s, 24f * s, white);
            UIKit.Circle(c, 15f * s, 14f * s, 15f * s, new Color(0.1f, 0.25f, 0.45f, 1f));
            UIKit.Box(c, 9f * s, 30f * s, 12f * s, 8f * s, UIKit.Orange, true);
            return c;
        }

        // ======================================================
        //  PANTALLA: TÍTULO
        // ======================================================
        void ShowTitle()
        {
            ClearScreen();
            TC(layer, "ASTRO JUNIOR", 0, 60, 1920, 170, 150, UIKit.Yellow);
            TC(layer, "Entrenador de astronautas", 0, 230, 1920, 70, 56, UIKit.White);
            UIKit.Label(layer, "Equipo Polarsten · NASA Space Apps Challenge 2026", 0, 305, 1920, 46, 30, UIKit.Gray, TextAnchor.MiddleCenter, FontStyle.Normal);
            DrawPolar(layer, 960, 530, 1.25f, true);
            UIKit.Btn(layer, "¡JUGAR!", 700, 790, 520, 130, UIKit.Green, delegate { ShowSites(); }, 64);
            UIKit.Btn(layer, "¿De dónde salen los datos?", 700, 935, 520, 70, UIKit.PanelLight, delegate { ShowAbout(); }, 28);
            titleStatus = UIKit.Label(layer, wx.Headline(), 0, 1020, 1920, 44, 26, UIKit.Cyan, TextAnchor.MiddleCenter, FontStyle.Normal);
            screen = 1;
            extraHolder = UIKit.NewRect("Extra", layer, 0, 0, 1920, 1080);
            DrawExtras();
        }

        void ShowAbout()
        {
            UIKit.Clear(modal);
            Image dim = UIKit.Box(modal, 0, 0, 1920, 1080, new Color(0f, 0f, 0f, 0.7f), false);
            dim.raycastTarget = true;
            UIKit.Box(modal, 210, 50, 1500, 980, UIKit.Panel);
            TC(modal, "¿De dónde salen los datos?", 240, 65, 1440, 80, 56, UIKit.Yellow);
            string s =
                "EN VIVO · NASA DONKI (api.nasa.gov): llamaradas solares (FLR) y tormentas de partículas (SEP) de los últimos 30 días. Deciden qué tan seguido hay tormentas solares en el juego.\n\n" +
                "Si no hay internet, el juego usa un ARCHIVO de grandes llamaradas reales (2017 y 2024) y lo marca como \"archivo\", nunca como \"en vivo\".\n\n" +
                "Radiación en Marte: 0.67 mSv por día · rover Curiosity (NASA/SwRI, Science 2014).\n" +
                "Radiación en la Luna: 1.37 mSv por día · lander Chang'e-4 (China y Alemania, Science Advances 2020).\n" +
                "Límite de radiación: NASA permite 600 mSv en toda la carrera de un astronauta (NASA-STD-3001). En este juego la misión permite 120.\n" +
                "Comunicación: 1.3 segundos a la Luna, de 3 a 22 minutos a Marte (NASA SCaN).\n\n" +
                "Extras de NASA (si hay internet): Foto del día (APOD), foto reciente del rover Curiosity (Mars Rover Photos), medición de radiación en la Estación Espacial (RadLab) y un estudio real del repositorio OSDR al final de tu misión. Datos curiosos de comida: Agencia Espacial Canadiense (CSA).\n\n" +
                "Números de JUEGO (simplificados, no medidos): energía solar por semana, aire y comida que gasta la tripulación, efecto de cada capa de escudo y la dosis de las tormentas. Cada turno = 1 semana.";
            T(modal, s, 250, 160, 1420, 700, 28, UIKit.White);
            UIKit.Btn(modal, "Cerrar", 760, 900, 400, 90, UIKit.Green, delegate { UIKit.Clear(modal); }, 40);
        }

        // ======================================================
        //  PANTALLA: ELEGIR DESTINO
        // ======================================================
        void ShowSites()
        {
            ClearScreen();
            TC(layer, "Elige tu destino", 0, 25, 1920, 120, 84, UIKit.Yellow);
            SiteCard(GameData.Moon, 90, 165);
            SiteCard(GameData.Mars, 1010, 165);
            UIKit.Label(layer, "mSv = milisievert, la unidad que mide cuánta radiación recibe tu cuerpo.", 0, 965, 1920, 44, 28, UIKit.Gray, TextAnchor.MiddleCenter, FontStyle.Normal);
            UIKit.Btn(layer, "Volver", 40, 25, 200, 70, UIKit.PanelLight, delegate { ShowTitle(); }, 32);
        }

        void SiteCard(SiteDef d, float x, float y)
        {
            bool moon = d.id == SiteId.Moon;
            UIKit.Box(layer, x, y, 820, 780, UIKit.Panel);
            RectTransform pl = UIKit.NewRect("Planeta", layer, x + 300, y + 30, 220, 220);
            Color pc = moon ? new Color(0.78f, 0.78f, 0.82f, 1f) : new Color(0.82f, 0.38f, 0.22f, 1f);
            Color cr = moon ? new Color(0.6f, 0.6f, 0.66f, 1f) : new Color(0.62f, 0.27f, 0.15f, 1f);
            UIKit.Circle(pl, 110, 110, 220, pc);
            UIKit.Circle(pl, 70, 80, 50, cr);
            UIKit.Circle(pl, 150, 130, 70, cr);
            UIKit.Circle(pl, 100, 170, 34, cr);
            AddBob(pl, x + 300, y + 30, 8f, 1.6f, 0f);
            TC(layer, moon ? "LA LUNA" : "MARTE", x, y + 265, 820, 80, 68, UIKit.White);
            UIKit.Label(layer, d.baseName, x, y + 340, 820, 36, 28, UIKit.Cyan, TextAnchor.MiddleCenter, FontStyle.Normal);
            T(layer, d.stat1, x + 40, y + 395, 740, 100, 30, UIKit.White);
            T(layer, d.stat2, x + 40, y + 500, 740, 100, 30, UIKit.White);
            T(layer, d.stat3, x + 40, y + 605, 740, 70, 30, UIKit.White);
            SiteDef pick = d;
            UIKit.Btn(layer, moon ? "ELEGIR LA LUNA" : "ELEGIR MARTE", x + 160, y + 690, 500, 80, moon ? UIKit.PanelLight : UIKit.Orange, delegate { StartMission(pick); }, 40);
        }

        void StartMission(SiteDef s)
        {
            site = s;
            m = new Mission(s, wx, rnd.Next());
            usedFacts.Clear();
            ShowBriefing();
        }

        // ======================================================
        //  PANTALLA: INSTRUCCIONES
        // ======================================================
        void ShowBriefing()
        {
            ClearScreen();
            UIKit.Box(layer, 200, 70, 1520, 940, UIKit.Panel);
            TC(layer, "Tu misión en " + site.name, 200, 85, 1520, 100, 66, UIKit.Yellow);
            DrawPolar(layer, 420, 520, 1.0f, true);
            string s =
                "• Cuidas a 4 astronautas durante " + m.totalWeeks + " semanas.\n" +
                "• Meta: sobrevivir y juntar " + m.scienceGoal + " puntos de ciencia.\n" +
                "• Cada semana repartes energía con + y -. Cada nivel cuesta " + Mission.CostPerLevel + " de energía.\n" +
                "• Soporte vital = aire. Invernadero = comida. Escudo = menos radiación. Ciencia = puntos.\n" +
                "• La batería guarda energía, pero si se llena, lo que sobra se pierde.\n" +
                "• Pasarán cosas inesperadas. ¡Tú decides y todo tiene un costo!";
            T(layer, s, 640, 210, 1020, 600, 34, UIKit.White);
            UIKit.Btn(layer, "¡DESPEGAR!", 700, 850, 520, 120, UIKit.Green, delegate { ShowMain(); }, 60);
            screen = 2;
            extraHolder = UIKit.NewRect("Extra", layer, 0, 0, 1920, 1080);
            DrawExtras();
        }

        // ======================================================
        //  PANTALLA: LA BASE (juego principal)
        // ======================================================
        void ShowMain()
        {
            ClearScreen();
            BuildMain();
            inMain = true;
            RefreshMain();
            for (int i = 0; i < bars.Count; i++) { bars[i].cur = bars[i].target; bars[i].Apply(); }
        }

        void BuildMain()
        {
            // ---- barra de arriba ----
            UIKit.Box(layer, 0, 0, 1920, 92, UIKit.Panel, false);
            tTitle = TB(layer, "", 40, 8, 520, 76, 50, UIKit.White);
            TB(layer, site.baseName + " · " + site.name, 570, 8, 560, 76, 38, UIKit.Cyan);
            badge = UIKit.Box(layer, 1160, 19, 730, 54, UIKit.Green, true);
            tBadge = TC(layer, "", 1170, 19, 710, 54, 26, UIKit.White);

            // ---- columna izquierda: estado ----
            UIKit.Box(layer, 30, 110, 490, 940, UIKit.Panel);
            TB(layer, "ESTADO DE LA BASE", 50, 118, 450, 44, 32, UIKit.Cyan);
            string[] names = { "Energía", "Aire", "Comida", "Radiación" };
            string[] caps =
            {
                "La batería guarda energía para la noche y las emergencias.",
                "Si llega a 0, la tripulación no puede respirar.",
                "Si no hay comida por 2 semanas, la misión termina.",
                "mSv = unidad de radiación. Si llegas al límite, la misión termina. El escudo ayuda siempre."
            };
            Color[] mc = { UIKit.Yellow, UIKit.Cyan, UIKit.Green, UIKit.Orange };
            for (int i = 0; i < 4; i++)
            {
                float y0 = 172f + i * 146f;
                TB(layer, names[i], 50, y0, 280, 40, 32, mc[i]);
                tMeterVal[i] = TR(layer, "", 290, y0, 210, 40, 28, UIKit.White);
                meter[i] = NewBar(layer, 50, y0 + 46f, 450, 34, mc[i]);
                T(layer, caps[i], 50, y0 + 88f, 450, 50, 20, UIKit.Gray);
            }
            TB(layer, "Escudo de regolito", 50, 765, 300, 36, 30, UIKit.Orange);
            tShieldVal = TR(layer, "", 290, 765, 210, 36, 26, UIKit.White);
            for (int i = 0; i < 10; i++) shieldSq[i] = UIKit.Box(layer, 50f + i * 45f, 808, 40, 40, new Color(1f, 1f, 1f, 0.12f), true);
            TB(layer, "Ciencia", 50, 868, 300, 36, 30, UIKit.Purple);
            tScienceVal = TR(layer, "", 290, 868, 210, 36, 26, UIKit.White);
            scienceBar = NewBar(layer, 50, 912, 450, 30, UIKit.Purple);
            T(layer, "Meta: " + m.scienceGoal + " puntos para ganar la 2.ª estrella.", 50, 952, 450, 40, 20, UIKit.Gray);

            // ---- centro: escena + Polar + clima espacial ----
            BuildScene(550, 110);
            DrawPolar(layer, 610, 705, 0.5f, true);
            UIKit.Box(layer, 700, 605, 680, 185, new Color(0.95f, 0.97f, 1f, 1f), true);
            tTip = T(layer, "", 720, 612, 640, 172, 28, UIKit.Dark);
            UIKit.Box(layer, 550, 805, 830, 245, UIKit.Panel);
            TB(layer, "CLIMA ESPACIAL REAL · NASA DONKI", 570, 810, 790, 36, 28, UIKit.Cyan);
            tWeather = T(layer, "", 570, 850, 790, 195, 23, UIKit.White);

            // ---- columna derecha: panel de control ----
            UIKit.Box(layer, 1400, 110, 490, 940, UIKit.Panel);
            TB(layer, "PANEL DE CONTROL", 1420, 118, 450, 44, 32, UIKit.Yellow);
            T(layer, "Cada nivel cuesta " + Mission.CostPerLevel + " de energía. Reparte con + y -.", 1420, 160, 450, 32, 22, UIKit.Gray);
            for (int i = 0; i < 4; i++)
            {
                float y = 200f + i * 138f;
                Sys s = (Sys)i;
                UIKit.Box(layer, 1412, y, 466, 128, UIKit.PanelLight, true);
                TB(layer, SysName[i], 1428, y + 6f, 290, 36, 30, SysColor[i]);
                T(layer, SysDesc[i], 1428, y + 42f, 296, 52, 19, UIKit.Gray);
                pips[i] = new Image[Mission.MaxLevel];
                for (int k = 0; k < Mission.MaxLevel; k++)
                    pips[i][k] = UIKit.Box(layer, 1428f + k * 46f, y + 98f, 40, 22, new Color(1f, 1f, 1f, 0.15f), true);
                tFx[i] = TB(layer, "", 1572, y + 94f, 150, 30, 21, UIKit.White);
                btnMinus[i] = UIKit.Btn(layer, "-", 1730, y + 28f, 66, 66, UIKit.Red, delegate { m.Lower(s); RefreshMain(); }, 46);
                btnPlus[i] = UIKit.Btn(layer, "+", 1806, y + 28f, 66, 66, UIKit.Green, delegate { m.Raise(s); RefreshMain(); }, 46);
            }
            TB(layer, "Energía de esta semana", 1420, 756, 450, 36, 28, UIKit.Yellow);
            tEnergyInfo = T(layer, "", 1420, 794, 450, 78, 21, UIKit.White);
            energyBar = NewBar(layer, 1420, 876, 450, 26, UIKit.Yellow);
            UIKit.Btn(layer, "TERMINAR SEMANA", 1412, 920, 466, 116, UIKit.Green, delegate { OnEndWeek(); }, 40);
        }

        void BuildScene(float x, float y)
        {
            float W = 830f, H = 470f;
            RectTransform sc = UIKit.NewRect("Escena", layer, x, y, W, H);
            sc.gameObject.AddComponent<RectMask2D>();
            bool moon = site.id == SiteId.Moon;
            Color sky = moon ? new Color(0.02f, 0.03f, 0.09f, 1f) : new Color(0.55f, 0.32f, 0.25f, 1f);
            Color ground = moon ? new Color(0.62f, 0.62f, 0.66f, 1f) : new Color(0.72f, 0.35f, 0.2f, 1f);
            Color dark = moon ? new Color(0.5f, 0.5f, 0.55f, 1f) : new Color(0.6f, 0.28f, 0.16f, 1f);

            UIKit.Box(sc, 0, 0, W, H, sky, false);
            if (moon)
            {
                for (int i = 0; i < 45; i++)
                    UIKit.Circle(sc, (float)rnd.NextDouble() * W, (float)rnd.NextDouble() * 300f, 2f + (float)rnd.NextDouble() * 3f, new Color(1f, 1f, 1f, 0.8f));
                UIKit.Circle(sc, 710, 90, 84, new Color(0.2f, 0.45f, 0.95f, 1f));
                UIKit.Circle(sc, 694, 78, 30, new Color(0.3f, 0.75f, 0.4f, 1f));
                UIKit.Circle(sc, 729, 108, 24, new Color(0.3f, 0.75f, 0.4f, 1f));
            }
            else
            {
                UIKit.Box(sc, 0, 190, W, 150, new Color(0.9f, 0.6f, 0.45f, 0.35f), false);
            }
            sunImg = UIKit.Circle(sc, 110, 80, moon ? 64f : 36f, new Color(1f, 0.95f, 0.7f, 1f));

            // domo (se dibuja antes del suelo para que el suelo tape su mitad de abajo)
            UIKit.Circle(sc, 415, 335, 200, new Color(0.7f, 0.9f, 1f, 0.6f));
            UIKit.Circle(sc, 415, 335, 160, new Color(1f, 0.95f, 0.7f, 0.35f));

            // suelo
            UIKit.Circle(sc, 415, 1230, 1800, ground);
            UIKit.Circle(sc, 120, 410, 90, dark);
            UIKit.Circle(sc, 640, 440, 120, dark);
            UIKit.Circle(sc, 330, 450, 60, dark);
            UIKit.Circle(sc, 760, 385, 46, dark);
            UIKit.Circle(sc, 520, 395, 36, dark);

            // montículos de regolito = capas de escudo
            for (int i = 0; i < 10; i++)
            {
                float a = (170f - i * 16f) * Mathf.Deg2Rad;
                float mx = 415f + Mathf.Cos(a) * 112f;
                float my = 340f - Mathf.Sin(a) * 112f;
                mounds[i] = UIKit.Circle(sc, mx, my, 46f, moon ? new Color(0.45f, 0.45f, 0.5f, 1f) : new Color(0.55f, 0.28f, 0.18f, 1f));
            }

            // módulos
            UIKit.Box(sc, 160, 305, 130, 64, new Color(0.9f, 0.92f, 0.97f, 1f), true);
            UIKit.Circle(sc, 200, 335, 24, new Color(0.2f, 0.3f, 0.5f, 1f));
            UIKit.Circle(sc, 250, 335, 24, new Color(0.2f, 0.3f, 0.5f, 1f));
            UIKit.Box(sc, 540, 312, 104, 56, new Color(0.9f, 0.92f, 0.97f, 1f), true);
            UIKit.Circle(sc, 575, 340, 20, new Color(0.2f, 0.3f, 0.5f, 1f));
            UIKit.Box(sc, 285, 336, 40, 16, new Color(0.7f, 0.72f, 0.78f, 1f), false);
            UIKit.Box(sc, 505, 336, 40, 16, new Color(0.7f, 0.72f, 0.78f, 1f), false);
            UIKit.Box(sc, 618, 262, 5, 52, new Color(0.8f, 0.82f, 0.88f, 1f), false);
            UIKit.Circle(sc, 620, 260, 16, UIKit.Red);

            // paneles solares
            panelImgs.Clear();
            for (int i = 0; i < 3; i++)
            {
                float px = 668f + i * 52f;
                UIKit.Box(sc, px + 24f, 398f, 4, 22, new Color(0.7f, 0.72f, 0.78f, 1f), false);
                Image p = UIKit.Box(sc, px, 366f + i * 3f, 46, 32, new Color(0.15f, 0.35f, 0.85f, 1f), true);
                p.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 10f);
                panelImgs.Add(p);
            }

            // astronautas
            RectTransform a1 = DrawAstronaut(sc, 330, 372, 1.0f);
            AddBob(a1, 330, 372, 3f, 7f, 40f);
            RectTransform a2 = DrawAstronaut(sc, 455, 385, 1.0f);
            AddBob(a2, 455, 385, 3f, 6f, 35f);
            RectTransform a3 = DrawAstronaut(sc, 120, 378, 0.9f);
            AddBob(a3, 120, 378, 3f, 8f, 30f);
            RectTransform a4 = DrawAstronaut(sc, 560, 392, 0.9f);
            AddBob(a4, 560, 392, 3f, 6.5f, 25f);

            UIKit.Label(sc, site.baseName, 14, 8, 300, 36, 28, new Color(1f, 1f, 1f, 0.85f), TextAnchor.MiddleLeft, FontStyle.Bold);

            // filtros de color (noche, polvo, tormenta)
            nightOverlay = UIKit.Box(sc, 0, 0, W, H, new Color(0f, 0f, 0.06f, 0f), false);
            dustOverlay = UIKit.Box(sc, 0, 0, W, H, new Color(0.8f, 0.6f, 0.35f, 0f), false);
            stormOverlay = UIKit.Box(sc, 0, 0, W, H, new Color(1f, 0.1f, 0.1f, 0f), false);
        }

        // ---------------- Actualizar todo lo que cambia ----------------
        void RefreshMain()
        {
            if (!inMain || m == null) return;
            tTitle.text = "Semana " + m.week + " de " + m.totalWeeks;
            badge.color = wx.isLive ? new Color(0.1f, 0.55f, 0.3f, 1f) : new Color(0.75f, 0.4f, 0.1f, 1f);
            tBadge.text = wx.Headline();

            meter[0].target = m.battery / (float)m.batteryMax;
            tMeterVal[0].text = m.battery + " / " + m.batteryMax;
            meter[1].target = m.air / 100f;
            tMeterVal[1].text = Mathf.RoundToInt(m.air) + " / 100";
            meter[2].target = m.food / 100f;
            tMeterVal[2].text = Mathf.RoundToInt(m.food) + " / 100";
            float df = m.dose / m.doseLimit;
            meter[3].target = df;
            tMeterVal[3].text = Mission.F(m.dose) + " / " + Mission.F(m.doseLimit) + " mSv";
            meter[3].fill.color = df < 0.5f ? UIKit.Green : (df < 0.8f ? UIKit.Orange : UIKit.Red);
            tMeterVal[1].color = m.air < 30f ? UIKit.Red : UIKit.White;
            tMeterVal[2].color = m.food < 30f ? UIKit.Red : UIKit.White;

            int pending = m.level[(int)Sys.Shield];
            for (int i = 0; i < 10; i++)
            {
                Color c = new Color(1f, 1f, 1f, 0.12f);
                if (i < m.shield) c = new Color(0.8f, 0.58f, 0.35f, 1f);
                else if (i < m.shield + pending) c = new Color(1f, 0.75f, 0.4f, 0.55f);
                shieldSq[i].color = c;
            }
            tShieldVal.text = m.shield + " / " + m.shieldMax + " capas";
            scienceBar.target = Mathf.Clamp01(m.science / (float)m.scienceGoal);
            tScienceVal.text = m.science + " / " + m.scienceGoal;

            for (int i = 0; i < 4; i++)
            {
                Sys s = (Sys)i;
                for (int k = 0; k < Mission.MaxLevel; k++)
                    pips[i][k].color = k < m.level[i] ? SysColor[i] : new Color(1f, 1f, 1f, 0.15f);
                int L = m.level[i];
                string fx = "";
                if (i == 0) fx = "+" + Mission.F(L * Mission.AirPerLevel) + " aire";
                else if (i == 1) fx = "+" + Mission.F(L * Mission.FoodPerLevel) + " comida";
                else if (i == 2) fx = "+" + L + " capas";
                else fx = "+" + (L * 10) + " puntos";
                tFx[i].text = fx;
                btnMinus[i].interactable = L > 0;
                btnPlus[i].interactable = m.CanRaise(s);
            }

            int solar = m.SolarNow, avail = m.PowerAvailable, used = m.PowerUsed;
            int left = avail - used;
            string e = "Disponible: " + avail + " (batería " + m.battery + " + Sol " + solar + ")\nVas a usar: " + used + "  ·  Sobra: " + left;
            if (left > m.batteryMax) e += "\n¡Ojo! La batería se llena y se perderían " + (left - m.batteryMax) + ".";
            else if (solar == 0) e += "\nEs de noche: solo cuentas con la batería.";
            tEnergyInfo.text = e;
            energyBar.target = avail > 0 ? used / (float)avail : 0f;

            string w = wx.Describe() + "\n" + wx.Strongest() + "\nProbabilidad de tormenta solar esta semana: " + Mathf.RoundToInt(wx.stormChance * 100f) + " %";
            if (site.longNights)
                w += "\nSol esta semana: " + (m.BaseSolar(m.week) > 0 ? "SÍ" : "NO, es de noche") + "  ·  Próxima semana: " + (m.IsSunny(m.week + 1) ? "día" : "NOCHE");
            if (m.dustWeeksLeft > 0) w += "\nHay polvo en el aire: llega menos luz por " + m.dustWeeksLeft + " semana(s) más.";
            w += "\nMensajes a la Tierra: " + site.commText + ".";
            tWeather.text = w;

            tTip.text = Tip();

            // escena
            bool sunny = m.BaseSolar(m.week) > 0;
            if (sunImg != null) sunImg.enabled = sunny;
            SetAlpha(nightOverlay, sunny ? 0f : 0.5f);
            SetAlpha(dustOverlay, m.dustWeeksLeft > 0 ? 0.3f : 0f);
            for (int i = 0; i < 10; i++) if (mounds[i] != null) mounds[i].enabled = i < m.shield;
            Color pc = !sunny ? new Color(0.14f, 0.16f, 0.28f, 1f) : (m.dustWeeksLeft > 0 ? new Color(0.5f, 0.45f, 0.4f, 1f) : new Color(0.15f, 0.35f, 0.85f, 1f));
            for (int i = 0; i < panelImgs.Count; i++) panelImgs[i].color = pc;
        }

        static void SetAlpha(Image img, float a)
        {
            if (img == null) return;
            Color c = img.color;
            c.a = a;
            img.color = c;
        }

        string Tip()
        {
            if (m.week == 1) return "¡Hola! Soy Polar. Reparte la energía con + y -. Cuida el aire y la comida, ¡y no olvides el escudo contra la radiación!";
            if (m.air < 30f) return "¡El aire está muy bajo! Sube el soporte vital antes de que sea tarde.";
            if (m.food < 30f) return "Casi no queda comida. Las plantas tardan en crecer: enciende el invernadero.";
            if (m.dose > m.doseLimit * 0.6f) return "Ya recibimos mucha radiación. Más capas de escudo ayudan durante toda la misión.";
            if (site.longNights && m.IsSunny(m.week) && !m.IsSunny(m.week + 1)) return "¡Cuidado! Viene la noche lunar: 2 semanas sin Sol. Guarda energía en la batería.";
            if (m.dustWeeksLeft > 0) return "El polvo tapa el Sol, así que tenemos menos energía. Gasta con cuidado.";
            if (m.PowerAvailable - m.PowerUsed > m.batteryMax) return "La batería se va a llenar y se perdería energía. ¡Úsala en escudo o ciencia!";
            string[] pool =
            {
                "Un escudo se construye una vez y protege toda la misión.",
                "La ciencia da puntos, pero no se come ni se respira. ¡Equilibra!",
                "Una tormenta solar puede llegar cualquier semana. ¿Tienes escudo?",
                "Si ahorras energía hoy, mañana tendrás más. Pero ¿y el aire y la comida?"
            };
            return pool[(m.week + (int)m.air) % pool.Length];
        }

        // ======================================================
        //  FIN DE SEMANA, EVENTOS E INFORME
        // ======================================================
        void OnEndWeek()
        {
            if (m == null || m.ended) return;
            currentReport = m.RunWeek();
            GameEvent ev = m.RollEvent();
            if (ev != null) ShowEvent(ev);
            else
            {
                currentReport.factTag = m.ChooseFactTag(null);
                ShowReport(null);
            }
        }

        void ShowEvent(GameEvent ev)
        {
            UIKit.Clear(modal);
            stormFlash = ev.isStorm;
            if (ev.isStorm) AudioFx.Alarm(); else AudioFx.Bad();
            Image dim = UIKit.Box(modal, 0, 0, 1920, 1080, new Color(0f, 0f, 0f, 0.72f), false);
            dim.raycastTarget = true;
            UIKit.Box(modal, 300, 50, 1320, 980, UIKit.Panel);
            TC(modal, ev.title, 320, 65, 1280, 100, 60, ev.isStorm ? UIKit.Orange : UIKit.Yellow);
            T(modal, ev.story, 350, 175, 1220, 150, 32, UIKit.White);
            float y0 = 345f;
            if (ev.realDataLine.Length > 0)
            {
                UIKit.Box(modal, 350, 330, 1220, 130, new Color(0.1f, 0.35f, 0.5f, 0.6f), true);
                T(modal, ev.realDataLine, 368, 336, 1186, 120, 24, UIKit.Cyan);
                y0 = 480f;
            }
            for (int i = 0; i < ev.choices.Count; i++)
            {
                int idx = i;
                ChoiceButton(modal, 350, y0 + i * 150f, 1220, 136, ev.choices[i], delegate { ResolveChoice(ev, idx); });
            }
        }

        void ChoiceButton(Transform parent, float x, float y, float w, float h, Choice ch, UnityAction onClick)
        {
            bool ok = m.CanAfford(ch);
            Image box = UIKit.Box(parent, x, y, w, h, ok ? UIKit.PanelLight : new Color(0.2f, 0.2f, 0.25f, 1f), true);
            box.raycastTarget = true;
            Button b = box.gameObject.AddComponent<Button>();
            b.targetGraphic = box;
            ColorBlock cb = b.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(0.8f, 0.9f, 1f, 1f);
            cb.pressedColor = new Color(0.6f, 0.6f, 0.7f, 1f);
            cb.disabledColor = new Color(0.6f, 0.6f, 0.6f, 1f);
            b.colors = cb;
            b.interactable = ok;
            UIKit.Label(box.transform, ch.label, 24, 8, w - 48, 60, 36, ok ? UIKit.White : UIKit.Gray, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.Label(box.transform, ok ? ch.hint : "No tienes suficiente energía en la batería para esto.", 24, 70, w - 48, 58, 26,
                        ok ? UIKit.Cyan : UIKit.Red, TextAnchor.UpperLeft, FontStyle.Normal);
            b.onClick.AddListener(delegate { AudioFx.Click(); onClick(); });
        }

        void ResolveChoice(GameEvent ev, int idx)
        {
            stormFlash = false;
            m.ApplyChoice(ev, idx, currentReport);
            ShowReport(ev);
        }

        void ShowReport(GameEvent ev)
        {
            UIKit.Clear(modal);
            Image dim = UIKit.Box(modal, 0, 0, 1920, 1080, new Color(0f, 0f, 0f, 0.72f), false);
            dim.raycastTarget = true;
            UIKit.Box(modal, 260, 30, 1400, 1020, UIKit.Panel);
            string head = "Informe de la semana " + m.week;
            TC(modal, head, 280, 40, 1360, 80, 52, UIKit.Yellow);

            int n = Mathf.Min(currentReport.lines.Count, 12);
            for (int i = 0; i < n; i++)
            {
                ReportLine l = currentReport.lines[i];
                Color c = l.tone > 0 ? new Color(0.55f, 1f, 0.65f, 1f) : (l.tone < 0 ? new Color(1f, 0.6f, 0.55f, 1f) : UIKit.White);
                UIKit.Label(modal, l.text, 300, 125f + i * 42f, 1320, 40, 26, c, TextAnchor.MiddleLeft, FontStyle.Normal);
            }

            Fact f = GameData.PickFact(currentReport.factTag, site, rnd, usedFacts);
            UIKit.Box(modal, 290, 650, 1340, 260, new Color(0.12f, 0.3f, 0.45f, 1f), true);
            TB(modal, "¿Sabías que? " + f.title, 310, 656, 1300, 44, 32, UIKit.Yellow);
            T(modal, f.text, 310, 702, 1300, 140, 27, UIKit.White);
            UIKit.Label(modal, "Fuente: " + f.source, 310, 852, 1300, 50, 20, UIKit.Gray, TextAnchor.UpperLeft, FontStyle.Italic);

            UIKit.Btn(modal, "CONTINUAR", 610, 930, 700, 100, UIKit.Green, delegate { OnContinue(); }, 50);
        }

        void OnContinue()
        {
            m.FinishWeek();
            if (m.ended) { ShowEnd(); return; }
            UIKit.Clear(modal);
            RefreshMain();
        }

        // ======================================================
        //  PANTALLA: RESULTADO FINAL
        // ======================================================
        void ShowEnd()
        {
            ClearScreen();
            bool ok = m.success;
            if (ok) AudioFx.Good(); else AudioFx.Bad();
            TC(layer, m.EndTitle(), 0, 30, 1920, 130, 100, ok ? UIKit.Yellow : UIKit.Orange);
            TC(layer, m.EndText(), 260, 165, 1400, 110, 34, UIKit.White);
            for (int k = 0; k < 3; k++)
                UIKit.StarIcon(layer, 760f + k * 200f, 340f, 150f, k < m.Stars ? UIKit.Yellow : new Color(1f, 1f, 1f, 0.15f));

            DrawPolar(layer, 130, 640, 0.75f, true);

            UIKit.Box(layer, 260, 440, 640, 360, UIKit.Panel);
            TB(layer, "Tu misión en números", 280, 448, 600, 44, 32, UIKit.Cyan);
            string st =
                "Ciencia: " + m.science + " de " + m.scienceGoal + "\n" +
                "Radiación recibida: " + Mission.F(m.dose) + " de " + Mission.F(m.doseLimit) + " mSv\n" +
                "Aire al final: " + Mathf.RoundToInt(m.air) + " de 100\n" +
                "Comida al final: " + Mathf.RoundToInt(m.food) + " de 100\n" +
                "Capas de escudo: " + m.shield + " de " + m.shieldMax + "\n" +
                "Energía perdida: " + m.wastedPower;
            T(layer, st, 280, 498, 600, 290, 28, UIKit.White);

            UIKit.Box(layer, 940, 440, 720, 360, UIKit.Panel);
            TB(layer, "Lo que aprendimos", 960, 448, 680, 44, 32, UIKit.Yellow);
            string les = "";
            List<string> ls = m.Lessons();
            for (int i = 0; i < ls.Count; i++) les += "• " + ls[i] + "\n";
            T(layer, les, 960, 498, 680, 295, 24, UIKit.White);

            UIKit.Btn(layer, "JUGAR OTRA VEZ", 440, 840, 520, 110, UIKit.Green, delegate { ShowSites(); }, 44);
            UIKit.Btn(layer, "MENÚ", 1000, 840, 480, 110, UIKit.PanelLight, delegate { ShowTitle(); }, 44);
            UIKit.Label(layer, "Los ingenieros de verdad también aprenden de cada intento. ¡Prueba otra estrategia!", 0, 975, 1920, 50, 28, UIKit.Gray, TextAnchor.MiddleCenter, FontStyle.Normal);
            screen = 3;
            extraHolder = UIKit.NewRect("Extra", layer, 0, 0, 1920, 1080);
            bool highRad = m.dose > m.doseLimit * 0.5f || m.shield < 3;
            NasaExtras.FetchOsdr(this, highRad ? "space radiation astronauts" : "bone loss microgravity");
            DrawExtras();
        }

        // ======================================================
        //  DATOS EXTRA DE NASA (APOD, foto de Marte, RadLab, OSDR)
        // ======================================================
        void OnExtrasChanged()
        {
            if (extraHolder != null && screen != 0) DrawExtras();
        }

        static void SetPhoto(Image img, Texture2D tex)
        {
            img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.color = Color.white;
        }

        void DrawExtras()
        {
            if (extraHolder == null) return;
            UIKit.Clear(extraHolder);

            if (screen == 1 && NasaExtras.ApodTex != null)
            {
                float x = 1350, y = 420, w = 480, h = 270;
                UIKit.Box(extraHolder, x - 15, y - 50, w + 30, h + 170, UIKit.Panel);
                TC(extraHolder, "FOTO DEL DÍA · NASA", x, y - 46, w, 36, 24, UIKit.Cyan);
                Image img = UIKit.Box(extraHolder, x, y, w, h, Color.white, false);
                SetPhoto(img, NasaExtras.ApodTex);
                UIKit.Label(extraHolder, NasaExtras.ApodTitle, x, y + h + 6, w, 60, 24, UIKit.White, TextAnchor.UpperLeft, FontStyle.Bold);
                UIKit.Label(extraHolder, NasaExtras.ApodCredit, x, y + h + 70, w, 40, 18, UIKit.Gray, TextAnchor.UpperLeft, FontStyle.Normal);
            }
            else if (screen == 2)
            {
                bool mars = site != null && site.id == SiteId.Mars;
                float bx = 640, by = 600;
                float tx = bx, tw = 1020;
                string txt = "";
                if (mars)
                {
                    UIKit.Box(extraHolder, bx, by, 330, 186, new Color(0f, 0f, 0f, 0.35f), true);
                    if (NasaExtras.MarsTex != null)
                    {
                        Image img = UIKit.Box(extraHolder, bx + 3, by + 3, 324, 180, Color.white, false);
                        SetPhoto(img, NasaExtras.MarsTex);
                        txt = NasaExtras.MarsInfo + "\n";
                    }
                    else
                    {
                        TC(extraHolder, NasaExtras.MarsDone ? "Foto de Marte no disponible ahora" : "Buscando una foto real de Marte...", bx, by, 330, 186, 22, UIKit.Gray);
                    }
                    tx = bx + 360; tw = 660;
                }
                if (NasaExtras.IssLine != null) txt += (txt.Length > 0 ? "\n" : "") + NasaExtras.IssLine;
                if (txt.Length > 0) T(extraHolder, txt, tx, by, tw, 235, 22, UIKit.Cyan);
            }
            else if (screen == 3 && NasaExtras.OsdrLine != null)
            {
                UIKit.Label(extraHolder, "Ciencia real para seguir leyendo (NASA OSDR): " + NasaExtras.OsdrLine, 120, 802, 1680, 30, 21, UIKit.Cyan, TextAnchor.MiddleCenter, FontStyle.Normal);
            }
        }

    }
}
