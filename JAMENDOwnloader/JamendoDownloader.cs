/*
 * Copyright (c) 2011 Daniel Kirstenpfad - http://www.technology-ninja.com
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 *
 * Redistributions of source code must retain the above copyright notice,
 * this list of conditions and the following disclaimer.
 *
 * Redistributions in binary form must reproduce the above copyright
 * notice, this list of conditions and the following disclaimer in the
 * documentation and/or other materials provided with the distribution.
 *
 * Neither the name of the project's author nor the names of its
 * contributors may be used to endorse or promote products derived from
 * this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
 * LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS
 * FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
 * HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED
 * TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
 * PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
 * LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using JAMENDOwnloader.XML;
using System.Net;
using CommandLine.Utility;

namespace JAMENDOwnloader
{
    public class JamendoDownloader
    {
        #region Help Text
        static void DisplayHelpText()
        {
            Console.WriteLine("You need to specify more parameters:");
            Console.WriteLine();
            Console.WriteLine("JAMENDOwnloader </type:<mp3|ogg>> [/threads:2] </input:<catalog-xml-file>> </output:<directory>> [options]");
            Console.WriteLine();
            Console.WriteLine("there are some optional parameters available:");
            Console.WriteLine(" /coversize:<200-600> -   the desired size of cover-art, Default: 400");
            Console.WriteLine(" /donotdownload       -   this will not download anything but only output a GraphQL script.");
            Console.WriteLine(" /?                   -   displays this help text");
            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine("   JAMENDOwnloader /type:mp3 /threads:4 /input:catalog.xml /output:Jamendo");
            Console.WriteLine("This will use catalog.xml in the same folder to download mp3-formatted tracks and output it");
            Console.WriteLine("into the ./Jamendo folder");
        }
        #endregion

        static void Main(string[] args)
        {
            Console.WriteLine("JAMENDOwnloader 1.0");
            Console.WriteLine("Copyright (c) 2011 Daniel Kirstenpfad - http://www.technology-ninja.com");
            Console.WriteLine();

            Arguments CommandLine = new Arguments(args);
            String CatalogFile = "";
            String DownloadPath = "";
            byte NumberOfThreads = 1;   // default
            bool DoNotDownload = false;
            String DownloadType = ".mp3";
            String JamendoDownloadType = "mp31";
            Int32 CoverSize = 400;

            #region Parameter handling

            #region /?
            if (CommandLine["?"] != null)
            {
                DisplayHelpText();
                return;           
            }
            #endregion

            #region /input
            if (CommandLine["input"] != null)
                CatalogFile = CommandLine["input"];
            else
            {
                Console.WriteLine("You need to define a catalog xml file using the /input: parameter.");
                return;
            }
            #endregion

            #region /output
            if (CommandLine["output"] != null)
            {
                DownloadPath = CommandLine["output"];            
                if (!Directory.Exists(DownloadPath))
                {
                    Console.WriteLine("Output directory does not exists!");
                    return;
                }
            }
            else
            {
                Console.WriteLine("You need to define an output folder using the /output: parameter.");
                return;
            }
            #endregion

            #region /threads
            if (CommandLine["threads"] != null)
                NumberOfThreads = Convert.ToByte(CommandLine["threads"]);
            #endregion

            #region /threads
            if (CommandLine["threads"] != null)
                NumberOfThreads = Convert.ToByte(CommandLine["threads"]);
            #endregion

            #region /coversize
            if (CommandLine["coversize"] != null)
                CoverSize = Convert.ToInt32(CommandLine["coversize"]);
            #endregion

            #region /type
            if (CommandLine["type"] != null)
                if (CommandLine["type"].ToUpper() == "OGG")
                {
                    DownloadType = ".ogg";
                    JamendoDownloadType = "ogg2";
                }
            #endregion

            #region /donotdownload
            if (CommandLine["donotdownload"] != null)
            {
                DoNotDownload = true;
            }
            #endregion

            #region not enough parameters
            if (args.Length < 2)
            {
                DisplayHelpText();
                return;
            }
            #endregion

            #endregion

            #region Initialize
            ParallelDownloader pDownloader = new ParallelDownloader(NumberOfThreads);
            TextReader reader = new StreamReader(CatalogFile);
            XmlSerializer serializer = new XmlSerializer(typeof(JamendoData));
            JamendoData xmldata = (JamendoData)serializer.Deserialize(reader);
            TextWriter graphQLOutputFile = new StreamWriter(DownloadPath + "\\jamendo.gql", false);

            #endregion

            #region Parse the XML
            Console.Write("Parsing XML Data...");

            Console.WriteLine("done!");
            Console.WriteLine("Whoohooo - we have " + xmldata.Artists.LongLength + " Artists in the catalog.");
            #endregion

            long DownloadedArtists = 0;
            long DownloadedAlbums = 0;
            long DownloadedTracks = 0;

            #region Now iterate through all artists, albums and tracks and find out which ones should be downloaded

            #region GraphQL scheme
            graphQLOutputFile.WriteLine("CREATE VERTEX TYPE City ATTRIBUTES (String Name)");
            graphQLOutputFile.WriteLine("CREATE VERTEX TYPE State ATTRIBUTES (String Name)");
            graphQLOutputFile.WriteLine("CREATE VERTEX TYPE Country ATTRIBUTES (String Name)");
            graphQLOutputFile.WriteLine("CREATE VERTEX TYPE Location ATTRIBUTES (Double Longitude, Double Latitude, Country Country, State State, City City)");
            graphQLOutputFile.WriteLine("CREATE VERTEX TYPE Genre ATTRIBUTES (String GenreName)");
            graphQLOutputFile.WriteLine("CREATE VERTEX TYPE Album ATTRIBUTES (String Name, Int64 ID");
            graphQLOutputFile.WriteLine("CREATE VERTEX TYPE Artist ATTRIBUTES (String Name, Int64 ID, String URL, String ImageURL, SET<Album> Albums)");
            #endregion

            foreach (JamendoDataArtistsArtist _artist in xmldata.Artists)
            {
                Console.WriteLine(" \\- " + PathValidation.CleanFileName(_artist.name));

                String ArtistPath = DownloadPath + Path.DirectorySeparatorChar + PathValidation.CleanFileName(_artist.name);

                #region handle artist metadata

                graphQLOutputFile.WriteLine("INSERT INTO Artist VALUES(Name='" + _artist.name.Replace("'","\\'") + "',ID=" + _artist.id + ",URL='" + _artist.url + "', ImageURL='" + _artist.image+"')");

                #endregion

                #region eventually create artist directory
                if (!Directory.Exists(ArtistPath))
                {
                    if (!DoNotDownload)
                        Directory.CreateDirectory(ArtistPath);
                }
                #endregion

                foreach (JamendoDataArtistsArtistAlbumsAlbum _album in _artist.Albums)
                {
                    Console.WriteLine("     \\ - " + PathValidation.CleanFileName(_album.name));
                    String AlbumPath = ArtistPath + Path.DirectorySeparatorChar + PathValidation.CleanFileName(_album.name);

                    #region handle album metadata
                    #endregion

                    #region eventually create album directory
                    if (!Directory.Exists(AlbumPath))
                    {
                        if (!DoNotDownload)
                            Directory.CreateDirectory(AlbumPath);
                    }
                    #endregion

                    #region Get Album-Art
                    String AlbumArtPath = AlbumPath+Path.DirectorySeparatorChar+"cover.jpg";

                    if (!File.Exists(AlbumArtPath))
                    {
                        String AlbumArt ="";
                        //if (_album.id.Length == 3)
                        //    AlbumArt = "http://imgjam.com/albums/s0/" + _album.id + "/covers/1.400.jpg";

                        //if (_album.id.Length == 4)
                        //    AlbumArt = "http://imgjam.com/albums/s" + _album.id.Substring(0, 1) + "/" + _album.id + "/covers/1.400.jpg";

                        //if (_album.id.Length == 5)
                        //    AlbumArt = "http://imgjam.com/albums/s" + _album.id.Substring(0, 2) + "/" + _album.id + "/covers/1.400.jpg";

                        AlbumArt = "http://api.jamendo.com/get2/image/album/redirect/?id="+_album.id+"&imagesize="+CoverSize;

                        try
                        {
                            //WebClient webClient = new WebClient();
                            //webClient.DownloadFile(AlbumArt, AlbumArtPath);
                            if (!DoNotDownload)
                                pDownloader.AddToQueue(AlbumArt, AlbumArtPath);

                            Console.WriteLine("           \\ - Cover");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("URL:" +AlbumArt);
                            Console.WriteLine(e.Message);
                        }
                        
                    }
                    #endregion                    

                    long AlbumTracks = 0;

                    foreach (JamendoDataArtistsArtistAlbumsAlbumTracksTrack _track in _album.Tracks)
                    {
                        AlbumTracks++;

                        String TrackNumber = "";

                        if (AlbumTracks < 10)
                            TrackNumber = "0" + AlbumTracks;
                        else
                            TrackNumber = "" + AlbumTracks;

                        String TrackPath = AlbumPath + Path.DirectorySeparatorChar + PathValidation.CleanFileName(TrackNumber + " - " + _track.name) + DownloadType;

                        #region handle track metadata
                        #endregion

                        #region Download if not existing
                        if (!File.Exists(TrackPath))
                        {
                            try
                            {
                                Console.WriteLine("           \\ - " + PathValidation.CleanFileName(_track.name) + ", " + _track.duration + ", " + _track.id3genre);
                                //WebClient webClient = new WebClient();
                                //webClient.DownloadFile("http://api.jamendo.com/get2/stream/track/redirect/?id=" + _track.id + "&streamencoding=" + JamendoDownloadType, TrackPath);
                                if (!DoNotDownload)
                                    pDownloader.AddToQueue("http://api.jamendo.com/get2/stream/track/redirect/?id=" + _track.id + "&streamencoding=" + JamendoDownloadType, TrackPath);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("URL:" + "http://api.jamendo.com/get2/stream/track/redirect/?id=" + _track.id + "&streamencoding=" + JamendoDownloadType);
                                Console.WriteLine(e.Message);
                            }

                        }
                        #endregion
                        DownloadedTracks++;
                    }
                    DownloadedAlbums++;
                }
                DownloadedArtists++;
            }
            #endregion

            graphQLOutputFile.Flush();
            graphQLOutputFile.Close();
        }
    }
}
