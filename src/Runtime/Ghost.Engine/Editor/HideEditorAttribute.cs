namespace Ghost.Engine.Editor;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public class HideEditorAttribute : Attribute
{
}