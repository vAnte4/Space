// ============================================================
//  GameData.cs  -  TODO EL CONTENIDO EDUCATIVO (textos, datos, eventos)
//  Esta es la carpeta de la persona que investiga datos NASA:
//  aquí se editan destinos, datos curiosos y eventos SIN tocar la lógica.
// ============================================================
using System;
using System.Collections.Generic;

namespace AstroJunior
{
    public class Fact
    {
        public string tag;      // radiation, shield, comms, dust, night, air, food, sun, power, general
        public string site;     // "" = cualquiera, "moon" o "mars"
        public string title;
        public string text;
        public string source;   // SIEMPRE poner de dónde sale el dato
        public Fact(string tag, string site, string title, string text, string source)
        {
            this.tag = tag; this.site = site; this.title = title; this.text = text; this.source = source;
        }
    }

    public static class GameData
    {
        // ---------------------------------------------------------
        //  DESTINOS
        //  Radiación: valores REALES medidos en la superficie.
        //  Luna: 1369 microsieverts/día (Chang'e-4, Zhang et al., Science Advances 2020)
        //  Marte: 0.67 mSv/día (Curiosity RAD, Hassler et al., Science 2014)
        //  La energía solar (solarPerWeek) es un número de JUEGO, no un dato medido.
        // ---------------------------------------------------------
        public static readonly SiteDef Moon = new SiteDef
        {
            id = SiteId.Moon,
            name = "la Luna",
            baseName = "Base Selene",
            doseMsvPerDay = 1.37f,
            doseSource = "Chang'e-4 LND (Zhang et al., Science Advances, 2020)",
            longNights = true,
            solarPerWeek = 18,
            longCommDelay = false,
            commText = "Ida y vuelta: ~3 segundos",
            stat1 = "Radiación: ~1.4 mSv por día (¡más del doble que en la Estación Espacial!)",
            stat2 = "Mensajes a la Tierra: 1.3 segundos (casi instantáneo)",
            stat3 = "Energía: ¡noches de 2 semanas sin Sol!"
        };

        public static readonly SiteDef Mars = new SiteDef
        {
            id = SiteId.Mars,
            name = "Marte",
            baseName = "Base Ares",
            doseMsvPerDay = 0.67f,
            doseSource = "Curiosity RAD (Hassler et al., Science, 2014)",
            longNights = false,
            solarPerWeek = 10,
            longCommDelay = true,
            commText = "Un mensaje tarda de 3 a 22 minutos",
            stat1 = "Radiación: ~0.67 mSv por día (menos que en la Luna: el planeta te tapa la mitad del cielo)",
            stat2 = "Mensajes a la Tierra: de 3 a 22 MINUTOS (¡hay que decidir solos!)",
            stat3 = "Energía: poco Sol y tormentas de polvo"
        };

        public static SiteDef Get(SiteId id) { return id == SiteId.Moon ? Moon : Mars; }

        // ---------------------------------------------------------
        //  DATOS CURIOSOS ("¿Sabías que?")
        //  Cada uno sale en el informe de la semana según lo que pasó.
        // ---------------------------------------------------------
        public static readonly List<Fact> Facts = new List<Fact>
        {
            new Fact("radiation", "", "Radiación en Marte",
                "El rover Curiosity midió unos 0.67 mSv de radiación por día en Marte. Dentro de la nave, en el viaje, midió casi el triple: 1.8 mSv por día.",
                "NASA / SwRI – instrumento RAD de Curiosity (Hassler et al., Science, 2014)"),
            new Fact("radiation", "", "Radiación en la Luna",
                "Un mSv (milisievert) es la unidad con la que se mide cuánta radiación recibe el cuerpo. En la Luna se midió ~1.4 mSv por día, más del doble que dentro de la Estación Espacial.",
                "Chang'e-4 LND (Zhang et al., Science Advances, 2020) – datos de China y Alemania"),
            new Fact("radiation", "", "Límites de radiación",
                "NASA limita la radiación que un astronauta puede recibir en toda su carrera a 600 mSv. En la Tierra recibimos unos 3 mSv al año. Por eso esta misión tiene un límite de 120 mSv (la quinta parte).",
                "NASA – estándar NASA-STD-3001 (límite de carrera 600 mSv)"),
            new Fact("shield", "", "Cómo parar la radiación",
                "La mejor defensa es poner materia en el camino: regolito (polvo y piedras del suelo), agua o plásticos. Por eso se estudia cubrir o enterrar las bases lunares.",
                "NASA – investigación de protección contra radiación (resumen educativo)"),
            new Fact("shield", "", "Refugio para tormentas solares",
                "NASA diseña hábitats y naves con refugios de tormenta: un rincón bien protegido donde la tripulación espera mientras pasa una tormenta solar.",
                "NASA – estándares de radiación para misiones Artemis"),
            new Fact("comms", "", "Esperando respuesta",
                "La luz tarda ~1.3 segundos en llegar a la Luna, pero entre 3 y 22 minutos en llegar a Marte. ¡Una pregunta y su respuesta pueden tardar hasta 44 minutos!",
                "NASA – Space Communications and Navigation (SCaN)"),
            new Fact("dust", "mars", "Tormentas de polvo",
                "En 2018 una tormenta de polvo cubrió casi todo Marte y el rover Opportunity, que usaba paneles solares, dejó de comunicarse. Una base necesita energía de reserva.",
                "NASA / JPL – rover Opportunity"),
            new Fact("dust", "moon", "Polvo lunar",
                "El polvo de la Luna tiene bordes afilados y se pega a todo, incluso a los trajes. Los astronautas del Apolo lo metían sin querer dentro de la nave.",
                "NASA – programa Apolo"),
            new Fact("night", "", "La larga noche lunar",
                "Una noche en la Luna dura unos 14 días terrestres. Sin Sol, los paneles no dan energía: hacen falta baterías grandes u otras fuentes, como pequeños reactores nucleares.",
                "NASA – Fission Surface Power / Artemis"),
            new Fact("air", "", "¿Cuánto oxígeno necesita un astronauta?",
                "Cada astronauta respira cerca de 0.84 kg de oxígeno al día. Una tripulación de 4 gasta unos 3.4 kg diarios.",
                "NASA – soporte vital de la Estación Espacial Internacional"),
            new Fact("air", "mars", "Oxígeno hecho en Marte",
                "El instrumento MOXIE de Perseverance fabricó oxígeno a partir del dióxido de carbono de Marte: 122 gramos en total, hasta 12 gramos por hora.",
                "NASA / JPL – MOXIE (2023)"),
            new Fact("food", "", "Cultivar en el espacio",
                "En la Estación Espacial los astronautas ya cultivaron y comieron lechuga (2015) y chiles (2021).",
                "NASA – Veggie y Advanced Plant Habitat"),
            new Fact("food", "", "Comida para el espacio... y para la Tierra",
                "Aprender a producir alimentos en la Luna o en lugares remotos sirve para cuidar la salud y la nutrición de los astronautas, y también ayuda a comunidades aisladas aquí en la Tierra.",
                "Agencia Espacial Canadiense (CSA) – Food production on the Moon and in remote areas"),
            new Fact("general", "", "Los huesos también viajan",
                "En microgravedad los huesos y los músculos se debilitan. Por eso los astronautas de la Estación Espacial hacen ejercicio cerca de 2 horas cada día. NASA guarda estudios reales sobre esto en el repositorio OSDR.",
                "NASA OSDR – investigación de biología y salud en el espacio"),
            new Fact("radiation", "", "Medidores en la Estación Espacial",
                "Dentro de la Estación Espacial hay instrumentos que miden la radiación todo el tiempo. Sus mediciones se publican en RadLab, una base de datos abierta de NASA que cualquiera puede consultar.",
                "NASA OSDR – RadLab (dosimetría de radiación)"),
            new Fact("sun", "", "El Sol y sus ciclos",
                "El Sol tiene un ciclo de actividad de unos 11 años. En 2024 estuvo cerca de su máximo, con llamaradas muy fuertes como la X9.0 del 3 de octubre de 2024.",
                "NASA – Solar Cycle 25 / SDO"),
            new Fact("sun", "", "¿Qué es DONKI?",
                "DONKI es la base de datos del clima espacial de NASA: anota llamaradas, eyecciones de masa coronal y tormentas de partículas para avisar a satélites y astronautas.",
                "NASA CCMC – DONKI (Space Weather Database)"),
            new Fact("power", "mars", "Poca luz en Marte",
                "Marte está más lejos del Sol: recibe cerca del 43 % de la luz que recibe la Tierra. Por eso sus paneles solares dan menos energía.",
                "NASA – Mars Fact Sheet"),
            new Fact("power", "", "Una batería llena no guarda más",
                "Si la batería ya está llena, la energía extra se pierde. Los ingenieros calculan paneles y baterías para equilibrar el día y la noche.",
                "Principio de ingeniería de sistemas de energía"),
            new Fact("general", "mars", "Un día en Marte",
                "Un día en Marte (se llama sol) dura 24 horas y 39 minutos.",
                "NASA – Mars Exploration Program"),
            new Fact("general", "", "Trabajo en equipo",
                "Los planificadores de misiones de NASA reparten energía, aire, comida y tiempo todos los días, igual que tú. ¡Cada decisión tiene un costo!",
                "NASA – planificación de misiones")
        };

        public static Fact PickFact(string tag, SiteDef site, Random rng, HashSet<int> used)
        {
            string siteKey = site.id == SiteId.Moon ? "moon" : "mars";
            List<int> pool = new List<int>();
            for (int i = 0; i < Facts.Count; i++)
            {
                if (used.Contains(i)) continue;
                Fact f = Facts[i];
                if (f.site.Length > 0 && f.site != siteKey) continue;
                if (f.tag == tag) pool.Add(i);
            }
            if (pool.Count == 0)
            {
                for (int i = 0; i < Facts.Count; i++)
                {
                    if (used.Contains(i)) continue;
                    Fact f = Facts[i];
                    if (f.site.Length > 0 && f.site != siteKey) continue;
                    pool.Add(i);
                }
            }
            if (pool.Count == 0) { used.Clear(); return PickFact(tag, site, rng, used); }
            int pick = pool[rng.Next(pool.Count)];
            used.Add(pick);
            return Facts[pick];
        }

        // ---------------------------------------------------------
        //  EVENTOS ALEATORIOS
        //  Cada decisión tiene un costo. NO hay opción gratis sin consecuencias.
        // ---------------------------------------------------------
        static Choice C(string label, string hint, string result)
        {
            Choice c = new Choice();
            c.label = label; c.hint = hint; c.result = result;
            return c;
        }

        static Choice AskEarth(int cost)
        {
            Choice c = C("Preguntar a Control de Misión en la Tierra",
                         "Gasta " + (-cost) + " de energía en la radio. ¿Llegará la respuesta a tiempo?", "");
            c.asksEarth = true;
            c.dBattery = cost;
            return c;
        }

        public static GameEvent MakeEvent(string id, Mission m)
        {
            bool mars = m.site.id == SiteId.Mars;
            GameEvent e = new GameEvent();
            e.id = id;
            Choice c;

            switch (id)
            {
                case "sep":
                    e.title = "¡Tormenta de partículas del Sol!";
                    e.isStorm = true;
                    e.factTag = "shield";
                    e.dose = m.wx.StormDoseMsv;
                    e.story = "El Sol lanzó partículas muy veloces. Llegarán en pocas horas y aquí no hay una atmósfera gruesa que te proteja. Sin ninguna protección recibirías " +
                              Mission.F(e.dose) + " mSv de golpe.";
                    e.realDataLine = "Dato real de NASA DONKI: " + m.wx.Describe() + " " + m.wx.Strongest();
                    c = C("Refugiarse en el centro de la base", "Recibes ¼ de la dosis, pero pierdes la ciencia de esta semana.",
                          "Toda la tripulación se refugió en el centro de la base, rodeada de agua y provisiones.");
                    c.doseMult = 0.25f; c.cancelScience = true; e.choices.Add(c);
                    c = C("Seguir trabajando afuera", "Recibes la dosis completa, pero conservas tu ciencia.",
                          "Siguieron trabajando mientras llegaba la tormenta.");
                    e.choices.Add(c);
                    e.choices.Add(AskEarth(-1));
                    e.bestChoice = 0; e.defaultChoice = 1;
                    break;

                case "dust":
                    e.factTag = "dust";
                    if (mars)
                    {
                        e.title = "¡Tormenta de polvo en Marte!";
                        e.story = "Una nube enorme de polvo cubre el cielo y tapa el Sol. Tus paneles solares van a producir mucho menos energía.";
                        c = C("Aguantar la tormenta", "No gastas nada, pero tendrás muy poca energía solar por 2 semanas.", "Esperaron a que pasara la tormenta.");
                        c.dustWeeks = 2; c.dustFactor = 0.3f; e.choices.Add(c);
                        c = C("Enviar robots a limpiar los paneles", "Cuesta 4 de energía; el polvo solo afecta 1 semana.", "Los robots limpiaron los paneles solares.");
                        c.dBattery = -4; c.dustWeeks = 1; c.dustFactor = 0.3f; e.choices.Add(c);
                        e.choices.Add(AskEarth(-1));
                        e.bestChoice = 1; e.defaultChoice = 0;
                    }
                    else
                    {
                        e.title = "¡Polvo lunar pegajoso!";
                        e.story = "El polvo de la Luna se carga de electricidad y se pega a los paneles solares. Reciben menos luz.";
                        c = C("Seguir así", "No gastas nada, pero tendrás 30 % menos de energía solar por 2 semanas.", "Dejaron los paneles polvorientos.");
                        c.dustWeeks = 2; c.dustFactor = 0.7f; e.choices.Add(c);
                        c = C("Limpiar los paneles con cepillos", "Cuesta 3 de energía; los paneles quedan limpios.", "Limpiaron los paneles con cepillos.");
                        c.dBattery = -3; e.choices.Add(c);
                        e.choices.Add(AskEarth(-1));
                        e.bestChoice = 1; e.defaultChoice = 0;
                    }
                    break;

                case "leak":
                    e.title = "¡Fuga de aire!";
                    e.factTag = "air";
                    e.story = "Un sello de un módulo está dejando escapar aire hacia el exterior. Si no lo arreglas, pierdes aire de reserva.";
                    c = C("Reparar ahora", "Cuesta 4 de energía.", "Los astronautas repararon el sello a tiempo.");
                    c.dBattery = -4; e.choices.Add(c);
                    c = C("Dejarlo para después", "No gastas energía, pero pierdes 15 de aire.", "Dejaron la fuga para más tarde.");
                    c.dAir = -15f; e.choices.Add(c);
                    e.choices.Add(AskEarth(-1));
                    e.bestChoice = 0; e.defaultChoice = 1;
                    break;

                case "mold":
                    e.title = "¡Moho en el invernadero!";
                    e.factTag = "food";
                    e.story = "Algunas plantas se enfermaron. Si no actúas, perderás parte de la cosecha.";
                    c = C("Tirar las plantas dañadas", "Pierdes 8 de comida.", "Tiraron las plantas enfermas.");
                    c.dFood = -8f; e.choices.Add(c);
                    c = C("Usar luz ultravioleta extra", "Cuesta 3 de energía; pierdes solo 2 de comida.", "La luz ultravioleta frenó el moho.");
                    c.dBattery = -3; c.dFood = -2f; e.choices.Add(c);
                    e.choices.Add(AskEarth(-1));
                    e.bestChoice = 1; e.defaultChoice = 0;
                    break;

                case "rock":
                    e.title = "¡Roca misteriosa!";
                    e.factTag = "general";
                    e.story = "Tu rover encontró una roca que puede contar historias de hace miles de millones de años.";
                    c = C("Estudiarla a fondo", "Cuesta 3 de energía y te da +25 puntos de ciencia.", "Estudiaron la roca con todos los instrumentos.");
                    c.dBattery = -3; c.dScience = 25; e.choices.Add(c);
                    c = C("Tomar una foto y seguir", "Gratis, pero solo +5 puntos de ciencia.", "Tomaron una foto y siguieron.");
                    c.dScience = 5; e.choices.Add(c);
                    e.bestChoice = 0; e.defaultChoice = 1;
                    break;

                default: // "excav"
                    e.title = "¡El robot excavador funciona genial!";
                    e.factTag = "shield";
                    e.story = "Hoy tu robot excavador tiene energía de sobra para un trabajo extra. ¿En qué lo usas?";
                    c = C("Cubrir la base con más regolito", "Cuesta 2 de energía y da +2 capas de escudo.", "El robot cubrió la base con más regolito.");
                    c.dBattery = -2; c.dShield = 2; e.choices.Add(c);
                    c = C("Cavar una zona de investigación", "Cuesta 2 de energía y da +15 puntos de ciencia.", "El robot preparó una zona para investigar.");
                    c.dBattery = -2; c.dScience = 15; e.choices.Add(c);
                    c = C("Dejarlo descansar", "No gastas energía, pero pierdes la oportunidad.", "Guardaron al robot para otro día.");
                    e.choices.Add(c);
                    e.bestChoice = 0; e.defaultChoice = 2;
                    break;
            }
            return e;
        }
    }
}
