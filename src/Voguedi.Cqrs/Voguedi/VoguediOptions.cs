using System;
using System.Collections.Generic;
using System.Reflection;

namespace Voguedi
{
    public class VoguediOptions
    {
        #region Private Fields

        readonly List<IServiceRegistrar> registrars = new List<IServiceRegistrar>();

        #endregion

        #region Internal Fields

        public const string DiagnosticListenerName = "VoguediDiagnosticListener";

        #endregion

        #region Internal Properties

        internal IReadOnlyList<IServiceRegistrar> Registrars => registrars;

        internal Assembly[] Assemblies { get; set; }

        #endregion

        #region Public Properties

        public string DefaultCommandGroupName { get; set; } = "commands";

        public int DefaultCommandTopicQueueCount { get; set; } = 1;

        public string DefaultApplicationMessageGroupName { get; set; } = "applicationMessages";

        public int DefaultApplicationTopicQueueCount { get; set; } = 1;

        public string DefaultEventGroupName { get; set; } = "events";

        public int DefaultEventTopicQueueCount { get; set; } = 1;

        public int AggregateRootExpiration { get; set; } = 3 * 24 * 60 * 60 * 1000;

        public int MemoryQueueExpiration { get; set; } = 5 * 1000;

        public string CommandExecutedResultReplyAddress { get; set; }

        #endregion

        #region Public Methods

        public void Register(IServiceRegistrar registrar)
        {
            if (registrar == null)
                throw new ArgumentNullException(nameof(registrar));

            registrars.Add(registrar);
        }

        public void RegisterAssemblies(params Assembly[] assemblies) => Assemblies = assemblies;

        #endregion
    }
}
