using System.Data;
using Dapper;

namespace ProyectoInfoAplicada.Repository
{
    public class CustomerRepositoryDapper : ICustomerRepository
    {

        private readonly IDbConnection _db;
        private readonly ILogger<CustomerRepositoryDapper> _logger;


        public CustomerRepositoryDapper(IDbConnection db, ILogger<CustomerRepositoryDapper> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<bool> ExistsCustomerIdAsync(int customerId, CancellationToken ct = default)
        {
            const string sql = @"SELECT 1 FROM Sales.Customer WHERE CustomerID = @Id";
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var res = await _db.QueryFirstOrDefaultAsync<int?>(new CommandDefinition(sql, new { Id = customerId }, cancellationToken: ct));
                
                return res.HasValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comprobando existencia de CustomerId={customerId}", customerId);
                return false; // o lanzar, según prefieras la política de errores
            }
        }
    }
}
