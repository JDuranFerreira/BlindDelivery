using System.Collections.Generic;
using BlindDelivery.Core.Balance;
using BlindDelivery.Core.Items;
using BlindDelivery.Core.UI;
using BlindDelivery.Gameplay.Balance;
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
        [SerializeField] RectLevelGeometry geometry;
        [SerializeField] UnityEngine.Object pointerBlockerSource;
        [SerializeField] GameObject jumppadPrefab;
        [SerializeField] GameObject cardboxPrefab;
        [SerializeField] GameObject bridgePrefab;
        [SerializeField, Min(0)] int startingPoints = 100;

        readonly List<GuidanceItem> placedItems = new List<GuidanceItem>();
        readonly ItemCooldownTracker cooldowns = new ItemCooldownTracker();

        BlindDelivery.Core.Input.IInputService input;
        ItemBudget budget;
        IPointerBlocker pointerBlocker;
        GameBalance data;
        GuidanceItem ghost;
        GuidanceItem dragged;
        Vector3 dragOrigin;
        Vector3 dragOffset;
        int nextInstanceId;
        bool wasPointerDown;

        public IReadOnlyList<GuidanceItem> PlacedItems => placedItems;

        public ItemCooldownTracker Cooldowns => cooldowns;

        public ItemBudget Budget => budget;

        public ItemType? SelectedType { get; private set; }

        void Start()
        {
            if (balance == null) balance = FindAnyObjectByType<GameBalanceAsset>();
            if (geometry == null) geometry = FindAnyObjectByType<RectLevelGeometry>();
            data = balance != null ? balance.ToGameBalance() : new GameBalance();
            budget = new ItemBudget(startingPoints);
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

        void Update()
        {
            if (budget == null) return;
            cooldowns.Tick(Time.deltaTime);

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
            bool onFloor = geometry != null && geometry.Contains(bounds.center.x, bounds.min.y - 0.08f);
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
                bool landed = geometry != null && geometry.Contains(bounds.center.x, bounds.min.y - 0.08f);
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

        /// <summary>Toolbar entry point. Selecting the active tool again cancels the ghost.</summary>
        public void SelectType(int index)
        {
            ItemType type = index == 0 ? ItemType.Spring
                : index == 1 ? ItemType.Cushion
                : index == 2 ? ItemType.Ramp
                : ItemType.Ramp;
            SelectType(type);
        }

        public void SelectType(ItemType type)
        {
            if (type != ItemType.Spring && type != ItemType.Cushion && type != ItemType.Ramp) return;
            if (SelectedType == type)
            {
                CancelSelection();
                return;
            }

            CancelSelection();
            SelectedType = type;
            GameObject prefab = PrefabFor(type);
            if (prefab == null)
            {
                Debug.LogError($"[Items] No prefab assigned for {type}.", this);
                SelectedType = null;
                return;
            }

            Vector3 position = input != null && input.PointerPosition.HasValue
                ? PointerWorld(input.PointerPosition.Value)
                : transform.position;
            ghost = CreateItem(prefab, position);
            ghost.SetGhost(IsValidPlacement(ghost, position));
        }

        void HandleTap(CoreVector2 screen)
        {
            if (pointerBlocker != null && pointerBlocker.BlocksWorldInput) return;
            if (ghost == null) return;

            Vector3 position = PointerWorld(screen);
            ghost.transform.position = position;
            if (!IsValidPlacement(ghost, position))
            {
                ghost.SetGhost(false);
                return;
            }

            ghost.Commit();
            placedItems.Add(ghost);
            cooldowns.Start(ghost.InstanceId, CooldownFor(ghost.Type));
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
            if (valid) cooldowns.Start(dragged.InstanceId, CooldownFor(dragged.Type));
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

        GuidanceItem CreateItem(GameObject prefab, Vector3 position)
        {
            GameObject instance = Instantiate(prefab, position, Quaternion.identity, transform);
            instance.name = prefab.name + " (Placed)";
            GuidanceItem item = instance.GetComponent<GuidanceItem>();
            if (item == null)
            {
                Debug.LogError($"[Items] Prefab '{prefab.name}' has no GuidanceItem component.", this);
                Destroy(instance);
                return null;
            }

            item.AssignId(++nextInstanceId);
            return item;
        }

        bool IsValidPlacement(GuidanceItem item, Vector3 position)
        {
            if (item == null) return false;
            item.transform.position = position;

            Bounds bounds = item.WorldBounds;
            Bounds level = geometry != null ? geometry.LevelBounds : new Bounds(Vector3.zero, new Vector3(20f, 20f, 1f));
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

        GameObject PrefabFor(ItemType type)
        {
            switch (type)
            {
                case ItemType.Spring: return jumppadPrefab;
                case ItemType.Cushion: return cardboxPrefab;
                case ItemType.Ramp: return bridgePrefab;
                default: return null;
            }
        }

        int CostFor(ItemType type) => data != null && data.ItemMoveCosts.TryGetValue(type, out int cost) ? cost : 0;

        float CooldownFor(ItemType type) => data != null && data.ItemCooldownSeconds.TryGetValue(type, out float seconds) ? seconds : 0f;
    }
}
