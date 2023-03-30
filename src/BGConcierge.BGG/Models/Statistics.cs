namespace BGConcierge.BGG.Models;

public class Statistics
{
    public int UsersRated { get; set; }
    public double Avarage { get; set; }
    public double BayesAverage { get; set; }
    public List<Rank> Ranks { get; set; }
    public double StandardDeviation { get; set; }
    public double Median { get; set; }
    public int Owned { get; set; }
    public int Trading { get; set; }
    public int Wanting { get; set; }
    public int Whishing { get; set; }
    public int Comments { get; set; }
    public int Weights { get; set; }
    public double AverageWeight { get; set; }

    public Statistics()
    {
        Ranks = new List<Rank>();
    }
}