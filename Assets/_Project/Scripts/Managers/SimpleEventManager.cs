using System;
using System.Collections.Generic;

public static class GameEvents
{
    public const string GameOver = "GameOver";
    public const string WaterStateChanged = "WaterStateChanged";
    public const string GameStateChanged = "GameStateChanged";
}

public static class SimpleEventManager
{
    private static readonly Dictionary<string, Action<object>> _events =
        new Dictionary<string, Action<object>>();

    public static void Subscribe(string eventName, Action<object> listener)
    {
        if (string.IsNullOrEmpty(eventName) || listener == null)
            return;

        if (_events.TryGetValue(eventName, out var existing))
        {
            existing += listener;
            _events[eventName] = existing;
        }
        else
        {
            _events[eventName] = listener;
        }
    }

    public static void Unsubscribe(string eventName, Action<object> listener)
    {
        if (string.IsNullOrEmpty(eventName) || listener == null)
            return;

        if (_events.TryGetValue(eventName, out var existing))
        {
            existing -= listener;

            if (existing == null)
                _events.Remove(eventName);
            else
                _events[eventName] = existing;
        }
    }

    /// <summary>
    /// Fire event with optional payload. Pass null if you don't need data.
    /// </summary>
    public static void Emit(string eventName, object payload = null)
    {
        if (string.IsNullOrEmpty(eventName))
            return;

        if (_events.TryGetValue(eventName, out var callback))
        {
            callback?.Invoke(payload);
        }
    }

    /// <summary>
    /// Convenience generic wrapper so you don't have to cast manually on emit.
    /// Listener still uses object, but emit is type-safe.
    /// </summary>
    public static void Emit<T>(string eventName, T payload)
    {
        Emit(eventName, (object)payload);
    }
}
