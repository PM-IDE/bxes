namespace Bxes.Xes;

public static class XesConstants
{
  public const string DefaultName = "name";
  
  public const string TraceTagName = "trace";
  public const string EventTagName = "event";
  public const string ExtensionTagName = "extension";
  public const string ClassifierTagName = "classifier";
  public const string GlobalTagName = "global";

  public const string ClassifierNameAttribute = DefaultName;
  public const string ClassifierKeysAttribute = "keys";

  public const string ExtensionNameAttribute = DefaultName;
  public const string ExtensionPrefixAttribute = "prefix";
  public const string ExtensionUriAttribute = "uri";

  public const string GlobalScopeAttribute = "scope";

  public const string StringTagName = "string";
  public const string DateTagName = "date";
  public const string IntTagName = "int";
  public const string FloatTagName = "float";
  public const string BoolTagName = "boolean";

  public const string KeyAttributeName = "key";
  public const string ValueAttributeName = "value";

  public const string ConceptName = "concept:name";
  public const string TimeTimestamp = "time:timestamp";
  public const string LifecycleTransition = "lifecycle:transition";
}