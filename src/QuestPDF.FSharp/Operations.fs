namespace QuestPDF.FSharp

open System
open QuestPDF.Fluent
open QuestPDF.Infrastructure

/// <summary>Page numbering of a merged document, listed in <c>Pdf.merge [ ... ]</c>.</summary>
[<RequireQualifiedAccess>]
module Merge =
    /// <summary>
    /// Numbers the pages of each merged document from 1, so page number text reflects the document it belongs to. This
    /// is the default.
    /// </summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Infrastructure.MergedDocument.UseOriginalPageNumbers"/>.</remarks>
    let originalPageNumbers: DocumentPart =
        DocumentPart (MergeSetter (fun merged -> merged.UseOriginalPageNumbers ()))

    /// <summary>Numbers the pages of the merged document consecutively, across the boundaries of its documents.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Infrastructure.MergedDocument.UseContinuousPageNumbers"/>.</remarks>
    let continuousPageNumbers: DocumentPart =
        DocumentPart (MergeSetter (fun merged -> merged.UseContinuousPageNumbers ()))

/// <summary>Builds merged documents for <c>Pdf.merge</c>.</summary>
module internal Merging =
    let merge (parts: DocumentPart list) (documents: IDocument list) : MergedDocument =
        let metadata = DocumentMetadata ()
        let settings = DocumentSettings ()
        let mutable merged = Document.Merge (documents)

        for part in parts do
            match part.Kind with
            | MetadataSetter set -> set metadata
            | SettingsSetter set -> set settings
            | MergeSetter set -> merged <- set merged
            | PageDefinition _ -> invalidArg (nameof parts) "Pages belong in document; Pdf.merge takes documents."

        merged.WithMetadata(metadata).WithSettings (settings)

/// <summary>An option of an overlay or underlay, listed in <c>PdfFile.overlay path [ ... ]</c>.</summary>
type PdfLayerPart = DocumentOperation.LayerConfiguration -> unit

/// <summary>An option of an attachment, listed in <c>PdfFile.attach path [ ... ]</c>.</summary>
type AttachmentPart = DocumentOperation.DocumentAttachment -> unit

/// <summary>A password or permission of a 40-bit encryption, listed in <c>PdfFile.encrypt40 [ ... ]</c>.</summary>
type Encryption40Part = DocumentOperation.Encryption40Bit -> unit

/// <summary>A password or permission of a 128-bit encryption, listed in <c>PdfFile.encrypt128 [ ... ]</c>.</summary>
type Encryption128Part = DocumentOperation.Encryption128Bit -> unit

/// <summary>A password or permission of a 256-bit encryption, listed in <c>PdfFile.encrypt256 [ ... ]</c>.</summary>
type Encryption256Part = DocumentOperation.Encryption256Bit -> unit

/// <summary>
/// A PDF file and the qpdf operations to apply to it on <c>PdfFile.save</c>. Each operation returns a new value; the
/// source file is read on save.
/// </summary>
/// <remarks>Wraps a QuestPDF <see cref="T:QuestPDF.Fluent.DocumentOperation"/>.</remarks>
[<Sealed>]
type PdfFile internal (path: string, password: string, operations: (DocumentOperation -> DocumentOperation) list) =
    member internal _.Path = path
    member internal _.Password = password

    /// The operations in reverse order of application.
    member internal _.Operations = operations

    member internal _.Then(operation: DocumentOperation -> DocumentOperation) =
        PdfFile (path, password, operation :: operations)

/// <summary>Options of an overlay or underlay. Each takes a qpdf page selector such as <c>"1-3,r1"</c>.</summary>
[<RequireQualifiedAccess>]
module PdfLayer =
    /// <summary>Sets the pages of the output that receive the layer. The default is every page.</summary>
    /// <remarks>Sets <see cref="P:QuestPDF.Fluent.DocumentOperation.LayerConfiguration.TargetPages"/>.</remarks>
    let targetPages (selector: string) : PdfLayerPart =
        closure (fun layer -> layer.TargetPages <- selector)

    /// <summary>Sets the pages of the layer file used first, in order. The default is every page.</summary>
    /// <remarks>Sets <see cref="P:QuestPDF.Fluent.DocumentOperation.LayerConfiguration.SourcePages"/>.</remarks>
    let sourcePages (selector: string) : PdfLayerPart =
        closure (fun layer -> layer.SourcePages <- selector)

    /// <summary>Sets the pages of the layer file that repeat once the source pages are used up.</summary>
    /// <remarks>Sets <see cref="P:QuestPDF.Fluent.DocumentOperation.LayerConfiguration.RepeatSourcePages"/>.</remarks>
    let repeatSourcePages (selector: string) : PdfLayerPart =
        closure (fun layer -> layer.RepeatSourcePages <- selector)

/// <summary>Options of a file attachment.</summary>
[<RequireQualifiedAccess>]
module Attachment =
    /// <summary>Sets the key of the attachment in the PDF file. The default is the file name.</summary>
    /// <remarks>Sets <see cref="P:QuestPDF.Fluent.DocumentOperation.DocumentAttachment.Key"/>.</remarks>
    let key (value: string) : AttachmentPart =
        closure (fun attachment -> attachment.Key <- value)

    /// <summary>Sets the name that PDF viewers show and save the attachment under. The default is the file name.</summary>
    /// <remarks>Sets <see cref="P:QuestPDF.Fluent.DocumentOperation.DocumentAttachment.AttachmentName"/>.</remarks>
    let name (value: string) : AttachmentPart =
        closure (fun attachment -> attachment.AttachmentName <- value)

    /// <summary>Sets the description.</summary>
    /// <remarks>Sets <see cref="P:QuestPDF.Fluent.DocumentOperation.DocumentAttachment.Description"/>.</remarks>
    let description (value: string) : AttachmentPart =
        closure (fun attachment -> attachment.Description <- value)

    /// <summary>Sets the MIME type, such as <c>text/plain</c>.</summary>
    /// <remarks>Sets <see cref="P:QuestPDF.Fluent.DocumentOperation.DocumentAttachment.MimeType"/>.</remarks>
    let mimeType (value: string) : AttachmentPart =
        closure (fun attachment -> attachment.MimeType <- value)

    /// <summary>Sets the creation date. The default is the creation time of the file.</summary>
    /// <remarks>Sets <see cref="P:QuestPDF.Fluent.DocumentOperation.DocumentAttachment.CreationDate"/>.</remarks>
    let created (date: DateTime) : AttachmentPart =
        closure (fun attachment -> attachment.CreationDate <- Nullable date)

    /// <summary>Sets the modification date. The default is the last write time of the file.</summary>
    /// <remarks>Sets <see cref="P:QuestPDF.Fluent.DocumentOperation.DocumentAttachment.ModificationDate"/>.</remarks>
    let modified (date: DateTime) : AttachmentPart =
        closure (fun attachment -> attachment.ModificationDate <- Nullable date)

    /// <summary>
    /// Sets whether the attachment replaces an attachment of the same key. The default is true; with false, a
    /// duplicate key raises an error on save.
    /// </summary>
    /// <remarks>Sets <see cref="P:QuestPDF.Fluent.DocumentOperation.DocumentAttachment.Replace"/>.</remarks>
    let replace (enabled: bool) : AttachmentPart =
        closure (fun attachment -> attachment.Replace <- enabled)

    /// <summary>Sets the relationship of the file to the document, as PDF/A-3 files declare it.</summary>
    /// <remarks>Sets <see cref="P:QuestPDF.Fluent.DocumentOperation.DocumentAttachment.Relationship"/>.</remarks>
    let relationship (value: DocumentAttachmentRelationship) : AttachmentPart =
        closure (fun attachment -> attachment.Relationship <- Nullable value)

/// <summary>
/// Passwords and permissions of an encryption, listed in <c>PdfFile.encrypt40</c>, <c>PdfFile.encrypt128</c> or
/// <c>PdfFile.encrypt256</c>. Every permission is allowed by default. A list bound apart from the call needs its part
/// type, as in <c>let common : Encryption256Part list = [ ... ]</c>.
/// </summary>
[<RequireQualifiedAccess>]
module Encryption =
    /// <summary>Sets the password that opens the file with the permissions of the encryption.</summary>
    /// <remarks>Sets <see cref="P:QuestPDF.Fluent.DocumentOperation.EncryptionBase.UserPassword"/>.</remarks>
    let userPassword (password: string) : 'Encryption -> unit when 'Encryption :> DocumentOperation.EncryptionBase =
        closure (fun (encryption: 'Encryption) -> encryption.UserPassword <- password)

    /// <summary>Sets the password that opens the file with every permission.</summary>
    /// <remarks>Sets <see cref="P:QuestPDF.Fluent.DocumentOperation.EncryptionBase.OwnerPassword"/>.</remarks>
    let ownerPassword (password: string) : 'Encryption -> unit when 'Encryption :> DocumentOperation.EncryptionBase =
        closure (fun (encryption: 'Encryption) -> encryption.OwnerPassword <- password)

    /// <summary>Allows or denies adding annotations and signatures.</summary>
    /// <remarks>
    /// Sets <see cref="P:QuestPDF.Fluent.DocumentOperation.Encryption40Bit.AllowAnnotation"/>,
    /// <see cref="P:QuestPDF.Fluent.DocumentOperation.Encryption128Bit.AllowAnnotation"/> or
    /// <see cref="P:QuestPDF.Fluent.DocumentOperation.Encryption256Bit.AllowAnnotation"/>.
    /// </remarks>
    let inline allowAnnotation (allowed: bool) : ^Encryption -> unit =
        fun (encryption: ^Encryption) -> (^Encryption: (member set_AllowAnnotation: bool -> unit) (encryption, allowed))

    /// <summary>Allows or denies copying text and graphics.</summary>
    /// <remarks>
    /// Sets <see cref="P:QuestPDF.Fluent.DocumentOperation.Encryption40Bit.AllowContentExtraction"/>,
    /// <see cref="P:QuestPDF.Fluent.DocumentOperation.Encryption128Bit.AllowContentExtraction"/> or
    /// <see cref="P:QuestPDF.Fluent.DocumentOperation.Encryption256Bit.AllowContentExtraction"/>.
    /// </remarks>
    let inline allowContentExtraction (allowed: bool) : ^Encryption -> unit =
        fun (encryption: ^Encryption) -> (^Encryption: (member set_AllowContentExtraction: bool -> unit) (encryption, allowed))

    /// <summary>Allows or denies modifying the document.</summary>
    /// <remarks>
    /// Sets <see cref="P:QuestPDF.Fluent.DocumentOperation.Encryption40Bit.AllowModification"/>,
    /// <see cref="P:QuestPDF.Fluent.DocumentOperation.Encryption128Bit.AllowModification"/> or
    /// <see cref="P:QuestPDF.Fluent.DocumentOperation.Encryption256Bit.AllowModification"/>.
    /// </remarks>
    let inline allowModification (allowed: bool) : ^Encryption -> unit =
        fun (encryption: ^Encryption) -> (^Encryption: (member set_AllowModification: bool -> unit) (encryption, allowed))

    /// <summary>Allows or denies printing.</summary>
    /// <remarks>
    /// Sets <see cref="P:QuestPDF.Fluent.DocumentOperation.Encryption40Bit.AllowPrinting"/>,
    /// <see cref="P:QuestPDF.Fluent.DocumentOperation.Encryption128Bit.AllowPrinting"/> or
    /// <see cref="P:QuestPDF.Fluent.DocumentOperation.Encryption256Bit.AllowPrinting"/>.
    /// </remarks>
    let inline allowPrinting (allowed: bool) : ^Encryption -> unit =
        fun (encryption: ^Encryption) -> (^Encryption: (member set_AllowPrinting: bool -> unit) (encryption, allowed))

    /// <summary>Allows or denies inserting, rotating and deleting pages; 128-bit and 256-bit encryption only.</summary>
    /// <remarks>
    /// Sets <see cref="P:QuestPDF.Fluent.DocumentOperation.Encryption128Bit.AllowAssembly"/> or
    /// <see cref="P:QuestPDF.Fluent.DocumentOperation.Encryption256Bit.AllowAssembly"/>.
    /// </remarks>
    let inline allowAssembly (allowed: bool) : ^Encryption -> unit =
        fun (encryption: ^Encryption) -> (^Encryption: (member set_AllowAssembly: bool -> unit) (encryption, allowed))

    /// <summary>Allows or denies filling form fields; 128-bit and 256-bit encryption only.</summary>
    /// <remarks>
    /// Sets <see cref="P:QuestPDF.Fluent.DocumentOperation.Encryption128Bit.AllowFillingForms"/> or
    /// <see cref="P:QuestPDF.Fluent.DocumentOperation.Encryption256Bit.AllowFillingForms"/>.
    /// </remarks>
    let inline allowFillingForms (allowed: bool) : ^Encryption -> unit =
        fun (encryption: ^Encryption) -> (^Encryption: (member set_AllowFillingForms: bool -> unit) (encryption, allowed))

    /// <summary>Encrypts the metadata along with the content, or leaves it readable; 128-bit and 256-bit encryption only.</summary>
    /// <remarks>
    /// Sets <see cref="P:QuestPDF.Fluent.DocumentOperation.Encryption128Bit.EncryptMetadata"/> or
    /// <see cref="P:QuestPDF.Fluent.DocumentOperation.Encryption256Bit.EncryptMetadata"/>.
    /// </remarks>
    let inline encryptMetadata (enabled: bool) : ^Encryption -> unit =
        fun (encryption: ^Encryption) -> (^Encryption: (member set_EncryptMetadata: bool -> unit) (encryption, enabled))

/// <summary>
/// Operations on existing PDF files, run by qpdf: <c>PdfFile.load path |&gt; PdfFile.takePages "1-3" |&gt; PdfFile.save out</c>.
/// Page selectors follow the qpdf syntax: <c>"1,3"</c>, ranges such as <c>"4-10"</c>, <c>r1</c> for the last page
/// and <c>x</c> for exclusions, as in <c>"1-10,x3-4"</c>.
/// </summary>
[<RequireQualifiedAccess>]
module PdfFile =
    let private configured (parts: ('Options -> unit) list) (options: 'Options) =
        for part in parts do
            part options

        options

    /// <summary>A PDF file at a path, read on save.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.DocumentOperation.LoadFile(System.String,System.String)"/>.</remarks>
    let load (path: string) : PdfFile =
        PdfFile (path, null, [])

    /// <summary>A password-protected PDF file at a path, read on save.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.DocumentOperation.LoadFile(System.String,System.String)"/>.</remarks>
    let loadProtected (password: string) (path: string) : PdfFile =
        PdfFile (path, password, [])

    /// <summary>Keeps the pages of a page selector, in the order of the selector.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.DocumentOperation.TakePages(System.String)"/>.</remarks>
    let takePages (selector: string) (file: PdfFile) : PdfFile =
        file.Then (fun operation -> operation.TakePages selector)

    /// <summary>Appends every page of the PDF file at a path.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.DocumentOperation.MergeFile(System.String,System.String)"/>.</remarks>
    let merge (path: string) (file: PdfFile) : PdfFile =
        file.Then (fun operation -> operation.MergeFile path)

    /// <summary>Appends the pages of a page selector from the PDF file at a path.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.DocumentOperation.MergeFile(System.String,System.String)"/>.</remarks>
    let mergePages (path: string) (selector: string) (file: PdfFile) : PdfFile =
        file.Then (fun operation -> operation.MergeFile (path, selector))

    /// <summary>Draws the pages of the PDF file at a path over the pages of the file.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.DocumentOperation.OverlayFile(QuestPDF.Fluent.DocumentOperation.LayerConfiguration)"/>.</remarks>
    let overlay (path: string) (parts: PdfLayerPart list) (file: PdfFile) : PdfFile =
        file.Then (fun operation -> operation.OverlayFile (configured parts (DocumentOperation.LayerConfiguration (FilePath = path))))

    /// <summary>Draws the pages of the PDF file at a path beneath the pages of the file.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.DocumentOperation.UnderlayFile(QuestPDF.Fluent.DocumentOperation.LayerConfiguration)"/>.</remarks>
    let underlay (path: string) (parts: PdfLayerPart list) (file: PdfFile) : PdfFile =
        file.Then (fun operation -> operation.UnderlayFile (configured parts (DocumentOperation.LayerConfiguration (FilePath = path))))

    /// <summary>
    /// Adds XML to the <c>rdf:Description</c> element of the XMP metadata, such as the ZUGFeRD or Factur-X
    /// properties of an invoice.
    /// </summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.DocumentOperation.ExtendMetadata(System.String)"/>.</remarks>
    let extendMetadata (xml: string) (file: PdfFile) : PdfFile =
        file.Then (fun operation -> operation.ExtendMetadata xml)

    /// <summary>Embeds the file at a path as an attachment.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.DocumentOperation.AddAttachment(QuestPDF.Fluent.DocumentOperation.DocumentAttachment)"/>.</remarks>
    let attach (path: string) (parts: AttachmentPart list) (file: PdfFile) : PdfFile =
        file.Then (fun operation -> operation.AddAttachment (configured parts (DocumentOperation.DocumentAttachment (FilePath = path))))

    /// <summary>Removes the encryption of the file.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.DocumentOperation.Decrypt"/>.</remarks>
    let decrypt (file: PdfFile) : PdfFile =
        file.Then (fun operation -> operation.Decrypt ())

    /// <summary>
    /// Removes the restrictions of a digitally signed file. The signatures become invalid; their appearances remain.
    /// </summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.DocumentOperation.RemoveRestrictions"/>.</remarks>
    let removeRestrictions (file: PdfFile) : PdfFile =
        file.Then (fun operation -> operation.RemoveRestrictions ())

    /// <summary>Encrypts the file with 40-bit RC4 encryption.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.DocumentOperation.Encrypt(QuestPDF.Fluent.DocumentOperation.Encryption40Bit)"/>.</remarks>
    let encrypt40 (parts: Encryption40Part list) (file: PdfFile) : PdfFile =
        file.Then (fun operation -> operation.Encrypt (configured parts (DocumentOperation.Encryption40Bit ())))

    /// <summary>Encrypts the file with 128-bit encryption.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.DocumentOperation.Encrypt(QuestPDF.Fluent.DocumentOperation.Encryption128Bit)"/>.</remarks>
    let encrypt128 (parts: Encryption128Part list) (file: PdfFile) : PdfFile =
        file.Then (fun operation -> operation.Encrypt (configured parts (DocumentOperation.Encryption128Bit ())))

    /// <summary>Encrypts the file with 256-bit AES encryption.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.DocumentOperation.Encrypt(QuestPDF.Fluent.DocumentOperation.Encryption256Bit)"/>.</remarks>
    let encrypt256 (parts: Encryption256Part list) (file: PdfFile) : PdfFile =
        file.Then (fun operation -> operation.Encrypt (configured parts (DocumentOperation.Encryption256Bit ())))

    /// <summary>Writes a linearized file, which PDF viewers can display before the whole file is downloaded.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.DocumentOperation.Linearize"/>.</remarks>
    let linearize (file: PdfFile) : PdfFile =
        file.Then (fun operation -> operation.Linearize ())

    /// <summary>Reads the file, applies its operations in order and writes the result to a path.</summary>
    /// <remarks>Maps to <see cref="M:QuestPDF.Fluent.DocumentOperation.Save(System.String)"/>.</remarks>
    let save (path: string) (file: PdfFile) : unit =
        (DocumentOperation.LoadFile (file.Path, file.Password), List.rev file.Operations)
        ||> List.fold (fun operation apply -> apply operation)
        |> _.Save(path)
