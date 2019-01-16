using System;
using System.Collections.Generic;
using System.Reflection;
using AspectCore.Configuration;

namespace Voguedi
{
    public sealed class VoguediOptions
    {
        #region Private Fields

        readonly List<IServiceRegistrar> registrars = new List<IServiceRegistrar>();

        #endregion

        #region Internal Properties

        internal IReadOnlyList<IServiceRegistrar> Registrars => registrars;

        internal Assembly[] Assemblies { get; set; }

        internal Action<IAspectConfiguration> AspectConfig { get; set; }

        #endregion

        #region Public Properties

        public string DefaultCommandGroupName { get; set; } = "commands";

        public string DefaultEventGroupName { get; set; } = "events";

        public int DefaultTopicQueueCount { get; set; } = 1;

        public int AggregateRootExpiration { get; set; } = 3 * 24 * 60 * 60 * 1000;

        public int MemoryQueueExpiration { get; set; } = 5 * 1000;

        #endregion

        #region Public Methods

        public void Register(IServiceRegistrar registrar)
        {
            if (registrar == null)
                throw new ArgumentNullException(nameof(registrar));

            registrars.Add(registrar);
        }

        public void RegisterAssemblies(params Assembly[] assemblies) => Assemblies = assemblies;

        public void RegisterAspectCore(Action<IAspectConfiguration> aspectConfig) => AspectConfig = aspectConfig;

        #endregion
    }
}
