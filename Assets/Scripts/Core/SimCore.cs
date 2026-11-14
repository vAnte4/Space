// ============================================================
//  SimCore.cs  -  LA LÓGICA DEL JUEGO (sin nada de Unity adentro)
//  Aquí viven las reglas: energía, aire, comida, radiación, eventos.
//  Si quieres cambiar el equilibrio del juego, casi todo está en
//  las constantes de arriba de la clase Mission.
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;

namespace AstroJunior
{
    public enum SiteId { Moon, Mars }

    // Los 4 sistemas que el jugador controla
    public enum Sys { Life = 0, Greenhouse = 1, Shield = 2, Science = 3 }

    // ------------------------------------------------------------
    //  Un destino (Luna o Marte)
    // ------------------------------------------------------------
    public class SiteDef
    {
        public SiteId id;
        public string name;
        public string baseName;
        public float doseMsvPerDay;      // radiación REAL medida en la superficie (mSv por día)
        public string doseSource;        // de dónde sale ese dato
        public bool longNights;          // true = noches lunares de 2 semanas sin Sol
        public int solarPerWeek;         // energía solar por semana cuando hay Sol (unidades de juego)
        public bool longCommDelay;       // true = los mensajes a la Tierra tardan minutos
        public string commText;          // texto corto sobre la comunicación
        public string stat1, stat2, stat3; // textos de la tarjeta de selección

        public float DoseMsvPerWeek { get { return doseMsvPerDay * 7f; } }
    }

    // ------------------------------------------------------------
    //  Eventos aleatorios con decisiones
    // ------------------------------------------------------------
    public class Choice
    {
        public string label;           // texto grande del botón
        public string hint;            // costo / beneficio en letra chica
        public string result;          // lo que pasó (sale en el informe)
        public int dBattery;           // cambio en la batería (negativo = cuesta energía)
        public float dAir, dFood;      // cambio en aire / comida
        public int dScience, dShield;  // puntos de ciencia / capas de escudo
        public float doseMult = 1f;    // multiplica la dosis de la tormenta (0.25 = refugio)
        public bool cancelScience;     // true = pierdes la ciencia de esta semana
        public int dustWeeks;          // semanas de polvo que bajan la energía solar
        public float dustFactor = 1f;  // cuánta luz llega con polvo (0.3 = 30 %)
        public bool asksEarth;         // true = "Preguntar a la Tierra"
    }

    public class GameEvent
    {
        public string id;
        public string title;
        public string story;
        public string realDataLine = "";   // dato real de NASA (si aplica)
        public string factTag = "general";
        public float dose;                 // mSv sin protección (solo tormentas solares)
        public bool isStorm;
        public int bestChoice;             // lo que recomendaría Control de Misión
        public int defaultChoice;          // lo que pasa si la respuesta llega tarde
        public List<Choice> choices = new List<Choice>();
    }

    // ------------------------------------------------------------
    //  Informe semanal
    // ------------------------------------------------------------
    public class ReportLine
    {
        public int tone;      // +1 bueno, 0 neutro, -1 malo
        public string text;
    }

    public class WeekReport
    {
        public List<ReportLine> lines = new List<ReportLine>();
        public string factTag = "general";
        public void Add(int tone, string text)
        {
            ReportLine l = new ReportLine();
            l.tone = tone;
            l.text = text;
            lines.Add(l);
        }
    }

    // ------------------------------------------------------------
    //  Clima espacial (lo llena SpaceWeatherService con datos de DONKI)
    // ------------------------------------------------------------
    public class FlareRec
    {
        public string cls;    // por ejemplo "X1.2"
        public string time;   // por ejemplo "2024-10-03T12:18Z"
        public FlareRec(string c, string t) { cls = c; time = t; }
    }

    public class SpaceWeatherInfo
    {
        public bool isLive;
        public bool loading;
        public string range = "";
        public int flares, mFlares, xFlares, sepEvents;
        public string strongest = "-";
        public string strongestDate = "";
        public float stormChance = 0.2f;

        // Dosis (mSv sin protección) de una tormenta de partículas del juego.
        // Es un valor de JUEGO, escalado según qué tan fuertes fueron las llamaradas reales.
        public float StormDoseMsv
        {
            get
            {
                if (xFlares > 0) return 30f;
                if (mFlares > 0) return 20f;
                return 12f;
            }
        }

        public static SpaceWeatherInfo Loading()
        {
            SpaceWeatherInfo w = new SpaceWeatherInfo();
            w.loading = true;
            return w;
        }

        public static SpaceWeatherInfo Build(List<FlareRec> list, int sep, bool live, string range)
        {
            SpaceWeatherInfo w = new SpaceWeatherInfo();
            w.isLive = live;
            w.range = range;
            w.sepEvents = sep;
            float best = -1f;
            for (int i = 0; i < list.Count; i++)
            {
                FlareRec f = list[i];
                if (f == null || string.IsNullOrEmpty(f.cls)) continue;
                w.flares++;
                char c = char.ToUpperInvariant(f.cls[0]);
                if (c == 'X') w.xFlares++;
                else if (c == 'M') w.mFlares++;
                float rk = Rank(f.cls);
                if (rk > best) { best = rk; w.strongest = f.cls; w.strongestDate = NiceDate(f.time); }
            }
            // Probabilidad de tormenta por semana: más llamaradas reales = más riesgo en el juego
            float p = 0.10f + 0.04f * w.xFlares + 0.015f * w.mFlares + 0.05f * w.sepEvents;
            w.stormChance = Math.Max(0.10f, Math.Min(0.45f, p));
            return w;
        }

        // Si no hay internet (o la API falla) usamos grandes llamaradas REALES guardadas.
        // Se muestran como "ARCHIVO", nunca como "en vivo".
        public static SpaceWeatherInfo Archive()
        {
            List<FlareRec> a = new List<FlareRec>();
            a.Add(new FlareRec("X9.3", "2017-09-06"));
            a.Add(new FlareRec("X8.2", "2017-09-10"));
            a.Add(new FlareRec("X5.8", "2024-05-11"));
            a.Add(new FlareRec("X8.7", "2024-05-14"));
            a.Add(new FlareRec("X9.0", "2024-10-03"));
            return Build(a, 0, false, "2017-2024");
        }

        public string Headline()
        {
            if (loading) return "Conectando con NASA DONKI...";
            return isLive ? "DATOS EN VIVO · NASA DONKI" : "DATOS DE ARCHIVO · NASA DONKI (no en vivo)";
        }

        public string Describe()
        {
            if (loading) return "Esperando datos del Sol...";
            string head = isLive ? "Últimos 30 días (" + range + ")" : "Archivo de grandes llamaradas reales (" + range + ")";
            return head + ": " + xFlares + " llamaradas clase X (las más fuertes), " + mFlares + " clase M y " + sepEvents + " tormentas de partículas.";
        }

        public string Strongest()
        {
            if (loading || flares == 0) return "Sin llamaradas registradas en este periodo.";
            return "La más fuerte: clase " + strongest + (strongestDate.Length > 0 ? " el " + strongestDate : "") + ".";
        }

        static float Rank(string cls)
        {
            char c = char.ToUpperInvariant(cls[0]);
            float n = 0f;
            if (cls.Length > 1) float.TryParse(cls.Substring(1), NumberStyles.Float, CultureInfo.InvariantCulture, out n);
            float l = 0f;
            if (c == 'X') l = 4000f; else if (c == 'M') l = 3000f; else if (c == 'C') l = 2000f; else if (c == 'B') l = 1000f;
            return l + n;
        }

        static string NiceDate(string iso)
        {
            if (string.IsNullOrEmpty(iso) || iso.Length < 10) return "";
            return iso.Substring(8, 2) + "/" + iso.Substring(5, 2) + "/" + iso.Substring(0, 4);
        }
    }

    // ------------------------------------------------------------
    //  LA MISIÓN (todas las reglas)
    // ------------------------------------------------------------
    public class Mission
    {
        // ---- Números de equilibrio: cámbialos para hacerlo más fácil o más difícil ----
        public const int CostPerLevel = 2;        // cada nivel de un sistema cuesta 2 de energía
        public const int MaxLevel = 3;            // máximo 3 niveles por sistema
        public const float AirUse = 14f;          // aire que consume la tripulación por semana
        public const float AirPerLevel = 7f;      // aire que fabrica cada nivel de soporte vital
        public const float FoodUse = 10f;         // comida que come la tripulación por semana
        public const float FoodPerLevel = 5f;     // comida que produce cada nivel de invernadero
        public const float ShieldPerLayer = 0.07f;// cada capa de regolito quita 7 % de la radiación
        public const float StartAir = 70f, StartFood = 70f;
        public const int StartBattery = 12;

        public SiteDef site;
        public SpaceWeatherInfo wx;
        public Random rng;

        public int week = 1, totalWeeks = 12;
        public int battery = StartBattery, batteryMax = 30;
        public float air = StartAir, food = StartFood;
        public float dose = 0f, doseLimit = 120f;      // límite de la misión (mSv)
        public int shield = 0, shieldMax = 10;
        public int science = 0, scienceGoal = 60;
        public int[] level = new int[4];
        public int dustWeeksLeft = 0;
        public float dustFactor = 1f;
        public int foodEmptyWeeks = 0;
        public bool ended = false, success = false;
        public string endReason = "";

        // Estadísticas para el resumen final
        public int stormsSeen, sheltered, wastedPower, askedEarth, askedEarthLate;
        public int lastScienceGain;
        int factCounter = 0;

        public Mission(SiteDef site, SpaceWeatherInfo wx, int seed)
        {
            this.site = site;
            this.wx = wx;
            this.rng = new Random(seed);
        }

        // ---------------- Energía solar ----------------
        public bool IsSunny(int w)
        {
            if (!site.longNights) return true;
            return ((w - 1) % 4) < 2;   // 2 semanas de día, 2 semanas de noche (como la Luna)
        }

        public int BaseSolar(int w) { return IsSunny(w) ? site.solarPerWeek : 0; }

        public int SolarNow
        {
            get
            {
                float f = (dustWeeksLeft > 0) ? dustFactor : 1f;
                return (int)Math.Round(BaseSolar(week) * f);
            }
        }

        // ---------------- Reparto de energía ----------------
        public int PowerAvailable { get { return battery + SolarNow; } }

        public int PowerUsed
        {
            get
            {
                int t = 0;
                for (int i = 0; i < level.Length; i++) t += level[i];
                return t * CostPerLevel;
            }
        }

        public bool CanRaise(Sys s)
        {
            int i = (int)s;
            if (level[i] >= MaxLevel) return false;
            if (PowerUsed + CostPerLevel > PowerAvailable) return false;
            if (s == Sys.Shield && shield + level[i] >= shieldMax) return false;
            return true;
        }

        public bool Raise(Sys s)
        {
            if (!CanRaise(s)) return false;
            level[(int)s]++;
            return true;
        }

        public void Lower(Sys s)
        {
            if (level[(int)s] > 0) level[(int)s]--;
        }

        public float ShieldFactor(int layers) { return 1f - ShieldPerLayer * layers; }
        public float ShieldFactor() { return ShieldFactor(shield); }

        // ---------------- Una semana ----------------
        // Paso 1: producir y consumir
        public WeekReport RunWeek()
        {
            WeekReport r = new WeekReport();
            int solar = SolarNow;
            int used = PowerUsed;

            // Energía
            int raw = battery + solar - used;
            int wasted = Math.Max(0, raw - batteryMax);
            battery = Math.Max(0, Math.Min(batteryMax, raw));
            wastedPower += wasted;
            r.Add(0, "Energía: +" + solar + " del Sol, -" + used + " que gastaste. Batería: " + battery + "/" + batteryMax + ".");
            if (wasted > 0) r.Add(-1, "¡Se perdieron " + wasted + " de energía porque la batería estaba llena!");

            // Aire
            float lifeGain = level[(int)Sys.Life] * AirPerLevel;
            float dAir = lifeGain - AirUse;
            air = Clamp(air + dAir, 0f, 100f);
            r.Add(dAir >= 0 ? 1 : -1, "Aire: " + Signed(dAir) + "  (la tripulación respira " + F(AirUse) + " y el soporte vital fabrica " + F(lifeGain) + ").");

            // Comida
            float gh = level[(int)Sys.Greenhouse] * FoodPerLevel;
            float dFood = gh - FoodUse;
            food = Clamp(food + dFood, 0f, 100f);
            r.Add(dFood >= 0 ? 1 : -1, "Comida: " + Signed(dFood) + "  (la tripulación come " + F(FoodUse) + " y el invernadero cultiva " + F(gh) + ").");

            // Escudo (se construye primero, así protege esta misma semana)
            int add = Math.Min(level[(int)Sys.Shield], shieldMax - shield);
            shield += add;
            if (add > 0) r.Add(1, "Escudo: +" + add + " capas de regolito (" + shield + "/" + shieldMax + ").");

            // Ciencia
            lastScienceGain = level[(int)Sys.Science] * 10;
            science += lastScienceGain;
            if (lastScienceGain > 0) r.Add(1, "Ciencia: +" + lastScienceGain + " puntos.");

            // Radiación de fondo
            float unshielded = site.DoseMsvPerWeek;
            float bd = unshielded * ShieldFactor();
            dose += bd;
            r.Add(0, "Radiación de fondo: +" + F(bd) + " mSv (total " + F(dose) + " de " + F(doseLimit) + ").");
            if (shield > 0) r.Add(1, "Tu escudo evitó " + F(unshielded - bd) + " mSv esta semana.");

            if (dustWeeksLeft > 0) dustWeeksLeft--;
            return r;
        }

        // Paso 2: ¿pasa algo?
        public GameEvent RollEvent()
        {
            if (rng.NextDouble() < wx.stormChance) return GameData.MakeEvent("sep", this);
            if (rng.NextDouble() < 0.5)
            {
                string[] pool = site.longCommDelay
                    ? new string[] { "dust", "dust", "leak", "mold", "rock", "excav" }
                    : new string[] { "dust", "leak", "mold", "rock", "excav" };
                return GameData.MakeEvent(pool[rng.Next(pool.Length)], this);
            }
            return null;
        }

        public bool CanAfford(Choice c) { return battery >= -c.dBattery; }

        // Paso 3: aplicar la decisión del jugador
        public void ApplyChoice(GameEvent ev, int idx, WeekReport rep)
        {
            Choice c = ev.choices[idx];
            if (c.asksEarth)
            {
                askedEarth++;
                battery = Math.Max(0, battery + c.dBattery);   // gastar energía en la radio
                if (site.longCommDelay)
                {
                    askedEarthLate++;
                    rep.Add(-1, "Preguntaste a la Tierra, pero la respuesta tarda entre 3 y 22 minutos... ¡el problema no esperó!");
                    c = ev.choices[ev.defaultChoice];
                }
                else
                {
                    rep.Add(1, "Control de Misión respondió en 3 segundos y te recomendó la mejor opción.");
                    c = ev.choices[ev.bestChoice];
                }
            }

            if (!string.IsNullOrEmpty(c.result)) rep.Add(0, c.result);

            if (c.dBattery != 0)
            {
                battery = Math.Max(0, Math.Min(batteryMax, battery + c.dBattery));
                rep.Add(c.dBattery > 0 ? 1 : -1, "Energía: " + Signed(c.dBattery) + ".");
            }
            if (c.dAir != 0f) { air = Clamp(air + c.dAir, 0f, 100f); rep.Add(c.dAir > 0 ? 1 : -1, "Aire: " + Signed(c.dAir) + "."); }
            if (c.dFood != 0f) { food = Clamp(food + c.dFood, 0f, 100f); rep.Add(c.dFood > 0 ? 1 : -1, "Comida: " + Signed(c.dFood) + "."); }
            if (c.dScience != 0) { science = Math.Max(0, science + c.dScience); rep.Add(1, "Ciencia: +" + c.dScience + " puntos."); }
            if (c.dShield != 0)
            {
                int before = shield;
                shield = Math.Min(shieldMax, shield + c.dShield);
                if (shield > before) rep.Add(1, "Escudo: +" + (shield - before) + " capas (" + shield + "/" + shieldMax + ").");
            }
            if (c.cancelScience && lastScienceGain > 0)
            {
                science = Math.Max(0, science - lastScienceGain);
                rep.Add(-1, "Perdiste los " + lastScienceGain + " puntos de ciencia de esta semana (nadie salió a trabajar).");
                lastScienceGain = 0;
            }
            if (c.dustWeeks > 0) { dustWeeksLeft = c.dustWeeks; dustFactor = c.dustFactor; }

            if (ev.dose > 0f)
            {
                float full = ev.dose * c.doseMult;
                float d = full * ShieldFactor();
                dose += d;
                stormsSeen++;
                if (c.doseMult < 1f) sheltered++;
                rep.Add(d > 12f ? -1 : 0, "Tormenta solar: +" + F(d) + " mSv de radiación.");
                if (shield > 0) rep.Add(1, "Tu escudo te ahorró " + F(full - d) + " mSv en esta tormenta.");
            }
            rep.factTag = ChooseFactTag(ev);
        }

        // Paso 4: cerrar la semana y avanzar
        public void FinishWeek()
        {
            if (food <= 0f) foodEmptyWeeks++; else foodEmptyWeeks = 0;

            if (air <= 0f) End(false, "air");
            else if (dose >= doseLimit) End(false, "dose");
            else if (foodEmptyWeeks >= 2) End(false, "food");
            else if (week >= totalWeeks) End(true, "ok");
            else
            {
                week++;
                ClampAllocation();
            }
        }

        void End(bool ok, string reason)
        {
            ended = true;
            success = ok;
            endReason = reason;
        }

        // Mantiene el reparto de la semana anterior, pero sin pasarse de la energía disponible
        public void ClampAllocation()
        {
            if (shield >= shieldMax) level[(int)Sys.Shield] = 0;
            else level[(int)Sys.Shield] = Math.Min(level[(int)Sys.Shield], shieldMax - shield);
            int[] order = new int[] { 3, 2, 1, 0 };
            int guard = 0;
            while (PowerUsed > PowerAvailable && guard++ < 50)
            {
                for (int k = 0; k < order.Length; k++)
                {
                    if (level[order[k]] > 0) { level[order[k]]--; break; }
                }
            }
        }

        // ---------------- Resultado ----------------
        public int Stars
        {
            get
            {
                if (!success) return 0;
                int s = 1;
                if (science >= scienceGoal) s++;
                if (dose <= doseLimit * 0.7f && air >= 40f && food >= 40f) s++;
                return s;
            }
        }

        public string EndTitle()
        {
            if (success) return Stars >= 3 ? "¡MISIÓN PERFECTA!" : "¡MISIÓN CUMPLIDA!";
            return "Misión interrumpida";
        }

        public string EndText()
        {
            if (success)
                return "¡Los 4 astronautas sobrevivieron " + totalWeeks + " semanas en " + site.name + "! " +
                       (science >= scienceGoal ? "Además lograste la meta de ciencia." : "Pero faltaron puntos de ciencia para la meta (" + scienceGoal + ").");
            if (endReason == "air") return "Se acabó el aire de la base. Los astronautas fueron evacuados a salvo.";
            if (endReason == "dose") return "La tripulación llegó al límite de radiación de la misión y regresó a la Tierra.";
            return "Se acabó la comida durante demasiado tiempo y la misión tuvo que terminar.";
        }

        public List<string> Lessons()
        {
            List<string> l = new List<string>();
            if (endReason == "air") l.Add("El aire es lo primero: sin soporte vital nada más importa. Los ingenieros le dan siempre la prioridad más alta.");
            if (endReason == "dose") l.Add("La radiación se acumula semana tras semana. Un escudo se construye una vez y protege toda la misión.");
            if (endReason == "food") l.Add("Las plantas tardan en crecer: hay que encender el invernadero ANTES de quedarse sin comida.");
            if (wastedPower >= 4) l.Add("Perdiste " + wastedPower + " de energía con la batería llena. Una batería no puede guardar infinito: hay que planear el día y la noche.");
            if (stormsSeen > 0 && sheltered == 0) l.Add("No te refugiaste en ninguna tormenta solar. En la vida real, el refugio es la mejor defensa mientras dura la tormenta.");
            if (stormsSeen > 0 && sheltered > 0) l.Add("Te refugiaste " + sheltered + (sheltered == 1 ? " vez" : " veces") + " en tormentas solares: así protegen los ingenieros a las tripulaciones reales.");
            if (askedEarthLate > 0) l.Add("En Marte preguntaste a la Tierra y la respuesta tardó demasiado. Por eso los astronautas de Marte tendrán que decidir solos.");
            if (askedEarth > 0 && askedEarthLate == 0) l.Add("En la Luna la Tierra responde en segundos, ¡pero en Marte tardaría hasta 22 minutos!");
            if (success && science < scienceGoal) l.Add("Sobrevivir es lo más importante, pero cada semana de ciencia cuenta. ¿Puedes lograr las dos cosas?");
            if (success && science >= scienceGoal && l.Count == 0) l.Add("Equilibraste energía, aire, comida, radiación y ciencia. ¡Eso es lo que hacen los planificadores de misiones de NASA!");
            if (l.Count == 0) l.Add("Cada decisión cambia algo: ahorrar energía hoy puede costar aire o comida mañana.");
            if (l.Count > 4) l.RemoveRange(4, l.Count - 4);
            return l;
        }

        // Elige un dato curioso relacionado con lo que pasó
        public string ChooseFactTag(GameEvent ev)
        {
            if (ev != null && !string.IsNullOrEmpty(ev.factTag)) return ev.factTag;
            factCounter++;
            if (site.longNights && IsSunny(week) && !IsSunny(week + 1)) return "night";
            if (air < 35f) return "air";
            if (food < 35f) return "food";
            if (dose > doseLimit * 0.5f) return "radiation";
            string[] pool = new string[] { "shield", "sun", "power", "comms", "general", "radiation" };
            return pool[factCounter % pool.Length];
        }

        // ---------------- Utilidades ----------------
        public static float Clamp(float v, float lo, float hi) { return v < lo ? lo : (v > hi ? hi : v); }
        public static string F(float v) { return v.ToString("0.#", CultureInfo.InvariantCulture); }
        public static string Signed(float v) { return (v >= 0 ? "+" : "") + F(v); }
    }
}
