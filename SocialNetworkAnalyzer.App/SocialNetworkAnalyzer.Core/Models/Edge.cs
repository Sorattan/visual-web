using System;

namespace SocialNetworkAnalyzer.Core.Models;

public readonly struct Edge : IEquatable<Edge>
{
    public int A { get; }
    public int B { get; }

    public Edge(int a, int b)
    {
        if (a == b) throw new ArgumentException("Self-loop (A == B) olamaz.");

        if (a < b) { A = a; B = b; }
        else { A = b; B = a; }
    }

    public bool Equals(Edge other) => A == other.A && B == other.B;
    public override bool Equals(object? obj) => obj is Edge other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(A, B);
    public override string ToString() => $"{A}-{B}";
}