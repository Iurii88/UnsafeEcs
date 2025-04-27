namespace UnsafeEcs.Core.Entities
{
    public interface IQueryFilter
    {
        public bool Validate(Entity entity);
    }
}