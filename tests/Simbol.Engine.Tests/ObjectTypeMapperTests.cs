namespace Simbol.Engine.Tests;

using System.IO.BACnet;
using Simbol.Engine.Factories;
using Simbol.Engine.Models;

public class ObjectTypeMapperTests
{
    [Theory]
    [InlineData("analog-input", BacnetObjectTypes.OBJECT_ANALOG_INPUT, ObjectValueCategory.Analog, false)]
    [InlineData("analog-output", BacnetObjectTypes.OBJECT_ANALOG_OUTPUT, ObjectValueCategory.Analog, true)]
    [InlineData("analog-value", BacnetObjectTypes.OBJECT_ANALOG_VALUE, ObjectValueCategory.Analog, true)]
    [InlineData("binary-input", BacnetObjectTypes.OBJECT_BINARY_INPUT, ObjectValueCategory.Binary, false)]
    [InlineData("binary-output", BacnetObjectTypes.OBJECT_BINARY_OUTPUT, ObjectValueCategory.Binary, true)]
    [InlineData("binary-value", BacnetObjectTypes.OBJECT_BINARY_VALUE, ObjectValueCategory.Binary, true)]
    [InlineData("multi-state-input", BacnetObjectTypes.OBJECT_MULTI_STATE_INPUT, ObjectValueCategory.MultiState, false)]
    [InlineData("multi-state-output", BacnetObjectTypes.OBJECT_MULTI_STATE_OUTPUT, ObjectValueCategory.MultiState, true)]
    [InlineData("multi-state-value", BacnetObjectTypes.OBJECT_MULTI_STATE_VALUE, ObjectValueCategory.MultiState, true)]
    public void Resolve_AllValidTypes_ReturnsCorrectMapping(
        string typeName, BacnetObjectTypes expectedType, ObjectValueCategory expectedCategory, bool expectedWritable)
    {
        var (type, category, isWritable) = ObjectTypeMapper.Resolve(typeName);

        Assert.Equal(expectedType, type);
        Assert.Equal(expectedCategory, category);
        Assert.Equal(expectedWritable, isWritable);
    }

    [Theory]
    [InlineData("Analog-Input", BacnetObjectTypes.OBJECT_ANALOG_INPUT)]
    [InlineData("ANALOG-INPUT", BacnetObjectTypes.OBJECT_ANALOG_INPUT)]
    [InlineData("Binary-Output", BacnetObjectTypes.OBJECT_BINARY_OUTPUT)]
    public void Resolve_CaseInsensitive(string typeName, BacnetObjectTypes expected)
    {
        var (type, _, _) = ObjectTypeMapper.Resolve(typeName);

        Assert.Equal(expected, type);
    }

    [Fact]
    public void Resolve_UnknownType_ThrowsArgument()
    {
        Assert.Throws<ArgumentException>(() => ObjectTypeMapper.Resolve("foo"));
    }
}
