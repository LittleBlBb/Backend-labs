using FluentValidation;
using Models.Dto.V1.Requests;

namespace WebApplication1.Validators;

public class V1QueryOrdersRequestValidator: AbstractValidator<V1QueryOrdersRequest>
{
    public V1QueryOrdersRequestValidator()
    {
        RuleFor(x => x)
            .NotEmpty();
    }

    public class QueryOrderValidator : AbstractValidator<V1QueryOrdersRequest>
    {
        public QueryOrderValidator()
        {
            RuleForEach(x => x.Ids).GreaterThan(0);
            
            RuleForEach(x => x.CustomerIds).GreaterThan(0);
            
            RuleFor(x => x.Page).GreaterThan(0);
            
            RuleFor(x => x.PageSize).GreaterThan(0);
            
            RuleFor(x => x.IncludeOrderItems).NotEmpty();
        }
    }
}