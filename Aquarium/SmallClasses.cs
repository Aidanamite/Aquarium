using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Aquarium
{
    public class CachedPredicate<T>
    {
        T v;
        bool l = false;
        Func<T> f;
        public CachedPredicate(Func<T> function) => f = function;
        public T Value
        {
            get
            {
                if (l)
                    return v;
                v = f();
                l = true;
                return v;
            }
        }
    }

    public enum FishSize
    {
        Tiny,
        Small,
        Large
    }
    public class FishPrefab
    {
        public string modelName;
        public FishSize size;
        public FishAnimator prefab;
        public Predicate<Renderer> modelPredicate;
        public (Predicate<Mesh> mesh, Predicate<Material> material)? modelPredicates;
        public string DisplayName;
        public Func<(Mesh, Material[])> getModel;
        public string initOnScene;
        public float scale = 1;
        public float? HeadLengthOverride;
        public float? BendLengthOverride;
        public bool? WiggleXOverride;
        public bool? WiggleYOverride;
        public float? WiggleVolumeOverride;
        public float? WiggleSpeedOverride;
        public float? WiggleAmplitudeOverride;
    }

    public class AquariumPrefab
    {
        public GenerationSettings Generation;
        public int InventoryWidth;
        public int InventoryHeight;
        public float FishVolume;
        public string UniqueName;
        public int UniqueIndex;
        public string DisplayName;
        public string IconName;
        public AquariumBlock Prefab;
        public FishSize MaxSize;
        public AquariumPrefab(GenerationSettings generation, int inventoryWidth, int inventoryHeight, float fishVolume, string uniqueName, int uniqueIndex, string displayName, string iconName, FishSize maxSize)
            => (Generation, InventoryWidth, InventoryHeight, FishVolume, UniqueName, UniqueIndex, DisplayName, IconName, MaxSize)
             = (generation, inventoryWidth, inventoryHeight, fishVolume, uniqueName, uniqueIndex, displayName, iconName, maxSize);
    }

    public class ItemGenerationSettings
    {
        public string UniqueName;
        public int UniqueIndex;
        public string DisplayName;
        public string IconName;
        public Item_Base Item;
        public Action<Item_Base> AdditionalSetup;
    }

    public class DisableBySnapping : MonoBehaviour
    {
        Collider _c;
        public Collider Collider => _c ? _c : (_c = GetComponent<Collider>());
        public bool DisableOnSnapping = false;
        void OnEnable() => Update();
        void Update()
        {
            if (Collider && ComponentManager<Network_Player>.Value && ComponentManager<Network_Player>.Value.BlockCreator?.selectedBlock)
            {
                Collider.enabled = ComponentManager<Network_Player>.Value.BlockCreator?.selectedBlock.snapsToQuads != DisableOnSnapping;
            }
        }
    }
}
