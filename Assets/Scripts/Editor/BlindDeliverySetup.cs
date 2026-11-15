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

        static readonly string[] GameLayers = { "Tiles", "Items", "Boy", "Hazards", "WindZones", "Sensors" };

        // Symmetric pairs that collide physically. Sensors collide with nothing: scans are raycasts,
        // which ignore the collision matrix. Hazards and wind zones are triggers, so they only pair with the boy.
        static readonly (string, string)[] CollidingPairs =
        {
            ("Boy", "Tiles"),
            ("Boy", "Items"),
            ("Boy", "Hazards"),
            ("Boy", "WindZones"),
            ("Items", "Tiles"),
            ("Items", "Items"),
        };

        [MenuItem("Blind Delivery/Setup/Phase 0 Foundation")]
        public static void RunPhase0()
        {
            EnsureLayers();
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
