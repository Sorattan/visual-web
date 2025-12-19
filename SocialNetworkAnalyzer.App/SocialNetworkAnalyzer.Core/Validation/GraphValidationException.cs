using System;

namespace SocialNetworkAnalyzer.Core.Validation;

public sealed class GraphValidationException : Exception
{
    public GraphValidationException(string message) : base(message) { }
}