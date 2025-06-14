using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml;
using BGConcierge.BGG;
using BGConcierge.BGG.Models;
using Polly;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var startIndex = Convert.ToInt32(args?.Length > 0 ? args[0] : "0");
        var endIndex = Convert.ToInt32(args?.Length > 1 ? args[1] : "450000");

        var startTime = DateTime.Now;
        const int PAGE_SIZE = 20;
        const int PARALLEL_REQUESTS = 2;
        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        var retryPolicy = Policy
            .Handle<Refit.ApiException>()
            .WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) +
                TimeSpan.FromMilliseconds(new Random().Next(0, 500))
            );

        File.WriteAllText("./database.json", "[");
        bool isFirstItem = true;
        var realIndex = 1;
        var realTotal = endIndex - startIndex;
        using (var proxy = BGGClientFactory.GetXmlApi())
        {
            for (int i = startIndex; i < endIndex; i += PAGE_SIZE * PARALLEL_REQUESTS)
            {
                try
                {
                    realIndex += PAGE_SIZE * PARALLEL_REQUESTS;
                    LogProgress(startTime, realIndex++, realTotal);

                    // Criar duas tasks para requisições paralelas
                    var tasks = new List<Task<XmlDocument>>();
                    for (int j = 0; j < PARALLEL_REQUESTS; j++)
                    {
                        var currentIndex = i + (j * PAGE_SIZE);
                        if (currentIndex >= endIndex) break;

                        var ids = string.Join(",", Enumerable.Range(currentIndex, PAGE_SIZE));
                        var task = retryPolicy.ExecuteAsync(async () =>
                        {
                            Console.WriteLine($"Requisitando lote {currentIndex} até {currentIndex + PAGE_SIZE - 1}");
                            return await proxy.GetThings(ids);
                        });
                        tasks.Add(task);
                    }

                    // Aguarda todas as requisições completarem
                    var results = await Task.WhenAll(tasks);

                    // Processa os resultados
                    foreach (var xml in results)
                    {
                        if (xml.DocumentElement?.ChildNodes?.Count == 0)
                            continue;

                        var boardgames = ProcessXmlDocument(xml);

                        // Salva os resultados
                        foreach (var boardgame in boardgames)
                        {
                            File.AppendAllText(
                                "./database.json",
                                $"{(isFirstItem ? string.Empty : ",")}\n{JsonSerializer.Serialize(boardgame, jsonOptions)}"
                            );
                            isFirstItem = false;
                        }
                    }

                    // Delay entre grupos de requisições
                    await Task.Delay(TimeSpan.FromSeconds(2));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Erro: {ex.Message}");
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
            }
        }
        File.AppendAllText("./database.json", "]");
    }

    private static List<Boardgame> ProcessXmlDocument(XmlDocument xml)
    {
        var boardgames = new List<Boardgame>();

        Parallel.ForEach(xml.DocumentElement.ChildNodes.Cast<XmlNode>().Where(n => n is XmlElement), node =>
        {
            var boardgame = new Boardgame();
            boardgame.Id = Int32.TryParse(node.Attributes["objectid"]?.Value, out var id) ? id : -1;
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
                        boardgame.YearPublished = Int32.TryParse(child.InnerText, out var year) ? year : -1;
                    }
                    else if (child.Name == "minplayers")
                    {
                        boardgame.MinPlayers = Int32.TryParse(child.InnerText, out var minPlayers) ? minPlayers : -1;
                    }
                    else if (child.Name == "maxplayers")
                    {
                        boardgame.MaxPlayers = Int32.TryParse(child.InnerText, out var maxPlayers) ? maxPlayers : -1;
                    }
                    else if (child.Name == "playingtime")
                    {
                        boardgame.PlayingTime = Int32.TryParse(child.InnerText, out var playingTime) ? playingTime : -1;
                    }
                    else if (child.Name == "minplaytime")
                    {
                        boardgame.MinPlayTime = Int32.TryParse(child.InnerText, out var minPlayTime) ? minPlayTime : -1;
                    }
                    else if (child.Name == "maxplaytime")
                    {
                        boardgame.MaxPlayTime = Int32.TryParse(child.InnerText, out var maxPlayTime) ? maxPlayTime : -1;
                    }
                    else if (child.Name == "age")
                    {
                        boardgame.MinAge = Int32.TryParse(child.InnerText, out var age) ? age : -1;
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
                                statistics.UsersRated = Int32.TryParse(rating.InnerText, out var usersRated) ? usersRated : -1;
                            }
                            else if (rating.Name == "average")
                            {
                                //  <average value="7.60363"/>
                                statistics.Avarage = Double.TryParse(rating.InnerText, CultureInfo.InvariantCulture, out var average) ? average : -1.0;
                            }
                            else if (rating.Name == "bayesaverage")
                            {
                                // <bayesaverage value="7.06781"/>
                                statistics.BayesAverage = Double.TryParse(rating.InnerText, CultureInfo.InvariantCulture, out var bayesaverage) ? bayesaverage : -1.0;
                            }
                            else if (rating.Name == "stddev")
                            {
                                // <stddev value="1.57315"/>
                                statistics.StandardDeviation = Double.TryParse(rating.InnerText, CultureInfo.InvariantCulture, out var stddev) ? stddev : -1.0;
                            }
                            else if (rating.Name == "median")
                            {
                                // <median value="0"/>
                                statistics.Median = Double.TryParse(rating.InnerText, CultureInfo.InvariantCulture, out var median) ? median : -1.0;
                            }
                            else if (rating.Name == "owned")
                            {
                                // <owned value="7919"/>
                                statistics.Owned = Int32.TryParse(rating.InnerText, out var owned) ? owned : -1;
                            }
                            else if (rating.Name == "trading")
                            {
                                // <trading value="250"/>
                                statistics.Trading = Int32.TryParse(rating.InnerText, out var trading) ? trading : -1;
                            }
                            else if (rating.Name == "wanting")
                            {
                                // <wanting value="510"/>
                                statistics.Wanting = Int32.TryParse(rating.InnerText, out var wanting) ? wanting : -1;
                            }
                            else if (rating.Name == "wishing")
                            {
                                // <wishing value="2118"/>
                                statistics.Whishing = Int32.TryParse(rating.InnerText, out var wishing) ? wishing : -1;
                            }
                            else if (rating.Name == "numcomments")
                            {
                                // <numcomments value="2118"/>
                                statistics.Comments = Int32.TryParse(rating.InnerText, out var numcomments) ? numcomments : -1;
                            }
                            else if (rating.Name == "numweights")
                            {
                                // <numweights value="2118"/>
                                statistics.Weights = Int32.TryParse(rating.InnerText, out var numweights) ? numweights : -1;
                            }
                            else if (rating.Name == "averageweight")
                            {
                                // <averageweight value="2118"/>
                                statistics.AverageWeight = Double.TryParse(rating.InnerText, CultureInfo.InvariantCulture, out var averageweight) ? averageweight : -1.0;
                            }
                            else if (rating.Name == "ranks")
                            {
                                // <ranks>
                                foreach (XmlElement rank in rating.ChildNodes.Cast<XmlNode>().Where(n => n is XmlElement))
                                {
                                    // <rank type="subtype" id="1" name="boardgame" friendlyname="Board Game Rank" value="368" bayesaverage="7.06781"/>
                                    statistics.Ranks.Add(new Rank()
                                    {
                                        Id = Int32.TryParse(rank.Attributes["id"]?.Value, out var numweights) ? numweights : -1,
                                        Name = rank.Attributes["name"]?.Value,
                                        Type = rank.Attributes["type"]?.Value,
                                        FriendlyName = rank.Attributes["friendlyname"]?.Value,
                                        Value = Int32.TryParse(rank.Attributes["value"]?.Value, out var val) ? val : -1,
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

        return boardgames;
    }

    private static void LogProgress(DateTime startTime, int current, int total)
    {
        var elapsed = DateTime.Now - startTime;
        var progress = (double)current / (double)total * 100;
        var estimatedTotal = (double)elapsed.TotalMinutes / (double)((double)progress / (double)100);
        var remaining = TimeSpan.FromMinutes((double)estimatedTotal - (double)elapsed.TotalMinutes);

        Console.WriteLine(new string('-', 50));
        Console.WriteLine($"📊 Progresso: {progress:F2}% ({current}/{total})");
        Console.WriteLine($"⏱️ Tempo decorrido: {elapsed.ToString(@"hh\:mm\:ss")}");
        Console.WriteLine($"🎯 Tempo restante est.: {remaining.ToString(@"hh\:mm\:ss")}");
        Console.WriteLine(new string('-', 50));
    }

    // private static async Task Main(string[] args)
    // {
    //     var startIndex = Convert.ToInt32(args != null && args.Length > 0 ? args[0] : "0");
    //     var endIndex = Convert.ToInt32(args != null && args.Length > 1 ? args[1] : "450000");

    //     var startTime = DateTime.Now;
    //     var page = 20;
    //     List<Boardgame> boardgames = new List<Boardgame>();
    //     var jsonOptions = new JsonSerializerOptions
    //     {
    //         WriteIndented = true
    //     };

    //     File.WriteAllText("./database.json", "[");
    //     bool isFirstItem = true; // Flag para controlar a primeira entrada
    //     using (var proxy = BGGClientFactory.GetXmlApi())
    //     {

    //         var random = new Random();
    //         for (int i = startIndex; i < endIndex; i += page)
    //         {
    //             boardgames.Clear();
    //             Console.WriteLine($"Start: {i} End: {endIndex}");
    //             Console.WriteLine($"{(DateTime.Now - startTime).ToString("c")} {(decimal)i / endIndex * 100}% Total:{boardgames.Count}");
    //             Console.WriteLine($"Requesting {i} to {i + page - 1}...");
    //             var xml = await Policy
    //                 .Handle<Refit.ApiException>()
    //                 .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
    //                 .ExecuteAsync(async () =>
    //                     await proxy.GetThings(string.Join(",", Enumerable.Range(i, page))));

    //             if (xml.DocumentElement?.ChildNodes?.Count == 0)
    //                 break;



    //             foreach (var boardgame in boardgames)
    //             {
    //                 File.AppendAllText("./database.json", $"{(isFirstItem ? string.Empty : ",")}\n{JsonSerializer.Serialize(boardgame, jsonOptions)}");
    //                 isFirstItem = false;
    //             }
    //         }
    //     }
    //     File.AppendAllText("./database.json", "]");
    // }
}
