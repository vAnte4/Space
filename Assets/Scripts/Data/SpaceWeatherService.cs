// ============================================================
//  SpaceWeatherService.cs  -  Datos REALES del clima espacial
//  Llama a la API DONKI de NASA (https://api.nasa.gov/):
//     - FLR = llamaradas solares (solar flares)
//     - SEP = tormentas de partículas solares
//  de los últimos 30 días. Si no hay internet o la API falla,
//  usa un ARCHIVO de grandes llamaradas reales y lo marca como
//  "ARCHIVO" (nunca lo presenta como si fuera en vivo).
//
//  Llave propia de api.nasa.gov (1000 pedidos/hora). Es gratis: si el repo es
//  público, después del hackathon pidan una nueva en https://api.nasa.gov/.
// ============================================================
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace AstroJunior
{
    [Serializable] public class FlareItem { public string flrID; public string beginTime; public string peakTime; public string classType; }
    [Serializable] public class FlareList { public FlareItem[] items; }
    [Serializable] public class SepItem { public string sepID; public string eventTime; }
    [Serializable] public class SepList { public SepItem[] items; }

    public static class SpaceWeatherService
    {
        public static string ApiKey = "dgmLgGJFK9QfmrwmfbHnwRfqrr867hpbcfpGWWMH";   // llave gratis de api.nasa.gov (del equipo Polarsten)
        const string BaseUrl = "https://api.nasa.gov/DONKI/";

        public static IEnumerator Fetch(Action<SpaceWeatherInfo> done)
        {
            string end = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string start = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd");
            string range = start + " a " + end;
            string q = "?startDate=" + start + "&endDate=" + end + "&api_key=" + ApiKey;

            string flrJson = null;
            string sepJson = null;
            yield return Get(BaseUrl + "FLR" + q, delegate (string s) { flrJson = s; });
            yield return Get(BaseUrl + "SEP" + q, delegate (string s) { sepJson = s; });

            List<FlareRec> flares = ParseFlares(flrJson);
            if (flares == null)
            {
                Debug.Log("[Clima espacial] No se pudo leer DONKI. Usando datos de archivo.");
                done(SpaceWeatherInfo.Archive());
                yield break;
            }
            int sep = ParseSepCount(sepJson);
            done(SpaceWeatherInfo.Build(flares, sep < 0 ? 0 : sep, true, range));
        }

        static IEnumerator Get(string url, Action<string> result)
        {
            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                req.timeout = 8;
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success) result(req.downloadHandler.text);
                else result(null);
            }
        }

        // Devuelve null si la respuesta no sirve
        static List<FlareRec> ParseFlares(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            string t = json.Trim();
            if (!t.StartsWith("[")) return null;      // error de la API (por ejemplo límite de pedidos)
            try
            {
                FlareList list = JsonUtility.FromJson<FlareList>("{\"items\":" + t + "}");
                List<FlareRec> res = new List<FlareRec>();
                if (list != null && list.items != null)
                {
                    for (int i = 0; i < list.items.Length; i++)
                    {
                        FlareItem f = list.items[i];
                        if (f == null || string.IsNullOrEmpty(f.classType)) continue;
                        res.Add(new FlareRec(f.classType, string.IsNullOrEmpty(f.peakTime) ? f.beginTime : f.peakTime));
                    }
                }
                return res;
            }
            catch (Exception e)
            {
                Debug.Log("[Clima espacial] Error leyendo FLR: " + e.Message);
                return null;
            }
        }

        // Devuelve -1 si no se pudo leer
        static int ParseSepCount(string json)
        {
            if (string.IsNullOrEmpty(json)) return -1;
            string t = json.Trim();
            if (!t.StartsWith("[")) return -1;
            try
            {
                SepList list = JsonUtility.FromJson<SepList>("{\"items\":" + t + "}");
                return (list != null && list.items != null) ? list.items.Length : 0;
            }
            catch (Exception)
            {
                return -1;
            }
        }
    }
}
