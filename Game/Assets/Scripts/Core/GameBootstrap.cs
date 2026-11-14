using UnityEngine;
using UnityEngine.SceneManagement;

namespace VividOfFade
{
    /// <summary>
    /// M1 の試作は、シーンに何も置かなくても SampleScene で Play すれば始まる。
    /// 本番用のシーン（Title / Hub / Run）を作ったら、この自動起動は外す。
    /// </summary>
    public static class GameBootstrap
    {
        const string PrototypeScene = "SampleScene";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (SceneManager.GetActiveScene().name != PrototypeScene) return;
            if (Object.FindAnyObjectByType<GameWorld>() != null || Object.FindAnyObjectByType<Loader>() != null) return;
            new GameObject("Loader").AddComponent<Loader>(); // 読み込み画面を出しながら準備して、終わったら GameWorld を作る
        }
    }
}
