using System;
using VRseBuilder.Core.Framework;
using VRseBuilder.Core.Logging;

#region TwoButtonsTouchTrigger Trigger

/**
 *  @brief     Trigger that detects when two buttons are touched within a specified time duration
 *
 * <summary>
 * Monitors the TwoButtonsTouch component and triggers based on whether both buttons 
 * are pressed within the configured time window or not. This trigger supports two options:
 * - TwoButtonsPressed: Triggers when both buttons are touched within the time limit
 * - TwoButtonsNotPressed: Triggers when the buttons are NOT touched within the time limit
 * </summary>
 *
 *  @author   VRse Builder
 *  <note>
 *      Example JSON:
 *      {
 *        "Name": "TwoButtonsTouchTrigger",
 *        "Query": "TwoButtonTouchObject",
 *        "Option": "TwoButtonsPressed",
 *        "Data": {}
 *      }
 *  </note>
 */
public class TwoButtonsTouchTrigger : PlayableTrigger
{
    private readonly VRseLogger _logger = new(nameof(TwoButtonsTouchTrigger));

    public enum ETwoButtonsTouchTriggerOptions
    {
        TwoButtonsPressed,
        TwoButtonsNotPressed
    }

    private class Parameters
    {
        // Add any additional parameters if needed in the future
    }

    private ETwoButtonsTouchTriggerOptions _triggerOptions;
    private TwoButtonsTouch _twoButtonsTouch;

    public override string Verb => _triggerOptions switch
    {
        ETwoButtonsTouchTriggerOptions.TwoButtonsPressed => "Both buttons pressed in time on",
        ETwoButtonsTouchTriggerOptions.TwoButtonsNotPressed => "Buttons not pressed in time on",
        _ => "Interacted with"
    };

    public override void Deserialize(Node node)
    {
        base.Deserialize(node);
        try
        {
            // Parse trigger option
            _triggerOptions = base.node.Option switch
            {
                nameof(ETwoButtonsTouchTriggerOptions.TwoButtonsPressed) => ETwoButtonsTouchTriggerOptions.TwoButtonsPressed,
                nameof(ETwoButtonsTouchTriggerOptions.TwoButtonsNotPressed) => ETwoButtonsTouchTriggerOptions.TwoButtonsNotPressed,
                _ => ETwoButtonsTouchTriggerOptions.TwoButtonsPressed
            };

            // Get the TwoButtonsTouch component from target GameObject
            if (node.TargetGameObject != null)
            {
                if (node.TargetGameObject.TryGetComponent<TwoButtonsTouch>(out _twoButtonsTouch))
                {
                    _logger.Debug("TwoButtonsTouch component found on '{0}'", node.TargetGameObject.name);
                }
                else
                {
                    _logger.Error("TwoButtonsTouch component not found on Target GameObject '{0}'", node.TargetGameObject.name);
                }
            }
            else
            {
                _logger.Error("Target GameObject is null for query: {0}", node.Query);
            }

            _logger.Debug("Deserialization completed successfully with option: {0}", _triggerOptions);
        }
        catch (Exception e)
        {
            _logger.Error("Exception caught while deserializing: {0}", e);
        }
    }

    public override void OnBegin()
    {
        _logger.Debug("OnBegin called for trigger option: {0}", _triggerOptions);

        try
        {
            if (_twoButtonsTouch != null)
            {
                RegisterEvents();
                _logger.Info("TwoButtonsTouchTrigger started successfully, waiting for {0}", _triggerOptions);
            }
            else
            {
                _logger.Error("Cannot start trigger: TwoButtonsTouch component is null");
                OnEnd();
            }
        }
        catch (Exception e)
        {
            _logger.Error("Exception in OnBegin: {0}", e);
            OnEnd();
        }

        InvokeOnBeginEvent();
    }

    private void RegisterEvents()
    {
        if (_twoButtonsTouch != null)
        {
            switch (_triggerOptions)
            {
                case ETwoButtonsTouchTriggerOptions.TwoButtonsPressed:
                    _twoButtonsTouch.onButtonsPressedInTime.AddListener(OnTwoButtonsPressed);
                    _logger.Debug("Registered listener for TwoButtonsPressed event");
                    break;

                case ETwoButtonsTouchTriggerOptions.TwoButtonsNotPressed:
                    _twoButtonsTouch.onButtonsNotPressedInTime.AddListener(OnTwoButtonsNotPressed);
                    _logger.Debug("Registered listener for TwoButtonsNotPressed event");
                    break;

                default:
                    _logger.Warning("Unknown trigger option: {0}", _triggerOptions);
                    break;
            }
        }
        else
        {
            _logger.Error("Cannot register events: TwoButtonsTouch component is null");
        }
    }

    private void UnregisterEvents()
    {
        if (_twoButtonsTouch != null)
        {
            switch (_triggerOptions)
            {
                case ETwoButtonsTouchTriggerOptions.TwoButtonsPressed:
                    _twoButtonsTouch.onButtonsPressedInTime.RemoveListener(OnTwoButtonsPressed);
                    _logger.Debug("Unregistered listener for TwoButtonsPressed event");
                    break;

                case ETwoButtonsTouchTriggerOptions.TwoButtonsNotPressed:
                    _twoButtonsTouch.onButtonsNotPressedInTime.RemoveListener(OnTwoButtonsNotPressed);
                    _logger.Debug("Unregistered listener for TwoButtonsNotPressed event");
                    break;

                default:
                    _logger.Warning("Unknown trigger option: {0}", _triggerOptions);
                    break;
            }
        }
    }

    private void OnTwoButtonsPressed()
    {
        _logger.Info("Two buttons pressed in time - Trigger condition met");
        OnEnd();
    }

    private void OnTwoButtonsNotPressed()
    {
        _logger.Info("Two buttons not pressed in time - Trigger condition met");
        OnEnd();
    }

    public override void OnEnd()
    {
        _logger.Debug("OnEnd called");

        // Unsubscribe from all events
        UnregisterEvents();

        InvokeOnEndEvent();
    }

    public override void OnSkip()
    {
        _logger.Info("Trigger skipped");

        // Clean up event subscriptions
        UnregisterEvents();

        InvokeOnSkippedEvent();
    }

    public override bool DoesRequireQuery() => true;

    public override bool IsQueryValid(out string validationMessage)
    {
        if (node.TargetGameObject == null)
        {
            validationMessage = $"TwoButtonsTouchTrigger requires a Target GameObject with TwoButtonsTouch component. Query: '{node.Query}' could not be resolved.";
            _logger.Error(validationMessage);
            return false;
        }

        if (!node.TargetGameObject.TryGetComponent(out TwoButtonsTouch _))
        {
            validationMessage = $"TwoButtonsTouchTrigger requires a TwoButtonsTouch component on '{node.TargetGameObject.name}'.";
            _logger.Error(validationMessage);
            return false;
        }

        validationMessage = string.Empty;
        return true;
    }

    public override void OnReset(int chapterIndex, int momentIndex)
    {
        _logger.Debug("OnReset called for chapter {0}, moment {1}", chapterIndex, momentIndex);
        UnregisterEvents();
    }

    public override void OnPause() => _logger.Debug("OnPause called");

    public override void OnResume() => _logger.Debug("OnResume called");
}

#endregion
