using FluentValidation;
using FluentValidation.AspNetCore;
using Models.Dto.V1.Requests;

namespace WebApplication1.Validators;

public class V1AuditLogOrderRequestValidator : AbstractValidator<V1AuditLogOrderRequest>
{
    public V1AuditLogOrderRequestValidator()
    {
        RuleFor(x => x.Orders)
            .NotEmpty();

        RuleForEach(x => x.Orders)
            .SetValidator(new AuditValidator())
            .When(x => x.Orders is not null);
    }

    public class AuditValidator : AbstractValidator<V1AuditLogOrderRequest.LogOrder>
    {
        public AuditValidator()
        {
            RuleFor(x => x.OrderId)
                .GreaterThan(0);
            
            RuleFor(x => x.OrderItemId)
                .GreaterThan(0);
            
            RuleFor(x => x.CustomerId)
                .GreaterThan(0);
            
            RuleFor(x => x.OrderStatus)
                .NotEmpty();
        }
    }
}
