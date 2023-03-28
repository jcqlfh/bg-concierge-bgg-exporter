using BGConcierge.BGG;
using System.Xml;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var page = 200;
        using (var proxy = BGGClientFactory.GetXmlApi2())
        {
            var random = new Random();
            for( int i = 0; i < 100; i+=page)
            {
                var xml = await proxy.GetThings(new List<int>(Enumerable.Range(i, page)));
                if (xml.DocumentElement?.ChildNodes.Count > 0)
                    Parallel.ForEach(xml.DocumentElement.ChildNodes.OfType<XmlNode>(), node =>
                    {
                        if (node != null && node is XmlElement)
                        {
                            //<item type="boardgame" id="1">
                            if(node.Attributes != null && node.Attributes["id"] != null)
                                Console.WriteLine(node.Attributes["id"].Value ?? "");
                            
                            //<thumbnail>https://cf.geekdo-images.com/rpwCZAjYLD940NWwP3SRoA__thumb/img/YT6svCVsWqLrDitcMEtyazVktbQ=/fit-in/200x150/filters:strip_icc()/pic4718279.jpg</thumbnail>
                            
                            //<name type="primary" sortindex="5" value="Die Macher"/>

                            //<description>From Patreon:&#10;&#10;Exploration of the City of Parapette with Part 2! In this module, you will discover the unique 4-quartered layout of the city and a whole encounter table for each of those quarters, allowing you loads of story-prompt-potential for exploring the streets of this huge castle town.&#10;&#10;I'm afraid I don't have the first chapter of 'Rookstone Legacy' available yet. Its very nearly finished but I want it to be perfect and complete before I release it. Its been a crazy month with work and also going to London MCM Comiccon which took up a whole weekend! It was a great show though, awesome to be able to start going back to events!&#10;&#10;</description>
                            //<yearpublished value="1986"/>
                            //<minplayers value="3"/>
                            //<maxplayers value="5"/>
                            //<playingtime value="240"/>
                            //<minplaytime value="240"/>
                            //<maxplaytime value="240"/>
                            //<minage value="14"/>

                            // <link type="boardgamecategory" id="1021" value="Economic"/>
                            // <link type="boardgamecategory" id="1026" value="Negotiation"/>
                            // <link type="boardgamecategory" id="1001" value="Political"/>
                            // <link type="boardgamemechanic" id="2916" value="Alliances"/>
                            // <link type="boardgamemechanic" id="2080" value="Area Majority / Influence"/>
                            // <link type="boardgamemechanic" id="2012" value="Auction/Bidding"/>
                            // <link type="boardgamemechanic" id="2072" value="Dice Rolling"/>
                            // <link type="boardgamemechanic" id="2040" value="Hand Management"/>
                            // <link type="boardgamemechanic" id="2020" value="Simultaneous Action Selection"/>

                            // <statistics page="1">
                            // <ratings>
                            // <usersrated value="5621"/>
                            // <average value="7.60363"/>
                            // <bayesaverage value="7.06781"/>
                            // <ranks>
                            // <rank type="subtype" id="1" name="boardgame" friendlyname="Board Game Rank" value="368" bayesaverage="7.06781"/>
                            // <rank type="family" id="5497" name="strategygames" friendlyname="Strategy Game Rank" value="212" bayesaverage="7.2002"/>
                            // </ranks>
                            // <stddev value="1.57315"/>
                            // <median value="0"/>
                            // <owned value="7919"/>
                            // <trading value="250"/>
                            // <wanting value="510"/>
                            // <wishing value="2118"/>
                            // <numcomments value="2060"/>
                            // <numweights value="776"/>
                            // <averageweight value="4.3144"/>
                            // </ratings>
                            // </statistics>
                        }
                    });
                Thread.Sleep(random.Next(500, 1000));
            }
        }
    }
}