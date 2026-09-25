namespace QuestPDF.FSharp

open QuestPDF.Fluent
open QuestPDF.Infrastructure

/// <summary>Gradient and shadow modifiers of the box around the content.</summary>
[<AutoOpen>]
module BoxEffects =
    /// <summary>
    /// Fills the background with a linear gradient through the colours, evenly spaced, at an angle in degrees; the angle
    /// accepts int, int64, float, float32 or decimal.
    /// </summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.BackgroundLinearGradient(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Color[])"/>.</remarks>
    let inline backgroundGradient degrees (colors: Color list) : Modifier =
        Measured.backgroundGradient (toFloatWith NumberWitness degrees) colors

    /// <summary>
    /// Draws the border with a linear gradient through the colours, evenly spaced, at an angle in degrees; the angle
    /// accepts int, int64, float, float32 or decimal. The border thickness comes from <c>border</c> and its variants.
    /// </summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.BorderLinearGradient(QuestPDF.Infrastructure.IContainer,System.Single,QuestPDF.Infrastructure.Color[])"/>.</remarks>
    let inline borderGradient degrees (colors: Color list) : Modifier =
        Measured.borderGradient (toFloatWith NumberWitness degrees) colors

    /// <summary>
    /// Draws a shadow behind the box, configured by the settings of the <c>Shadow</c> module. Unset settings keep the
    /// QuestPDF defaults: no offset, blur or spread, in grey.
    /// </summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.StyledBoxExtensions.Shadow(QuestPDF.Infrastructure.IContainer,QuestPDF.Infrastructure.BoxShadowStyle)"/>.</remarks>
    let shadow (parts: ShadowPart list) : Modifier =
        closure (fun (Slot container) ->
            let style = BoxShadowStyle ()

            for part in parts do
                part style

            Slot (container.Shadow style))

/// <summary>The settings of a <c>shadow [ ... ]</c>.</summary>
[<RequireQualifiedAccess>]
module Shadow =
    /// <summary>Moves the shadow right by a distance; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Sets <see cref="P:QuestPDF.Infrastructure.BoxShadowStyle.OffsetX"/>.</remarks>
    let inline offsetX value : ShadowPart =
        Measured.shadowOffsetX (len value)

    /// <summary>Moves the shadow down by a distance; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Sets <see cref="P:QuestPDF.Infrastructure.BoxShadowStyle.OffsetY"/>.</remarks>
    let inline offsetY value : ShadowPart =
        Measured.shadowOffsetY (len value)

    /// <summary>
    /// Moves the shadow right and down by two distances; each accepts int, int64, float, float32 or decimal (points) or
    /// Length.
    /// </summary>
    /// <remarks>
    /// Sets <see cref="P:QuestPDF.Infrastructure.BoxShadowStyle.OffsetX"/> and
    /// <see cref="P:QuestPDF.Infrastructure.BoxShadowStyle.OffsetY"/>.
    /// </remarks>
    let inline offset x y : ShadowPart =
        let setX = Measured.shadowOffsetX (len x)
        let setY = Measured.shadowOffsetY (len y)

        fun style ->
            setX style
            setY style

    /// <summary>Softens the shadow edge over a radius; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Sets <see cref="P:QuestPDF.Infrastructure.BoxShadowStyle.Blur"/>.</remarks>
    let inline blur value : ShadowPart =
        Measured.shadowBlur (len value)

    /// <summary>Grows the shadow on every side by a distance; accepts int, int64, float, float32 or decimal (points) or Length.</summary>
    /// <remarks>Sets <see cref="P:QuestPDF.Infrastructure.BoxShadowStyle.Spread"/>.</remarks>
    let inline spread value : ShadowPart =
        Measured.shadowSpread (len value)

    /// <summary>Sets the shadow colour.</summary>
    /// <remarks>Sets <see cref="P:QuestPDF.Infrastructure.BoxShadowStyle.Color"/>.</remarks>
    let color (color: Color) : ShadowPart =
        closure (fun style -> style.Color <- color)
