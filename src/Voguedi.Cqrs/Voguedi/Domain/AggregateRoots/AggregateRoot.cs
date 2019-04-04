using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Voguedi.Domain.Events;
using Voguedi.Domain.ValueObjects;

namespace Voguedi.Domain.AggregateRoots
{
    public abstract class AggregateRoot<TIdentity> : ValueObject, IAggregateRoot<TIdentity>
    {
        #region Private Fields

        readonly BlockingCollection<IEvent> uncommittedEvents;
        readonly ConcurrentDictionary<Type, MethodInfo> eventHandleMethodMapping;

        #endregion

        #region Ctors

        protected AggregateRoot() { }

        protected AggregateRoot(TIdentity id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(TIdentity));

            Id = id;
            uncommittedEvents = new BlockingCollection<IEvent>(new ConcurrentQueue<IEvent>());
            eventHandleMethodMapping = new ConcurrentDictionary<Type, MethodInfo>();
        }

        #endregion

        #region Private Methods

        void HandleEvent(IEvent e)
        {
            var handleMethod = GetEventHandleMethod(e);

            if (handleMethod != null)
                handleMethod.Invoke(this, new[] { e });
            else
                throw new ArgumentException($"事件处理失败，不存在处理方法。 [AggregateRootType = {GetType()}, AggregateRootId = {Id}, EventType = {e.GetType()}]", nameof(e));
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
                throw new ArgumentException($"无法重复添加类型相同的事件。 [AggregateRootType = {GetType()}, AggregateRootId = {Id}, EventType = {e.GetType()}]", nameof(e));

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

        #region ValueObject

        protected override IEnumerable<object> GetEqualityPropertryValues()
        {
            yield return Id;
        }

        #endregion

        #region IAggregateRoot<TIdentity>

        public TIdentity Id { get; protected set; }

        public long Version { get; protected set; }

        string IAggregateRoot.Id => Id?.ToString();

        public IReadOnlyList<IEvent> GetUncommittedEvents() => uncommittedEvents.ToList();

        public void CommitEvents(long committedVersion)
        {
            if (committedVersion != Version + 1)
                throw new ArgumentException($"版本提交失败，与当前版本不匹配。 [AggregateRootType = {GetType()}, AggregateRootId = {Id}, CurrentVerion = {Version}, CommittedVersion = {committedVersion}]", nameof(committedVersion));

            Version = committedVersion;
            uncommittedEvents.Clear();
        }

        public void ReplayEvents(IReadOnlyList<EventStream> eventStreams)
        {
            if (eventStreams?.Count > 0)
            {
                foreach (var eventStream in eventStreams)
                {
                    if (eventStream.AggregateRootId != Id?.ToString())
                        throw new ArgumentException($"事件重放失败，与当前 Id 不同。 [AggregateRootType = {GetType()}, AggregateRootId = {Id}, EventStream = {eventStream}]", nameof(eventStream));

                    if (eventStream.Version != Version + 1)
                        throw new ArgumentException($"事件重放失败，与当前版本不匹配。 [AggregateRootType = {GetType()}, AggregateRootId = {Id}, Version = {Version}, EventStream = {eventStream}]", nameof(eventStream));

                    foreach (var e in eventStream.Events)
                        HandleEvent(e);

                    Version = eventStream.Version;
                }
            }
        }

        #endregion
    }
}
