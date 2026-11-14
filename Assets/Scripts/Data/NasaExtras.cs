// ============================================================
//  NasaExtras.cs  -  Más datos REALES de NASA (todos opcionales)
//
//  Si no hay internet o una API falla, simplemente NO se muestra
//  ese recuadro (el juego sigue igual). Nada se inventa.
//
//   1) APOD  (api.nasa.gov/planetary/apod)  -> "Foto del día" en el título
//   2) Mars Rover Photos (api.nasa.gov/mars-photos) -> foto reciente de
//      Curiosity en la pantalla de Marte
//   3) RadLab (visualization.osdr.nasa.gov/radlab/api) -> medición real de
//      radiación hecha en la Estación Espacial (dato de ARCHIVO, no en vivo)
//   4) OSDR (osdr.nasa.gov/osdr/data/search) -> un estudio real de NASA
//      relacionado con lo que pasó en tu partida (pantalla final)
//
//  NOTA WebGL: las imágenes dependen de CORS del servidor; si el
//  navegador las bloquea, el recuadro simplemente no aparece.
// ============================================================
using System;
using System.Collections;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace AstroJunior
{
    [Serializable] public class ApodData { public string title; public string url; public string media_type; public string copyright; public string date; }
    [Serializable] public class MarsCam { public string name; public string full_name; }
    [Serializable] public class MarsRoverInfo { public string name; }
    [Serializable] public class MarsPhotoItem { public int sol; public string img_src; public string earth_date; public MarsCam camera; public MarsRoverInfo rover; }
    [Serializable] public class MarsPhotoList { public MarsPhotoItem[] latest_photos; }

    public static class NasaExtras
    {
        // ----- Resultados (null = no disponible) -----
        public static Texture2D ApodTex;
        public static string ApodTitle, ApodCredit;

        public static Texture2D MarsTex;
        public static string MarsInfo;
        public static bool MarsDone;          // true cuando terminó el intento (sirva o no)

        public static string IssLine;         // texto listo para mostrar
        public static string OsdrLine;        // estudio real (se reinicia en cada partida)

        // La interfaz se suscribe aquí para redibujar cuando llega algo
        public static Action Changed;

        static void Notify() { if (Changed != null) Changed(); }

        // Ventana FIJA de ejemplo de la documentación de RadLab (instrumento DosTel, ISS)
        const string IssFrom = "2022-04-01T23:00";
        const string IssTo = "2022-04-02T01:05";

        public static void StartAll(MonoBehaviour host)
        {
            host.StartCoroutine(FetchApod());
            host.StartCoroutine(FetchMars());
            host.StartCoroutine(FetchIss());
        }

        // ======================================================
        //  1) APOD
        // ======================================================
        static IEnumerator FetchApod()
        {
            string json = null;
            yield return GetText("https://api.nasa.gov/planetary/apod?api_key=" + SpaceWeatherService.ApiKey, 10, delegate (string s) { json = s; });
            ApodData d = ParseApod(json);
            if (d == null) yield break;
            Texture2D tex = null;
            yield return GetTexture(d.url, 20, delegate (Texture2D t) { tex = t; });
            if (tex == null) yield break;
            ApodTex = tex;
            ApodTitle = d.title;
            string cr = string.IsNullOrEmpty(d.copyright) ? "NASA" : d.copyright.Replace("\n", " ").Trim();
            ApodCredit = "NASA APOD · " + d.date + " · Crédito: " + cr;
            Notify();
        }

        static ApodData ParseApod(string json)
        {
            if (string.IsNullOrEmpty(json) || !json.TrimStart().StartsWith("{")) return null;
            try
            {
                ApodData d = JsonUtility.FromJson<ApodData>(json);
                if (d == null || string.IsNullOrEmpty(d.url) || d.media_type != "image") return null;
                return d;
            }
            catch (Exception) { return null; }
        }

        // ======================================================
        //  2) FOTO REAL DE MARTE (rover Curiosity)
        // ======================================================
        static IEnumerator FetchMars()
        {
            string json = null;
            yield return GetText("https://api.nasa.gov/mars-photos/api/v1/rovers/curiosity/latest_photos?api_key=" + SpaceWeatherService.ApiKey, 12, delegate (string s) { json = s; });
            MarsPhotoItem p = PickMarsPhoto(json);
            if (p == null) { MarsDone = true; Notify(); yield break; }
            string url = p.img_src.Replace("http://", "https://");
            Texture2D tex = null;
            yield return GetTexture(url, 20, delegate (Texture2D t) { tex = t; });
            MarsDone = true;
            if (tex != null)
            {
                MarsTex = tex;
                string cam = (p.camera != null && !string.IsNullOrEmpty(p.camera.name)) ? p.camera.name : "?";
                MarsInfo = "Foto REAL de Marte tomada por el rover Curiosity (cámara " + cam + ") en el sol " + p.sol + " (" + p.earth_date + "). Un sol es un día marciano.";
            }
            Notify();
        }

        static MarsPhotoItem PickMarsPhoto(string json)
        {
            if (string.IsNullOrEmpty(json) || !json.TrimStart().StartsWith("{")) return null;
            try
            {
                MarsPhotoList list = JsonUtility.FromJson<MarsPhotoList>(json);
                if (list == null || list.latest_photos == null || list.latest_photos.Length == 0) return null;
                string[] pref = { "MAST", "NAVCAM", "FHAZ", "RHAZ" };    // cámaras que muestran el paisaje
                for (int k = 0; k < pref.Length; k++)
                    for (int i = 0; i < list.latest_photos.Length; i++)
                    {
                        MarsPhotoItem it = list.latest_photos[i];
                        if (it != null && !string.IsNullOrEmpty(it.img_src) && it.camera != null &&
                            it.camera.name != null && it.camera.name.StartsWith(pref[k])) return it;
                    }
                for (int i = 0; i < list.latest_photos.Length; i++)
                    if (list.latest_photos[i] != null && !string.IsNullOrEmpty(list.latest_photos[i].img_src)) return list.latest_photos[i];
                return null;
            }
            catch (Exception) { return null; }
        }

        // ======================================================
        //  3) RadLab: radiación medida en la Estación Espacial
        // ======================================================
        static IEnumerator FetchIss()
        {
            string url = "https://visualization.osdr.nasa.gov/radlab/api/?spacecraft=ISS&instrument=DosTel"
                + "&timestamp%3E%3D" + IssFrom + "&timestamp%3C" + IssTo
                + "&absorbed_dose_rate&format=csv";
            string csv = null;
            yield return GetText(url, 12, delegate (string s) { csv = s; });
            double mean = MeanColumn(csv, "absorbed_dose_rate");
            if (double.IsNaN(mean) || mean <= 0) yield break;
            double perDay = mean * 24.0 / 1000.0;     // µGy/h -> mGy/día
            IssLine = "Dato real de RadLab (NASA OSDR): el instrumento DosTel, dentro de la Estación Espacial, midió "
                + mean.ToString("0.0", CultureInfo.InvariantCulture) + " µGy por hora (≈ "
                + perDay.ToString("0.00", CultureInfo.InvariantCulture) + " mGy al día). Medición de archivo (1 abr 2022), no en vivo.";
            Notify();
        }

        // Promedio de una columna de un CSV (busca el nombre en el encabezado). NaN si no se puede.
        static double MeanColumn(string csv, string column)
        {
            if (string.IsNullOrEmpty(csv)) return double.NaN;
            string[] lines = csv.Split('\n');
            if (lines.Length < 2) return double.NaN;
            string[] head = lines[0].Split(',');
            int col = -1;
            for (int i = 0; i < head.Length; i++)
                if (head[i].Trim().Trim('"').ToLowerInvariant() == column) { col = i; break; }
            if (col < 0) return double.NaN;
            double sum = 0; int n = 0;
            for (int r = 1; r < lines.Length; r++)
            {
                string[] c = lines[r].Split(',');
                if (col >= c.Length) continue;
                double v;
                if (double.TryParse(c[col].Trim().Trim('"'), NumberStyles.Float, CultureInfo.InvariantCulture, out v)) { sum += v; n++; }
            }
            return n == 0 ? double.NaN : sum / n;
        }

        // ======================================================
        //  4) OSDR: un estudio real relacionado con tu partida
        // ======================================================
        public static void FetchOsdr(MonoBehaviour host, string term)
        {
            OsdrLine = null;
            host.StartCoroutine(OsdrRoutine(term));
        }

        static IEnumerator OsdrRoutine(string term)
        {
            string json = null;
            yield return GetText("https://osdr.nasa.gov/osdr/data/search?term=" + UnityWebRequest.EscapeURL(term) + "&size=3", 15, delegate (string s) { json = s; });
            string title = JsonStr(json, "Study Title");
            if (string.IsNullOrEmpty(title)) yield break;
            string acc = JsonStr(json, "Accession");
            title = title.Trim();
            if (title.Length > 110) title = title.Substring(0, 107) + "...";
            OsdrLine = "\"" + title + "\"" + (string.IsNullOrEmpty(acc) ? "" : " (" + acc + ")") + " · en inglés";
            Notify();
        }

        // Lee el primer valor de texto de una llave de un JSON (sin librerías externas)
        public static string JsonStr(string json, string key)
        {
            if (string.IsNullOrEmpty(json)) return null;
            int i = json.IndexOf("\"" + key + "\"", StringComparison.Ordinal);
            if (i < 0) return null;
            i += key.Length + 2;
            while (i < json.Length && (json[i] == ' ' || json[i] == ':' || json[i] == '\n' || json[i] == '\r' || json[i] == '\t')) i++;
            if (i >= json.Length || json[i] != '"') return null;
            i++;
            StringBuilder sb = new StringBuilder();
            while (i < json.Length)
            {
                char c = json[i];
                if (c == '"') return sb.ToString();
                if (c == '\\' && i + 1 < json.Length)
                {
                    char n = json[i + 1];
                    if (n == 'n' || n == 'r' || n == 't') { sb.Append(' '); i += 2; continue; }
                    if (n == 'u' && i + 5 < json.Length)
                    {
                        int code;
                        if (int.TryParse(json.Substring(i + 2, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code)) sb.Append((char)code);
                        i += 6; continue;
                    }
                    sb.Append(n); i += 2; continue;
                }
                sb.Append(c); i++;
            }
            return null;
        }

        // ======================================================
        //  Ayudantes de red
        // ======================================================
        static IEnumerator GetText(string url, int timeoutSec, Action<string> result)
        {
            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                req.timeout = timeoutSec;
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success) result(req.downloadHandler.text);
                else result(null);
            }
        }

        static IEnumerator GetTexture(string url, int timeoutSec, Action<Texture2D> result)
        {
            using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(url))
            {
                req.timeout = timeoutSec;
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    Texture2D t = DownloadHandlerTexture.GetContent(req);
                    result(t);
                }
                else result(null);
            }
        }
    }
}
