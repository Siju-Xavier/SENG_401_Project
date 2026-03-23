namespace Core {
    using System;
    using System.Collections.Generic;

    public enum EventType { FireStarted, FireSpread, FireExtinguished, TileUpdated, BudgetChanged, GameEnded, LevelUp, RoundComplete, UnitDeployed, EnvironmentImpact, FireNoLongerEdge }

    public class EventBroker {
        private static EventBroker _instance;
        public static EventBroker Instance => _instance ??= new EventBroker();

        private Dictionary<EventType, Action<object>> subscribers = new Dictionary<EventType, Action<object>>();

        public void Subscribe(EventType type, Action<object> callback) {
            if (!subscribers.ContainsKey(type)) subscribers[type] = null;
            subscribers[type] += callback;
        }

        public void Unsubscribe(EventType type, Action<object> callback) {
            if (subscribers.ContainsKey(type)) subscribers[type] -= callback;
        }

        public void Publish(EventType type, object data = null) {
            if (subscribers.ContainsKey(type)) subscribers[type]?.Invoke(data);
        }
    }
}
