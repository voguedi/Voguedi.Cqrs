namespace Voguedi.Domain.Entities
{
    public interface IEntity<TIdentity>
    {
        #region Properties

        TIdentity Id { get; }

        #endregion
    }
}
