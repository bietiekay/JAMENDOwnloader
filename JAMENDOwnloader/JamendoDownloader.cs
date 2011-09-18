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

namespace JAMENDOwnloader
{
    class JamendoDownloader
    {
        static void Main(string[] args)
        {
            Console.WriteLine("JAMENDOwnloader 1.0");
            Console.WriteLine("Copyright (c) 2011 Daniel Kirstenpfad - http://www.technology-ninja.com");
            Console.WriteLine();
            if (args.Length < 3)
            {
                Console.WriteLine("You need to specify more parameters:");
                Console.WriteLine();
                Console.WriteLine("  JAMENDOwnloader <download-type> <catalog-xml-file> <directory>");
                Console.WriteLine();
                Console.WriteLine("  allowed download-types: mp3, ogg");
                Console.WriteLine();
                Console.WriteLine("Example:");
                Console.WriteLine(" JAMENDOwnloader mp3 catalog.xml Jamendo");
                return;
            }


            #region Parse the XML
            Console.Write("Parsing XML Data...");
            TextReader reader = new StreamReader(args[1]);
            XmlSerializer serializer = new XmlSerializer(typeof(JamendoData));
            JamendoData xmldata = (JamendoData)serializer.Deserialize(reader);
            Console.WriteLine("done!");
            Console.WriteLine("Whoohooo - we have " + xmldata.Artists.LongLength + " Artists in the catalog.");
            #endregion

            if (!Directory.Exists(args[2]))
            {
                Console.WriteLine("Output directory does not exists!");
                return;
            }

            String DownloadPath = args[2];

            String DownloadType = ".mp3";
            String JamendoDownloadType = "mp31";

            if (args[0].ToUpper() == "OGG")
            {
                DownloadType = ".ogg";
                JamendoDownloadType = "ogg2";
            }

            long DownloadedArtists = 0;
            long DownloadedAlbums = 0;
            long DownloadedTracks = 0;

            #region Now iterate through all artists, albums and tracks and find out which ones should be downloaded

            foreach (JamendoDataArtistsArtist _artist in xmldata.Artists)
            {
                Console.WriteLine(" \\- "+_artist.name);

                String ArtistPath = DownloadPath + Path.DirectorySeparatorChar + PathValidation.CleanFileName(_artist.name);

                #region handle artist metadata
                #endregion

                #region eventually create artist directory
                if (!Directory.Exists(ArtistPath))
                {
                    Directory.CreateDirectory(ArtistPath);
                }
                #endregion

                foreach (JamendoDataArtistsArtistAlbumsAlbum _album in _artist.Albums)
                {
                    Console.WriteLine("     \\ - "+_album.name);
                    String AlbumPath = ArtistPath + Path.DirectorySeparatorChar + PathValidation.CleanFileName(_album.name);

                    #region handle album metadata
                    #endregion

                    #region eventually create album directory
                    if (!Directory.Exists(AlbumPath))
                    {
                        Directory.CreateDirectory(AlbumPath);
                    }
                    #endregion

                    foreach (JamendoDataArtistsArtistAlbumsAlbumTracksTrack _track in _album.Tracks)
                    {
                        Console.WriteLine("           \\ - " + _track.name);
                        String TrackPath = AlbumPath + Path.DirectorySeparatorChar + PathValidation.CleanFileName(_track.name)+DownloadType;

                        #region handle track metadata
                        #endregion

                        #region Download if not existing
                        if (!File.Exists(TrackPath))
                        {
                            WebClient webClient = new WebClient();
                            webClient.DownloadFile("http://api.jamendo.com/get2/stream/track/redirect/?id=" + _track.id + "&streamencoding="+JamendoDownloadType, TrackPath);
                        }
                        #endregion
                        DownloadedTracks++;
                    }
                    DownloadedAlbums++;
                }
                DownloadedArtists++;
            }
            #endregion

        }
    }
}
