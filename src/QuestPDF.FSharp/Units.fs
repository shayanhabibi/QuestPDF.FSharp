namespace QuestPDF.FSharp

open System.ComponentModel
open QuestPDF.Infrastructure

/// <summary>A length in a QuestPDF unit. The unit reaches QuestPDF unconverted.</summary>
/// <remarks>Build one with a number times a unit: <c>5 * mm</c>, <c>2.5 * cm</c>.</remarks>
[<Struct>]
type Length =
    {
        /// <summary>The magnitude, in <c>Unit</c>.</summary>
        Value: float32
        Unit: Unit
    }

/// <summary>A unit of length. A number times a unit is a <see cref="T:QuestPDF.FSharp.Length"/>.</summary>
[<Struct>]
type LengthUnit =
    | LengthUnit of Unit

    /// <summary>The length of an integer count of the unit.</summary>
    static member (*)(value: int, unit: LengthUnit) : Length =
        let (LengthUnit questPdfUnit) = unit

        { Value = float32 value
          Unit = questPdfUnit }

    /// <summary>The length of a count of the unit.</summary>
    static member (*)(value: float, unit: LengthUnit) : Length =
        let (LengthUnit questPdfUnit) = unit

        { Value = float32 value
          Unit = questPdfUnit }

    /// <summary>The length of a count of the unit.</summary>
    static member (*)(value: float32, unit: LengthUnit) : Length =
        let (LengthUnit questPdfUnit) = unit
        { Value = value; Unit = questPdfUnit }

    /// <summary>The length of an integer count of the unit.</summary>
    static member (*)(value: int64, unit: LengthUnit) : Length =
        let (LengthUnit questPdfUnit) = unit

        { Value = float32 value
          Unit = questPdfUnit }

    /// <summary>The length of a count of the unit.</summary>
    static member (*)(value: decimal, unit: LengthUnit) : Length =
        let (LengthUnit questPdfUnit) = unit

        { Value = float32 value
          Unit = questPdfUnit }

/// <summary>The QuestPDF units of length.</summary>
[<AutoOpen>]
module Units =
    /// <summary>Points: 1/72 inch, the unit of bare numbers.</summary>
    let pt = LengthUnit Unit.Point

    /// <summary>Millimetres.</summary>
    let mm = LengthUnit Unit.Millimetre

    /// <summary>Centimetres.</summary>
    let cm = LengthUnit Unit.Centimetre

    /// <summary>Inches.</summary>
    let inch = LengthUnit Unit.Inch

    /// <summary>Mils: 1/1000 inch.</summary>
    let mil = LengthUnit Unit.Mil

    /// <summary>Feet.</summary>
    let feet = LengthUnit Unit.Feet

/// <summary>
/// The overload set behind <c>len</c>: an int, int64, float, float32 or decimal is a number of points, and a
/// <c>Length</c> is unchanged.
/// </summary>
[<EditorBrowsable(EditorBrowsableState.Never)>]
type LengthWitness =
    | LengthWitness

    /// <summary>An integer number of points.</summary>
    static member ToLength(witness: LengthWitness, value: int) : Length =
        { Value = float32 value
          Unit = Unit.Point }

    /// <summary>A number of points.</summary>
    static member ToLength(witness: LengthWitness, value: float) : Length =
        { Value = float32 value
          Unit = Unit.Point }

    /// <summary>A number of points.</summary>
    static member ToLength(witness: LengthWitness, value: float32) : Length =
        { Value = value; Unit = Unit.Point }

    /// <summary>An integer number of points.</summary>
    static member ToLength(witness: LengthWitness, value: int64) : Length =
        { Value = float32 value
          Unit = Unit.Point }

    /// <summary>A number of points.</summary>
    static member ToLength(witness: LengthWitness, value: decimal) : Length =
        { Value = float32 value
          Unit = Unit.Point }

    static member ToLength(witness: LengthWitness, value: Length) : Length = value

/// <summary>The overload set behind numeric style arguments: int, int64, float, float32 and decimal.</summary>
[<EditorBrowsable(EditorBrowsableState.Never)>]
type NumberWitness =
    | NumberWitness

    static member ToFloat(witness: NumberWitness, value: int) : float =
        float value

    static member ToFloat(witness: NumberWitness, value: int64) : float =
        float value

    static member ToFloat(witness: NumberWitness, value: float) : float = value

    static member ToFloat(witness: NumberWitness, value: float32) : float =
        float value

    static member ToFloat(witness: NumberWitness, value: decimal) : float =
        float value

/// <summary>Conversion of length and numeric arguments.</summary>
[<AutoOpen>]
module LengthOps =
    /// <summary>Resolves a <c>ToLength</c> overload on the witness type or on the value type.</summary>
    [<EditorBrowsable(EditorBrowsableState.Never)>]
    let inline toLengthWith (witness: ^W) (value: ^a) : Length =
        ((^W or ^a): (static member ToLength: ^W * ^a -> Length) (witness, value))

    /// <summary>Resolves a <c>ToFloat</c> overload on the witness type or on the value type.</summary>
    [<EditorBrowsable(EditorBrowsableState.Never)>]
    let inline toFloatWith (witness: ^W) (value: ^a) : float =
        ((^W or ^a): (static member ToFloat: ^W * ^a -> float) (witness, value))

    /// <summary>The length of an int, int64, float, float32 or decimal (in points) or of a <c>Length</c>.</summary>
    let inline len value =
        toLengthWith LengthWitness value

/// <summary>Operations on lengths.</summary>
[<RequireQualifiedAccess>]
module Length =
    /// <summary>The length in points, computed in float32 with the conversion factors QuestPDF uses.</summary>
    /// <remarks>
    /// The same length in two units can differ in the last float32 digit: <c>2 * cm</c> is 56.692913 pt and
    /// <c>20 * mm</c> is 56.692917 pt.
    /// </remarks>
    let points (length: Length) : float32 =
        let factor =
            match length.Unit with
            | Unit.Point -> 1f
            | Unit.Inch -> 72f
            | Unit.Feet -> 72f * 12f
            | Unit.Mil -> 72f / 1000f
            | Unit.Centimetre -> 72f / 2.54f
            | Unit.Millimetre -> 72f / 25.4f
            | Unit.Meter -> 72f / 0.0254f
            | other -> invalidArg (nameof length) $"Unknown unit {other}"

        length.Value * factor
