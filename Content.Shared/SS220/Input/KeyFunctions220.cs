// EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Input;

namespace Content.Shared.SS220.Input;

[KeyFunctions]
public static class KeyFunctions220
{
    public static readonly BoundKeyFunction CalculatorType0 = "CalculatorType0";
    public static readonly BoundKeyFunction CalculatorType1 = "CalculatorType1";
    public static readonly BoundKeyFunction CalculatorType2 = "CalculatorType2";
    public static readonly BoundKeyFunction CalculatorType3 = "CalculatorType3";
    public static readonly BoundKeyFunction CalculatorType4 = "CalculatorType4";
    public static readonly BoundKeyFunction CalculatorType5 = "CalculatorType5";
    public static readonly BoundKeyFunction CalculatorType6 = "CalculatorType6";
    public static readonly BoundKeyFunction CalculatorType7 = "CalculatorType7";
    public static readonly BoundKeyFunction CalculatorType8 = "CalculatorType8";
    public static readonly BoundKeyFunction CalculatorType9 = "CalculatorType9";
    public static readonly BoundKeyFunction CalculatorTypeDot = "CalculatorTypeDot";
    public static readonly BoundKeyFunction CalculatorTypeAdd = "CalculatorTypeAdd";
    public static readonly BoundKeyFunction CalculatorTypeSubtract = "CalculatorTypeSubtract";
    public static readonly BoundKeyFunction CalculatorTypeMultiply = "CalculatorTypeMultiply";
    public static readonly BoundKeyFunction CalculatorTypeDivide = "CalculatorTypeDivide";
    public static readonly BoundKeyFunction CalculatorEnter = "CalculatorEnter";
    public static readonly BoundKeyFunction CalculatorClear = "CalculatorClear";

    public static void AddCalculatorKeys(IInputCmdContext context)
    {
        context.AddFunction(CalculatorType0);
        context.AddFunction(CalculatorType1);
        context.AddFunction(CalculatorType2);
        context.AddFunction(CalculatorType3);
        context.AddFunction(CalculatorType4);
        context.AddFunction(CalculatorType5);
        context.AddFunction(CalculatorType6);
        context.AddFunction(CalculatorType7);
        context.AddFunction(CalculatorType8);
        context.AddFunction(CalculatorType9);
        context.AddFunction(CalculatorTypeDot);
        context.AddFunction(CalculatorTypeAdd);
        context.AddFunction(CalculatorTypeSubtract);
        context.AddFunction(CalculatorTypeMultiply);
        context.AddFunction(CalculatorTypeDivide);
        context.AddFunction(CalculatorEnter);
        context.AddFunction(CalculatorClear);
    }
}
