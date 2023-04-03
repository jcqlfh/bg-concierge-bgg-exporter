using BGConcierge.BGG;
using BGConcierge.BGG.Models;
using System.Xml;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var startTime = DateTime.Now;
        var page = 200;
        List<Boardgame> boardgames = new List<Boardgame>();
        using (var proxy = BGGClientFactory.GetXmlApi2())
        {
            var random = new Random();
            for( int i = 0; i < 400000; i+=page)
            {
                Console.WriteLine($"{(DateTime.Now-startTime).ToString("c")} {(decimal)i/400000m*100}% Total:{boardgames.Count}");
                XmlDocument xml = null;
                
                try
                {
                    xml = await proxy.GetThings(new List<int>(Enumerable.Range(i, page)));
                }
                catch
                {
                    Thread.Sleep(10000);
                    xml = await proxy.GetThings(new List<int>(Enumerable.Range(i, page)));
                }

                if (xml.DocumentElement?.ChildNodes?.Count == 0)
                    break;

                Parallel.ForEach(xml.DocumentElement.ChildNodes.Cast<XmlNode>().Where(n => n is XmlElement), node =>
                {
                    var boardgame = new Boardgame();
                    //<item type="boardgame" id="1">
                    boardgame.Id = Convert.ToInt32(node.Attributes["id"]?.Value);
                    
                    try
                    {
                        foreach (XmlElement child in node.ChildNodes.Cast<XmlNode>().Where(n => n is XmlElement))
                        {
                            if (child.Name == "thumbnail")
                            {
                                //<thumbnail>https://cf.geekdo-images.com/rpwCZAjYLD940NWwP3SRoA__thumb/img/YT6svCVsWqLrDitcMEtyazVktbQ=/fit-in/200x150/filters:strip_icc()/pic4718279.jpg</thumbnail>
                                boardgame.Thumbnail = child.InnerText;
                            }
                            else if (child.Name == "name" && child.Attributes["type"]?.Value == "primary")
                            {
                                //<name type="primary" sortindex="5" value="Die Macher"/>
                                boardgame.Name = child.Attributes["value"]?.Value;
                            }
                            else if (child.Name == "description")
                            {
                                //<description>From Patreon:&#10;&#10;Exploration of the City of Parapette with Part 2! In this module, you will discover the unique 4-quartered layout of the city and a whole encounter table for each of those quarters, allowing you loads of story-prompt-potential for exploring the streets of this huge castle town.&#10;&#10;I'm afraid I don't have the first chapter of 'Rookstone Legacy' available yet. Its very nearly finished but I want it to be perfect and complete before I release it. Its been a crazy month with work and also going to London MCM Comiccon which took up a whole weekend! It was a great show though, awesome to be able to start going back to events!&#10;&#10;</description>
                                boardgame.Description = child.InnerText;
                            }
                            else if (child.Name == "yearpublished")
                            {
                                //<yearpublished value="1986"/>
                                boardgame.YearPublished = Convert.ToInt32(child.Attributes["value"]?.Value);
                            }
                            else if (child.Name == "minplayers")
                            {
                                //<minplayers value="3"/>
                                boardgame.MinPlayers = Convert.ToInt32(child.Attributes["value"]?.Value);
                            }
                            else if (child.Name == "maxplayers")
                            {
                                //<maxplayers value="5"/>
                                boardgame.MaxPlayers = Convert.ToInt32(child.Attributes["value"]?.Value);
                            }
                            else if (child.Name == "playingtime")
                            {
                                //<playingtime value="240"/>
                                boardgame.PlayingTime = Convert.ToInt32(child.Attributes["value"]?.Value);
                            }
                            else if (child.Name == "minplaytime")
                            {
                                //<minplaytime value="240"/>
                                boardgame.MinPlayTime = Convert.ToInt32(child.Attributes["value"]?.Value);
                            }
                            else if (child.Name == "maxplaytime")
                            {
                                //<maxplaytime value="240"/>
                                boardgame.MaxPlayTime = Convert.ToInt32(child.Attributes["value"]?.Value);
                            }
                            else if (child.Name == "minage")
                            {
                                //<minage value="14"/>
                                boardgame.MaxPlayTime = Convert.ToInt32(child.Attributes["value"]?.Value);
                            }
                            else if (child.Name == "link" && child.Attributes["type"]?.Value == "boardgamecategory")
                            {
                                // <link type="boardgamecategory" id="1021" value="Economic"/>
                                boardgame.Categories.Add(child.Attributes["value"]?.Value);
                            }
                            else if (child.Name == "link" && child.Attributes["type"]?.Value == "boardgamemechanic")
                            {
                                // <link type="boardgamemechanic" id="2916" value="Alliances"/>
                                boardgame.Mechanics.Add(child.Attributes["value"]?.Value);
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
                                        statistics.UsersRated = Convert.ToInt32(rating.Attributes["value"]?.Value);
                                    }
                                    else if (rating.Name == "average")
                                    {
                                        //  <average value="7.60363"/>
                                        statistics.Avarage = Convert.ToDouble(rating.Attributes["value"]?.Value);
                                    }
                                    else if (rating.Name == "bayesaverage")
                                    {
                                        // <bayesaverage value="7.06781"/>
                                        statistics.BayesAverage = Convert.ToDouble(rating.Attributes["value"]?.Value);
                                    }
                                    else if (rating.Name == "stddev")
                                    {
                                        // <stddev value="1.57315"/>
                                        statistics.StandardDeviation = Convert.ToDouble(rating.Attributes["value"]?.Value);
                                    }
                                    else if (rating.Name == "median")
                                    {
                                        // <median value="0"/>
                                        statistics.Median = Convert.ToDouble(rating.Attributes["value"]?.Value);
                                    }
                                    else if (rating.Name == "owned")
                                    {
                                        // <owned value="7919"/>
                                        statistics.Owned = Convert.ToInt32(rating.Attributes["value"]?.Value);
                                    }
                                    else if (rating.Name == "trading")
                                    {
                                        // <trading value="250"/>
                                        statistics.Trading = Convert.ToInt32(rating.Attributes["value"]?.Value);
                                    }
                                    else if (rating.Name == "wanting")
                                    {
                                        // <wanting value="510"/>
                                        statistics.Wanting = Convert.ToInt32(rating.Attributes["value"]?.Value);
                                    }
                                    else if (rating.Name == "wishing")
                                    {
                                        // <wishing value="2118"/>
                                        statistics.Whishing = Convert.ToInt32(rating.Attributes["value"]?.Value);
                                    }
                                    else if (rating.Name == "numcomments")
                                    {
                                        // <numcomments value="2118"/>
                                        statistics.Comments = Convert.ToInt32(rating.Attributes["value"]?.Value);
                                    }
                                    else if (rating.Name == "numweights")
                                    {
                                        // <numweights value="2118"/>
                                        statistics.Weights = Convert.ToInt32(rating.Attributes["value"]?.Value);
                                    }
                                    else if (rating.Name == "averageweight")
                                    {
                                        // <averageweight value="2118"/>
                                        statistics.AverageWeight = Convert.ToDouble(rating.Attributes["value"]?.Value);
                                    }
                                    else if (rating.Name == "ranks")
                                    {
                                        // <ranks>
                                        foreach (XmlElement rank in rating.ChildNodes.Cast<XmlNode>().Where(n => n is XmlElement))
                                        {
                                            // <rank type="subtype" id="1" name="boardgame" friendlyname="Board Game Rank" value="368" bayesaverage="7.06781"/>
                                            statistics.Ranks.Add(new Rank() {
                                                Id = Convert.ToInt32(rank.Attributes["id"]?.Value ?? "-1"),
                                                Name = rank.Attributes["name"]?.Value,
                                                Type = rank.Attributes["type"]?.Value,
                                                FriendlyName = rank.Attributes["friendlyname"]?.Value,
                                                Value = Convert.ToInt32(rating.Attributes["value"]?.Value ?? "-1")
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
            File.WriteAllText("./database.json",jsonString);
        }
    }
}