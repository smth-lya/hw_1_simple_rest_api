using FluentValidation;
using HW1.Api.WebAPI.Models;

namespace HW1.Api.WebAPI.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Имя пользователя обязательно");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Пароль обязателен");
    }
}