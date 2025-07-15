namespace PQBI.MultiTenancy
{
    /// <summary>
    /// Represents a Tenant in the system.
    /// A tenant is a isolated customer for the application
    /// which has it's own users, roles and other application entities.
    /// </summary>
    /// 

    //Every POCO which inherites from this interface is beign subject under object mapper.
    public interface IObjectMapperClonable
    {
    }
}