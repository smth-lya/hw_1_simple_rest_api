using FluentValidation;
using HW1.Api.WebAPI.Models;

namespace HW1.Api.WebAPI.Validators;

public class UsersFilterRequestValidator : AbstractValidator<UsersFilterRequest>
{
    public UsersFilterRequestValidator()
    {
        RuleFor(x => x.ToDate)
            .GreaterThanOrEqualTo(x => x.FromDate)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
            .WithMessage("Значение ToDate должно быть больше или равно значению FromDate");
    }
}