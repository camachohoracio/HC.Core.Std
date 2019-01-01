namespace HC.Core.Factories
{
    public interface IServicesFactory
    {
        ServiceInterfaceType GetServiceInstance<ServiceInterfaceType>();
        void RegisterService<ServiceInterfaceType>(ServiceInterfaceType instance);
        ResolveType Resolve<ResolveType>();
    }
}


