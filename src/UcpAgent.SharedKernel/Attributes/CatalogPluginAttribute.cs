namespace UcpAgent.SharedKernel.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class CatalogPluginAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
