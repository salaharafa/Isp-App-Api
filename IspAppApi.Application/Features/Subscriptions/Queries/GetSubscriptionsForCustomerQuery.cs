using IspAppApi.Application.Interfaces;
using IspAppApi.Domain.Entities;
using MediatR;

namespace IspAppApi.Application.Features.Subscriptions.Queries
{
    public record GetSubscriptionsForCustomerQuery(int CustomerId) : IRequest<List<Subscription>>;

    public class GetSubscriptionsForCustomerQueryHandler : IRequestHandler<GetSubscriptionsForCustomerQuery, List<Subscription>>
    {
        private readonly ISubscriptionRepository _subscriptionRepository;

        public GetSubscriptionsForCustomerQueryHandler(ISubscriptionRepository subscriptionRepository)
        {
            _subscriptionRepository = subscriptionRepository;
        }

        public async Task<List<Subscription>> Handle(GetSubscriptionsForCustomerQuery request, CancellationToken cancellationToken)
        {
            return await _subscriptionRepository.GetSubscriptionsForCustomerAsync(request.CustomerId);
        }
    }
}
