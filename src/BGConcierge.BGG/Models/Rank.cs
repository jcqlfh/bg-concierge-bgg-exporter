namespace BGConcierge.BGG.Models;

public class Rank
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public string FriendlyName { get; set; }
    public int Value { get; set; }
    public double BayesAverage { get; set; }
}