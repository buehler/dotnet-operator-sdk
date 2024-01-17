﻿namespace KubeOps.Abstractions.Entities.Attributes;

/// <summary>
/// Define a regex validator for the property.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class PatternAttribute(string regexPattern) : Attribute
{
    /// <summary>
    /// The regex pattern to be used.
    /// </summary>
    public string RegexPattern => regexPattern;
}
