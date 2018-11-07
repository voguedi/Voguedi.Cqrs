using System;

namespace Voguedi.Utilities
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

        #endregion
    }
}
