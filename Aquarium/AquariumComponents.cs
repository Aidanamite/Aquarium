using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Aquarium
{
    public class AquariumBlock : Storage_Small
    {
        public MovementRules Rules;
        public int InventoryWidth;
        public int InventoryHeight;
        public float FishVolume;
        public FishSize MaxSize;
        public Dictionary<string,List<FishAnimator>> spawnedFish = new Dictionary<string, List<FishAnimator>>();
        public override void OnFinishedPlacement()
        {
            base.OnFinishedPlacement();
            if (GetInventoryReference() is AquariumInventory inv)
            {
                inv.aquarium = this;
                inv.SetSlots(InventoryWidth, InventoryHeight);
            }
        }
        public void OnClose(Network_Player player)
        {
            var target = new Dictionary<string, int>();
            var foundVolume = 0f;
            var failed = new Dictionary<string, int>();
            var sizeFail = new HashSet<string>();
            foreach (var s in GetInventoryReference().allSlots)
                if (s && s.HasValidItemInstance() && Main.fishPrefabs.TryGetValue(s.itemInstance.UniqueName,out var prefab) && prefab.prefab)
                {
                    if (prefab.size > MaxSize)
                    {
                        sizeFail.Add(prefab.DisplayName);
                        continue;
                    }
                    if (prefab.size.GetVolume() + foundVolume > FishVolume)
                    {
                        if (!failed.TryGetValue(prefab.DisplayName, out var f))
                            f = 0;
                        failed[prefab.DisplayName] = f + s.itemInstance.Amount;
                        continue;
                    }
                    if (!target.TryGetValue(s.itemInstance.UniqueName, out var current))
                        current = 0;
                    for (int i = 0; i < s.itemInstance.Amount; i++)
                    {
                        if (prefab.size.GetVolume() + foundVolume > FishVolume)
                        {
                            if (!failed.TryGetValue(prefab.DisplayName, out var f))
                                f = 0;
                            failed[prefab.DisplayName] = f + s.itemInstance.Amount - i;
                            break;
                        }
                        current++;
                        foundVolume += prefab.size.GetVolume();
                    }
                    target[s.itemInstance.UniqueName] = current;
                }
            if (failed.Count > 0 || sizeFail.Count > 0)
            {
                var failMessage = $"Failed to spawn some fish in a {buildableItem.settings_Inventory.DisplayName}:";
                foreach (var i in sizeFail)
                    failMessage += $"\n - This tank is too small for {i}";
                foreach (var p in failed)
                    failMessage += $"\n - Tank has reached max capacity. {p.Value} {p.Key} failed to spawn";
                Debug.LogWarning(failMessage);
            }
            var remove = new HashSet<string>(spawnedFish.Keys);
            foreach (var t in target)
            {
                remove.Remove(t.Key);
                if (!spawnedFish.TryGetValue(t.Key, out var l))
                    spawnedFish[t.Key] = l = new List<FishAnimator>();
                while (l.Count < t.Value)
                {
                    var n = Instantiate(Main.fishPrefabs[t.Key].prefab, transform);
                    l.Add(n);
                    n.OnSpawn(Rules);
                }
                while (l.Count > t.Value)
                {
                    Destroy(l[t.Value].gameObject);
                    l.RemoveAt(t.Value);
                }
            }
            foreach (var r in remove)
            {
                foreach (var f in spawnedFish[r])
                    Destroy(f.gameObject);
                spawnedFish.Remove(r);
            }
        }
    }

    public class AquariumInventory : Inventory
    {
        public AquariumBlock aquarium;
        public Transform SlotContainer;
        public Vector2 Initial;
        public void SetSlots(int width, int height)
        {
            foreach (Transform s in SlotContainer)
                DestroyImmediate(s.gameObject);
            allSlots.Clear();
            var r = GetComponent<RectTransform>();
            var g = SlotContainer.GetComponent<GridLayoutGroup>();
            g.constraintCount = width;
            r.offsetMax = new Vector2(Initial.x + (g.cellSize.x + g.spacing.x) * width, r.offsetMax.y);
            r.sizeDelta = new Vector2(r.sizeDelta.x, Initial.y + (g.cellSize.y + g.spacing.y) * height);
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    Instantiate(slotPrefab, SlotContainer, false);
            InitializeSlots();
        }
    }
}
