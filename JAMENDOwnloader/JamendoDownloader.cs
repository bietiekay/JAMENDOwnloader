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
using System.Threading;

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
            Console.WriteLine(" /donotoutputdata     -   this will supress any album/artist/song data output.");
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
            bool DoNotOutputData = false;
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

            #region /donotoutputdata
            if (CommandLine["donotoutputdata"] != null)
            {
                DoNotOutputData = true;
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
            Console.Write("Parsing XML Data...");
            ParallelDownloader pDownloader = new ParallelDownloader(NumberOfThreads);
            TextReader reader = new StreamReader(CatalogFile);
            XmlSerializer serializer = new XmlSerializer(typeof(JamendoData));
            JamendoData xmldata = (JamendoData)serializer.Deserialize(reader);

            List<String> Scheme = new List<string>();
            List<String> ID3Inserts = new List<string>();
            List<String> TagsInsert = new List<string>();
            List<String> TrackInsert = new List<string>();
            List<String> AlbumInsert = new List<string>();
            List<String> ArtistInsert = new List<string>();
            List<String> ArtistAlbumEdges = new List<string>();
            List<String> AlbumTrackEdges = new List<string>();
            #endregion

            #region Parse the XML
            Console.WriteLine("done!");
            Console.WriteLine("Whoohooo - we have " + xmldata.Artists.LongLength + " Artists in the catalog.");
            #endregion

            long DownloadedArtists = 0;
            long DownloadedAlbums = 0;
            long DownloadedTracks = 0;

            #region Now iterate through all artists, albums and tracks and find out which ones should be downloaded

            #region GraphQL scheme
            Scheme.Add("CREATE VERTEX TYPE City ATTRIBUTES (String Name)");
            Scheme.Add("CREATE VERTEX TYPE State ATTRIBUTES (String Name)");
            Scheme.Add("CREATE VERTEX TYPE Country ATTRIBUTES (String Name)");
            Scheme.Add("CREATE VERTEX TYPE Location ATTRIBUTES (Double Longitude, Double Latitude, Country Country, State State, City City)");
            Scheme.Add("CREATE VERTEX TYPE ID3Genre ATTRIBUTES (Byte ID, String Name)");
          
            Scheme.Add("CREATE VERTEX TYPE Tag ATTRIBUTES (String Tagname)");
            Scheme.Add("CREATE VERTEX TYPE Track ATTRIBUTES (UInt64 ID, Double Duration, String Name, String License, Int64 TrackNumber, String MusicbrainzID, ID3Genre ID3Genre, SET<Tag> Tags) INDICES (ID)");
            Scheme.Add("CREATE VERTEX TYPE Album ATTRIBUTES (String Name, UInt64 ID, DateTime ReleaseDate, Byte ID3Genre, String ArtworkLicense, String URL, String MusicbrainzID, SET<Track> Tracks) INDICES (ID)");
            
            Scheme.Add("CREATE VERTEX TYPE Artist ATTRIBUTES (String Name, UInt64 ID, String URL, String ImageURL, SET<Album> Albums) INDICES (ID)");
            Scheme.Add("ALTER VERTEX TYPE Album ADD INCOMINGEDGES (Artist.Albums Album)");

            ID3Genre id3genre_ = new ID3Genre();
            foreach (Byte _id3id in id3genre_.ID3.Keys)
            {
                ID3Inserts.Add("INSERT INTO ID3Genre VALUES(ID=" + _id3id + ",Name='" + id3genre_.ID3[_id3id] + "')");
            }


            #endregion

            foreach (JamendoDataArtistsArtist _artist in xmldata.Artists)
            {
                if (!DoNotOutputData)
                    Console.WriteLine(" \\- " + PathValidation.CleanFileName(_artist.name));

                String ArtistPath = DownloadPath + Path.DirectorySeparatorChar + PathValidation.CleanFileName(_artist.name);

                #region handle artist metadata
                // we need to build the artist insert statement...
                StringBuilder artist_insert = new StringBuilder();
                artist_insert.Append("INSERT INTO Artist VALUES(");

                if (_artist.id != null)
                {
                    artist_insert.Append("ID=" + _artist.id + ",");
                }

                if (_artist.name != null)
                {
                    artist_insert.Append("Name='" + _artist.name.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", "") + "',");
                }

                if (_artist.url != null)
                {
                    artist_insert.Append("URL='" + _artist.url.Replace("\\","\\\\").Replace("'", "\\'").Replace("\n","") + "',");
                }

                if (_artist.image != null)
                {
                    artist_insert.Append("ImageURL='" + _artist.image.Replace("\\","\\\\").Replace("'", "\\'").Replace("\n","") + "',");
                }

                // if there's a , left-over at the end, remove it...
                if (artist_insert[artist_insert.Length-1] == ',')
                    artist_insert.Remove(artist_insert.Length - 1, 1);

                artist_insert.Append(")");

                ArtistInsert.Add(artist_insert.ToString());

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
                    if (!DoNotOutputData)
                        Console.WriteLine("     \\ - " + PathValidation.CleanFileName(_album.name));
                    String AlbumPath = ArtistPath + Path.DirectorySeparatorChar + PathValidation.CleanFileName(_album.name);

                    #region handle album metadata
                    // we need to build the album insert statement...
                    StringBuilder album_insert = new StringBuilder();
                    album_insert.Append("INSERT INTO Album VALUES(");

                    if (_album.id != null)
                    {
                        album_insert.Append("ID=" + _album.id + ",");
                    }

                    if (_album.name != null)
                    {
                        album_insert.Append("Name='" + _album.name.Replace("\\","\\\\").Replace("'", "\\'").Replace("\n","") + "',");
                    }

                    if (_album.id3genre != null)
                    {
                        album_insert.Append("ID3Genre=" + _album.id3genre + ",");
                    }

                    if (_album.license_artwork != null)
                    {
                        album_insert.Append("ArtworkLicense='" + _album.license_artwork.Replace("\\","\\\\").Replace("'", "\\'").Replace("\n","") + "',");
                    }

                    if (_album.url != null)
                    {
                        album_insert.Append("URL='" + _album.url.Replace("\\","\\\\").Replace("'", "\\'").Replace("\n","") + "',");
                    }

                    if (_album.mbgid != null)
                    {
                        if (_album.mbgid != "")
                            album_insert.Append("MusicbrainzID='" + _album.mbgid + "',");
                    }

                    if (_album.releasedate != null)
                    {
                        DateTime releasedateparsed;
                        // check if we can parse it, so it should be easily parseable by GraphDB
                        bool parsedSuccessfully = DateTime.TryParse(_album.releasedate, out releasedateparsed);

                        // we could parse!! yay!
                        if (parsedSuccessfully)
                            album_insert.Append("ReleaseDate='"+_album.releasedate+"',");
                    }

                    // we are filling the tracks later in the process...
                    
                    // if there's a , left-over at the end, remove it...
                    if (album_insert[album_insert.Length - 1] == ',')
                        album_insert.Remove(album_insert.Length - 1, 1);

                    album_insert.Append(")");

                    AlbumInsert.Add(album_insert.ToString());

                    // add the Artist->Album edge
                    ArtistAlbumEdges.Add("LINK Artist(ID = " + _artist.id + ") VIA Albums TO Album(ID=" + _album.id + ")");

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

                            if (!DoNotOutputData)
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
                        // we need to build the track insert statement...
                        StringBuilder track_insert = new StringBuilder();
                        track_insert.Append("INSERT INTO Track VALUES(");

                        if (_track.id != null)
                        {
                            track_insert.Append("ID=" + _track.id + ",");
                        }

                        if (_track.name != null)
                        {
                            track_insert.Append("Name='" + _track.name.Replace("\\","\\\\").Replace("'", "\\'").Replace("\n","") + "',");
                        }

                        if (_track.duration != null)
                        {
                            track_insert.Append("Duration=" + _track.duration+ ",");
                        }

                        if (_track.license != null)
                        {
                            track_insert.Append("License='" + _track.license.Replace("\\","\\\\").Replace("'", "\\'").Replace("\n","") + "',");
                        }

                        if (_track.numalbum != null)
                        {
                            track_insert.Append("TrackNumber=" + _track.numalbum+ ",");
                        }

                        if (_track.mbgid != null)
                        {
                            if (_track.mbgid != "")
                                track_insert.Append("MusicbrainzID='" + _track.mbgid + "',");
                        }

                        if (_track.id3genre != null)
                        {
                            if (id3genre_.ID3.ContainsKey(Convert.ToByte(_track.id3genre)))
                            {
                                track_insert.Append("ID3Genre=REF(ID=" + _track.id3genre + "),");
                            }
                            
                        }

                        if (_track.Tags != null)
                        {
                            //track_insert.Append("ID3Genre=" + _track.id3genre + ",");
                            //Thread.Sleep(1);
                        }


                        // we are filling the tracks later in the process...

                        // if there's a , left-over at the end, remove it...
                        if (track_insert[track_insert.Length - 1] == ',')
                            track_insert.Remove(track_insert.Length - 1, 1);

                        track_insert.Append(")");

                        TrackInsert.Add(track_insert.ToString());

                        // add the Album->Track edge
                        AlbumTrackEdges.Add("LINK Album(ID = " + _album.id + ") VIA Tracks TO Track(ID=" + _track.id + ")");

                        #endregion

                        #region Download if not existing
                        if (!File.Exists(TrackPath))
                        {
                            try
                            {
                                if (!DoNotOutputData)
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
            
            // Write the GraphQL file..
            Console.Write("Writing GraphQL file...");
            TextWriter scheme_file = new StreamWriter(DownloadPath + "\\jamendo_scheme.gql", false);
            TextWriter insert_file = new StreamWriter(DownloadPath + "\\jamendo_insert.gql", false);
            TextWriter link_file = new StreamWriter(DownloadPath + "\\jamendo_link.gql", false);

            long StatementCounter = 0;
            long Counter = 0;

            Console.Write("Scheme, ");
            // first the scheme
            Counter = 0;
            foreach (String _graphqlline in Scheme)
            {
                StatementCounter++;
                Counter++;
                scheme_file.WriteLine(_graphqlline);
            }
            Console.Write("(" + Counter + ")");

            Console.Write("ID3Genre, ");
            Counter = 0;
            foreach (String _graphqlline in ID3Inserts)
            {
                StatementCounter++;
                Counter++;
                scheme_file.WriteLine(_graphqlline);
            }
            Console.Write("(" + Counter + ")");


            Console.Write("Tags, ");
            Counter = 0;
            foreach (String _graphqlline in TagsInsert)
            {
                StatementCounter++;
                Counter++;
                insert_file.WriteLine(_graphqlline);
            }
            Console.Write("(" + Counter + ")");

            Console.Write("Songs, ");
            Counter = 0;            
            foreach (String _graphqlline in TrackInsert)
            {
                StatementCounter++;
                Counter++;
                insert_file.WriteLine(_graphqlline);
            }
            Console.Write("(" + Counter + ")");

            Console.Write("Albums, ");
            Counter = 0; 
            foreach (String _graphqlline in AlbumInsert)
            {
                StatementCounter++;
                Counter++;
                insert_file.WriteLine(_graphqlline);
            }
            Console.Write("(" + Counter + ")");

            Console.Write("Artists,");
            Counter = 0;
            foreach (String _graphqlline in ArtistInsert)
            {
                StatementCounter++;
                Counter++;
                insert_file.WriteLine(_graphqlline);
            }
            Console.Write("(" + Counter + ")");

            Console.Write(" Artist->Album,");
            Counter = 0; 
            foreach (String _graphqlline in ArtistAlbumEdges)
            {
                StatementCounter++;
                Counter++;
                link_file.WriteLine(_graphqlline);
            }
            Console.Write("(" + Counter + ")");

            Console.Write(" Album->Tracks,");
            Counter = 0;
            foreach (String _graphqlline in AlbumTrackEdges)
            {
                StatementCounter++;
                Counter++;
                link_file.WriteLine(_graphqlline);
            }
            Console.Write("(" + Counter + ")");

            scheme_file.Flush();
            insert_file.Flush();
            link_file.Flush();
            scheme_file.Close();
            insert_file.Close();
            link_file.Close();
            Console.WriteLine(" - done.");
            Console.WriteLine("All statements: " + StatementCounter);

            Console.ReadLine();
        }
    }
}
