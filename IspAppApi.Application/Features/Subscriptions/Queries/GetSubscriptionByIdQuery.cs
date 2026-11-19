using IspAppApi.Application.Interfaces;
using IspAppApi.Domain.Entities;
using MediatR;

namespace IspAppApi.Application.Features.Subscriptions.Queries
{
    public record GetSubscriptionByIdQuery(int Id) : IRequest<Subscription?>;

    public class GetSubscriptionByIdQueryHandler : IRequestHandler<GetSubscriptionByIdQuery, Subscription?>
    {
        private readonly ISubscriptionRepository _subscriptionRepository;

        public GetSubscriptionByIdQueryHandler(ISubscriptionRepository subscriptionRepository)
        {
            _subscriptionRepository = subscriptionRepository;
        }

        public async Task<Subscription?> Handle(GetSubscriptionByIdQuery request, CancellationToken cancellationToken)
        {
            return await _subscriptionRepository.GetSubscriptionAsync(request.Id);
        }
    }
}
