using UnityEngine;

namespace VividOfFade
{
    /// <summary>M1 用の固定の1部屋。中心が原点の長方形で、外周を壁で囲む。</summary>
    public sealed class Room : MonoBehaviour
    {
        public float HalfW { get; private set; }
        public float HalfH { get; private set; }

        public void Build(int width, int height)
        {
            HalfW = width * 0.5f;
            HalfH = height * 0.5f;
            MakeLayer("Wall", PixelArt.WallTile, new Vector2(width + 2, height + 2), SortOrder.Floor);
            MakeLayer("Floor", PixelArt.FloorTile, new Vector2(width, height), SortOrder.Floor + 1);
        }

        void MakeLayer(string name, Sprite sprite, Vector2 size, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = size;
            sr.sortingOrder = order;
        }

        public Vector2 Clamp(Vector2 p, float radius)
        {
            return new Vector2(
                Mathf.Clamp(p.x, -HalfW + radius, HalfW - radius),
                Mathf.Clamp(p.y, -HalfH + radius, HalfH - radius));
        }

        public bool Contains(Vector2 p) => Mathf.Abs(p.x) <= HalfW && Mathf.Abs(p.y) <= HalfH;
    }
}
