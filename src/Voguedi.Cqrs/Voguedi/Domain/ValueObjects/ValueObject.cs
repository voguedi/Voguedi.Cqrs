using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Voguedi.Domain.ValueObjects
{
    public abstract class ValueObject
    {
        #region Protected Methods

        protected virtual IEnumerable<object> GetEqualityPropertryValues() => GetType().GetTypeInfo().GetProperties().Select(item => item.GetValue(this));

        #endregion

        #region Public Methods

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            if (obj == null)
                return false;

            if (GetType() != obj.GetType())
                return false;

            var other = obj as ValueObject;
            return other != null && GetEqualityPropertryValues().SequenceEqual(other.GetEqualityPropertryValues());
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return GetEqualityPropertryValues().Aggregate(17, (hashCode, value) => hashCode * 23 + (value?.GetHashCode() ?? 0));
            }
        }

        public static bool operator ==(ValueObject left, ValueObject right)
        {
            if (left is null)
                return right is null;

            return left.Equals(right);
        }

        public static bool operator !=(ValueObject left, ValueObject right) => !(left == right);

        #endregion
    }
}
