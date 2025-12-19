namespace SocialNetworkAnalyzer.Core.Models;

public sealed class Node
{
    public int Id { get; }
    public string Label { get; private set; }
    public double Activity { get; private set; }
    public double Interaction { get; private set; }

    public double X { get; set; }
    public double Y { get; set; }

    public Node(int id, string label, double activity, double interaction, double x = 0, double y = 0)
    {
        Id = id;
        Label = label;
        Activity = activity;
        Interaction = interaction;
        X = x;
        Y = y;
    }

    public void Update(string? label = null, double? activity = null, double? interaction = null)
    {
        if (label is not null) Label = label;
        if (activity.HasValue) Activity = activity.Value;
        if (interaction.HasValue) Interaction = interaction.Value;
    }
}