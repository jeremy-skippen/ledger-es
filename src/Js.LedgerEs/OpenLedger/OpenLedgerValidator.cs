using FluentValidation;

namespace Js.LedgerEs.OpenLedger;

public class OpenLedgerValidator : AbstractValidator<OpenLedger>
{
    public OpenLedgerValidator()
    {
        RuleFor(r => r.LedgerId)
            .NotEmpty()
            .MaximumLength(36);

        RuleFor(r => r.LedgerName)
            .NotEmpty()
            .MaximumLength(255);
    }
}
