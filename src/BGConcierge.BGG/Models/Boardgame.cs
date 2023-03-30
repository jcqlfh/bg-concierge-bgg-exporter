namespace BGConcierge.BGG.Models;

public class Boardgame
{
    public int Id { get; set; }
    public string Thumbnail { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int YearPublished { get; set; }
    public int MinPlayers { get; set; }
    public int MaxPlayers { get; set; }
    public int PlayingTime { get; set; }
    public int MinPlayTime { get; set; }
    public int MaxPlayTime { get; set; }
    public int MinAge { get; set; }
    public List<string> Categories { get; set; }
    public List<string> Mechanics { get; set; }
    public Statistics Statistics { get; set; }

    public Boardgame()
    {
        Categories = new List<string>();
        Mechanics = new List<string>();
        Statistics = new Statistics();
    }
}