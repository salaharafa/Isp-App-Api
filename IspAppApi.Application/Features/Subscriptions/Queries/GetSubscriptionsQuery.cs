using IspAppApi.Application.Interfaces;
using IspAppApi.Domain.Entities;
using MediatR;

namespace IspAppApi.Application.Features.Subscriptions.Queries
{
    public record GetSubscriptionsQuery() : IRequest<List<Subscription>>;

    public class GetSubscriptionsQueryHandler : IRequestHandler<GetSubscriptionsQuery, List<Subscription>>
    {
        private readonly ISubscriptionRepository _subscriptionRepository;

        public GetSubscriptionsQueryHandler(ISubscriptionRepository subscriptionRepository)
        {
            _subscriptionRepository = subscriptionRepository;
        }

        public async Task<List<Subscription>> Handle(GetSubscriptionsQuery request, CancellationToken cancellationToken)
        {
            return await _subscriptionRepository.GetSubscriptionsAsync();
        }
    }
}
