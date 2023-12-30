using HarmonyLib;
using HMLLibrary;
using RaftModLoader;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using I2.Loc;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Aquarium
{
    public class Main : Mod
    {
        public const float glassThickness = 0.02f;

        public static Main instance;
        public static List<Object> created = new List<Object>();
        Harmony harmony;
        public static LanguageSourceData language;
        public static Dictionary<string, FishPrefab> fishPrefabs = new Dictionary<string, FishPrefab>
        {
            ["Raw_Pomfret"] = new FishPrefab() { modelName = "pomfret", size = FishSize.Tiny, DisplayName = "Pomfret" },
            ["Raw_Tilapia"] = new FishPrefab() { modelName = "tilapia", size = FishSize.Small, DisplayName = "Tilapia" },
            ["Raw_Herring"] = new FishPrefab() { modelName = "herring", size = FishSize.Tiny, DisplayName = "Herring" },
            ["Raw_Mackerel"] = new FishPrefab() { modelName = "mackrill", size = FishSize.Small, DisplayName = "Mackerel" },
            ["Raw_Catfish"] = new FishPrefab() { modelName = "CatFish (1)", size = FishSize.Large, DisplayName = "Catfish" },
            ["Raw_Salmon"] = new FishPrefab() { modelName = "salmon", size = FishSize.Large, DisplayName = "Salmon" },
            ["Head_Shark"] = new FishPrefab() {
                getModel = () => GetModel(Traverse.Create(ComponentManager<Network_Host_Entities>.Value).Method("GetNetworkBehaviourPrefabFromType", AI_NetworkBehaviourType.Shark).GetValue<AI_NetworkBehaviour>().GetComponentInChildren<SkinnedMeshRenderer>(true)).Value,
                initOnScene = Raft_Network.GameSceneName,
                size = FishSize.Large,
                DisplayName = "Shark",
                scale = 0.2f
            },
            ["Head_PoisonPuffer"] = new FishPrefab()
            {
                getModel = () => GetModel(Traverse.Create(ComponentManager<Network_Host_Entities>.Value).Method("GetNetworkBehaviourPrefabFromType", AI_NetworkBehaviourType.PufferFish).GetValue<AI_NetworkBehaviour>().GetComponentInChildren<SkinnedMeshRenderer>(true)).Value,
                initOnScene = Raft_Network.GameSceneName,
                size = FishSize.Large,
                DisplayName = "Puffer Fish",
                scale = 0.9f,
                HeadLengthOverride = 0.2f
            },
            ["Head_AnglerFish"] = new FishPrefab()
            {
                getModel = () => (VanillaModels["AnglerFish"], Traverse.Create(ComponentManager<Network_Host_Entities>.Value).Method("GetNetworkBehaviourPrefabFromType", AI_NetworkBehaviourType.AnglerFish).GetValue<AI_NetworkBehaviour>().GetComponentInChildren<SkinnedMeshRenderer>(true).sharedMaterials),
                initOnScene = Raft_Network.GameSceneName,
                size = FishSize.Large,
                DisplayName = "Angler Fish",
                scale = 0.8f,
                BendLengthOverride = 1,
                HeadLengthOverride = 0.5f
            },
            ["Placeable_RhinoSharkTrophy"] = new FishPrefab()
            {
                getModel = () => (VanillaModels["RhinoShark"], Traverse.Create(ComponentManager<Network_Host_Entities>.Value).Method("GetNetworkBehaviourPrefabFromType", AI_NetworkBehaviourType.Boss_Varuna).GetValue<AI_NetworkBehaviour>().GetComponentInChildren<SkinnedMeshRenderer>(true).sharedMaterials),
                initOnScene = Raft_Network.GameSceneName,
                size = FishSize.Large,
                DisplayName = "Rhino Shark",
                scale = 0.15f,
                HeadLengthOverride = 0.2f,
                BendLengthOverride = 1
            },
            ["DecoFish_Stingray"] = new FishPrefab()
            {
                getModel = () => (VanillaModels["Stingray"], Traverse.Create(ComponentManager<Network_Host_Entities>.Value).Method("GetNetworkBehaviourPrefabFromType", AI_NetworkBehaviourType.Stingray).GetValue<AI_NetworkBehaviour>().GetComponentInChildren<SkinnedMeshRenderer>(true).sharedMaterials),
                initOnScene = Raft_Network.GameSceneName,
                size = FishSize.Large,
                DisplayName = "Stingray",
                scale = 0.15f,
                WiggleXOverride = false,
                WiggleYOverride = true
            },
            ["DecoFish_Whale"] = new FishPrefab()
            {
                getModel = () => (VanillaModels["Whale"], Traverse.Create(ComponentManager<Network_Host_Entities>.Value).Method("GetNetworkBehaviourPrefabFromType", AI_NetworkBehaviourType.Whale).GetValue<AI_NetworkBehaviour>().GetComponentInChildren<SkinnedMeshRenderer>(true).sharedMaterials),
                initOnScene = Raft_Network.GameSceneName,
                size = FishSize.Large,
                DisplayName = "Whale",
                scale = 0.04f,
                HeadLengthOverride = 0.2f,
                WiggleXOverride = false,
                WiggleYOverride = true
            },
            ["DecoFish_Dolphin"] = new FishPrefab()
            {
                getModel = () => (VanillaModels["Dolphin"], Traverse.Create(ComponentManager<Network_Host_Entities>.Value).Method("GetNetworkBehaviourPrefabFromType", AI_NetworkBehaviourType.Dolphin).GetValue<AI_NetworkBehaviour>().GetComponentInChildren<SkinnedMeshRenderer>(true).sharedMaterials),
                initOnScene = Raft_Network.GameSceneName,
                size = FishSize.Large,
                DisplayName = "Dolphin",
                scale = 0.3f,
                HeadLengthOverride = 0.2f,
                WiggleXOverride = false,
                WiggleYOverride = true
            }
        };

        public static ItemGenerationSettings[] itemSettings = new[]
        {
            new ItemGenerationSettings() {
                DisplayName = "Decorational fishing bait@Use for catching random sea creatures! Used by anglers with an eye for fashion.",
                UniqueName = "FishingBait_Decor",
                UniqueIndex = 7999,
                IconName = "Bait",
                AdditionalSetup = x => x.SetRecipe(new[] { new CostMultiple(new[] { ItemManager.GetItemByName("Flower_Blue") }, 1), new CostMultiple(new[] { ItemManager.GetItemByName("Scrap") }, 1) }, CraftingCategory.Tools, 5)
            },
            new ItemGenerationSettings() {
                DisplayName = "Mini Whale@Place inside an aquarium to see yo mama swimming around",
                UniqueName = "DecoFish_Whale",
                UniqueIndex = 7998,
                IconName = "Whale"
            },
            new ItemGenerationSettings() {
                DisplayName = "Mini Dolphin@Place inside an aquarium to see a small dolphin swimming around",
                UniqueName = "DecoFish_Dolphin",
                UniqueIndex = 7997,
                IconName = "Dolphin"
            },
            new ItemGenerationSettings() {
                DisplayName = "Mini Stingray@Place inside an aquarium to see a small stringray swimming around",
                UniqueName = "DecoFish_Stingray",
                UniqueIndex = 7996,
                IconName = "Stingray"
            }
        };
        public static AquariumPrefab[] aquariumSettings = new[]
        {
            new AquariumPrefab(new CylinderGeneration(60, 2.5f * BlockCreator.BlockSize, 4 * BlockCreator.HalfFloorHeight, glassThickness, 1), 5, 8, 100, "Placeable_CylinderAquarium_Large", 8000, "Large Cylinder Aquarium (5x5x4)@A large-sized aquarium for holding a lot of fish", "CylinderAquarium", FishSize.Large),
            new AquariumPrefab(new CylinderGeneration(40, 1.5f * BlockCreator.BlockSize, 2 * BlockCreator.HalfFloorHeight, glassThickness, 0.8f), 5, 4, 40, "Placeable_CylinderAquarium_Medium", 8001, "Medium Cylinder Aquarium (3x3x2)@A medium-sized aquarium for holding some fish", "CylinderAquarium", FishSize.Large),
            new AquariumPrefab(new CylinderGeneration(20, 0.5f * BlockCreator.BlockSize, 1 * BlockCreator.HalfFloorHeight, glassThickness, 0.3f), 5, 2, 10, "Placeable_CylinderAquarium_Small", 8002, "Small Cylinder Aquarium (1x1x1)@A small-sized aquarium for holding a few fish", "CylinderAquarium", FishSize.Small),
            new AquariumPrefab(new BoxGeneration(5 * BlockCreator.BlockSize, 5 * BlockCreator.BlockSize, 4 * BlockCreator.HalfFloorHeight, glassThickness, 1), 5, 8, 100, "Placeable_BoxAquarium_Large", 8003, "Large Box Aquarium (5x5x4)@A large-sized aquarium for holding a lot of fish", "BoxAquarium", FishSize.Large),
            new AquariumPrefab(new BoxGeneration(3 * BlockCreator.BlockSize, 3 * BlockCreator.BlockSize, 2 * BlockCreator.HalfFloorHeight, glassThickness, 0.8f), 5, 4, 40, "Placeable_BoxAquarium_Medium", 8004, "Medium Box Aquarium (3x3x2)@A medium-sized aquarium for holding some fish", "BoxAquarium", FishSize.Large),
            new AquariumPrefab(new BoxGeneration(1 * BlockCreator.BlockSize, 1 * BlockCreator.BlockSize, 1 * BlockCreator.HalfFloorHeight, glassThickness, 0.3f), 5, 2, 10, "Placeable_BoxAquarium_Small", 8005, "Small Box Aquarium (1x1x1)@A small-sized aquarium for holding a few fish", "BoxAquarium", FishSize.Small),
            new AquariumPrefab(new OpenBoxGeneration(0.5f, 0.5f, 0.25f, glassThickness, 0.15f, false), 1, 1, 0.5f, "Placeable_BoxAquarium_Tiny", 8006, "Fish Bowl@A tiny aquarium for holding a one tiny fish", "BoxAquarium", FishSize.Tiny),
            new AquariumPrefab(new BowlGeneration(16, 0.25f, 0.25f, glassThickness, 0.15f, false), 1, 1, 0.5f, "Placeable_BowlAquarium_Tiny", 8007, "Fish Bowl@A tiny aquarium for holding a one tiny fish", "CylinderAquarium", FishSize.Tiny)
        };
        static ModData entry;
        public override bool CanUnload(ref string message)
        {
            if (SceneManager.GetActiveScene().name == Raft_Network.GameSceneName && ComponentManager<Raft_Network>.Value.remoteUsers.Count > 1)
            {
                message = "Mod cannot be unloaded on while in a multiplayer";
                return false;
            }
            return base.CanUnload(ref message);
        }
        public static readonly int boneCount = 10;
        public static Transform prefabHolder;
        public static RectTransform rectPrefabHolder;
        public static Dictionary<string,Mesh> VanillaModels = new Dictionary<string, Mesh>();
        public void Awake()
        {
            if (SceneManager.GetActiveScene().name == Raft_Network.GameSceneName && ComponentManager<Raft_Network>.Value.remoteUsers.Count > 1)
                throw new ModLoadException("Mod cannot be loaded on while in a multiplayer");
        }
        class ModLoadException : Exception { public ModLoadException(string message) : base(message) { } }
        public IEnumerator Start()
        {
            instance = this;
            entry = modlistEntry;
            var loadingNote = HNotify.instance.AddNotification(HNotify.NotificationType.spinning, $"Loading [{name}]");
            IEnumerator AutoClose()
            {
                while (instance && loadingNote)
                    yield return null;
                if (loadingNote)
                    loadingNote.Close();
                yield break;
            }
            loadingNote.StartCoroutine(AutoClose());
            prefabHolder = new GameObject("Prefab Holder").transform;
            prefabHolder.gameObject.SetActive(false);
            DontDestroyOnLoad(prefabHolder.gameObject);
            created.Add(prefabHolder.gameObject);
            rectPrefabHolder = new GameObject("Rect Prefab Holder").AddComponent<RectTransform>();
            rectPrefabHolder.SetParent(prefabHolder, false);
            language = new LanguageSourceData()
            {
                mDictionary = new Dictionary<string, TermData>() {
                    ["CraftingSub/BoxAquariums"] = new TermData() { Languages = new[] { "Box Aquariums" } },
                    ["CraftingSub/CylinderAquariums"] = new TermData() { Languages = new[] { "Cylinder Aquariums" } },
                    ["CraftingSub/BowlAquariums"] = new TermData() { Languages = new[] { "Bowl Aquariums" } }
                },
                mLanguages = new List<LanguageData> { new LanguageData() { Code = "en", Name = "English" } }
            };
            LocalizationManager.Sources.Add(language);

            var bundleRequest = AssetBundle.LoadFromMemoryAsync(modlistEntry.modinfo.modFiles["aquariummaterials"]);
            yield return bundleRequest;
            var bundle = bundleRequest.assetBundle;
            created.Add(bundle);
            var glassMaterial = bundle.LoadAsset<Material>("glass");
            created.Add(glassMaterial);
            var waterMaterial = bundle.LoadAsset<Material>("waterSubsurface");
            created.Add(waterMaterial);
            bundleRequest = AssetBundle.LoadFromMemoryAsync(modlistEntry.modinfo.modFiles["vanillamodels"]);
            yield return bundleRequest;
            bundle = bundleRequest.assetBundle;
            created.Add(bundle);
            foreach (var n in bundle.LoadAllAssets<Mesh>())
            {
                void Rotate(Quaternion r)
                {
                    var v = n.vertices;
                    for (int i = 0; i < v.Length; i++)
                        v[i] = r * v[i];
                    n.vertices = v;
                }
                if (n.name == "AnglerFish")
                    Rotate(Quaternion.Euler(90, 0, 0));
                if (n.name == "Whale" || n.name == "Stingray" || n.name == "Dolphin")
                    Rotate(Quaternion.Euler(180, 0, 0));
                created.Add(VanillaModels[n.name] = n);
            }
            TrySetupPrefabs();
            SceneManager.sceneLoaded += OnSceneLoaded;
            {
                var cookerItem = ItemManager.GetItemByName("Placeable_CookingStand_Purifier_One");
                var boxItem = ItemManager.GetItemByName("Placeable_Storage_Small");
                var floorItem = ItemManager.GetItemByName("Block_Floor_Wood");
                var wallItem = ItemManager.GetItemByName("Block_Wall_Wood");
                var floorTemplate = floorItem.settings_buildable.GetBlockPrefab(0).colliderPrefab.GetComponentsInChildren<BlockQuad>(true).First(x => x.AcceptsDpsType(DPS.Floor));
                var wallTemplate = wallItem.settings_buildable.GetBlockPrefab(0).colliderPrefab.GetComponentsInChildren<BlockQuad>(true).First(x => x.AcceptsDpsType(DPS.Wall));
                var ignores = new[] { boxItem, floorItem, ItemManager.GetItemByName("Block_Pillar_Wood"), wallItem };
                var boxPrefab = boxItem.settings_buildable.GetBlockPrefab(DPS.Floor) ?? boxItem.settings_buildable.GetBlockPrefab(DPS.Default);
                var inventoryPrefab = (boxPrefab as Storage_Small).inventoryPrefab;
                var newInventory = inventoryPrefab.InstantiateAsPrefab().ReplaceComponent<AquariumInventory>();
                newInventory.transform.localPosition = inventoryPrefab.transform.localPosition;
                newInventory.transform.localScale = inventoryPrefab.transform.localScale;
                foreach (var s in newInventory.GetComponentsInChildren<Slot>(true))
                {
                    if (!newInventory.SlotContainer)
                        newInventory.SlotContainer = s.transform.parent;
                    DestroyImmediate(s.gameObject);
                }
                var invRect = newInventory.GetComponent<RectTransform>();
                var gridLayout = newInventory.SlotContainer.GetComponent<GridLayoutGroup>();
                newInventory.Initial = new Vector2(invRect.offsetMax.x - (gridLayout.cellSize.x + gridLayout.spacing.x) * 4, invRect.sizeDelta.y - (gridLayout.cellSize.y + gridLayout.spacing.y) * 2);
                foreach (var prefab in aquariumSettings)
                {
                    var newItem = boxItem.Clone(prefab.UniqueIndex, prefab.UniqueName);
                    newItem.name = newItem.UniqueName;
                    newItem.settings_Inventory.LocalizationTerm = "Item/" + newItem.UniqueName;
                    newItem.SetRecipe(prefab.Generation.GenerateRecipe(), boxItem.settings_recipe.CraftingCategory, 1, subCategory: prefab.IconName + "s");
                    language.mDictionary[newItem.settings_Inventory.LocalizationTerm] = new TermData() { Languages = new[] { prefab.DisplayName + $"\nMax Fish Size: {prefab.MaxSize}\nTotal Capacity: {prefab.FishVolume}" } };
                    newItem.settings_Inventory.Sprite = GetItemIcon(prefab.IconName);
                    var newBlock = boxPrefab.InstantiateAsPrefab().ReplaceComponent<AquariumBlock>();
                    newBlock.name = newItem.UniqueName;
                    DestroyImmediate(newBlock.GetComponent<Animator>());
                    DestroyImmediate(newBlock.GetComponent<Collider>());
                    DestroyImmediate(newBlock.GetComponent<AnimatorMessageForwarder>());
                    foreach (var r in newBlock.GetComponentsInChildren<Renderer>(true))
                        DestroyImmediate(r.gameObject);
                    newBlock.SetStorageFillers(null);
                    newBlock.ReplaceValues(boxItem, newItem);
                    var model = new GameObject("_model");
                    model.transform.SetParent(newBlock.transform, false);
                    model.AddComponent<MeshFilter>().sharedMesh = prefab.Generation.GenerateMesh(newItem.UniqueName);
                    model.AddComponent<MeshRenderer>().sharedMaterials = new[] { glassMaterial, waterMaterial };
                    newBlock.Rules = prefab.Generation.GenerateMovement();
                    newBlock.inventoryPrefab = newInventory;
                    newBlock.InventoryWidth = prefab.InventoryWidth;
                    newBlock.InventoryHeight = prefab.InventoryHeight;
                    newBlock.FishVolume = prefab.FishVolume;
                    newBlock.MaxSize = prefab.MaxSize;
                    if (prefab.Generation.Snap)
                    {
                        newBlock.canRotateFreely = false;
                        newBlock.snapsToQuads = true;
                        newBlock.blockCollisionMask = floorItem.settings_buildable.GetBlockPrefab(0).blockCollisionMask;
                    }
                    var colliders = prefab.Generation.GenerateColliders(newBlock);
                    newBlock.blockColliders = colliders.Where(x => x is BoxCollider).Cast<BoxCollider>().ToArray();
                    newBlock.onoffColliders = colliders;
                    if (prefab.Generation.Snap)
                    {
                        newBlock.colliderPrefab = CreateEmptyPrefab();
                        newBlock.colliderPrefab.name = "ColliderPrefab";
                        prefab.Generation.PopulateQuadPrefab(newBlock.colliderPrefab.transform, floorTemplate, wallTemplate);
                    }
                    var interactable = newBlock.GetComponent<RaycastInteractable>();
                    foreach (var c in colliders)
                    {
                        if (!c.GetComponent<RaycastInteractable>())
                            c.gameObject.AddComponent<RaycastInteractable_Redirect>().SetRaycastInteractableTarget(interactable);
                        if (!c.GetComponent<AdvancedCollision>())
                            c.gameObject.AddComponent<AdvancedCollision>();
                    }
                    newItem.settings_buildable.SetBlockPrefabs(new[] { newBlock });
                    foreach (var q in Resources.FindObjectsOfTypeAll<SO_BlockQuadType>())
                        if (q.AcceptsBlock(cookerItem))
                            Traverse.Create(q).Field("acceptableBlockTypes").GetValue<List<Item_Base>>().Add(newItem);
                    foreach (var q in Resources.FindObjectsOfTypeAll<SO_BlockCollisionMask>())
                        if (prefab.Generation.Snap ? ignores.Any(x => q.IgnoresBlock(x)) : q.IgnoresBlock(boxItem))
                            Traverse.Create(q).Field("blockTypesToIgnore").GetValue<List<Item_Base>>().Add(newItem);
                    RAPI.RegisterItem(newItem);
                }
            }
            var plankItem = ItemManager.GetItemByName("Plank");
            foreach (var item in itemSettings)
            {
                var newItem = plankItem.Clone(item.UniqueIndex, item.UniqueName);
                newItem.name = newItem.UniqueName;
                newItem.settings_Inventory.LocalizationTerm = "Item/" + newItem.UniqueName;
                language.mDictionary[newItem.settings_Inventory.LocalizationTerm] = new TermData() { Languages = new[] { item.DisplayName } };
                newItem.settings_Inventory.Sprite = GetItemIcon(item.IconName);
                item.Item = newItem;
                item.AdditionalSetup?.Invoke(newItem);
                RAPI.RegisterItem(newItem);
            }
            InsertFishingOptions();

            (harmony = new Harmony("com.aidanamite.AquariumMod")).PatchAll();
            Traverse.Create(typeof(LocalizationManager)).Field("OnLocalizeEvent").GetValue<LocalizationManager.OnLocalizeCallback>().Invoke();
            Log("Mod has been loaded!");
            loadingNote.Close();
            yield break;
        }

        public void OnModUnload()
        {
            RemoveFishingOptions();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            harmony?.UnpatchAll(harmony.Id);
            LocalizationManager.Sources.Remove(language);
            var remove = new HashSet<int>();
            foreach (var o in created)
                if (o is Item_Base i)
                    remove.Add(i.UniqueIndex);
            foreach (var q in Resources.FindObjectsOfTypeAll<SO_BlockQuadType>())
                Traverse.Create(q).Field("acceptableBlockTypes").GetValue<List<Item_Base>>().RemoveAll(x => remove.Contains(x.UniqueIndex));
            foreach (var q in Resources.FindObjectsOfTypeAll<SO_BlockCollisionMask>())
                Traverse.Create(q).Field("blockTypesToIgnore").GetValue<List<Item_Base>>().RemoveAll(x => remove.Contains(x.UniqueIndex));
            ItemManager.GetAllItems().RemoveAll(x => remove.Contains(x.UniqueIndex));
            foreach (var b in BlockCreator.GetPlacedBlocks())
                if (b.buildableItem != null && remove.Contains(b.buildableItem.UniqueIndex))
                    BlockCreator.RemoveBlock(b, null, true);
            foreach (var o in created)
                if (o)
                try
                {
                    if (o is AssetBundle b)
                        b.Unload(false);
                    else
                        Destroy(o);
                }
                catch (Exception e) { Debug.LogError(e); }
            created.Clear();
            Log("Mod has been unloaded!");
        }

        public void TrySetupPrefabs(string sceneName = null)
        {
            var renderers = new CachedPredicate<Renderer[]>(Resources.FindObjectsOfTypeAll<Renderer>);
            var meshes = new CachedPredicate<Mesh[]>(Resources.FindObjectsOfTypeAll<Mesh>);
            var materials = new CachedPredicate<Material[]>(Resources.FindObjectsOfTypeAll<Material>);
            Transform GetModelParent(string itemname) => ItemManager.GetItemByName(itemname).settings_buildable.GetBlockPrefab(0).transform.Find("Models");
            var search = new[] { GetModelParent("Placeable_TrophyBoard_Small"), GetModelParent("Placeable_TrophyBoard_Medium") };
            foreach (var fish in fishPrefabs.Values)
                if (!fish.prefab && (sceneName == null || sceneName == fish.initOnScene))
                    try
                    {
                        (Mesh mesh, Material[] materials)? model = null;
                        if (fish.getModel != null)
                            model = fish.getModel();
                        if (model == null)
                            foreach (var t in search)
                            {
                                if (fish.modelPredicate != null)
                                    foreach (Transform child in t)
                                        if (child.GetComponent<Renderer>() && fish.modelPredicate(child.GetComponent<Renderer>()))
                                        {
                                            model = GetModel(child);
                                            break;
                                        }
                                if (model == null && fish.modelName != null)
                                    model = GetModel(t.Find(fish.modelName));
                                if (model != null)
                                    break;
                            }
                        if (model == null && (fish.modelPredicate != null || fish.modelName != null))
                            model = GetModel(renderers.Value.FirstOrDefault(x => (fish.modelPredicate?.Invoke(x) ?? false) || (fish.modelName != null && fish.modelName == x.name)));
                        if (model == null && fish.modelPredicates != null && fish.modelPredicates.Value.mesh != null && fish.modelPredicates.Value.material != null)
                        {
                            var mesh = meshes.Value.FirstOrDefault(x => fish.modelPredicates.Value.mesh(x));
                            if (mesh)
                            {
                                var mat = materials.Value.FirstOrDefault(x => fish.modelPredicates.Value.material(x));
                                if (mat)
                                    model = (mesh, new[] { mat });
                            }
                        }
                        if (model != null && model.Value.mesh)
                        {
                            try
                            {
                                var myObj = new GameObject(fish.DisplayName);
                                myObj.transform.SetParent(prefabHolder, false);
                                var mesh = Instantiate(model.Value.mesh);
                                created.Add(mesh);
                                mesh.name = fish.DisplayName + " Mesh";
                                var skin = myObj.AddComponent<SkinnedMeshRenderer>();
                                var min = float.PositiveInfinity;
                                var max = float.NegativeInfinity;
                                foreach (var v in mesh.vertices)
                                {
                                    min = Math.Min(min, v.z);
                                    max = Math.Max(max, v.z);
                                }
                                var animator = myObj.AddComponent<FishAnimator>();
                                fish.prefab = animator;
                                var cur = myObj.transform;
                                animator.StepLength = Mathf.Abs((max - min) / boneCount) * fish.scale;
                                for (int i = 0; i <= boneCount; i++)
                                {
                                    animator.Bones[i] = new GameObject("Bone" + i).transform;
                                    animator.Bones[i].SetParent(cur, false);
                                    cur = animator.Bones[i];
                                    if (i != 0)
                                        cur.localPosition = new Vector3(0, 0, -animator.StepLength);
                                }
                                skin.rootBone = myObj.transform;
                                skin.bones = animator.Bones;
                                var verts = mesh.vertices;
                                var weights = new BoneWeight[verts.Length];
                                for (int i = 0; i < verts.Length; i++)
                                {
                                    var pos = (max - verts[i].z) * fish.scale / animator.StepLength;
                                    var index = (int)pos;
                                    var offset = pos % 1;
                                    var w = new BoneWeight();
                                    w.boneIndex0 = index;
                                    w.weight0 = 1 - offset;
                                    w.boneIndex1 = (index + 1) % animator.Bones.Length;
                                    w.weight1 = offset;
                                    w.boneIndex2 = (index + 2) % animator.Bones.Length;
                                    w.boneIndex3 = (index + 3) % animator.Bones.Length;
                                    weights[i] = w;
                                    verts[i] = (verts[i] - new Vector3(0, 0, max)) * fish.scale;
                                }
                                mesh.boneWeights = weights;
                                mesh.vertices = verts;
                                mesh.RecalculateBounds();
                                var binds = new Matrix4x4[animator.Bones.Length];
                                for (int i = 0; i < binds.Length; i++)
                                    binds[i] = animator.Bones[i].worldToLocalMatrix * skin.rootBone.localToWorldMatrix;
                                mesh.bindposes = binds;
                                skin.sharedMesh = mesh;
                                skin.sharedMaterials = model.Value.materials;
                                animator.Size = fish.size == FishSize.Large ? 0.2f : fish.size == FishSize.Tiny ? 0.05f : 0.1f;
                                if (fish.BendLengthOverride != null)
                                    animator.BendLength = fish.BendLengthOverride.Value;
                                if (fish.HeadLengthOverride != null)
                                    animator.HeadLength = fish.HeadLengthOverride.Value;
                                if (fish.WiggleAmplitudeOverride != null)
                                    animator.WiggleAmplitude = fish.WiggleAmplitudeOverride.Value;
                                if (fish.WiggleSpeedOverride != null)
                                    animator.WiggleSpeed = fish.WiggleSpeedOverride.Value;
                                if (fish.WiggleVolumeOverride != null)
                                    animator.WiggleVolume = fish.WiggleVolumeOverride.Value;
                                if (fish.WiggleXOverride != null)
                                    animator.WiggleX = fish.WiggleXOverride.Value;
                                if (fish.WiggleYOverride != null)
                                    animator.WiggleY = fish.WiggleYOverride.Value;
                            }
                            catch (Exception e)
                            {
                                Debug.LogError($"An error occured while setting up model for {fish.DisplayName} >> {e}");
                            }
                        }
                        else if (fish.initOnScene == null || fish.initOnScene == sceneName)
                            Debug.LogWarning($"Failed to find model to generate prefab for {fish.DisplayName}");
                    }
                    catch (Exception e)
                    {
                        if (fish.initOnScene == null || fish.initOnScene == sceneName)
                            Debug.LogError($"An error occured while setting up model for {fish.DisplayName} >> {e}");
                    }
        }

        public void InsertFishingOptions()
        {
            var helper = ComponentManager<CanvasHelper>.Value;
            if (!helper) return;
            var menu = helper.GetMenu(MenuType.FishingBait);
            if (menu == null || menu.menuObjects.Count == 0) return;
            RemoveFishingOptions();
            var obj = menu.menuObjects[0].transform.GetComponent<RectTransform>();
            var group = menu.menuObjects[0].GetComponentInChildren<HorizontalLayoutGroup>(true);
            var newOption = Instantiate(group.transform.GetChild(1), group.transform);
            newOption.name = "UI_FishingBait_Item_Decor";
            var ui = newOption.GetComponent<UI_Cost_Interactable_FishingBait>();
            ui.baitToEquip = itemSettings.First(x => x.UniqueName == "FishingBait_Decor").Item;
            ui.Refresh(new Cost (ui.baitToEquip,1));
            obj.sizeDelta += new Vector2(group.spacing + newOption.GetComponent<RectTransform>().rect.width, 0);
            group.GetComponent<RectTransform>().sizeDelta += new Vector2(group.spacing + newOption.GetComponent<RectTransform>().rect.width, 0);
        }

        public void RemoveFishingOptions()
        {
            var helper = ComponentManager<CanvasHelper>.Value;
            if (!helper) return;
            var menu = helper.GetMenu(MenuType.FishingBait);
            if (menu == null || menu.menuObjects.Count == 0) return;
            var obj = menu.menuObjects[0].transform.GetComponent<RectTransform>();
            var group = menu.menuObjects[0].GetComponentInChildren<HorizontalLayoutGroup>(true);
            var c = 0;
            var s = 0f;
            foreach (var t in group.GetComponentsInChildren<UI_Cost_Interactable_FishingBait>(true))
                if (t.baitToEquip?.UniqueName == "FishingBait_Decor")
                {
                    s += t.GetComponent<RectTransform>().rect.width;
                    Destroy(t.gameObject);
                    c++;
                }
            if (c > 0)
            {
                obj.sizeDelta -= new Vector2(group.spacing * c + s, 0);
                group.GetComponent<RectTransform>().sizeDelta -= new Vector2(group.spacing * c + s, 0);
            }
        }

        Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>();
        public Sprite GetItemIcon(string image) => icons.TryGetValue(image, out var s) ? s : icons[image] = LoadImage(image + ".png", true).ToSprite();
        public Texture2D LoadImage(string filename, bool removeMipMaps = false, FilterMode? mode = null, bool leaveReadable = true)
        {
            var t = new Texture2D(0, 0);
            t.LoadImage(GetEmbeddedFileBytes(filename),!removeMipMaps && !leaveReadable);
            if (removeMipMaps)
            {
                var t2 = new Texture2D(t.width, t.height, t.format, false);
                t2.SetPixels(t.GetPixels(0));
                t2.Apply(false, !leaveReadable);
                Destroy(t);
                t = t2;
            }
            if (mode != null)
                t.filterMode = mode.Value;
            created.Add(t);
            return t;
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode mode) => TrySetupPrefabs(scene.name);

        static (Mesh, Material[])? GetModel(Component component)
        {
            if (!component)
                return null;
            var c = component.GetComponent<SkinnedMeshRenderer>();
            if (c && c.sharedMesh && c.sharedMaterials != null && c.sharedMaterials.Any(x => x))
                return (c.sharedMesh, c.sharedMaterials);
            var r = component.GetComponent<MeshRenderer>();
            var f = component.GetComponent<MeshFilter>();
            if (f && f.sharedMesh && r && r.sharedMaterials != null && r.sharedMaterials.Any(x => x))
                return (f.sharedMesh, r.sharedMaterials);
            return null;
        }

        public static GameObject CreateEmptyPrefab()
        {
            var g = new GameObject("Prefab");
            g.transform.SetParent(prefabHolder, false);
            return g;
        }

        static List<GameObject> spawnedDisplays = new List<GameObject>();
        [ConsoleCommand("SpawnDisplayFish", "Syntax: 'SpawnDisplayFish [fish name]'  Spawns a display model for one of the fish that can appear in an aquarium. Use without a name to get a list of names")]
        static string SpawnDisplayFish(string[] args)
        {
            if (args == null || args.Length < 1)
                return "Options:" + fishPrefabs.Values.Join(x => "\n - " + x.DisplayName + (x.prefab ? "" : " (not yet initialized)"), "");
            var n = args.Join(delimiter: " ");
            foreach (var i in fishPrefabs.Values)
                if (i.DisplayName.Equals(n,StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!i.prefab)
                        return i.DisplayName + " has not initialized yet. You may need to load into a world at least once before spawning this fish";
                    var g = Instantiate(i.prefab);
                    created.Add(g.gameObject);
                    spawnedDisplays.Add(g.gameObject);
                    g.transform.position = Camera.main.transform.position + Camera.main.transform.forward;
                    g.name = i.DisplayName + " Display Model";
                    return "Spawned " + i.DisplayName;
            }
            return "No fish called " + n;
        }

        [ConsoleCommand("ClearDisplayFish", "Clears all display models spawned using the SpawnDisplayFish command")]
        static string ClearDisplayFish(string[] args)
        {
            var i = 0;
            foreach (var g in spawnedDisplays)
                if (g) {
                    Destroy(g);
                    i++;
                }
            spawnedDisplays.Clear();
            return "Cleared " + i + " display models";
        }
    }
}