using System;
using System.Collections.Generic;
using BlindDelivery.Core.Balance;
using BlindDelivery.Core.Levels;
using BlindDelivery.Core.Items;
using BlindDelivery.Core.UI;
using BlindDelivery.Gameplay.Balance;
using BlindDelivery.Gameplay.Levels;
using BlindDelivery.Gameplay.Movement;
using UnityEngine;
using CoreVector2 = BlindDelivery.Core.Input.Vector2;

namespace BlindDelivery.Gameplay.Items
{
    /// <summary>
    /// Places, previews and drags the jumppad (spring), cardbox (cushion) and bridge (ramp).
    /// Uses the IInputService seam: select a tool, a ghost follows the pointer, tap to confirm,
    /// hold to cancel or to pick up a placed item, release to drop it. First placements are free,
    /// moves drain the point pool, and every change starts that item's cooldown (GDD §4/§5).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ItemPlacementController : MonoBehaviour
    {
        [SerializeField] GameBalanceAsset balance;
        [SerializeField] LevelData levelData;
        [SerializeField] Level level;
        [SerializeField] RectLevelGeometry geometry;
        [SerializeField] UnityEngine.Object pointerBlockerSource;
        [SerializeField] GameObject jumppadPrefab;
        [SerializeField] GameObject cardboxPrefab;
        [SerializeField] GameObject bridgePrefab;
        [SerializeField, Min(0)] int startingPoints = 100;

        readonly List<GuidanceItem> placedItems = new List<GuidanceItem>();
        readonly ItemCooldownTracker cooldowns = new ItemCooldownTracker();

        ItemFactory factory;

        const float GroundProbeOffset = 0.08f;

        BlindDelivery.Core.Input.IInputService input;
        ItemBudget budget;
        IPointerBlocker pointerBlocker;
        GameBalance data;
        LevelConfig levelConfig;
        GuidanceItem ghost;
        GuidanceItem dragged;
        Vector3 dragOrigin;
        Vector3 dragOffset;
        int nextInstanceId;
        bool wasPointerDown;

        public IReadOnlyList<GuidanceItem> PlacedItems => placedItems;

        public ItemCooldownTracker Cooldowns => cooldowns;

        public ItemBudget Budget => budget;

        /// <summary>Tool slots the factory can actually build — never a hardcoded list.</summary>
        public IReadOnlyList<ItemType> SupportedTypes => factory != null ? factory.SupportedTypes : Array.Empty<ItemType>();

        public bool IsToolSupported(ItemType type) => factory != null && factory.CanCreate(type);

        public float CooldownSeconds(ItemType type) => CooldownFor(type);

        /// <summary>First step of every fixed step, before the sensory scan (Tech Plan §4.1).</summary>
        public void TickCooldowns(float deltaTime)
        {
            cooldowns.Tick(deltaTime);
            foreach (GuidanceItem item in placedItems)
            {
                float duration = CooldownFor(item.Type);
                float ratio = duration > 0f ? Mathf.Clamp01(cooldowns.Remaining(item.InstanceId) / duration) : 0f;
                item.SetCooldown(ratio);
            }
        }

        public ItemType? SelectedType { get; private set; }

        void Start()
        {
            if (balance == null) balance = FindAnyObjectByType<GameBalanceAsset>();
            if (geometry == null) geometry = FindAnyObjectByType<RectLevelGeometry>();
            data = balance != null ? balance.ToGameBalance() : new GameBalance();
            levelConfig = levelData != null ? levelData.ToLevelConfig() : null;
            if (levelConfig == null)
            {
                if (level == null) level = FindAnyObjectByType<Level>();
                if (level != null && level.Data != null) levelConfig = level.Data.ToLevelConfig();
            }

            budget = new ItemBudget(levelConfig != null ? levelConfig.StartingPoints : startingPoints);
            factory = levelData != null && levelData.Tools.Count > 0
                ? CreateFactory(levelData)
                : new ItemFactory(new[]
                {
                    new ItemFactoryEntry(ItemType.JumpPad, jumppadPrefab),
                    new ItemFactoryEntry(ItemType.Cardbox, cardboxPrefab),
                    new ItemFactoryEntry(ItemType.Bridge, bridgePrefab),
                });
            pointerBlocker = pointerBlockerSource as IPointerBlocker;
            if (pointerBlocker == null)
            {
                foreach (MonoBehaviour candidate in FindObjectsByType<MonoBehaviour>())
                {
                    if (candidate is IPointerBlocker found)
                    {
                        pointerBlocker = found;
                        break;
                    }
                }
            }

            RegisterSceneItems();

            if (AppBootstrap.Instance != null)
            {
                AppBootstrap.Instance.Services.TryGet<BlindDelivery.Core.Input.IInputService>(out input);
            }

            if (input != null)
            {
                input.OnTap += HandleTap;
                input.OnHold += HandleHold;
            }
            else
            {
                Debug.LogWarning("[Items] No IInputService available; item placement is inactive.", this);
            }
        }

        /// <summary>Builds the factory from authored item definitions — no type switch in code.</summary>
        static ItemFactory CreateFactory(LevelData data)
        {
            var entries = new List<ItemFactoryEntry>();
            foreach (ItemDefinitionAsset tool in data.Tools)
            {
                // Level fixtures (wind zones etc.) are not player tools and never enter the factory.
                if (tool != null && tool.Prefab != null && tool.Usage == ItemUsage.Guidance)
                    entries.Add(new ItemFactoryEntry(tool.Type, tool.Prefab));
            }

            return new ItemFactory(entries);
        }

        void Update()
        {
            if (budget == null) return;
            bool pointerDown = input != null && input.IsPointerDown;
            if (wasPointerDown && !pointerDown && dragged != null) EndDrag();
            wasPointerDown = pointerDown;
            if (input == null) return;

            if (ghost != null && input.PointerPosition.HasValue)
            {
                Vector3 position = PointerWorld(input.PointerPosition.Value);
                ghost.transform.position = position;
                ghost.SetGhost(IsValidPlacement(ghost, position));
            }
            else if (dragged != null && input.PointerPosition.HasValue)
            {
                Vector3 position = PointerWorld(input.PointerPosition.Value) + dragOffset;
                dragged.transform.position = position;
                dragged.SetGhost(IsValidPlacement(dragged, position));
            }
        }

        void OnDestroy()
        {
            if (input == null) return;
            input.OnTap -= HandleTap;
            input.OnHold -= HandleHold;
        }

        /// <summary>
        /// An item placed on the floor settles immediately; one placed in mid-air becomes a
        /// dynamic Core body and falls until it lands.
        /// </summary>
        void SettleOrDrop(GuidanceItem item)
        {
            if (item == null) return;
            Bounds bounds = item.WorldBounds;
            bool onFloor = geometry != null && geometry.Contains(bounds.center.x, bounds.min.y - GroundProbeOffset);
            if (onFloor)
            {
                item.MarkSettled();
                geometry?.Rebuild();
            }
            else
            {
                item.BeginDrop();
            }
        }

        /// <summary>Advanced by SimulationRunner each fixed step, after the physics world is stepped.</summary>
        public void StepItems(float deltaTime)
        {
            if (placedItems.Count == 0) return;
            bool rebuild = false;
            foreach (GuidanceItem item in placedItems)
            {
                if (!item.IsPlaced || item.IsSettled) continue;
                item.StepPhysics(deltaTime);
                if (item.IsSettled)
                {
                    rebuild = true;
                    continue;
                }

                Bounds bounds = item.WorldBounds;
                bool landed = geometry != null && geometry.Contains(bounds.center.x, bounds.min.y - GroundProbeOffset);
                bool belowLevel = geometry != null && bounds.max.y < geometry.LevelBounds.min.y - 3f;
                if (landed || belowLevel)
                {
                    item.MarkSettled();
                    rebuild = true;
                }
            }

            if (rebuild) geometry?.Rebuild();
        }

        /// <summary>
        /// Items authored in the level are already in the scene with their CoreBody2D enabled.
        /// Register them as placed so interactions can see them and level geometry includes them.
        /// </summary>
        void RegisterSceneItems()
        {
            GuidanceItem[] sceneItems = FindObjectsByType<GuidanceItem>();
            foreach (GuidanceItem item in sceneItems)
            {
                if (!item.StartsInLevel || placedItems.Contains(item)) continue;
                item.AssignId(++nextInstanceId);
                item.Commit();
                item.MarkSettled();
                placedItems.Add(item);
            }

            if (sceneItems.Length > 0) geometry?.Rebuild();
        }

        public void SelectType(ItemType type)
        {
            if (!IsToolSupported(type)) return;
            if (SelectedType == type)
            {
                CancelSelection();
                return;
            }

            CancelSelection();
            SelectedType = type;
            Vector3 position = input != null && input.PointerPosition.HasValue
                ? PointerWorld(input.PointerPosition.Value)
                : transform.position;
            try
            {
                ghost = factory.Create(type, position, transform, ++nextInstanceId);
            }
            catch (InvalidOperationException ex)
            {
                Debug.LogError("[Items] " + ex.Message, this);
                SelectedType = null;
                return;
            }

            ghost.SetGhost(IsValidPlacement(ghost, position));
        }

        void HandleTap(CoreVector2 screen)
        {
            if (pointerBlocker != null && pointerBlocker.BlocksWorldInput) return;
            if (ghost == null)
            {
                if (SelectedType.HasValue) CancelSelection();
                return;
            }

            Vector3 position = PointerWorld(screen);
            if (IsOutsideLevel(position))
            {
                // GDD §4: tapping outside the map cancels the pending placement.
                CancelSelection();
                return;
            }

            ghost.transform.position = position;
            if (!IsValidPlacement(ghost, position))
            {
                ghost.SetGhost(false);
                return;
            }

            ghost.Commit();
            placedItems.Add(ghost);
            cooldowns.Start(ghost.InstanceId, CooldownFor(ghost.Type));
            ghost.SetCooldown(1f);
            SettleOrDrop(ghost);
            ghost = null;
            SelectedType = null;
        }

        void HandleHold(CoreVector2 screen)
        {
            if (pointerBlocker != null && pointerBlocker.BlocksWorldInput) return;
            if (ghost != null || SelectedType.HasValue)
            {
                // Long press on the ghost cancels the pending placement (GDD §4).
                CancelSelection();
                return;
            }

            Vector3 position = PointerWorld(screen);
            foreach (GuidanceItem item in placedItems)
            {
                if (!item.IsPlaced || cooldowns.IsActive(item.InstanceId)) continue;
                Bounds bounds = item.WorldBounds;
                if (bounds.Contains(position))
                {
                    dragged = item;
                    dragOrigin = item.transform.position;
                    dragOffset = dragOrigin - position;
                    item.SetGhost(false);
                    geometry?.Rebuild();
                    return;
                }
            }
        }

        void EndDrag()
        {
            if (dragged == null) return;

            Vector3 position = dragged.transform.position;
            bool valid = IsValidPlacement(dragged, position);
            if (valid && !budget.TrySpend(CostFor(dragged.Type)))
            {
                valid = false;
            }

            if (!valid) dragged.transform.position = dragOrigin;
            dragged.Commit();
            if (valid)
            {
                cooldowns.Start(dragged.InstanceId, CooldownFor(dragged.Type));
                dragged.SetCooldown(1f);
            }
            SettleOrDrop(dragged);
            dragged = null;
        }

        void CancelSelection()
        {
            if (ghost != null)
            {
                Destroy(ghost.gameObject);
                ghost = null;
            }

            SelectedType = null;
        }

        /// <summary>Used when an item is consumed (e.g. a cardbox squashed on landing).</summary>
        public void RemoveItem(GuidanceItem item)
        {
            if (item == null || !placedItems.Remove(item)) return;
            cooldowns.Clear(item.InstanceId);
            geometry?.Rebuild();
        }

        /// <summary>
        /// A tap outside the map cancels placement. LevelBounds is only the union of solid floor
        /// shapes, so use the same padding as the placement validity check — otherwise every
        /// mid-air placement is treated as off-map and silently cancelled.
        /// </summary>
        bool IsOutsideLevel(Vector3 position)
        {
            Bounds level = geometry != null ? geometry.LevelBounds : RectLevelGeometry.DefaultLevelBounds;
            return position.x < level.min.x || position.x > level.max.x ||
                   position.y < level.min.y - 2f || position.y > level.max.y + 3f;
        }

        bool IsValidPlacement(GuidanceItem item, Vector3 position)
        {
            if (item == null) return false;
            item.transform.position = position;

            Bounds bounds = item.WorldBounds;
            Bounds level = geometry != null ? geometry.LevelBounds : RectLevelGeometry.DefaultLevelBounds;
            // Item pivots are bottom-centre, so the transform is not the item's centre. Compare the
            // sprite bounds instead, allowing items to rest on (or stack above) the level surface.
            if (bounds.center.x < level.min.x + 0.1f || bounds.center.x > level.max.x - 0.1f ||
                bounds.center.y < level.min.y - 2f || bounds.center.y > level.max.y + 3f)
            {
                return false;
            }

            foreach (float fx in new[] { 0.15f, 0.85f })
            {
                foreach (float fy in new[] { 0.15f, 0.85f })
                {
                    float x = Mathf.Lerp(bounds.min.x, bounds.max.x, fx);
                    float y = Mathf.Lerp(bounds.min.y, bounds.max.y, fy);
                    if (geometry != null && geometry.Contains(x, y)) return false;
                }
            }

            Bounds candidate = new Bounds(bounds.center, bounds.size - Vector3.one * 0.12f);
            foreach (GuidanceItem other in placedItems)
            {
                if (other == item || !other.IsPlaced) continue;
                Bounds occupied = other.WorldBounds;
                var shrunk = new Bounds(occupied.center, occupied.size - Vector3.one * 0.12f);
                if (candidate.Intersects(shrunk)) return false;
            }

            return true;
        }

        Vector3 PointerWorld(CoreVector2 screen)
        {
            Camera cam = worldCamera != null ? worldCamera : (worldCamera = Camera.main);
            if (cam == null) return transform.position;
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.X, screen.Y, cam.transform.position.z));
            world.z = 0f;
            return world;
        }

        Camera worldCamera;

        ItemDefinitionData DefinitionFor(ItemType type)
        {
            if (levelConfig == null) return null;
            foreach (ItemDefinitionData tool in levelConfig.GuidanceObjects)
            {
                if (tool != null && tool.Type == type) return tool;
            }

            return null;
        }

        int CostFor(ItemType type)
        {
            ItemDefinitionData definition = DefinitionFor(type);
            if (definition != null) return definition.MoveCost;
            return data != null && data.ItemMoveCosts.TryGetValue(type, out int cost) ? cost : 0;
        }

        float CooldownFor(ItemType type)
        {
            ItemDefinitionData definition = DefinitionFor(type);
            if (definition != null) return definition.CooldownSeconds;
            return data != null && data.ItemCooldownSeconds.TryGetValue(type, out float seconds) ? seconds : 0f;
        }
    }
}
