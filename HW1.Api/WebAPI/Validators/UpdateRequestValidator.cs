using FluentValidation;
using HW1.Api.WebAPI.Models;

namespace HW1.Api.WebAPI.Validators;

public class UpdateRequestValidator : AbstractValidator<UpdateRequest>
{
    public UpdateRequestValidator()
    {
        RuleFor(x => x.Username)
            .MinimumLength(3).When(x => !string.IsNullOrEmpty(x.Username))
            .WithMessage("Минимальная длина - 3 символа")
            .MaximumLength(20).WithMessage("Максимальная длина - 20 символов")
            .Matches("^[a-zA-Z0-9_]+$").WithMessage("Можно использовать только буквы, цифры и подчеркивание");

        RuleFor(x => x.Password)
            .MinimumLength(6).When(x => !string.IsNullOrEmpty(x.Password))
            .WithMessage("Минимальная длина пароля - 6 символов");
    }
}