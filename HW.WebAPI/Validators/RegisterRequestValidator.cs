using FluentValidation;
using HW.WebAPI.Models;

namespace HW.WebAPI.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    private readonly IUserService _userService;
    private readonly ILogger<RegisterRequestValidator> _logger;

    public RegisterRequestValidator(IUserService userService, ILogger<RegisterRequestValidator> logger)
    {
        _userService = userService;
        _logger = logger;

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Имя пользователя обязательно")
            .MinimumLength(3).WithMessage("Минимальная длина - 3 символа")
            .MaximumLength(20).WithMessage("Максимальная длина - 20 символов")
            .Matches("^[a-zA-Z0-9_]+$").WithMessage("Можно использовать только буквы, цифры и подчеркивание")
            .Must(BeUniqueUsername).WithMessage("Имя пользователя уже занято");
        
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Пароль обязателен")
            .MinimumLength(6).WithMessage("Минимальная длина пароля - 6 символов");
    }

    // TODO: временно синхронная проверка через .Result для обхода AsyncValidatorInvokedSynchronouslyException
    private bool BeUniqueUsername(string username)
    {
        return _userService.GetUserByUsernameAsync(username).Result == null;
    }
}