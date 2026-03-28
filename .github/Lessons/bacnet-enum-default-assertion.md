# Lesson: BacnetObjectTypes default value in xUnit assertions

**Date:** 2025-07-25
**Context:** Writing unit tests for ObjectTypeMapper.Resolve case-insensitivity

## Problem
Used `Assert.NotEqual(default, type)` to verify that `ObjectTypeMapper.Resolve` returned a valid `BacnetObjectTypes` enum value. This fails because `OBJECT_ANALOG_INPUT` has the numeric value `0`, which is the `default` for the enum.

## Fix
Use explicit expected values in the assertion instead of comparing against `default`:
```csharp
[InlineData("Analog-Input", BacnetObjectTypes.OBJECT_ANALOG_INPUT)]
public void Resolve_CaseInsensitive(string typeName, BacnetObjectTypes expected)
{
    var (type, _, _) = ObjectTypeMapper.Resolve(typeName);
    Assert.Equal(expected, type);
}
```

## Rule
Never use `Assert.NotEqual(default, ...)` with enums where the first member may be `0`. Always assert against specific expected values.
