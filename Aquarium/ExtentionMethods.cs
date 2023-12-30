using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;
using Object = UnityEngine.Object;
using System.Reflection;

namespace Aquarium
{
    static class ExtentionMethods
    {
        public static void ShowData(this Material material)
        {
            var found = $" - \"{material.name}\" (shader name: \"{material.shader.name}\")";
            for (int i = 0; i < material.shader.GetPropertyCount(); i++)
            {
                string t = material.shader.GetPropertyType(i).ToString();
                var n = material.shader.GetPropertyName(i);
                string value = null;
                switch (material.shader.GetPropertyType(i))
                {
                    case UnityEngine.Rendering.ShaderPropertyType.Texture:
                        var b = material.GetTexture(n);
                        if (b == null)
                            t = "Unknown Texture";
                        else
                        {
                            t = b.GetType().FullName;
                            value = b.name;
                        }
                        break;
                    case UnityEngine.Rendering.ShaderPropertyType.Range:
                        Array a = material.GetColorArray(n);
                        if (a == null)
                            a = material.GetFloatArray(n);
                        if (a == null)
                            a = material.GetMatrixArray(n);
                        if (a == null)
                            a = material.GetVectorArray(n);
                        if (a == null)
                            t = "Unknown Range";
                        else
                        {
                            t = a.GetType().FullName;
                            value = a.GetValue(0).ToString();
                            for (int j = 1; j < a.Length; j++)
                                value += ", " + a.GetValue(j);
                        }
                        break;
                    case UnityEngine.Rendering.ShaderPropertyType.Vector:
                        var c = material.GetVector(n);
                        if (c == null)
                            t = "Unknown Vector";
                        else
                        {
                            t = c.GetType().FullName;
                            value = $"({c.x}, {c.y}, {c.z}, {c.w})";
                        }
                        break;
                    case UnityEngine.Rendering.ShaderPropertyType.Float:
                        value = material.GetFloat(n).ToString();
                        break;
                    case UnityEngine.Rendering.ShaderPropertyType.Color:
                        var v = material.GetColor(n);
                        value = $"({v.r}, {v.g}, {v.b}, {v.a})";
                        break;
                }
                found += $"\nProperty: {n} ({t})\nDescription: {material.shader.GetPropertyDescription(i)}" + (value == null ? "" : $"\nValue: {value}");
            }
            Debug.Log(found);
        }

        public static void SortedAdd<T>(this List<T> list, T value, IComparer<T> compare = null)
        {
            if (compare == null)
                compare = Comparer<T>.Default;
            if (list.Count == 0)
            {
                list.Add(value);
                return;
            }
            var p = list.BinarySearch(value, compare);
            list.Insert(p < 0 ? ~p : p, value);
        }

        public static Y Join<X, Y>(this IEnumerable<X> collection, Func<X, Y> getter, Func<Y, Y, Y> joiner, Predicate<X> condition = null)
        {
            if (condition == null)
                condition = x => true;
            var first = true;
            var current = default(Y);
            foreach (var item in collection)
                if (condition(item))
                {
                    if (first)
                    {
                        first = false;
                        current = getter(item);
                    }
                    else
                        current = joiner(current, getter(item));
                }
            return current;
        }

        public static Vector3 MoveTowards(this Vector3 current, Vector3 target, float maxDeltaDistance, out float distanceMoved)
        {
            var difference = target - current;
            var magnitude = difference.magnitude;
            if (magnitude <= maxDeltaDistance)
            {
                distanceMoved = magnitude;
                return target;
            }
            distanceMoved = maxDeltaDistance;
            return current + ((magnitude != 0 ? difference / magnitude : Vector3.one) * maxDeltaDistance);
        }
        public static float MoveTowards(this float current, float target, float maxDelta, out float moved)
        {
            var difference = target - current;
            var abs = Mathf.Abs(difference);
            if (abs <= maxDelta)
            {
                moved = difference;
                return target;
            }
            moved = maxDelta;
            return current + ((difference < 0 ? -1 : 1) * maxDelta);
        }
        public static float Even(this float value) => value - (value % 2);
        public static float Odd(this float value) => value.Even() + (value < 0 ? -1 : 1);
        public static Quaternion TransformRotation(this Transform t, Quaternion localRotation) => t ? t.rotation * localRotation : localRotation;
        public static Quaternion InverseTransformRotation(this Transform t, Quaternion globalRotation) => t ? Quaternion.Inverse(t.rotation) * globalRotation : globalRotation;
        public static Vector3 ToVector3(this float[] axes) => new Vector3(axes[0], axes[1], axes[2]);
        public static float[] ToArray(this Vector3 vector) => new[] { vector.x, vector.y, vector.z };

        public static Item_Base Clone(this Item_Base source, int uniqueIndex, string uniqueName)
        {
            Item_Base item = ScriptableObject.CreateInstance<Item_Base>();
            item.Initialize(uniqueIndex, uniqueName, source.MaxUses);
            item.settings_buildable = source.settings_buildable.Clone();
            item.settings_consumeable = source.settings_consumeable.Clone();
            item.settings_cookable = source.settings_cookable.Clone();
            item.settings_equipment = source.settings_equipment.Clone();
            item.settings_Inventory = source.settings_Inventory.Clone();
            item.settings_recipe = source.settings_recipe.Clone();
            item.settings_usable = source.settings_usable.Clone();
            Main.created.Add(item);
            return item;
        }
        public static void SetRecipe(this Item_Base item, CostMultiple[] cost, CraftingCategory category = CraftingCategory.Resources, int amountToCraft = 1, bool learnedFromBeginning = false, string subCategory = null, int subCatergoryOrder = 0)
        {
            Traverse recipe = Traverse.Create(item.settings_recipe);
            recipe.Field("craftingCategory").SetValue(category);
            recipe.Field("amountToCraft").SetValue(amountToCraft);
            recipe.Field("learnedFromBeginning").SetValue(learnedFromBeginning);
            recipe.Field("subCategory").SetValue(subCategory);
            recipe.Field("subCatergoryOrder").SetValue(subCatergoryOrder);
            item.settings_recipe.NewCost = cost;
        }

        public static Sprite ToSprite(this Texture2D texture, Rect? rect = null, Vector2? pivot = null)
        {
            var s = Sprite.Create(texture, rect ?? new Rect(0, 0, texture.width, texture.height), pivot ?? new Vector2(0.5f, 0.5f));
            Main.created.Add(s);
            return s;
        }

        public static void CopyFieldsOf(this object value, object source)
        {
            var t1 = value.GetType();
            var t2 = source.GetType();
            while (!t1.IsAssignableFrom(t2))
                t1 = t1.BaseType;
            while (t1 != typeof(Object) && t1 != typeof(object))
            {
                foreach (var f in t1.GetFields(~BindingFlags.Default))
                    if (!f.IsStatic)
                        f.SetValue(value, f.GetValue(source));
                t1 = t1.BaseType;
            }
        }

        public static T ReplaceComponent<T>(this Component original, int serializationLayers = 0) where T : Component
        {
            var g = original.gameObject;
            var n = g.AddComponent<T>();
            n.CopyFieldsOf(original);
            g.ReplaceValues(original, n, serializationLayers);
            Object.DestroyImmediate(original);
            return n;
        }
        public static void ReplaceValues(this Component value, object original, object replacement, int serializableLayers = 0)
        {
            foreach (var c in value.GetComponentsInChildren<Component>(true))
                (c as object).ReplaceValues(original, replacement, serializableLayers);
        }
        public static void ReplaceValues(this GameObject value, object original, object replacement, int serializableLayers = 0)
        {
            foreach (var c in value.GetComponentsInChildren<Component>(true))
                (c as object).ReplaceValues(original, replacement, serializableLayers);
        }

        public static void ReplaceValues(this object value, object original, object replacement, int serializableLayers = 0)
        {
            if (value == null)
                return;
            var t = value.GetType();
            while (t != typeof(Object) && t != typeof(object))
            {
                foreach (var f in t.GetFields(~BindingFlags.Default))
                    if (!f.IsStatic)
                    {
                        if (f.GetValue(value) == original || (f.GetValue(value)?.Equals(original) ?? false))
                            try
                            {
                                f.SetValue(value, replacement);
                            }
                            catch { }
                        else if (f.GetValue(value) is IList)
                        {
                            var l = f.GetValue(value) as IList;
                            for (int i = 0; i < l.Count; i++)
                                if (l[i] == original || (l[i]?.Equals(original) ?? false))
                                    try
                                    {
                                        l[i] = replacement;
                                    }
                                    catch { }

                        }
                        else if (serializableLayers > 0 && (f.GetValue(value)?.GetType()?.IsSerializable ?? false))
                            f.GetValue(value).ReplaceValues(original, replacement, serializableLayers - 1);
                    }
                t = t.BaseType;
            }
        }

        public static GameObject InstantiateAsPrefab(this GameObject obj) => Object.Instantiate(obj, obj?.GetComponent<RectTransform>() ? Main.rectPrefabHolder : Main.prefabHolder, false);
        public static T InstantiateAsPrefab<T>(this T obj) where T : Component => Object.Instantiate(obj, obj?.GetComponent<RectTransform>() ? Main.rectPrefabHolder : Main.prefabHolder, false);

        public static Vector3 Max(this Vector3 self, Vector3 other) => new Vector3(Mathf.Max(self.x, other.x),Mathf.Max(self.y, other.y), Mathf.Max(self.z, other.z));
        public static Vector3 Min(this Vector3 self, Vector3 other) => new Vector3(Mathf.Min(self.x, other.x), Mathf.Min(self.y, other.y), Mathf.Min(self.z, other.z));

        public static void SetBlockPrefabs(this ItemInstance_Buildable buildable, Block[] prefabs) => Traverse.Create(buildable).Field("blockPrefabs").SetValue(prefabs);

        public static float GetVolume(this FishSize size) => size == FishSize.Tiny ? 0.5f : size == FishSize.Large ? 3 : 1;

        public static void SetStorageFillers(this Storage_Small storage, Storage_Small.StorageFill[] fillers) => Traverse.Create(storage).Field("storageFillers").SetValue(fillers);
        public static Storage_Small.StorageFill[] GetStorageFillers(this Storage_Small storage) => Traverse.Create(storage).Field("storageFillers").GetValue<Storage_Small.StorageFill[]>();

        public static T GetRandom<T>(this IEnumerable<T> collection, Predicate<T> predicate)
        {
            var l = new List<T>();
            foreach (var i in collection)
                if (predicate(i))
                    l.Add(i);
            return l.Count == 0 ? default : l[UnityEngine.Random.Range(0, l.Count)];
        }
    }
}
