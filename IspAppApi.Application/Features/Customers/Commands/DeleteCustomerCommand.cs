using IspAppApi.Application.Interfaces;
using MediatR;

namespace IspAppApi.Application.Features.Customers.Commands
{
    public record DeleteCustomerCommand(int Id) : IRequest<bool>;

    public class DeleteCustomerCommandHandler : IRequestHandler<DeleteCustomerCommand, bool>
    {
        private readonly ICustomerRepository _customerRepository;

        public DeleteCustomerCommandHandler(ICustomerRepository customerRepository)
        {
            _customerRepository = customerRepository;
        }

        public async Task<bool> Handle(DeleteCustomerCommand request, CancellationToken cancellationToken)
        {
            var customer = await _customerRepository.GetCustomerAsync(request.Id);
            if (customer == null)
            {
                return false;
            }

            await _customerRepository.DeleteCustomerAsync(customer);
            return true;
        }
    }
}
