using IspAppApi.Application.Interfaces;
using IspAppApi.Domain.Entities;
using MediatR;

namespace IspAppApi.Application.Features.Customers.Queries
{
    public record GetCustomerByIdQuery(int Id) : IRequest<Customer?>;

    public class GetCustomerByIdQueryHandler : IRequestHandler<GetCustomerByIdQuery, Customer?>
    {
        private readonly ICustomerRepository _customerRepository;

        public GetCustomerByIdQueryHandler(ICustomerRepository customerRepository)
        {
            _customerRepository = customerRepository;
        }

        public async Task<Customer?> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
        {
            return await _customerRepository.GetCustomerAsync(request.Id);
        }
    }
}
