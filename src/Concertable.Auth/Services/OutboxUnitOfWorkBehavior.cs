using Concertable.Auth.Data;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Concertable.Messaging.Infrastructure.Outbox;

namespace Concertable.Auth.Services;

internal interface IOutboxUnitOfWorkBehavior : IOutboxUnitOfWorkBehavior<AuthDbContext>;

internal sealed class OutboxUnitOfWorkBehavior(AuthDbContext context, IDbContextAccessor accessor)
    : OutboxUnitOfWorkBehavior<AuthDbContext>(context, accessor), IOutboxUnitOfWorkBehavior;
