using System;
using System.Collections.Generic;

namespace Voguedi
{
    public sealed class VoguediOptions
    {
        #region Private Fields

        readonly List<IServiceRegistrar> registrars = new List<IServiceRegistrar>();

        #endregion

        #region Internal Properties

        internal IReadOnlyList<IServiceRegistrar> Registrars => registrars;

        #endregion

        #region Public Properties

        public string DefaultCommandGroupName { get; set; } = "CommandGroup";

        public string DefaultEventGroupName { get; set; } = "EventGroup";

        public int DefaultTopicQueueCount { get; set; } = 1;

        public int MemoryQueueActiveExpiration { get; set; } = 5000;

        #endregion

        #region Public Methods

        public void Register(IServiceRegistrar registrar)
        {
            if (registrar == null)
                throw new ArgumentNullException(nameof(registrar));

            registrars.Add(registrar);
        }

        #endregion
    }
}
