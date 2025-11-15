using Lintellect.Api.Application.Common.Interfaces;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Lintellect.Api.Application.Messages.Commands.Analysis;



public record DeleteAnalysisHistoryCommand(Guid JobId) : IRequest;

public class DeleteAnalysisHistoryCommandHandler(IApplicationDbContext context) : IRequestHandler<DeleteAnalysisHistoryCommand>
{

    public async ValueTask<Unit> Handle(DeleteAnalysisHistoryCommand request, CancellationToken cancellationToken)
    {
        var query = context.AnalysisJobs.AsQueryable();
        if (request.JobId != Guid.Empty)
        {
            query = query.Where(x => x.Id == request.JobId);
        }

        await query.ExecuteDeleteAsync(cancellationToken);

        return Unit.Value;
    }
}
