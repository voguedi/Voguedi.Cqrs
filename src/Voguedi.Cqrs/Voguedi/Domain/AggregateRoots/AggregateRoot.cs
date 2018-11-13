using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Voguedi.Domain.Entities;
using Voguedi.Domain.Events;

namespace Voguedi.Domain.AggregateRoots
{
    public abstract class AggregateRoot<TIdentity> : Entity<TIdentity>, IAggregateRoot<TIdentity>
    {
        #region Private Fields

        readonly BlockingCollection<IEvent> uncommittedEvents = new BlockingCollection<IEvent>(new ConcurrentQueue<IEvent>());
        readonly ConcurrentDictionary<Type, MethodInfo> eventHandleMethodMapping = new ConcurrentDictionary<Type, MethodInfo>();

        #endregion

        #region Ctors

        protected AggregateRoot(): base() { }

        protected AggregateRoot(TIdentity id) : base(id) { }

        #endregion

        #region Private Methods

        void HandleEvent(IEvent e)
        {
            var handleMethod = GetEventHandleMethod(e);

            if (handleMethod != null)
                handleMethod.Invoke(this, new[] { e });
            else
                throw new ArgumentException(nameof(e), $"聚合根处理事件失败，不存在事件处理方法！");
        }

        MethodInfo GetEventHandleMethod(IEvent e)
        {
            var eventType = e.GetType();
            var value = eventHandleMethodMapping.GetOrAddIfNotNull(
                eventType,
                key =>
                {
                    var methods = from method in GetType().GetTypeInfo().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                  let parameters = method.GetParameters()
                                  where method.Name == "Handle" && parameters.Length == 1 && parameters.First().ParameterType == key
                                  select method;
                    return methods.FirstOrDefault();
                });

            if (value != null)
                return value;

            eventHandleMethodMapping.TryRemove(eventType);
            return null;
        }

        void EnqueueEvent(IEvent e)
        {
            if (uncommittedEvents.Any(item => item.GetType() == e.GetType()))
                throw new Exception($"聚合根无法重复添加事件！");

            uncommittedEvents.TryAdd(e);
        }

        #endregion

        #region Protected Methods

        protected void ApplyEvent<TEvent>(TEvent e)
            where TEvent : class, IEvent<TIdentity>
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            e.AggregateRootId = Id;
            e.Version = Version + 1;
            HandleEvent(e);
            EnqueueEvent(e);
        }

        #endregion

        #region IAggregateRoot<TIdentity>

        public long Version { get; private set; }

        public string GetAggregateRootId() => Id?.ToString();

        public Type GetAggregateRootType() => GetType();

        public IReadOnlyList<IEvent> GetUncommittedEvents() => uncommittedEvents.ToList();

        public void CommitEvents(long committedVersion)
        {
            if (committedVersion != Version + 1)
                throw new ArgumentException(nameof(committedVersion), $"聚合根版本提交失败，提交版本与当前版本不匹配！");

            Version = committedVersion;
            uncommittedEvents.Clear();
        }

        public void ReplayEvents(IReadOnlyList<EventStream> eventStreams)
        {
            if (eventStreams?.Count > 0)
            {
                var exceptedVersion = Version + 1;

                foreach (var eventStream in eventStreams)
                {
                    if (eventStream.AggregateRootId != Id?.ToString())
                        throw new ArgumentException(nameof(eventStream), $"聚合根重放事件失败，重放事件聚合根 Id 与当前 Id 不同！");

                    if (eventStream.Version != Version + 1)
                        throw new ArgumentException(nameof(eventStream), $"聚合根重放事件失败，重放事件版本与当前版本不匹配！");

                    foreach (var e in eventStream.Events)
                        HandleEvent(e);

                    Version = eventStream.Version;
                }
            }
        }

        #endregion
    }
}
