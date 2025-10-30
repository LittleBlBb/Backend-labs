using FluentValidation;

namespace WebApplication1.Validators;

public class ValidatorFactory(IServiceProvider serviceProvider)
{
    public IValidator<T> GetValidator<T>()
    {
        return serviceProvider.GetRequiredService<IValidator<T>>()!;
    }
}