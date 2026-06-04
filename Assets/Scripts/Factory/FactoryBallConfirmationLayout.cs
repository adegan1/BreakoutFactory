using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class FactoryBallConfirmationLayout : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform iconLayoutRoot;
    [SerializeField] private Transform positionLayoutRoot;
    [SerializeField] private Image iconPrefab;
    [SerializeField] private RectTransform positionSlotPrefab;
    [SerializeField] private GameObject unplacedMoldsText;

    [Header("Fallback")]
    [SerializeField] private BallTypeData defaultBallType;
    [SerializeField] private Sprite fallbackSprite;

    [Header("Behavior")]
    [SerializeField] private bool refreshOnEnable = true;
    [SerializeField] private bool allowDragReorder = true;
    [SerializeField, Min(1f)] private float dragReorderLerpSpeed = 18f;

    private readonly List<Image> iconPool = new List<Image>();
    private readonly List<RectTransform> positionSlotPool = new List<RectTransform>();
    private readonly Dictionary<Image, BallTypeData> craftedBallByIcon = new Dictionary<Image, BallTypeData>();
    private readonly HashSet<Image> craftedIconSet = new HashSet<Image>();
    private readonly List<IconDragHandle> dragSessionHandles = new List<IconDragHandle>();
    private readonly List<float> dragSessionSlotWorldXs = new List<float>();

    private IconDragHandle activeDragHandle;
    private IconDragHandle fallbackDraggingHandle;
    private int activeDragOriginalIndex = -1;
    private int activeDragTargetIndex = -1;
    private RectTransform activeDragPlaceholder;

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
        TickActiveDragSmoothing();
    }

    public void RefreshIcons()
    {
        if (iconLayoutRoot == null || positionLayoutRoot == null || iconPrefab == null)
        {
            UpdateUnplacedMoldsTextVisibility();
            return;
        }

        craftedBallByIcon.Clear();
        craftedIconSet.Clear();

        int moldCount = FindObjectsByType<BallMoldBuilding>(FindObjectsSortMode.None).Length;

        if (InventoryManager.HasInstance)
        {
            InventoryManager.Instance.EnsureCraftedBallDefaults(defaultBallType, moldCount);
        }

        EnsureIconPool(moldCount);
        EnsurePositionSlotPool(moldCount);

        for (int i = 0; i < iconPool.Count; i++)
        {
            iconPool[i].gameObject.SetActive(false);
        }

        for (int i = 0; i < positionSlotPool.Count; i++)
        {
            positionSlotPool[i].gameObject.SetActive(i < moldCount);
            if (i < moldCount)
            {
                positionSlotPool[i].SetSiblingIndex(i);
            }
        }

        if (moldCount <= 0)
        {
            UpdateUnplacedMoldsTextVisibility();
            return;
        }

        IReadOnlyList<BallTypeData> craftedBalls = InventoryManager.HasInstance
            ? InventoryManager.Instance.CraftedBalls
            : null;
        int craftedShown = craftedBalls != null ? Mathf.Min(craftedBalls.Count, moldCount) : 0;

        for (int i = 0; i < craftedShown; i++)
        {
            ApplyIcon(iconPool[i], craftedBalls[i], isCraftedBall: true);
            iconPool[i].rectTransform.SetSiblingIndex(i);
        }

        for (int i = craftedShown; i < moldCount; i++)
        {
            ApplyIcon(iconPool[i], defaultBallType, isCraftedBall: false);
            iconPool[i].rectTransform.SetSiblingIndex(i);
        }

        if (iconLayoutRoot is RectTransform iconRootRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(iconRootRect);
        }

        if (positionLayoutRoot is RectTransform positionRootRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(positionRootRect);
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

    private void EnsurePositionSlotPool(int count)
    {
        for (int i = positionSlotPool.Count; i < count; i++)
        {
            RectTransform slotRect;
            if (positionSlotPrefab != null)
            {
                slotRect = Instantiate(positionSlotPrefab, positionLayoutRoot);
            }
            else
            {
                GameObject slotObject = new GameObject("FactoryBallConfirmPositionSlot_" + i, typeof(RectTransform), typeof(LayoutElement));
                slotObject.transform.SetParent(positionLayoutRoot, false);
                slotRect = slotObject.GetComponent<RectTransform>();
                LayoutElement slotLayout = slotObject.GetComponent<LayoutElement>();
                slotLayout.preferredWidth = iconPrefab.rectTransform.rect.width;
                slotLayout.preferredHeight = iconPrefab.rectTransform.rect.height;
            }

            slotRect.gameObject.SetActive(false);
            positionSlotPool.Add(slotRect);
        }
    }

    private void ApplyIcon(Image iconImage, BallTypeData ballType, bool isCraftedBall)
    {
        if (iconImage == null)
        {
            return;
        }

        iconImage.sprite = ResolveSprite(ballType);
        iconImage.color = ballType != null && ballType.IsCompound ? ballType.TrailColor : Color.white;
        iconImage.gameObject.SetActive(iconImage.sprite != null);

        IconDragHandle dragHandle = EnsureDragHandle(iconImage);
        bool canDrag = allowDragReorder && ballType != null;
        dragHandle.Configure(this, iconImage, canDrag, BuildDragDisabledReason(iconImage, ballType, isCraftedBall));

        if (ballType != null)
        {
            craftedBallByIcon[iconImage] = ballType;
        }
        else
        {
            craftedBallByIcon.Remove(iconImage);
        }

        if (isCraftedBall && ballType != null)
        {
            craftedIconSet.Add(iconImage);
        }
        else
        {
            craftedIconSet.Remove(iconImage);
        }

        TooltipTrigger tooltip = iconImage.GetComponent<TooltipTrigger>();
        if (tooltip != null)
        {
            tooltip.SetContent(
                ballType != null ? ballType.LocalizedDisplayName : string.Empty,
                ballType != null ? ballType.LocalizedDescription : string.Empty);
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
            dragHandle.SetDraggedWorldPosition(worldPoint);

            if (dragHandle == activeDragHandle)
            {
                int craftedCount = dragSessionHandles.Count + 1;
                activeDragTargetIndex = FindClosestPositionSlotIndex(worldPoint.x, craftedCount);
            }
        }
    }

    private void HandleFallbackPointerDrag()
    {
        if (!allowDragReorder)
        {
            return;
        }

        if (activeDragHandle != null && activeDragHandle != fallbackDraggingHandle)
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
            fallbackDraggingHandle = null;
        }
    }

    private void BeginDragSession(IconDragHandle draggedHandle)
    {
        if (draggedHandle == null || draggedHandle.RectTransform == null)
        {
            return;
        }

        activeDragHandle = draggedHandle;

        List<IconDragHandle> visibleOrder = new List<IconDragHandle>();
        for (int i = 0; i < iconLayoutRoot.childCount; i++)
        {
            Transform child = iconLayoutRoot.GetChild(i);
            Image childImage = child != null ? child.GetComponent<Image>() : null;
            if (childImage == null || !childImage.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (!craftedBallByIcon.TryGetValue(childImage, out BallTypeData ballType) || ballType == null)
            {
                continue;
            }

            IconDragHandle handle = child.GetComponent<IconDragHandle>();
            if (handle != null && handle.IsDraggable)
            {
                visibleOrder.Add(handle);
            }
        }

        activeDragOriginalIndex = visibleOrder.IndexOf(draggedHandle);
        if (activeDragOriginalIndex < 0)
        {
            activeDragHandle = null;
            return;
        }

        activeDragTargetIndex = activeDragOriginalIndex;
        dragSessionHandles.Clear();
        dragSessionSlotWorldXs.Clear();
        CreateActiveDragPlaceholder(draggedHandle, activeDragOriginalIndex);

        for (int i = 0; i < visibleOrder.Count; i++)
        {
            IconDragHandle handle = visibleOrder[i];
            if (handle == null || handle.RectTransform == null)
            {
                dragSessionSlotWorldXs.Add(0f);
                continue;
            }

            dragSessionSlotWorldXs.Add(handle.RectTransform.position.x);
        }

        for (int i = 0; i < visibleOrder.Count; i++)
        {
            IconDragHandle handle = visibleOrder[i];
            if (handle == null)
            {
                continue;
            }

            handle.LockWorldY();
            handle.SetLayoutIgnored(true);
            if (handle != draggedHandle)
            {
                dragSessionHandles.Add(handle);
            }
        }

        draggedHandle.SetBlocksRaycasts(false);
        draggedHandle.RectTransform.SetAsLastSibling();

        if (iconLayoutRoot is RectTransform iconRootRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(iconRootRect);
        }

        if (positionLayoutRoot is RectTransform positionRootRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(positionRootRect);
        }
    }

    private void EndDragSession(IconDragHandle draggedHandle)
    {
        if (draggedHandle == null || draggedHandle.RectTransform == null || draggedHandle != activeDragHandle)
        {
            return;
        }

        int craftedCount = dragSessionHandles.Count + 1;
        if (craftedCount <= 0)
        {
            draggedHandle.SetLayoutIgnored(false);
            draggedHandle.SetBlocksRaycasts(true);
            ClearActiveDragPlaceholder();
            dragSessionHandles.Clear();
            dragSessionSlotWorldXs.Clear();
            activeDragHandle = null;
            activeDragOriginalIndex = -1;
            activeDragTargetIndex = -1;
            return;
        }

        if (Pointer.current != null)
        {
            activeDragTargetIndex = FindClosestPositionSlotIndex(Pointer.current.position.ReadValue().x, craftedCount);
        }

        int originalIndex = Mathf.Clamp(activeDragOriginalIndex, 0, craftedCount - 1);
        int targetIndex = Mathf.Clamp(activeDragTargetIndex, 0, craftedCount - 1);

        List<IconDragHandle> finalOrder = new List<IconDragHandle>(dragSessionHandles);
        finalOrder.Insert(Mathf.Clamp(targetIndex, 0, finalOrder.Count), draggedHandle);

        for (int i = 0; i < finalOrder.Count; i++)
        {
            IconDragHandle handle = finalOrder[i];
            if (handle?.RectTransform != null)
            {
                handle.RectTransform.SetSiblingIndex(i);
            }
        }

        ClearActiveDragPlaceholder();

        for (int i = 0; i < finalOrder.Count; i++)
        {
            IconDragHandle handle = finalOrder[i];
            if (handle == null)
            {
                continue;
            }

            handle.SetLayoutIgnored(false);
            handle.SetBlocksRaycasts(true);
        }

        if (iconLayoutRoot is RectTransform iconRootRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(iconRootRect);
        }

        if (InventoryManager.HasInstance)
        {
            List<BallTypeData> displayedOrder = BuildDisplayedBallOrder(positionSlotPool.Count);
            InventoryManager.Instance.SetCraftedBalls(displayedOrder);
        }

        dragSessionHandles.Clear();
        dragSessionSlotWorldXs.Clear();
        activeDragHandle = null;
        activeDragOriginalIndex = -1;
        activeDragTargetIndex = -1;
    }

    private void CreateActiveDragPlaceholder(IconDragHandle draggedHandle, int siblingIndex)
    {
        ClearActiveDragPlaceholder();

        if (draggedHandle == null || draggedHandle.RectTransform == null || iconLayoutRoot == null)
        {
            return;
        }

        GameObject placeholderObject = new GameObject("FactoryBallConfirmDragPlaceholder", typeof(RectTransform), typeof(LayoutElement));
        placeholderObject.transform.SetParent(iconLayoutRoot, false);

        activeDragPlaceholder = placeholderObject.GetComponent<RectTransform>();
        activeDragPlaceholder.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, iconLayoutRoot.childCount - 1));

        LayoutElement placeholderLayout = placeholderObject.GetComponent<LayoutElement>();
        LayoutElement sourceLayout = draggedHandle.GetComponent<LayoutElement>();

        float preferredWidth = draggedHandle.RectTransform.rect.width;
        float preferredHeight = draggedHandle.RectTransform.rect.height;
        if (sourceLayout != null)
        {
            if (sourceLayout.preferredWidth > 0f)
            {
                preferredWidth = sourceLayout.preferredWidth;
            }

            if (sourceLayout.preferredHeight > 0f)
            {
                preferredHeight = sourceLayout.preferredHeight;
            }

            placeholderLayout.minWidth = sourceLayout.minWidth;
            placeholderLayout.minHeight = sourceLayout.minHeight;
            placeholderLayout.flexibleWidth = sourceLayout.flexibleWidth;
            placeholderLayout.flexibleHeight = sourceLayout.flexibleHeight;
        }

        placeholderLayout.preferredWidth = preferredWidth;
        placeholderLayout.preferredHeight = preferredHeight;
    }

    private void ClearActiveDragPlaceholder()
    {
        if (activeDragPlaceholder != null)
        {
            Destroy(activeDragPlaceholder.gameObject);
            activeDragPlaceholder = null;
        }
    }

    private void TickActiveDragSmoothing()
    {
        if (activeDragHandle == null || dragSessionHandles.Count == 0 || activeDragTargetIndex < 0 || dragSessionSlotWorldXs.Count == 0)
        {
            return;
        }

        for (int i = 0; i < dragSessionHandles.Count; i++)
        {
            IconDragHandle handle = dragSessionHandles[i];
            if (handle == null || handle.RectTransform == null)
            {
                continue;
            }

            int slotIndex = i >= activeDragTargetIndex ? i + 1 : i;
            slotIndex = Mathf.Clamp(slotIndex, 0, dragSessionSlotWorldXs.Count - 1);
            if (slotIndex < 0 || slotIndex >= dragSessionSlotWorldXs.Count)
            {
                continue;
            }

            handle.LerpToWorldX(dragSessionSlotWorldXs[slotIndex], dragReorderLerpSpeed);
        }
    }

    private int FindClosestPositionSlotIndex(float worldX, int craftedCount)
    {
        int usableCount = dragSessionSlotWorldXs.Count > 0
            ? Mathf.Min(craftedCount, dragSessionSlotWorldXs.Count)
            : Mathf.Min(craftedCount, positionSlotPool.Count);
        if (usableCount <= 0)
        {
            return 0;
        }

        int bestIndex = 0;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < usableCount; i++)
        {
            float slotWorldX = dragSessionSlotWorldXs.Count > 0
                ? dragSessionSlotWorldXs[i]
                : (positionSlotPool[i] != null ? positionSlotPool[i].position.x : 0f);
            float distance = Mathf.Abs(worldX - slotWorldX);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private List<BallTypeData> BuildCraftedBallOrderFromHandleOrder(List<IconDragHandle> orderedHandles)
    {
        List<BallTypeData> reordered = new List<BallTypeData>();
        if (orderedHandles == null)
        {
            return reordered;
        }

        for (int i = 0; i < orderedHandles.Count; i++)
        {
            IconDragHandle handle = orderedHandles[i];
            Image image = handle != null ? handle.Image : null;
            if (image == null || !craftedIconSet.Contains(image))
            {
                continue;
            }

            if (craftedBallByIcon.TryGetValue(image, out BallTypeData ballType) && ballType != null)
            {
                reordered.Add(ballType);
            }
        }

        return reordered;
    }

    public List<BallTypeData> BuildDisplayedBallOrder(int maxCount)
    {
        List<BallTypeData> ordered = new List<BallTypeData>();
        if (iconLayoutRoot == null || maxCount <= 0)
        {
            return ordered;
        }

        for (int i = 0; i < iconLayoutRoot.childCount && ordered.Count < maxCount; i++)
        {
            Transform child = iconLayoutRoot.GetChild(i);
            Image childImage = child != null ? child.GetComponent<Image>() : null;
            if (childImage == null || !childImage.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (craftedBallByIcon.TryGetValue(childImage, out BallTypeData ballType) && ballType != null)
            {
                ordered.Add(ballType);
            }
        }

        return ordered;
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

    private string BuildDragDisabledReason(Image iconImage, BallTypeData ballType, bool isCraftedBall)
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

    private sealed class IconDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private FactoryBallConfirmationLayout owner;
        private Image iconImage;
        private bool canDrag;
        private string dragDisabledReason;
        private LayoutElement layoutElement;
        private CanvasGroup canvasGroup;
        private float lockedWorldY;
        private float currentWorldX;

        public RectTransform RectTransform => transform as RectTransform;
        public Image Image => iconImage;
        public bool IsDraggable => canDrag && owner != null && RectTransform != null;
        public string DisabledReason => dragDisabledReason;

        public void Configure(FactoryBallConfirmationLayout layoutOwner, Image image, bool draggable, string disabledReason)
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
        }

        public void BeginDragFallback()
        {
            if (!IsDraggable)
            {
                return;
            }

            owner.BeginDragSession(this);
        }

        public void EndDragFallback()
        {
            owner.EndDragSession(this);
        }

        public void LockWorldY()
        {
            if (RectTransform == null)
            {
                return;
            }

            lockedWorldY = RectTransform.position.y;
            currentWorldX = RectTransform.position.x;
        }

        public void SetDraggedWorldPosition(Vector3 worldPosition)
        {
            if (RectTransform == null || owner == null)
            {
                return;
            }

            float t = 1f - Mathf.Exp(-Mathf.Max(1f, owner.dragReorderLerpSpeed) * Time.unscaledDeltaTime);
            currentWorldX = Mathf.Lerp(currentWorldX, worldPosition.x, t);

            Vector3 next = RectTransform.position;
            next.x = currentWorldX;
            next.y = lockedWorldY;
            RectTransform.position = next;
        }

        public void LerpToWorldX(float worldX, float lerpSpeed)
        {
            if (RectTransform == null)
            {
                return;
            }

            float t = 1f - Mathf.Exp(-Mathf.Max(1f, lerpSpeed) * Time.unscaledDeltaTime);
            currentWorldX = Mathf.Lerp(currentWorldX, worldX, t);

            Vector3 next = RectTransform.position;
            next.x = currentWorldX;
            next.y = lockedWorldY;
            RectTransform.position = next;
        }

        public void SetLayoutIgnored(bool ignore)
        {
            if (layoutElement != null)
            {
                layoutElement.ignoreLayout = ignore;
            }
        }

        public void SetBlocksRaycasts(bool blocksRaycasts)
        {
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = blocksRaycasts;
            }
        }
    }
}
