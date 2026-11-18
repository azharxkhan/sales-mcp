using SalesMcp.Domain.Common;

namespace SalesMcp.Domain.Customers;

/// <summary>When the payment-term clock starts.</summary>
public enum PaymentTermsBasis
{
    /// <summary>Pay before the order is released. No credit is extended.</summary>
    CashWithOrder = 0,

    /// <summary>Due a number of days after the invoice date, for example 30 days.</summary>
    DaysFromInvoice = 1,

    /// <summary>Due a number of days after the end of the invoice month, common in South Africa ("30 days from statement").</summary>
    DaysFromEndOfMonth = 2,
}

/// <summary>How long a customer has to pay an invoice.</summary>
public readonly record struct PaymentTerms
{
    private PaymentTerms(PaymentTermsBasis basis, int days)
    {
        Basis = basis;
        Days = days;
    }

    /// <summary>Cash with order: no credit.</summary>
    public static PaymentTerms CashWithOrder { get; } = new(PaymentTermsBasis.CashWithOrder, 0);

    /// <summary>When the clock starts.</summary>
    public PaymentTermsBasis Basis { get; }

    /// <summary>Days allowed. 0 for cash with order.</summary>
    public int Days { get; }

    /// <summary>Validates and creates payment terms.</summary>
    public static Result<PaymentTerms> Create(PaymentTermsBasis basis, int days)
    {
        if (!Enum.IsDefined(basis))
        {
            return Result.Failure<PaymentTerms>(CustomerErrors.OptionUnknown);
        }

        var valid = basis == PaymentTermsBasis.CashWithOrder
            ? days == 0
            : days is >= 1 and <= CustomerErrors.MaxPaymentDays;

        return valid
            ? Result.Success(new PaymentTerms(basis, days))
            : Result.Failure<PaymentTerms>(CustomerErrors.PaymentDaysInvalid);
    }

    /// <summary>The date an invoice raised on <paramref name="invoiceDate"/> falls due.</summary>
    public DateOnly DueDate(DateOnly invoiceDate) => Basis switch
    {
        PaymentTermsBasis.DaysFromInvoice => invoiceDate.AddDays(Days),
        PaymentTermsBasis.DaysFromEndOfMonth =>
            new DateOnly(invoiceDate.Year, invoiceDate.Month, DateTime.DaysInMonth(invoiceDate.Year, invoiceDate.Month)).AddDays(Days),
        _ => invoiceDate,
    };

    /// <inheritdoc />
    public override string ToString() => Basis switch
    {
        PaymentTermsBasis.DaysFromInvoice => $"{Days} days from invoice",
        PaymentTermsBasis.DaysFromEndOfMonth => $"{Days} days from end of month",
        _ => "Cash with order",
    };
}
