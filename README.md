Code Analysis: CheckNullable
============================

Experimental Roslyn diagnostic extension.

![screen shot](https://cloud.githubusercontent.com/assets/75655/2623581/e46f973c-bcf9-11e3-9191-c1d27682f297.png)

`Nullable<T>` is a very useful type in .NET that allows users to have nullable variants of structs. This extension based on Roslyn adds extra checks to your code to see if nullables are used correctly.

If a nullable's `Value` property is accessed without first checking if the nullable actually has a value, a warning will be generated.

Installing
-----------

To run this extension:

1. Install [Roslyn](http://msdn.microsoft.com/en-US/roslyn)
2. Install the [extension .vsix](https://github.com/tomlokhorst/codeanalysis-checknullable/raw/master/extension/CheckNullable.vsix)

Does this extension actually work?
----------------------------------

Honestly? Not really...
The example shown above does work, but more for more complicated code, the analysis fails and has false positives. For example `if (x != null)` will be interpreted as a valid check, but `if (x != null && true)` won't be.

The code that looks to see if a nullable has been checked for non-null is a bit of a hack and needs to be replaced with a proper control/data flow analysis.


How can this be improved?
-------------
The [code](https://github.com/tomlokhorst/codeanalysis-checknullable/blob/master/src/CheckNullable/DiagnosticAnalyzer.cs) that does the analysis to check if nullables are safe to access is crap. I think the `SemanticModel` should be used, but I haven't looked to deeply into it.
