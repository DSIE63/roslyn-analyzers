' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.NetFramework.Analyzers
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes

Namespace Microsoft.NetFramework.VisualBasic.Analyzers
    ''' <summary>
    ''' CA2239: Provide deserialization methods for optional fields
    ''' </summary>
    <ExportCodeFixProvider(LanguageNames.VisualBasic), [Shared]>
    Public NotInheritable Class BasicProvideDeserializationMethodsForOptionalFieldsFixer
        Inherits ProvideDeserializationMethodsForOptionalFieldsFixer

    End Class
End Namespace
