using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using VRseBuilder.Core.Interaction;
using VRseBuilder.Core.Interfaces;

/// <summary>
/// Detects when two buttons are touched within a specified time duration.
/// Optimized version using HandTouchDetectable from VRseBuilder SDK.
/// </summary>
public class TwoButtonsTouch : MonoBehaviour
{
    [Header("Button References")]
    [SerializeField] private HandTouchDetectable _button1;
    [SerializeField] private HandTouchDetectable _button2;

    [Header("Timing Configuration")]
    [SerializeField] private float _durationBetweenTouches = 1f;

    [Header("Events")]
    public UnityEvent onButtonsPressedInTime;
    public UnityEvent onButtonsNotPressedInTime;

    // State tracking
    private bool _isButton1Touched;
    private bool _isButton2Touched;
    private Coroutine _timerCoroutine;
    private float _firstTouchTime;

    private void OnEnable()
    {
        if (_button1 != null)
        {
            _button1.OnHandTriggerEnter.AddListener(OnButton1Touched);
        }

        if (_button2 != null)
        {
            _button2.OnHandTriggerEnter.AddListener(OnButton2Touched);
        }
    }

    private void OnDisable()
    {
        // Clean up event listeners
        if (_button1 != null)
        {
            _button1.OnHandTriggerEnter.RemoveListener(OnButton1Touched);
        }

        if (_button2 != null)
        {
            _button2.OnHandTriggerEnter.RemoveListener(OnButton2Touched);
        }

        // Stop any running coroutine
        if (_timerCoroutine != null)
        {
            StopCoroutine(_timerCoroutine);
            _timerCoroutine = null;
        }
    }

    private void OnButton1Touched(IHand hand)
    {
        // If button 2 is already waiting for button 1
        if (_isButton2Touched && _timerCoroutine != null)
        {
            CheckBothButtonsTouched();
            return;
        }

        // Start waiting for button 2
        if (!_isButton1Touched)
        {
            _isButton1Touched = true;
            _firstTouchTime = Time.time;

            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
            }

            _timerCoroutine = StartCoroutine(WaitForSecondButton());
        }
    }

    private void OnButton2Touched(IHand hand)
    {
        // If button 1 is already waiting for button 2
        if (_isButton1Touched && _timerCoroutine != null)
        {
            CheckBothButtonsTouched();
            return;
        }

        // Start waiting for button 1
        if (!_isButton2Touched)
        {
            _isButton2Touched = true;
            _firstTouchTime = Time.time;

            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
            }

            _timerCoroutine = StartCoroutine(WaitForSecondButton());
        }
    }

    private void CheckBothButtonsTouched()
    {
        float elapsedTime = Time.time - _firstTouchTime;

        if (elapsedTime <= _durationBetweenTouches)
        {
            Debug.Log($"Both buttons touched in time ({elapsedTime:F2}s)");
            onButtonsPressedInTime?.Invoke();
        }
        else
        {
            Debug.Log($"Second button touched too late ({elapsedTime:F2}s)");
            onButtonsNotPressedInTime?.Invoke();
        }

        ResetState();
    }

    private IEnumerator WaitForSecondButton()
    {
        // Wait for the duration
        yield return new WaitForSeconds(_durationBetweenTouches);

        // If we're still waiting, the second button wasn't pressed in time
        Debug.Log("Second button not touched in time");
        onButtonsNotPressedInTime?.Invoke();

        ResetState();
    }

    private void ResetState()
    {
        _isButton1Touched = false;
        _isButton2Touched = false;
        _timerCoroutine = null;
        _firstTouchTime = 0f;
    }
}