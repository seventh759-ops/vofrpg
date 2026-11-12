using System;
using System.Collections.Generic;
using UnityEngine;

namespace VividOfFade
{
    /// <summary>地面に落ちた装備か火種1つ。</summary>
    public sealed class GroundLoot : MonoBehaviour
    {
        public SpriteRenderer Sr;
        public SpriteRenderer Beam;
        public Item Item;      // null なら火種
        public int Embers;

        public Vector2 From;
        public Vector2 To;
        public float FlightTime;
        public float T;
        public bool Landed;
        public bool Magnet;
        public float MagnetSpeed;
        public float Age;

        Vector2 pos;

        /// <summary>地面からの高さ（画面の上方向。飛んでいる間と弾むとき）。</summary>
        public float Height;

        /// <summary>論理座標。画面にはクォータービューに変換し、高さの分だけ上に描く。</summary>
        public Vector2 Pos
        {
            get => pos;
            set
            {
                pos = value;
                transform.position = Iso.ToScreen(value) + new Vector2(0f, Height);
            }
        }
    }

    /// <summary>
    /// ドロップの噴水・地面のドロップ・吸い寄せと拾う処理。
    /// 珍しい品は着地したときに光の柱を立て、Landed で GameWorld に知らせる（音・揺れ・スロー）。
    /// </summary>
    public sealed class LootSystem : MonoBehaviour
    {
        const int MaxGround = 150;

        readonly List<GroundLoot> active = new List<GroundLoot>();
        Pool<GroundLoot> pool;
        Inventory inventory;
        float vacuumTimer = -1f;

        public event Action<Item> Landed;
        public event Action<Item> PickedUp;
        public event Action<int> EmbersPickedUp;

        public int GroundCount => active.Count;

        public void Init(Inventory inventory)
        {
            this.inventory = inventory;
            pool = new Pool<GroundLoot>(Create);
        }

        GroundLoot Create()
        {
            var go = new GameObject("Loot");
            go.transform.SetParent(transform, false);
            var g = go.AddComponent<GroundLoot>();
            g.Sr = go.AddComponent<SpriteRenderer>();
            Lighting.MakeGlow(g.Sr); // ドロップは暗闇でも見える

            var beam = new GameObject("Beam");
            beam.transform.SetParent(go.transform, false);
            beam.transform.localScale = new Vector3(3f, 0.9f, 1f); // 高さ約1.8マス
            g.Beam = beam.AddComponent<SpriteRenderer>();
            Lighting.MakeGlow(g.Beam);
            g.Beam.sprite = PixelArt.Beam;

            go.SetActive(false);
            return g;
        }

        public void DropItem(Vector2 from, Item item, Room room, float spread = 1f)
        {
            var g = Spawn(from, room, spread);
            g.Item = item;
            g.Embers = 0;
            g.Sr.sprite = PixelArt.ItemIcon(item);
        }

        public void DropEmbers(Vector2 from, int amount, Room room, float spread = 1f)
        {
            var g = Spawn(from, room, spread);
            g.Item = null;
            g.Embers = amount;
            g.Sr.sprite = PixelArt.Ember;
        }

        /// <summary>ドロップの噴水。装備と火種を一斉にあふれさせる。</summary>
        public void Fountain(Vector2 at, Room room, int items, int level, float rarityBonus, int emberOrbs, int emberAmount)
        {
            for (int i = 0; i < emberOrbs; i++) DropEmbers(at, emberAmount, room, 2.2f);
            for (int i = 0; i < items; i++) DropItem(at, ItemGenerator.Roll(level, rarityBonus), room, 2.2f);
        }

        GroundLoot Spawn(Vector2 from, Room room, float spread)
        {
            if (active.Count >= MaxGround) MakeRoom();

            var g = pool.Get();
            g.From = from;
            Vector2 offset = UnityEngine.Random.insideUnitCircle * spread;
            if (offset.sqrMagnitude < 0.09f) offset = offset.normalized * 0.3f + new Vector2(0.01f, 0f);
            g.To = room.Clamp(from + offset, 0.4f);
            g.FlightTime = UnityEngine.Random.Range(0.35f, 0.55f);
            g.T = 0f;
            g.Landed = false;
            g.Magnet = false;
            g.MagnetSpeed = 0f;
            g.Age = 0f;
            g.Height = 0f;
            g.Pos = from;
            g.Sr.color = Color.white;
            g.Beam.enabled = false;
            active.Add(g);
            return g;
        }

        /// <summary>少し待ってから、地面のドロップをすべてプレイヤーへ吸い寄せる（波を全滅させたとき）。</summary>
        public void VacuumAfter(float delay)
        {
            vacuumTimer = delay;
        }

        public void Tick(float dt, Player player, Inventory inventory)
        {
            if (vacuumTimer >= 0f)
            {
                vacuumTimer -= dt;
                if (vacuumTimer < 0f)
                {
                    foreach (var g in active) g.Magnet = true; // 飛んでいる途中の品は着地してから寄ってくる
                }
            }

            Vector2 target = player.Pos;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var g = active[i];
                g.Age += dt;

                if (!g.Landed)
                {
                    // 放物線を描いて飛ぶ
                    g.T += dt / g.FlightTime;
                    float t = Mathf.Clamp01(g.T);
                    g.Height = 4f * t * (1f - t) * 1.2f;
                    g.Pos = Vector2.Lerp(g.From, g.To, t);
                    g.Sr.sortingOrder = SortOrder.For(g.To) + 2;
                    if (t >= 1f)
                    {
                        g.Landed = true;
                        if (g.Item != null && g.Item.Rarity >= Rarity.Rare)
                        {
                            g.Beam.enabled = true;
                            var c = RarityInfo.ColorOf(g.Item.Rarity);
                            c.a = 0.55f;
                            g.Beam.color = c;
                            g.Beam.sortingOrder = g.Sr.sortingOrder - 1;
                        }
                        Landed?.Invoke(g.Item);
                    }
                    continue;
                }

                bool canTake = g.Item == null || inventory.CanTake(g.Item);
                Vector2 to = target - g.Pos;
                float dist = to.magnitude;

                if (!g.Magnet && canTake && dist < GameConfig.MagnetRange) g.Magnet = true;

                if (g.Magnet && canTake)
                {
                    g.Height = 0f;
                    g.MagnetSpeed = Mathf.Min(g.MagnetSpeed + 40f * dt, 22f);
                    if (dist > 0.001f) g.Pos += to / dist * Mathf.Min(dist, g.MagnetSpeed * dt);
                    if (dist < GameConfig.PickupRange)
                    {
                        Collect(g, inventory);
                        ReleaseAt(i);
                    }
                    continue;
                }

                // 地面で小さく弾む
                g.Height = Mathf.Abs(Mathf.Sin(g.Age * 3f)) * 0.08f;
                g.Pos = g.To;
            }
        }

        /// <summary>
        /// 地面のドロップが多すぎるとき、1つ片付ける。失わないように、火種はそのまま所持に、
        /// 装備は分解した火種にする。片付ける順：古い火種 → 古い白 → 青 → … （珍しい品ほど残す）。
        /// </summary>
        void MakeRoom()
        {
            int victim = -1;
            for (int i = 0; i < active.Count; i++)
            {
                if (active[i].Item == null)
                {
                    victim = i;
                    break;
                }
            }
            for (int r = 0; victim < 0 && r <= (int)Rarity.Relic; r++)
            {
                for (int i = 0; i < active.Count; i++)
                {
                    if ((int)active[i].Item.Rarity != r) continue;
                    victim = i;
                    break;
                }
            }
            if (victim < 0) victim = 0;

            var g = active[victim];
            inventory.Embers += g.Item == null ? g.Embers : RarityInfo.SalvageOf(g.Item.Rarity);
            ReleaseAt(victim);
        }

        /// <summary>拾う火種の倍率（装備の効果。GameWorld が毎フレーム入れる）。</summary>
        public static float EmberMult = 1f;

        void Collect(GroundLoot g, Inventory inventory)
        {
            if (g.Item == null)
            {
                int amount = Mathf.RoundToInt(g.Embers * EmberMult);
                inventory.Embers += amount;
                EmbersPickedUp?.Invoke(amount);
                return;
            }
            if (inventory.Add(g.Item)) PickedUp?.Invoke(g.Item);
        }

        /// <summary>地面のドロップをまとめて拾う（層を下りるとき）。鞄に入らない装備は残念ながら置いていく。</summary>
        public void CollectAll(Inventory inventory)
        {
            foreach (var g in active)
            {
                if (g.Item == null) inventory.Embers += Mathf.RoundToInt(g.Embers * EmberMult);
                else if (inventory.CanTake(g.Item)) inventory.Add(g.Item);
            }
            Clear();
        }

        public void Clear()
        {
            vacuumTimer = -1f;
            for (int i = active.Count - 1; i >= 0; i--) ReleaseAt(i);
        }

        void ReleaseAt(int i)
        {
            var g = active[i];
            active.RemoveAt(i); // 古い順を保つため、ここは詰めて消す
            g.Item = null;
            pool.Release(g);
        }
    }
}
