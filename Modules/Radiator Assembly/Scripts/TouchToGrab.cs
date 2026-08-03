using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;
using VRseBuilder.Platform.MetaXR.Wrappers;

/// <summary>
/// Implements touch-to-grab functionality for MetaXR grabbable objects.
/// When GrabOnTouch is enabled, objects are automatically grabbed when a hand touches them.
/// 
/// This script uses conditional selection overrides that are evaluated at runtime,
/// ensuring that placement state and workflow system changes are properly respected.
/// </summary>
public class TouchToGrab : MonoBehaviour
{
    [SerializeField] private MetaXRGrabbableWrapper _wrapper;

    private void Start()
    {
        if (_wrapper == null)
            _wrapper = GetComponent<MetaXRGrabbableWrapper>();

        if (_wrapper == null)
        {
            Debug.LogError($"[TTR_TouchToGrab] No MetaXRGrabbableWrapper found on {gameObject.name}");
            return;
        }

        // Subscribe to hover events (when hand enters/exits the grabbable's interaction zone)
        if (_wrapper.grabInteractable != null)
        {
            _wrapper.grabInteractable.WhenInteractorAdded.Action += HandleHover;
            _wrapper.grabInteractable.WhenInteractorRemoved.Action += HandleUnhover;
        }

        if (_wrapper.handGrabInteractables != null)
        {
            foreach (var hgi in _wrapper.handGrabInteractables)
            {
                hgi.WhenInteractorAdded.Action += HandleHover;
                hgi.WhenInteractorRemoved.Action += HandleUnhover;
            }
        }
    }

    private void OnDestroy()
    {
        if (_wrapper == null) return;

        // Clean up event subscriptions
        if (_wrapper.grabInteractable != null)
        {
            _wrapper.grabInteractable.WhenInteractorAdded.Action -= HandleHover;
            _wrapper.grabInteractable.WhenInteractorRemoved.Action -= HandleUnhover;
        }

        if (_wrapper.handGrabInteractables != null)
        {
            foreach (var hgi in _wrapper.handGrabInteractables)
            {
                hgi.WhenInteractorAdded.Action -= HandleHover;
                hgi.WhenInteractorRemoved.Action -= HandleUnhover;
            }
        }
    }

    /// <summary>
    /// Called when a hand/controller enters the interaction zone of the grabbable.
    /// Sets a conditional override that forces selection if all grab conditions are met.
    /// </summary>
    private void HandleHover(IInteractor interactor)
    {
        if (!_wrapper.GrabOnTouch) return;

        // Set a CONDITIONAL override that is evaluated at runtime
        // This lambda is called repeatedly by the interaction system to determine if selection should occur
        // By using a conditional check instead of a static true/false, we ensure that:
        // 1. Changes to isGrabbable are immediately respected
        // 2. Placement state is checked even if workflow system overrides isGrabbable
        // 3. The override automatically becomes inactive when conditions change
        if (interactor is GrabInteractor gi)
        {
            gi.SetComputeShouldSelectOverride(() => ShouldAllowGrab(), false);
        }
        else if (interactor is HandGrabInteractor hgi)
        {
            hgi.SetComputeShouldSelectOverride(() => ShouldAllowGrab(), false);
        }
    }

    /// <summary>
    /// Called when a hand/controller exits the interaction zone of the grabbable.
    /// Clears the selection override to restore normal grab behavior.
    /// </summary>
    private void HandleUnhover(IInteractor interactor)
    {
        if (!_wrapper.GrabOnTouch) return;

        // Clear the override when no longer hovering
        if (interactor is GrabInteractor gi)
        {
            gi.ClearComputeShouldSelectOverride();
        }
        else if (interactor is HandGrabInteractor hgi)
        {
            hgi.ClearComputeShouldSelectOverride();
        }
    }

    /// <summary>
    /// Determines whether touch-to-grab should be allowed based on current state.
    /// This method is called repeatedly by the selection override system.
    /// 
    /// Returns false if:
    /// - GrabOnTouch is disabled
    /// - Object is not grabbable (isGrabbable = false)
    /// - Object is placed in a PlacePoint with disableGrabOnPlace enabled
    /// 
    /// The third check is critical: the workflow system may set isGrabbable back to true
    /// when setting up triggers, but we still need to respect the placement constraint.
    /// </summary>
    private bool ShouldAllowGrab()
    {
        // Check 1: GrabOnTouch feature must be enabled
        if (!_wrapper.GrabOnTouch)
            return false;

        // Check 2: Object must be marked as grabbable
        if (!_wrapper.IsGrabbable)
            return false;

        // Check 3: Object must not be in a "placed and locked" state
        // This handles the edge case where:
        // - Object A is placed with disableGrabOnPlace = true
        // - Object B is placed in a nearby PlacePoint
        // - Workflow system sets isGrabbable = true on Object A (for trigger setup)
        // - We still want to prevent grabbing Object A because it's placed
        if (_wrapper.PlacePointWrapper != null && 
            _wrapper.PlacePointWrapper.DisableGrabOnPlace &&
            _wrapper.CurrentState == VRseBuilder.Core.Interfaces.EGrabbableState.Placed)
        {
            return false;
        }

        // All checks passed - allow touch-to-grab
        return true;
    }
}
