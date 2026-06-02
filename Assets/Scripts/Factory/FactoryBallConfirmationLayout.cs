using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class FactoryBallConfirmationLayout : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform iconLayoutRoot;
    [SerializeField] private Image iconPrefab;
    [SerializeField] private GameObject unplacedMoldsText;

    [Header("Fallback")]
    [SerializeField] private BallTypeData defaultBallType;
    [SerializeField] private Sprite fallbackSprite;

    [Header("Behavior")]
    [SerializeField] private bool refreshOnEnable = true;
    [SerializeField] private bool allowDragReorder = true;
    [SerializeField, Min(1f)] private float dragReorderLerpSpeed = 18f;

    private readonly List<Image> iconPool = new List<Image>();
    private readonly Dictionary<Image, BallTypeData> craftedBallByIcon = new Dictionary<Image, BallTypeData>();
    private readonly HashSet<Image> craftedIconSet = new HashSet<Image>();
    private readonly List<IconDragHandle> dragSessionHandles = new List<IconDragHandle>();
    private readonly List<float> dragSessionSlotWorldXs = new List<float>();
    private readonly Dictionary<IconDragHandle, int> dragSessionCurrentSlots = new Dictionary<IconDragHandle, int>();
    private IconDragHandle fallbackDraggingHandle;
    private bool dragSessionActive;
    private IconDragHandle dragSessionPrimaryHandle;

    private void OnEnable()
    {
        if (InventoryManager.HasInstance)
        {
            InventoryManager.Instance.InventoryChanged += HandleInventoryChanged;
        }

        if (refreshOnEnable)
        {
            RefreshIcons();
        }
    }

    private void OnDisable()
    {
        if (InventoryManager.HasInstance)
        {
            InventoryManager.Instance.InventoryChanged -= HandleInventoryChanged;
        }
    }

    private void HandleInventoryChanged()
    {
        RefreshIcons();
    }

    private void Update()
    {
        HandleFallbackPointerDrag();
        TickDragSessionSmoothing();
    }

    public void RefreshIcons()
    {
        if (iconLayoutRoot == null || iconPrefab == null)
        {
            UpdateUnplacedMoldsTextVisibility();
            return;
        }

        craftedBallByIcon.Clear();
        craftedIconSet.Clear();

        int moldCount = FindObjectsByType<BallMoldBuilding>(FindObjectsSortMode.None).Length;

        EnsureIconPool(moldCount);
        for (int i = 0; i < iconPool.Count; i++)
        {
            iconPool[i].gameObject.SetActive(false);
        }

        if (moldCount <= 0)
        {
            return;
        }

        IReadOnlyList<BallTypeData> craftedBalls = InventoryManager.HasInstance
            ? InventoryManager.Instance.CraftedBalls
            : null;
        int craftedShown = craftedBalls != null ? Mathf.Min(craftedBalls.Count, moldCount) : 0;

        for (int i = 0; i < craftedShown; i++)
        {
            ApplyIcon(iconPool[i], craftedBalls[i], isCraftedBall: true);
        }

        for (int i = craftedShown; i < moldCount; i++)
        {
            ApplyIcon(iconPool[i], defaultBallType, isCraftedBall: false);
        }

        UpdateUnplacedMoldsTextVisibility();
    }

    private void UpdateUnplacedMoldsTextVisibility()
    {
        if (unplacedMoldsText == null)
        {
            return;
        }

        bool hasUnplacedMolds = false;
        if (InventoryManager.HasInstance)
        {
            IReadOnlyList<InventoryManager.InventoryEntry> buildingItems = InventoryManager.Instance.BuildingItems;
            if (buildingItems != null)
            {
                for (int i = 0; i < buildingItems.Count; i++)
                {
                    InventoryManager.InventoryEntry entry = buildingItems[i];
                    BuildingDefinition definition = entry != null ? entry.BuildingDefinition : null;
                    if (entry != null && entry.Quantity > 0 && IsBallMoldDefinition(definition))
                    {
                        hasUnplacedMolds = true;
                        break;
                    }
                }
            }
        }

        if (unplacedMoldsText.activeSelf != hasUnplacedMolds)
        {
            unplacedMoldsText.SetActive(hasUnplacedMolds);
        }
    }

    private static bool IsBallMoldDefinition(BuildingDefinition definition)
    {
        if (definition == null || definition.BehaviorPrefab == null)
        {
            return false;
        }

        return definition.BehaviorPrefab.GetComponent<BallMoldBuilding>() != null
            || definition.BehaviorPrefab.GetComponentInChildren<BallMoldBuilding>(true) != null;
    }

    private void EnsureIconPool(int count)
    {
        for (int i = iconPool.Count; i < count; i++)
        {
            Image spawnedIcon = Instantiate(iconPrefab, iconLayoutRoot);
            spawnedIcon.gameObject.name = "FactoryBallConfirmIcon_" + i;
            spawnedIcon.gameObject.SetActive(false);
            iconPool.Add(spawnedIcon);
        }
    }

    private void ApplyIcon(Image iconImage, BallTypeData ballType, bool isCraftedBall)
    {
        if (iconImage == null)
        {
            return;
        }

        iconImage.sprite = ResolveSprite(ballType);
        iconImage.color = Color.white;
        iconImage.gameObject.SetActive(iconImage.sprite != null);

        IconDragHandle dragHandle = EnsureDragHandle(iconImage);
        bool canDrag = allowDragReorder && ballType != null;
        dragHandle.Configure(this, iconImage, ballType, canDrag, BuildDragDisabledReason(iconImage, ballType));

        if (isCraftedBall && ballType != null)
        {
            craftedBallByIcon[iconImage] = ballType;
            craftedIconSet.Add(iconImage);
        }
        else
        {
            craftedIconSet.Remove(iconImage);
            craftedBallByIcon.Remove(iconImage);
        }

        TooltipTrigger tooltip = iconImage.GetComponent<TooltipTrigger>();
        if (tooltip != null)
        {
            tooltip.SetContent(
                ballType != null ? ballType.DisplayName : string.Empty,
                ballType != null ? ballType.Description : string.Empty);
        }
    }

    private Sprite ResolveSprite(BallTypeData ballType)
    {
        if (ballType != null && ballType.BallSprite != null)
        {
            return ballType.BallSprite;
        }

        if (defaultBallType != null && defaultBallType.BallSprite != null)
        {
            return defaultBallType.BallSprite;
        }

        return fallbackSprite;
    }

    private IconDragHandle EnsureDragHandle(Image iconImage)
    {
        IconDragHandle handle = iconImage.GetComponent<IconDragHandle>();
        if (handle == null)
        {
            handle = iconImage.gameObject.AddComponent<IconDragHandle>();
        }

        return handle;
    }

    private void HandleIconDragMoved(IconDragHandle dragHandle, PointerEventData eventData)
    {
        if (eventData == null)
        {
            return;
        }

        HandleIconDragMoved(dragHandle, eventData.position, eventData.pressEventCamera);
    }

    private void HandleIconDragMoved(IconDragHandle dragHandle, Vector2 pointerScreenPosition, Camera eventCamera)
    {
        if (dragHandle == null || iconLayoutRoot == null)
        {
            return;
        }

        RectTransform rootRect = iconLayoutRoot as RectTransform;
        if (rootRect == null)
        {
            return;
        }

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rootRect, pointerScreenPosition, eventCamera, out Vector3 worldPoint))
        {
            dragHandle.SetDraggedWorldX(worldPoint.x);
        }

        RepositionDraggedSiblingByPointer(dragHandle, pointerScreenPosition, eventCamera);
    }

    private void RepositionDraggedSiblingByPointer(IconDragHandle dragHandle, Vector2 pointerScreenPosition, Camera eventCamera)
    {
        if (dragHandle == null || iconLayoutRoot == null)
        {
            return;
        }

        RectTransform draggedRect = dragHandle.RectTransform;
        if (draggedRect == null)
        {
            return;
        }

        UpdateDragSessionTargets(dragHandle);
    }

    private void HandleIconDragEnded(IconDragHandle dragHandle)
    {
        if (!InventoryManager.HasInstance || dragHandle == null)
        {
            return;
        }

        List<BallTypeData> reordered = new List<BallTypeData>();
        for (int i = 0; i < iconLayoutRoot.childCount; i++)
        {
            Image childImage = iconLayoutRoot.GetChild(i).GetComponent<Image>();
            if (childImage != null
                && craftedIconSet.Contains(childImage)
                && craftedBallByIcon.TryGetValue(childImage, out BallTypeData ballType)
                && ballType != null)
            {
                reordered.Add(ballType);
            }
        }

        InventoryManager.Instance.SetCraftedBalls(reordered);
    }

    private void HandleFallbackPointerDrag()
    {
        if (!allowDragReorder)
        {
            return;
        }

        Mouse mouse = Mouse.current;
        Pointer pointer = Pointer.current;
        if (mouse == null || pointer == null)
        {
            return;
        }

        Vector2 pointerScreenPosition = pointer.position.ReadValue();
        Camera eventCamera = ResolveEventCamera();

        if (fallbackDraggingHandle == null)
        {
            if (!mouse.leftButton.wasPressedThisFrame)
            {
                return;
            }

            if (!TryGetDraggableHandleAtPointer(pointerScreenPosition, eventCamera, out IconDragHandle handle, out string blockedReason))
            {
                return;
            }

            fallbackDraggingHandle = handle;
            fallbackDraggingHandle.BeginDragFallback();
            return;
        }

        HandleIconDragMoved(fallbackDraggingHandle, pointerScreenPosition, eventCamera);

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            fallbackDraggingHandle.EndDragFallback();
            HandleIconDragEnded(fallbackDraggingHandle);
            fallbackDraggingHandle = null;
        }
    }

    private void BeginDragSession(IconDragHandle draggedHandle)
    {
        if (draggedHandle == null || iconLayoutRoot == null)
        {
            return;
        }

        if (dragSessionActive && dragSessionPrimaryHandle == draggedHandle)
        {
            return;
        }

        dragSessionActive = true;
        dragSessionPrimaryHandle = draggedHandle;
        dragSessionHandles.Clear();
        dragSessionSlotWorldXs.Clear();
        dragSessionCurrentSlots.Clear();

        for (int i = 0; i < iconPool.Count; i++)
        {
            Image iconImage = iconPool[i];
            if (iconImage == null || !iconImage.gameObject.activeInHierarchy)
            {
                continue;
            }

            IconDragHandle handle = iconImage.GetComponent<IconDragHandle>();
            if (handle == null || !handle.IsDraggable)
            {
                continue;
            }

            dragSessionHandles.Add(handle);
        }

        dragSessionHandles.Sort((a, b) => a.RectTransform.position.x.CompareTo(b.RectTransform.position.x));

        for (int i = 0; i < dragSessionHandles.Count; i++)
        {
            IconDragHandle handle = dragSessionHandles[i];
            dragSessionSlotWorldXs.Add(handle.RectTransform.position.x);
            handle.SetLockedWorldYFromCurrent();
            handle.SetLayoutIgnored(true);
            handle.SetSmoothTargetWorldX(handle.RectTransform.position.x);
            dragSessionCurrentSlots[handle] = i;
        }

        UpdateDragSessionTargets(draggedHandle);
    }

    private void EndDragSession(IconDragHandle draggedHandle)
    {
        if (!dragSessionActive)
        {
            return;
        }

        if (draggedHandle != null && draggedHandle.HasLastHoveredSlot)
        {
            Debug.Log($"[FactoryBallConfirmationLayout] Drag end commit: handle={draggedHandle.name}, lastHoveredSlot={draggedHandle.LastHoveredSlot}");
            dragSessionCurrentSlots[draggedHandle] = draggedHandle.LastHoveredSlot;
            CommitDragSessionSiblingOrder();
        }

        for (int i = 0; i < dragSessionHandles.Count; i++)
        {
            IconDragHandle handle = dragSessionHandles[i];
            if (handle == null)
            {
                continue;
            }

            if (!dragSessionCurrentSlots.TryGetValue(handle, out int slotIndex))
            {
                continue;
            }

            slotIndex = Mathf.Clamp(slotIndex, 0, dragSessionSlotWorldXs.Count - 1);
            handle.SnapToWorldX(dragSessionSlotWorldXs[slotIndex]);
        }

        for (int i = 0; i < dragSessionHandles.Count; i++)
        {
            dragSessionHandles[i].SetLayoutIgnored(false);
        }

        dragSessionHandles.Clear();
        dragSessionSlotWorldXs.Clear();
        dragSessionCurrentSlots.Clear();
        dragSessionPrimaryHandle = null;
        dragSessionActive = false;

        if (iconLayoutRoot is RectTransform rootRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
        }
    }

    private void UpdateDragSessionTargets(IconDragHandle draggedHandle)
    {
        if (!dragSessionActive || draggedHandle == null || dragSessionSlotWorldXs.Count == 0)
        {
            return;
        }

        if (!dragSessionCurrentSlots.TryGetValue(draggedHandle, out int draggedCurrentSlot))
        {
            return;
        }

        int targetSlot = FindClosestSlotIndex(draggedHandle.RectTransform.position.x);
        targetSlot = Mathf.Clamp(targetSlot, 0, dragSessionSlotWorldXs.Count - 1);
        draggedHandle.SetLastHoveredSlot(targetSlot);

        if (targetSlot != draggedCurrentSlot)
        {
            IconDragHandle occupant = null;
            for (int i = 0; i < dragSessionHandles.Count; i++)
            {
                IconDragHandle handle = dragSessionHandles[i];
                if (handle == null || handle == draggedHandle)
                {
                    continue;
                }

                if (dragSessionCurrentSlots.TryGetValue(handle, out int slotIndex) && slotIndex == targetSlot)
                {
                    occupant = handle;
                    break;
                }
            }

            dragSessionCurrentSlots[draggedHandle] = targetSlot;
            if (occupant != null)
            {
                dragSessionCurrentSlots[occupant] = draggedCurrentSlot;
            }

            Debug.Log($"[FactoryBallConfirmationLayout] Hover swap: dragged={draggedHandle.name}, fromSlot={draggedCurrentSlot}, toSlot={targetSlot}, occupant={(occupant != null ? occupant.name : "none")}");

            CommitDragSessionSiblingOrder();
        }

        for (int i = 0; i < dragSessionHandles.Count; i++)
        {
            IconDragHandle handle = dragSessionHandles[i];
            if (handle == null)
            {
                continue;
            }

            if (!dragSessionCurrentSlots.TryGetValue(handle, out int assignedSlot))
            {
                continue;
            }

            handle.SetSmoothTargetWorldX(dragSessionSlotWorldXs[assignedSlot]);
        }
    }

    private int FindClosestSlotIndex(float worldX)
    {
        int bestIndex = 0;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < dragSessionSlotWorldXs.Count; i++)
        {
            float distance = Mathf.Abs(worldX - dragSessionSlotWorldXs[i]);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private void CommitDragSessionSiblingOrder()
    {
        if (dragSessionHandles.Count == 0)
        {
            return;
        }

        dragSessionHandles.Sort((a, b) =>
        {
            int aIndex = dragSessionCurrentSlots.TryGetValue(a, out int ai) ? ai : int.MaxValue;
            int bIndex = dragSessionCurrentSlots.TryGetValue(b, out int bi) ? bi : int.MaxValue;
            return aIndex.CompareTo(bIndex);
        });

        for (int i = 0; i < dragSessionHandles.Count; i++)
        {
            RectTransform rect = dragSessionHandles[i] != null ? dragSessionHandles[i].RectTransform : null;
            if (rect != null)
            {
                rect.SetSiblingIndex(i);
            }
        }
    }

    private void TickDragSessionSmoothing()
    {
        if (!dragSessionActive)
        {
            return;
        }

        float deltaTime = Time.unscaledDeltaTime;
        for (int i = 0; i < dragSessionHandles.Count; i++)
        {
            IconDragHandle handle = dragSessionHandles[i];
            if (handle == null)
            {
                continue;
            }

            handle.TickSmoothMove(deltaTime, dragReorderLerpSpeed);
        }
    }

    private bool TryGetDraggableHandleAtPointer(Vector2 pointerScreenPosition, Camera eventCamera, out IconDragHandle handle, out string blockedReason)
    {
        handle = null;
        blockedReason = "No icon found under pointer.";

        for (int i = iconPool.Count - 1; i >= 0; i--)
        {
            Image iconImage = iconPool[i];
            if (iconImage == null || !iconImage.gameObject.activeInHierarchy)
            {
                continue;
            }

            RectTransform rectTransform = iconImage.rectTransform;
            if (!RectTransformUtility.RectangleContainsScreenPoint(rectTransform, pointerScreenPosition, eventCamera))
            {
                continue;
            }

            IconDragHandle iconHandle = iconImage.GetComponent<IconDragHandle>();
            if (iconHandle == null)
            {
                blockedReason = "Icon is missing IconDragHandle.";
                return false;
            }

            if (!iconHandle.IsDraggable)
            {
                blockedReason = string.IsNullOrEmpty(iconHandle.DisabledReason)
                    ? "Icon is not currently draggable."
                    : iconHandle.DisabledReason;
                return false;
            }

            handle = iconHandle;
            blockedReason = string.Empty;
            return true;
        }

        return false;
    }

    private Camera ResolveEventCamera()
    {
        if (iconLayoutRoot == null)
        {
            return null;
        }

        Canvas canvas = iconLayoutRoot.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return null;
        }

        return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
    }

    private string BuildDragDisabledReason(Image iconImage, BallTypeData ballType)
    {
        if (!allowDragReorder)
        {
            return "Allow Drag Reorder is disabled.";
        }

        if (ballType == null)
        {
            return "Ball type is null for this icon.";
        }

        if (iconImage == null)
        {
            return "Icon image is missing.";
        }

        if (!iconImage.raycastTarget)
        {
            return "Icon Image Raycast Target is disabled.";
        }

        if (EventSystem.current == null)
        {
            return "No active EventSystem found in scene.";
        }

        return string.Empty;
    }

    private sealed class IconDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private FactoryBallConfirmationLayout owner;
        private Image iconImage;
        private bool canDrag;
        private string dragDisabledReason;
        private LayoutElement layoutElement;
        private CanvasGroup canvasGroup;
        private float lockedWorldY;
        private float smoothTargetWorldX;
        private float currentWorldX;
        private int lastHoveredSlot = -1;

        public RectTransform RectTransform => transform as RectTransform;
        public bool IsDraggable => canDrag && owner != null && RectTransform != null;
        public string DisabledReason => dragDisabledReason;
        public int LastHoveredSlot => lastHoveredSlot;
        public bool HasLastHoveredSlot => lastHoveredSlot >= 0;

        public void Configure(FactoryBallConfirmationLayout layoutOwner, Image image, BallTypeData ballType, bool draggable, string disabledReason)
        {
            owner = layoutOwner;
            iconImage = image;
            canDrag = draggable;
            dragDisabledReason = disabledReason;

            if (layoutElement == null)
            {
                layoutElement = gameObject.GetComponent<LayoutElement>();
                if (layoutElement == null)
                {
                    layoutElement = gameObject.AddComponent<LayoutElement>();
                }
            }

            if (canvasGroup == null)
            {
                canvasGroup = gameObject.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            layoutElement.ignoreLayout = false;
            canvasGroup.blocksRaycasts = true;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!IsDraggable)
            {
                return;
            }

            BeginDragFallback();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsDraggable || owner == null)
            {
                return;
            }

            owner.HandleIconDragMoved(this, eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!IsDraggable || owner == null)
            {
                return;
            }

            EndDragFallback();
            owner.HandleIconDragEnded(this);
        }

        public void BeginDragFallback()
        {
            if (!IsDraggable)
            {
                return;
            }

            lockedWorldY = RectTransform.position.y;
            currentWorldX = RectTransform.position.x;

            layoutElement.ignoreLayout = true;
            canvasGroup.blocksRaycasts = false;

            owner.BeginDragSession(this);
            transform.SetAsLastSibling();

            if (owner.iconLayoutRoot is RectTransform rootRect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
            }
        }

        public void SetLastHoveredSlot(int slotIndex)
        {
            lastHoveredSlot = slotIndex;
        }

        public void EndDragFallback()
        {
            canvasGroup.blocksRaycasts = true;

            owner.EndDragSession(this);

            lastHoveredSlot = -1;

            if (owner != null && owner.iconLayoutRoot is RectTransform rootRect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
            }
        }

        public void SetDraggedWorldX(float worldX)
        {
            if (RectTransform == null)
            {
                return;
            }

            float t = 1f - Mathf.Exp(-Mathf.Max(1f, owner.dragReorderLerpSpeed) * Time.unscaledDeltaTime);
            currentWorldX = Mathf.Lerp(currentWorldX, worldX, t);

            Vector3 worldPosition = RectTransform.position;
            worldPosition.x = currentWorldX;
            worldPosition.y = lockedWorldY;
            RectTransform.position = worldPosition;
        }

        public void SetLockedWorldYFromCurrent()
        {
            if (RectTransform == null)
            {
                return;
            }

            lockedWorldY = RectTransform.position.y;
        }

        public void SetLayoutIgnored(bool ignore)
        {
            if (layoutElement != null)
            {
                layoutElement.ignoreLayout = ignore;
            }
        }

        public void SetSmoothTargetWorldX(float targetWorldX)
        {
            smoothTargetWorldX = targetWorldX;
        }

        public void TickSmoothMove(float deltaTime, float lerpSpeed)
        {
            if (RectTransform == null)
            {
                return;
            }

            float t = 1f - Mathf.Exp(-Mathf.Max(1f, lerpSpeed) * deltaTime);
            Vector3 worldPosition = RectTransform.position;
            worldPosition.x = Mathf.Lerp(worldPosition.x, smoothTargetWorldX, t);
            worldPosition.y = lockedWorldY;
            RectTransform.position = worldPosition;
        }

        public void SnapToWorldX(float worldX)
        {
            if (RectTransform == null)
            {
                return;
            }

            currentWorldX = worldX;
            Vector3 worldPosition = RectTransform.position;
            worldPosition.x = worldX;
            worldPosition.y = lockedWorldY;
            RectTransform.position = worldPosition;
        }

    }

}
