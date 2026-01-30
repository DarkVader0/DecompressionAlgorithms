namespace Buhlmann.Zhl16c.Enums;

public enum PlanError : byte
{
    Ok,
    Timeout,
    InappropriateGas,
    NoBailoutGas,
    InsufficientGas,
    ExceedsCns,
    NoDecoGas,
    InvalidInput
}