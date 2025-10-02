namespace ProyectoInfoAplicada.Repository
{
    public interface ICustomerRepository
    {
        Task<bool> ExistsCustomerIdAsync(int customerId, CancellationToken ct = default);
    }
}
