using System.Globalization;
using System.Text.Json;
using System.Xml;
using BGConcierge.BGG;
using BGConcierge.BGG.Models;
using Polly;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var startIndex = Convert.ToInt32(args != null && args.Length > 0 ? args[0] : "0");
        var endIndex = Convert.ToInt32(args != null && args.Length > 1 ? args[1] : "450000");

        var startTime = DateTime.Now;
        var page = 20;
        List<Boardgame> boardgames = new List<Boardgame>();
        using (var proxy = BGGClientFactory.GetXmlApi())
        {

            var random = new Random();
            for (int i = startIndex; i < endIndex; i += page)
            {
                Console.WriteLine($"{(DateTime.Now - startTime).ToString("c")} {(decimal)i / endIndex * 100}% Total:{boardgames.Count}");
                Console.WriteLine($"Requesting {i} to {i + page - 1}...");
                var xml = await Policy
                    .Handle<Refit.ApiException>()
                    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
                    .ExecuteAsync(async () =>
                        await proxy.GetThings(string.Join(",", Enumerable.Range(i, page))));

                if (xml.DocumentElement?.ChildNodes?.Count == 0)
                    break;

                Parallel.ForEach(xml.DocumentElement.ChildNodes.Cast<XmlNode>().Where(n => n is XmlElement), node =>
                {
                    var boardgame = new Boardgame();
                    boardgame.Id = Convert.ToInt32(node.Attributes["objectid"]?.Value);

                    try
                    {
                        foreach (XmlElement child in node.ChildNodes.Cast<XmlNode>().Where(n => n is XmlElement))
                        {
                            if (child.Name == "thumbnail")
                            {
                                boardgame.Thumbnail = child.InnerText;
                            }
                            else if (child.Name == "name" && child.Attributes["primary"]?.Value == "true")
                            {
                                boardgame.Name = child.InnerText;
                            }
                            else if (child.Name == "description")
                            {
                                boardgame.Description = child.InnerText;
                            }
                            else if (child.Name == "yearpublished")
                            {
                                boardgame.YearPublished = Convert.ToInt32(child.InnerText);
                            }
                            else if (child.Name == "minplayers")
                            {
                                boardgame.MinPlayers = Convert.ToInt32(child.InnerText);
                            }
                            else if (child.Name == "maxplayers")
                            {
                                boardgame.MaxPlayers = Convert.ToInt32(child.InnerText);
                            }
                            else if (child.Name == "playingtime")
                            {
                                boardgame.PlayingTime = Convert.ToInt32(child.InnerText);
                            }
                            else if (child.Name == "minplaytime")
                            {
                                boardgame.MinPlayTime = Convert.ToInt32(child.InnerText);
                            }
                            else if (child.Name == "maxplaytime")
                            {
                                boardgame.MaxPlayTime = Convert.ToInt32(child.InnerText);
                            }
                            else if (child.Name == "age")
                            {
                                boardgame.MaxPlayTime = Convert.ToInt32(child.InnerText);
                            }
                            else if (child.Name == "boardgamecategory")
                            {
                                boardgame.Categories.Add(child.InnerText);
                            }
                            else if (child.Name == "boardgamemechanic")
                            {
                                boardgame.Mechanics.Add(child.InnerText);
                            }
                            else if (child.Name == "statistics")
                            {
                                // <statistics page="1">
                                var statistics = boardgame.Statistics;

                                // <ratings>
                                foreach (XmlElement rating in child.ChildNodes.Cast<XmlNode>().Where(n => n is XmlElement).First().ChildNodes.Cast<XmlNode>().Where(n => n is XmlElement))
                                {
                                    if (rating.Name == "usersrated")
                                    {
                                        // <usersrated value="5621"/>
                                        statistics.UsersRated = Convert.ToInt32(rating.InnerText);
                                    }
                                    else if (rating.Name == "average")
                                    {
                                        //  <average value="7.60363"/>
                                        statistics.Avarage = Convert.ToDouble(rating.InnerText, CultureInfo.InvariantCulture);
                                    }
                                    else if (rating.Name == "bayesaverage")
                                    {
                                        // <bayesaverage value="7.06781"/>
                                        statistics.BayesAverage = Convert.ToDouble(rating.InnerText, CultureInfo.InvariantCulture);
                                    }
                                    else if (rating.Name == "stddev")
                                    {
                                        // <stddev value="1.57315"/>
                                        statistics.StandardDeviation = Convert.ToDouble(rating.InnerText, CultureInfo.InvariantCulture);
                                    }
                                    else if (rating.Name == "median")
                                    {
                                        // <median value="0"/>
                                        statistics.Median = Convert.ToDouble(rating.InnerText, CultureInfo.InvariantCulture);
                                    }
                                    else if (rating.Name == "owned")
                                    {
                                        // <owned value="7919"/>
                                        statistics.Owned = Convert.ToInt32(rating.InnerText);
                                    }
                                    else if (rating.Name == "trading")
                                    {
                                        // <trading value="250"/>
                                        statistics.Trading = Convert.ToInt32(rating.InnerText);
                                    }
                                    else if (rating.Name == "wanting")
                                    {
                                        // <wanting value="510"/>
                                        statistics.Wanting = Convert.ToInt32(rating.InnerText);
                                    }
                                    else if (rating.Name == "wishing")
                                    {
                                        // <wishing value="2118"/>
                                        statistics.Whishing = Convert.ToInt32(rating.InnerText);
                                    }
                                    else if (rating.Name == "numcomments")
                                    {
                                        // <numcomments value="2118"/>
                                        statistics.Comments = Convert.ToInt32(rating.InnerText);
                                    }
                                    else if (rating.Name == "numweights")
                                    {
                                        // <numweights value="2118"/>
                                        statistics.Weights = Convert.ToInt32(rating.InnerText);
                                    }
                                    else if (rating.Name == "averageweight")
                                    {
                                        // <averageweight value="2118"/>
                                        statistics.AverageWeight = Convert.ToDouble(rating.InnerText, CultureInfo.InvariantCulture);
                                    }
                                    else if (rating.Name == "ranks")
                                    {
                                        // <ranks>
                                        foreach (XmlElement rank in rating.ChildNodes.Cast<XmlNode>().Where(n => n is XmlElement))
                                        {
                                            // <rank type="subtype" id="1" name="boardgame" friendlyname="Board Game Rank" value="368" bayesaverage="7.06781"/>
                                            statistics.Ranks.Add(new Rank()
                                            {
                                                Id = Convert.ToInt32(rank.Attributes["id"]?.Value ?? "-1"),
                                                Name = rank.Attributes["name"]?.Value,
                                                Type = rank.Attributes["type"]?.Value,
                                                FriendlyName = rank.Attributes["friendlyname"]?.Value,
                                                Value = Int32.TryParse(rating.Attributes["value"]?.Value, out var val) ? val : -1,
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }

                    boardgames.Add(boardgame);
                });

                Thread.Sleep(random.Next(1000, 2000));
            }

            string jsonString = JsonSerializer.Serialize(boardgames);
            File.WriteAllText("./database.json", jsonString);
        }
    }
}
