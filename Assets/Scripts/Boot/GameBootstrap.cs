// ============================================================
//  GameBootstrap.cs  -  Arranca el juego solo.
//  No hay que arrastrar nada en el editor: al darle Play en
//  CUALQUIER escena, este script crea el juego completo.
// ============================================================
using UnityEngine;

namespace AstroJunior
{
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (Object.FindObjectOfType<GameUI>() != null) return;
            GameObject go = new GameObject("AstroJuniorGame");
            go.AddComponent<GameUI>();
        }
    }
}
