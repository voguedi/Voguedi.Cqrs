using System;

namespace Voguedi.Infrastructure
{
    public static class Utils
    {
        #region Public Methods

        public static int GetHashCode(string value)
        {
            var hashCode = 23;

            foreach (var item in value)
                hashCode = (hashCode << 5) - hashCode + item;

            if (hashCode < 0)
                hashCode = Math.Abs(hashCode);

            return hashCode;
        }

        public static int GetServerKey(string routingKey, int serverCount) => GetHashCode(routingKey) % serverCount;

        #endregion
    }
}
