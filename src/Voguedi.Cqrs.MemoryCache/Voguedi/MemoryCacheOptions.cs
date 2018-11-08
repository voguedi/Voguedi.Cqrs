using System;

namespace Voguedi
{
    public sealed class MemoryCacheOptions
    {
        #region Public Properties

        public TimeSpan SlidingExpiration { get; set; } = TimeSpan.FromDays(3);

        public DateTimeOffset? AbsoluteExpiration { get; set; }

        public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }

        #endregion
    }
}
