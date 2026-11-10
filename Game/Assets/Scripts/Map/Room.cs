using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// 試作用の固定の1部屋。論理座標では中心が原点の長方形で、画面ではひし形の床になる。
    /// 奥の2辺（画面の上側）に立体の壁を並べる。
    /// </summary>
    public sealed class Room : MonoBehaviour
    {
        public float HalfW { get; private set; }
        public float HalfH { get; private set; }

        public void Build(int width, int height)
        {
            HalfW = width * 0.5f;
            HalfH = height * 0.5f;

            var floor = PixelArt.IsoFloor;
            var floorAlt = PixelArt.IsoFloorAlt;
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    var p = new Vector2(i - HalfW + 0.5f, j - HalfH + 0.5f);
                    Place("Floor", (i + j) % 2 == 0 ? floor : floorAlt, p, SortOrder.Floor);
                }
            }

            // 奥の壁：右上の辺（x の外側）と左上の辺（y の外側）、それと奥の角
            var wall = PixelArt.IsoWall;
            for (int j = 0; j <= height; j++)
            {
                var p = new Vector2(HalfW + 0.5f, j - HalfH + 0.5f);
                Place("Wall", wall, p, SortOrder.For(p));
            }
            for (int i = 0; i < width; i++)
            {
                var p = new Vector2(i - HalfW + 0.5f, HalfH + 0.5f);
                Place("Wall", wall, p, SortOrder.For(p));
            }
        }

        void Place(string name, Sprite sprite, Vector2 logical, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = Iso.ToScreen(logical);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
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
