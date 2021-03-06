// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeQuality.CSharp.Analyzers.QualityGuidelines;
using Microsoft.CodeQuality.VisualBasic.Analyzers.QualityGuidelines;
using Test.Utilities;
using Xunit;

namespace Microsoft.CodeQuality.Analyzers.QualityGuidelines.UnitTests
{
    public class MarkMembersAsStaticFixerTests : CodeFixTestBase
    {
        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new MarkMembersAsStaticAnalyzer();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new MarkMembersAsStaticAnalyzer();
        }

        protected override CodeFixProvider GetBasicCodeFixProvider()
        {
            return new BasicMarkMembersAsStaticFixer();
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new CSharpMarkMembersAsStaticFixer();
        }

        [Fact]
        public void TestCSharp_SimpleMembers_NoReferences()
        {
            VerifyCSharpFix(@"
public class MembersTests
{
    internal static int s_field;
    public const int Zero = 0;

    public int Method1(string name)
    {
        return name.Length;
    }

    public void Method2() { }

    public void Method3()
    {
        s_field = 4;
    }

    public int Method4()
    {
        return Zero;
    }

    public int Property
    {
        get { return 5; }
    }

    public int Property2
    {
        set { s_field = value; }
    }

    public int MyProperty
    {
        get { return 10; }
        set { System.Console.WriteLine(value); }
    }

    public event System.EventHandler<System.EventArgs> CustomEvent { add {} remove {} }
}",
@"
public class MembersTests
{
    internal static int s_field;
    public const int Zero = 0;

    public static int Method1(string name)
    {
        return name.Length;
    }

    public static void Method2() { }

    public static void Method3()
    {
        s_field = 4;
    }

    public static int Method4()
    {
        return Zero;
    }

    public static int Property
    {
        get { return 5; }
    }

    public static int Property2
    {
        set { s_field = value; }
    }

    public static int MyProperty
    {
        get { return 10; }
        set { System.Console.WriteLine(value); }
    }

    public static event System.EventHandler<System.EventArgs> CustomEvent { add {} remove {} }
}");
        }

        [Fact]
        public void TestBasic_SimpleMembers_NoReferences()
        {
            VerifyBasicFix(@"
Imports System
Public Class MembersTests
    Shared s_field As Integer
    Public Const Zero As Integer = 0

    Public Function Method1(name As String) As Integer
        Return name.Length
    End Function

    Public Sub Method2()
    End Sub

    Public Sub Method3()
        s_field = 4
    End Sub

    Public Function Method4() As Integer
        Return Zero
    End Function

    Public Property MyProperty As Integer
        Get
            Return 10
        End Get
        Set
            System.Console.WriteLine(Value)
        End Set
    End Property

    Public Custom Event CustomEvent As EventHandler(Of EventArgs)
        AddHandler(value As EventHandler(Of EventArgs))
        End AddHandler
        RemoveHandler(value As EventHandler(Of EventArgs))
        End RemoveHandler
        RaiseEvent(sender As Object, e As EventArgs)
        End RaiseEvent
    End Event
End Class",
@"
Imports System
Public Class MembersTests
    Shared s_field As Integer
    Public Const Zero As Integer = 0

    Public Shared Function Method1(name As String) As Integer
        Return name.Length
    End Function

    Public Shared Sub Method2()
    End Sub

    Public Shared Sub Method3()
        s_field = 4
    End Sub

    Public Shared Function Method4() As Integer
        Return Zero
    End Function

    Public Shared Property MyProperty As Integer
        Get
            Return 10
        End Get
        Set
            System.Console.WriteLine(Value)
        End Set
    End Property

    Public Shared Custom Event CustomEvent As EventHandler(Of EventArgs)
        AddHandler(value As EventHandler(Of EventArgs))
        End AddHandler
        RemoveHandler(value As EventHandler(Of EventArgs))
        End RemoveHandler
        RaiseEvent(sender As Object, e As EventArgs)
        End RaiseEvent
    End Event
End Class");
        }

        [Fact]
        public void TestCSharp_ReferencesInSameType_MemberReferences()
        {
            VerifyCSharpFix(@"
using System;

public class C
{
    private C fieldC;
    private C PropertyC { get; set; }

    public int M1()
    {
        return 0;
    }

    public void M2(C paramC)
    {
        var localC = fieldC;
        Func<int> m1 = M1,
            m2 = paramC.M1,
            m3 = localC.M1,
            m4 = fieldC.M1,
            m5 = PropertyC.M1,
            m6 = fieldC.PropertyC.M1,
            m7 = this.M1;
    }
}",
@"
using System;

public class C
{
    private C fieldC;
    private C PropertyC { get; set; }

    public static int M1()
    {
        return 0;
    }

    public void M2(C paramC)
    {
        var localC = fieldC;
        Func<int> m1 = M1,
            m2 = M1,
            m3 = M1,
            m4 = M1,
            m5 = M1,
            m6 = M1,
            m7 = M1;
    }
}");
        }

        [Fact]
        public void TestBasic_ReferencesInSameType_MemberReferences()
        {
            VerifyBasicFix(@"
Imports System

Public Class C
    Private fieldC As C
    Private Property PropertyC As C

    Public Function M1() As Integer
        Return 0
    End Function

    Public Sub M2(paramC As C)
        Dim localC = fieldC
        Dim m As Func(Of Integer) = AddressOf M1,
            m2 As Func(Of Integer) = AddressOf paramC.M1,
            m3 As Func(Of Integer) = AddressOf localC.M1,
            m4 As Func(Of Integer) = AddressOf fieldC.M1,
            m5 As Func(Of Integer) = AddressOf PropertyC.M1,
            m6 As Func(Of Integer) = AddressOf fieldC.PropertyC.M1,
            m7 As Func(Of Integer) = AddressOf Me.M1
    End Sub
End Class",
@"
Imports System

Public Class C
    Private fieldC As C
    Private Property PropertyC As C

    Public Shared Function M1() As Integer
        Return 0
    End Function

    Public Sub M2(paramC As C)
        Dim localC = fieldC
        Dim m As Func(Of Integer) = AddressOf M1,
            m2 As Func(Of Integer) = AddressOf M1,
            m3 As Func(Of Integer) = AddressOf M1,
            m4 As Func(Of Integer) = AddressOf M1,
            m5 As Func(Of Integer) = AddressOf M1,
            m6 As Func(Of Integer) = AddressOf M1,
            m7 As Func(Of Integer) = AddressOf M1
    End Sub
End Class");
        }

        [Fact]
        public void TestCSharp_ReferencesInSameType_Invocations()
        {
            VerifyCSharpFix(@"
public class C
{
    private int x;
    private C fieldC;
    private C PropertyC { get; set; }

    public int M1()
    {
        return 0;
    }

    public void M2(C paramC)
    {
        var localC = fieldC;
        x = M1() + paramC.M1() + localC.M1() + fieldC.M1() + PropertyC.M1() + fieldC.PropertyC.M1() + this.M1();
    }
}",
@"
public class C
{
    private int x;
    private C fieldC;
    private C PropertyC { get; set; }

    public static int M1()
    {
        return 0;
    }

    public void M2(C paramC)
    {
        var localC = fieldC;
        x = M1() + M1() + M1() + M1() + M1() + M1() + M1();
    }
}");
        }

        [Fact]
        public void TestBasic_ReferencesInSameType_Invocations()
        {
            VerifyBasicFix(@"
Public Class C
    Private x As Integer
    Private fieldC As C
    Private Property PropertyC As C

    Public Function M1() As Integer
        Return 0
    End Function

    Public Sub M2(paramC As C)
        Dim localC = fieldC
        x = M1() + paramC.M1() + localC.M1() + fieldC.M1() + PropertyC.M1() + fieldC.PropertyC.M1() + Me.M1()
    End Sub
End Class",
@"
Public Class C
    Private x As Integer
    Private fieldC As C
    Private Property PropertyC As C

    Public Shared Function M1() As Integer
        Return 0
    End Function

    Public Sub M2(paramC As C)
        Dim localC = fieldC
        x = M1() + M1() + M1() + M1() + M1() + M1() + M1()
    End Sub
End Class");
        }

        [Fact]
        public void TestCSharp_ReferencesInSameFile_MemberReferences()
        {
            VerifyCSharpFix(@"
using System;

public class C
{
    public C PropertyC { get; set; }

    public int M1()
    {
        return 0;
    }
}

class C2
{
    private C fieldC;
    private C PropertyC { get; set; }

    public void M2(C paramC)
    {
        var localC = fieldC;
        Func<int> m1 = paramC.M1,
            m2 = localC.M1,
            m3 = fieldC.M1,
            m4 = PropertyC.M1,
            m5 = fieldC.PropertyC.M1;
    }
}",
@"
using System;

public class C
{
    public C PropertyC { get; set; }

    public static int M1()
    {
        return 0;
    }
}

class C2
{
    private C fieldC;
    private C PropertyC { get; set; }

    public void M2(C paramC)
    {
        var localC = fieldC;
        Func<int> m1 = C.M1,
            m2 = C.M1,
            m3 = C.M1,
            m4 = C.M1,
            m5 = C.M1;
    }
}");
        }

        [Fact]
        public void TestCSharp_ReferencesInSameFile_Invocations()
        {
            VerifyCSharpFix(@"
using System;

public class C
{
    public C PropertyC { get; set; }

    public int M1()
    {
        return 0;
    }
}

class C2
{
    private int x;
    private C fieldC;
    private C PropertyC { get; set; }

    public void M2(C paramC)
    {
        var localC = fieldC;
        x = paramC.M1() + localC.M1() + fieldC.M1() + PropertyC.M1() + fieldC.PropertyC.M1();
    }
}",
@"
using System;

public class C
{
    public C PropertyC { get; set; }

    public static int M1()
    {
        return 0;
    }
}

class C2
{
    private int x;
    private C fieldC;
    private C PropertyC { get; set; }

    public void M2(C paramC)
    {
        var localC = fieldC;
        x = C.M1() + C.M1() + C.M1() + C.M1() + C.M1();
    }
}");
        }

        [Fact]
        public void TestCSharp_ReferencesInMultipleFiles_MemberReferences()
        {
            VerifyCSharpFix(new[] { @"
using System;

public class C
{
    public C PropertyC { get; set; }

    public int M1()
    {
        return 0;
    }
}

class C2
{
    private C fieldC;
    private C PropertyC { get; set; }

    public void M2(C paramC)
    {
        var localC = fieldC;
        Func<int> m1 = paramC.M1,
            m2 = localC.M1,
            m3 = fieldC.M1,
            m4 = PropertyC.M1,
            m5 = fieldC.PropertyC.M1;
    }
}",
@"
using System;

class C3
{
    private C fieldC;
    private C PropertyC { get; set; }

    public void M3(C paramC)
    {
        var localC = fieldC;
        Func<int> m1 = paramC.M1,
            m2 = localC.M1,
            m3 = fieldC.M1,
            m4 = PropertyC.M1,
            m5 = fieldC.PropertyC.M1;
    }
}"
            },
            new[] {@"
using System;

public class C
{
    public C PropertyC { get; set; }

    public static int M1()
    {
        return 0;
    }
}

class C2
{
    private C fieldC;
    private C PropertyC { get; set; }

    public void M2(C paramC)
    {
        var localC = fieldC;
        Func<int> m1 = C.M1,
            m2 = C.M1,
            m3 = C.M1,
            m4 = C.M1,
            m5 = C.M1;
    }
}",
@"
using System;

class C3
{
    private C fieldC;
    private C PropertyC { get; set; }

    public void M3(C paramC)
    {
        var localC = fieldC;
        Func<int> m1 = C.M1,
            m2 = C.M1,
            m3 = C.M1,
            m4 = C.M1,
            m5 = C.M1;
    }
}" });
        }

        [Fact]
        public void TestCSharp_ReferencesInMultipleFiles_Invocations()
        {
            VerifyCSharpFix(new[] { @"
using System;

public class C
{
    public C PropertyC { get; set; }

    public int M1()
    {
        return 0;
    }
}

class C2
{
    private int x;
    private C fieldC;
    private C PropertyC { get; set; }

    public void M2(C paramC)
    {
        var localC = fieldC;
        x = paramC.M1() + localC.M1() + fieldC.M1() + PropertyC.M1() + fieldC.PropertyC.M1();
    }
}",
@"
using System;

class C3
{
    private int x;
    private C fieldC;
    private C PropertyC { get; set; }

    public void M3(C paramC)
    {
        var localC = fieldC;
        x = paramC.M1() + localC.M1() + fieldC.M1() + PropertyC.M1() + fieldC.PropertyC.M1();
    }
}" },
    new[] { @"
using System;

public class C
{
    public C PropertyC { get; set; }

    public static int M1()
    {
        return 0;
    }
}

class C2
{
    private int x;
    private C fieldC;
    private C PropertyC { get; set; }

    public void M2(C paramC)
    {
        var localC = fieldC;
        x = C.M1() + C.M1() + C.M1() + C.M1() + C.M1();
    }
}",
@"
using System;

class C3
{
    private int x;
    private C fieldC;
    private C PropertyC { get; set; }

    public void M3(C paramC)
    {
        var localC = fieldC;
        x = C.M1() + C.M1() + C.M1() + C.M1() + C.M1();
    }
}" });
        }

        [Fact]
        public void TestCSharp_ReferenceInArgument()
        {
            VerifyCSharpFix(@"
public class C
{
    private C fieldC;
    public C M1(C c)
    {
        return c;
    }

    public C M2(C paramC)
    {
        var localC = fieldC;
        return this.M1(paramC.M1(localC));
    }
}",
@"
public class C
{
    private C fieldC;
    public static C M1(C c)
    {
        return c;
    }

    public C M2(C paramC)
    {
        var localC = fieldC;
        return M1(M1(localC));
    }
}");
        }

        [Fact]
        public void TestBasic_ReferenceInArgument()
        {
            VerifyBasicFix(@"
Public Class C
    Private fieldC As C

    Public Function M1(c As C) As C
        Return c
    End Function

    Public Function M2(paramC As C) As C
        Dim localC = fieldC
        Return Me.M1(paramC.M1(localC))
    End Function
End Class",
@"
Public Class C
    Private fieldC As C

    Public Shared Function M1(c As C) As C
        Return c
    End Function

    Public Function M2(paramC As C) As C
        Dim localC = fieldC
        Return M1(M1(localC))
    End Function
End Class");
        }

        [Fact]
        public void TestCSharp_GenericMethod()
        {
            VerifyCSharpFix(@"
public class C
{
    private C fieldC;
    public C M1<T>(C c, T t)
    {
        return c;
    }

    public C M1<T>(T t, int i)
    {
        return fieldC;
    }
}

public class C2<T2>
{
    private C fieldC;
    public void M2(C paramC)
    {
        // Explicit type argument
        paramC.M1<int>(fieldC, 0);
        
        // Implicit type argument
        paramC.M1(fieldC, this);
    }
}",
@"
public class C
{
    private C fieldC;
    public static C M1<T>(C c, T t)
    {
        return c;
    }

    public C M1<T>(T t, int i)
    {
        return fieldC;
    }
}

public class C2<T2>
{
    private C fieldC;
    public void M2(C paramC)
    {
        // Explicit type argument
        C.M1<int>(fieldC, 0);

        // Implicit type argument
        C.M1(fieldC, this);
    }
}");
        }

        [Fact]
        public void TestBasic_GenericMethod()
        {
            VerifyBasicFix(@"
Public Class C
    Private fieldC As C

    Public Function M1(Of T)(c As C, t1 As T) As C
        Return c
    End Function

    Public Function M1(Of T)(t1 As T, i As Integer) As C
        Return fieldC
    End Function
End Class

Public Class C2(Of T2)
    Private fieldC As C

    Public Sub M2(paramC As C)
        ' Explicit type argument
        paramC.M1(Of Integer)(fieldC, 0)

        ' Implicit type argument
        paramC.M1(fieldC, Me)
    End Sub
End Class",
@"
Public Class C
    Private fieldC As C

    Public Shared Function M1(Of T)(c As C, t1 As T) As C
        Return c
    End Function

    Public Function M1(Of T)(t1 As T, i As Integer) As C
        Return fieldC
    End Function
End Class

Public Class C2(Of T2)
    Private fieldC As C

    Public Sub M2(paramC As C)
        ' Explicit type argument
        C.M1(Of Integer)(fieldC, 0)

        ' Implicit type argument
        C.M1(fieldC, Me)
    End Sub
End Class");
        }

        [Fact]
        public void TestCSharp_InvocationInInstance()
        {
            // We don't make the replacement if instance has an invocation.
            VerifyCSharpFix(@"
public class C
{
    private C fieldC;
    public C M1(C c)
    {
        return c;
    }

    public C M2(C paramC)
    {
        var localC = fieldC;
        return localC.M1(paramC).M1(paramC.M1(localC));
    }
}",
@"
public class C
{
    private C fieldC;
    public static C M1(C c)
    {
        return c;
    }

    public C M2(C paramC)
    {
        var localC = fieldC;
        return M1(paramC).M1(M1(localC));
    }
}", allowNewCompilerDiagnostics: true, validationMode: TestValidationMode.AllowCompileErrors);
        }

        [Fact]
        public void TestBasic_InvocationInInstance()
        {
            // We don't make the replacement if instance has an invocation.
            VerifyBasicFix(@"
Public Class C
    Private fieldC As C

    Public Function M1(c As C) As C
        Return c
    End Function

    Public Function M2(paramC As C) As C
        Dim localC = fieldC
        Return localC.M1(paramC).M1(paramC.M1(localC))
    End Function
End Class",
@"
Public Class C
    Private fieldC As C

    Public Shared Function M1(c As C) As C
        Return c
    End Function

    Public Function M2(paramC As C) As C
        Dim localC = fieldC
        Return M1(paramC).M1(M1(localC))
    End Function
End Class", allowNewCompilerDiagnostics: true, validationMode: TestValidationMode.AllowCompileErrors);
        }

        [Fact]
        public void TestCSharp_ConversionInInstance()
        {
            // We don't make the replacement if instance has a conversion.
            VerifyCSharpFix(@"
public class C
{
    private C fieldC;
    public object M1(C c)
    {
        return c;
    }

    public C M2(C paramC)
    {
        var localC = fieldC;
        return ((C)paramC).M1(localC);
    }
}",
@"
public class C
{
    private C fieldC;
    public static object M1(C c)
    {
        return c;
    }

    public C M2(C paramC)
    {
        var localC = fieldC;
        return ((C)paramC).M1(localC);
    }
}", allowNewCompilerDiagnostics: true, validationMode: TestValidationMode.AllowCompileErrors);
        }

        [Fact]
        public void TestBasic_ConversionInInstance()
        {
            // We don't make the replacement if instance has a conversion.
            VerifyBasicFix(@"
Public Class C
    Private fieldC As C

    Public Function M1(c As C) As Object
        Return c
    End Function

    Public Function M2(paramC As C) As C
        Dim localC = fieldC
        Return (CType(paramC, C)).M1(localC)
    End Function
End Class",
@"
Public Class C
    Private fieldC As C

    Public Shared Function M1(c As C) As Object
        Return c
    End Function

    Public Function M2(paramC As C) As C
        Dim localC = fieldC
        Return (CType(paramC, C)).M1(localC)
    End Function
End Class", allowNewCompilerDiagnostics: true, validationMode: TestValidationMode.AllowCompileErrors);
        }

        [Fact]
        public void TestCSharp_FixAll()
        {
            VerifyCSharpFix(new[] { @"
using System;

public class C
{
    public C PropertyC { get; set; }

    public int M1()
    {
        return 0;
    }

    public int M2()
    {
        return 0;
    }
}

class C2
{
    private int x;
    private C fieldC;
    private C PropertyC { get; set; }

    public void M2(C paramC)
    {
        var localC = fieldC;
        x = paramC.M1() + localC.M2() + fieldC.M1() + PropertyC.M2() + fieldC.PropertyC.M1();
    }
}",
@"
using System;

class C3
{
    private int x;
    private C fieldC;
    private C PropertyC { get; set; }

    public void M3(C paramC)
    {
        var localC = fieldC;
        x = paramC.M2() + localC.M1() + fieldC.M2() + PropertyC.M1() + fieldC.PropertyC.M2();
    }
}" },
    new[] { @"
using System;

public class C
{
    public C PropertyC { get; set; }

    public static int M1()
    {
        return 0;
    }

    public static int M2()
    {
        return 0;
    }
}

class C2
{
    private int x;
    private C fieldC;
    private C PropertyC { get; set; }

    public void M2(C paramC)
    {
        var localC = fieldC;
        x = C.M1() + C.M2() + C.M1() + C.M2() + C.M1();
    }
}",
@"
using System;

class C3
{
    private int x;
    private C fieldC;
    private C PropertyC { get; set; }

    public void M3(C paramC)
    {
        var localC = fieldC;
        x = C.M2() + C.M1() + C.M2() + C.M1() + C.M2();
    }
}" });
        }

        [Fact]
        public void TestBasic_FixAll()
        {
            VerifyBasicFix(new[] { @"
Imports System

Public Class C
    Public Property PropertyC As C

    Public Function M1() As Integer
        Return 0
    End Function

    Public Function M2() As Integer
        Return 0
    End Function
End Class

Class C2
    Private x As Integer
    Private fieldC As C
    Private Property PropertyC As C

    Public Sub M2(paramC As C)
        Dim localC = fieldC
        x = paramC.M1() + localC.M2() + fieldC.M1() + PropertyC.M2() + fieldC.PropertyC.M1()
    End Sub
End Class",
@"
Imports System

Class C3
    Private x As Integer
    Private fieldC As C
    Private Property PropertyC As C

    Public Sub M3(paramC As C)
        Dim localC = fieldC
        x = paramC.M2() + localC.M1() + fieldC.M2() + PropertyC.M1() + fieldC.PropertyC.M2()
    End Sub
End Class" },
    new[] { @"
Imports System

Public Class C
    Public Property PropertyC As C

    Public Shared Function M1() As Integer
        Return 0
    End Function

    Public Shared Function M2() As Integer
        Return 0
    End Function
End Class

Class C2
    Private x As Integer
    Private fieldC As C
    Private Property PropertyC As C

    Public Sub M2(paramC As C)
        Dim localC = fieldC
        x = C.M1() + C.M2() + C.M1() + C.M2() + C.M1()
    End Sub
End Class",
@"
Imports System

Class C3
    Private x As Integer
    Private fieldC As C
    Private Property PropertyC As C

    Public Sub M3(paramC As C)
        Dim localC = fieldC
        x = C.M2() + C.M1() + C.M2() + C.M1() + C.M2()
    End Sub
End Class" });
        }

        [Fact]
        public void TestCSharp_PropertyWithReferences()
        {
            VerifyCSharpFix(@"
public class C
{
    private C fieldC;
    public C M1 { get { return null; } set { } }

    public C M2(C paramC)
    {
        var x = this.M1;
        paramC.M1 = x;
        return fieldC;
    }
}",
@"
public class C
{
    private C fieldC;
    public static C M1 { get { return null; } set { } }

    public C M2(C paramC)
    {
        var x = M1;
        M1 = x;
        return fieldC;
    }
}");
        }

        [Fact]
        public void TestBasic_PropertyWithReferences()
        {
            VerifyBasicFix(@"
Public Class C
    Private fieldC As C

    Public Property M1 As C
        Get
            Return Nothing
        End Get
        Set(ByVal value As C)
        End Set
    End Property

    Public Function M2(paramC As C) As C
        Dim x = Me.M1
        paramC.M1 = x
        Return fieldC
    End Function
End Class",
@"
Public Class C
    Private fieldC As C

    Public Shared Property M1 As C
        Get
            Return Nothing
        End Get
        Set(ByVal value As C)
        End Set
    End Property

    Public Function M2(paramC As C) As C
        Dim x = M1
        M1 = x
        Return fieldC
    End Function
End Class");
        }
    }
}