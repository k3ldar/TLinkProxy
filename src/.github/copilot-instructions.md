# Copilot Instructions

# Code Comments

When in C# use #region instead of // ── style comments when outside of a method, inside methods/properties etc just use // \[some comment] no need for lots of -- everywhere

# Clean Code

One class per file, no multiple classes in one file unless they are private classes.
One statement per line, no multiple statements on one line.
Put string literals in file Constants.cs, not in code files, ensure naming reflects the purpose.
No magic numbers, put them in Constants.cs with descriptive names.
Use explicit types instead of var unless using for complex linq expressions or anonymous types.
Use C# aliases (string, int, bool, etc.) for declarations, parameters, fields, properties, and return types. Use CLR type names (String, Int32, Boolean, etc.) whenever accessing static members on those types.

Classes should be sealed unless they are designed for inheritance. If a class is designed for inheritance, it should be abstract.
Class layout matters, it needs to be ordered as follows:

* Fields
  * constants
  * static fields
  * local fields
* Properties
* Constructors
* public Methods
* protected/virual Methods
* private Methods



Prefer explicit types over var.

Example:

List<int> values = [];
SomeType service = new();


Dependencies should be injected through constructors.

Avoid service locators, static dependencies, and hidden runtime dependencies.

Use ArgumentNullException.ThrowIfNull() where appropriate.

nullable reference types are not enabled.

When/if building ensure no CC0009 and MSTEST0037 warnings or errors are present

# Tests
Ensure \[ExcludeFromCodeCoverage] is on test classes so they are not counted in code coverage metrics.

All test methods must follow the naming convention: MethodName\_StateUnderTest\_ExpectedBehavior

Do not use \[Ignore], \[Fact(Skip = ...)], or other mechanisms that disable tests unless explicitly requested.

Use the Arrange-Act-Assert (AAA) pattern in all unit tests.

Do not add comments such as:

// Arrange
// Act
// Assert

The system under test must be named: sut

The structure should be obvious from the code.

Unit tests should cover:

- Happy path
- Boundary conditions
- Null inputs
- Empty collections
- Invalid inputs
- Exception paths

Every public method should have corresponding unit tests unless a justification is provided.

# Testing Insights
If there is a problem with the code that prevents easy and effective testing, it is a code smell should be highlighted so I can decide what to do about it.

# Architectural Guidance

If architectural guidance is requested, provide advice and design recommendations only.

Do not generate implementation code unless explicitly requested.

# DateTime Handling

All DateTime values used in tests must be UTC.
