using Limousine.Core.Entities;
using Limousine.Core.Interfaces.Repositories;
using Limousine.Infrastracture.Data;
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace Limousine.Infrastracture.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private readonly Hashtable _repositories;
        private IVehicleRepository? _vehicleRepository;
        private IBookingRequestRepository? _bookingRequestRepository;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
            _repositories = new Hashtable();
        }

        public IVehicleRepository Vehicles => _vehicleRepository ??= new VehicleRepository(_context);
        public IBookingRequestRepository BookingRequests => _bookingRequestRepository ??= new BookingRequestRepository(_context);

        public IGenericRepository<T> Repository<T>() where T : BaseEntitiy
        {
            var type = typeof(T).Name;

            if (!_repositories.ContainsKey(type))
            {
                var repositoryType = typeof(GenericRepository<>);
                var repositoryInstance = Activator.CreateInstance(repositoryType.MakeGenericType(typeof(T)), _context);
                _repositories.Add(type, repositoryInstance);
            }

            return (IGenericRepository<T>)_repositories[type]!;
        }

        public async Task<int> CompleteAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
