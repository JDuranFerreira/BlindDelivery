using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using BlindDelivery.Gameplay.Balance;

namespace BlindDelivery.Editor
{
    /// <summary>
    /// Phase 0 foundation setup: physics layers, the 2D collision matrix and the GameBalance asset.
    /// Idempotent, so it is safe to run again. Also runnable in batch mode via -executeMethod.
    /// </summary>
    public static class BlindDeliverySetup
    {
        const string DataFolder = "Assets/Data";
        const string BalanceAssetPath = DataFolder + "/GameBalance.asset";

        static readonly string[] GameLayers = { "Tiles", "Items", "Deliveryman", "Hazards", "WindZones", "Sensors" };

        // Render order: first entry is drawn first (furthest back).
        static readonly string[] GameSortingLayers = { "Background", "Middleground", "Foreground" };

        // Symmetric pairs that collide physically. Sensors collide with nothing: scans are raycasts,
        // which ignore the collision matrix. Hazards and wind zones are triggers, so they only pair with the deliveryman.
        static readonly (string, string)[] CollidingPairs =
        {
            ("Deliveryman", "Tiles"),
            ("Deliveryman", "Items"),
            ("Deliveryman", "Hazards"),
            ("Deliveryman", "WindZones"),
            ("Items", "Tiles"),
            ("Items", "Items"),
        };

        [MenuItem("Blind Delivery/Setup/Phase 0 Foundation")]
        public static void RunPhase0()
        {
            EnsureLayers();
            EnsureSortingLayers();
            EnsureCollisionMatrix();
            EnsureGameBalanceAsset();
            AssetDatabase.SaveAssets();
            Debug.Log("[BlindDeliverySetup] Phase 0 foundation applied.");
        }

        static void EnsureLayers()
        {
            Object tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
            SerializedObject tagManager = new SerializedObject(tagManagerAsset);
            SerializedProperty layers = tagManager.FindProperty("layers");

            for (int i = 0; i < GameLayers.Length; i++)
            {
                int index = 6 + i;
                SerializedProperty slot = layers.GetArrayElementAtIndex(index);
                if (string.IsNullOrEmpty(slot.stringValue))
                {
                    slot.stringValue = GameLayers[i];
                }
                else if (slot.stringValue != GameLayers[i])
                {
                    Debug.LogWarning($"[BlindDeliverySetup] Layer {index} is '{slot.stringValue}', expected '{GameLayers[i]}'. Left unchanged.");
                }
            }

            tagManager.ApplyModifiedProperties();
        }

        /// <summary>Adds the game's sorting layers in render order without disturbing existing ones.</summary>
        public static void EnsureSortingLayers()
        {
            Object tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
            SerializedObject tagManager = new SerializedObject(tagManagerAsset);
            SerializedProperty sortingLayers = tagManager.FindProperty("m_SortingLayers");
            if (sortingLayers == null)
            {
                Debug.LogError("[BlindDeliverySetup] Sorting layers property not found in TagManager.asset.");
                return;
            }

            int nextId = 0;
            for (int i = 0; i < sortingLayers.arraySize; i++)
            {
                SerializedProperty layer = sortingLayers.GetArrayElementAtIndex(i);
                string name = layer.FindPropertyRelative("name").stringValue;
                int id = layer.FindPropertyRelative("uniqueID").intValue;
                if (id >= nextId) nextId = id + 1;
            }

            foreach (string wanted in GameSortingLayers)
            {
                bool exists = false;
                for (int i = 0; i < sortingLayers.arraySize; i++)
                {
                    if (sortingLayers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == wanted)
                    {
                        exists = true;
                        break;
                    }
                }

                if (exists) continue;

                int index = sortingLayers.arraySize;
                sortingLayers.InsertArrayElementAtIndex(index);
                SerializedProperty layer = sortingLayers.GetArrayElementAtIndex(index);
                layer.FindPropertyRelative("name").stringValue = wanted;
                layer.FindPropertyRelative("uniqueID").intValue = nextId++;
                layer.FindPropertyRelative("locked").boolValue = false;
            }

            tagManager.ApplyModifiedProperties();
        }

        static void EnsureCollisionMatrix()
        {
            int[] ids = GameLayers.Select(LayerMask.NameToLayer).ToArray();
            if (ids.Any(id => id < 0))
            {
                Debug.LogError("[BlindDeliverySetup] Game layers are missing. Run layer setup before the collision matrix.");
                return;
            }

            HashSet<(int, int)> allowed = new HashSet<(int, int)>();
            foreach ((string a, string b) in CollidingPairs)
            {
                int ia = LayerMask.NameToLayer(a);
                int ib = LayerMask.NameToLayer(b);
                allowed.Add((ia, ib));
                allowed.Add((ib, ia));
            }

            int managedMask = ids.Aggregate(0, (mask, id) => mask | (1 << id));
            foreach (int a in ids)
            {
                int mask = Physics2D.GetLayerCollisionMask(a) & ~managedMask;
                foreach (int b in ids)
                {
                    if (allowed.Contains((a, b)))
                    {
                        mask |= 1 << b;
                    }
                }

                Physics2D.SetLayerCollisionMask(a, mask);
            }
        }

        static void EnsureGameBalanceAsset()
        {
            if (AssetDatabase.LoadAssetAtPath<GameBalanceAsset>(BalanceAssetPath) != null)
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(DataFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Data");
            }

            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<GameBalanceAsset>(), BalanceAssetPath);
        }
    }
}
