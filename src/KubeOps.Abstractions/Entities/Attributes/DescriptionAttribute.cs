﻿namespace KubeOps.Abstractions.Entities.Attributes;

/// <summary>
/// Defines a description for a property. This precedes the description found in a
/// XML documentation file.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
public class DescriptionAttribute(string description) : Attribute
{
    /// <summary>
    /// The given description for the property.
    /// </summary>
    public string Description => description;
}
