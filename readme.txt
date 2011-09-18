JAMENDO Downloader Tool
Copyright (c) 2011 Daniel '@bietiekay' Kirstenpfad - http://www.technology-ninja.com - All rights reserved.

What is Jamendo?
================

This is a tool to help you download music from jamendo.com. Jamendo is a community of free, legal and 
unlimited music published under Creative Commons licenses. Share your music, download your favorite
artists!

What does it do?
================

This tool takes the downloadable jamendo catalog xml file (see Pre-Requisites) and reads it 
all in.

Then it iterates through all artists, their albums and their tracks and tries to download
those tracks. It stores all tracks into a directory structure which will look like this:

\ - ArtistName
	\ - AlbumName1
		\ - TrackName1
		\ - TrackName2
	\ - AlbumName2
		\ - TrackName1
		\ - TrackName2

If you run this tool on the Download directory and a new catalog xml file (which jamendo 
generates in regular intervals) it will determine which files you already have and which
files you need to be "in-sync".

Be warned that this download will be humongeous (>300k tracks) and you will need several
years to consume all of those tracks.

Additionally this tool will export all the meta-data in graph-form. When you import the
resulting GraphQL script in a sones GraphDB instance (http://github.com/sones/sones) you
are able to run quite interesting queries on the data.

Why?!
======

The resulting >300k track download set is huge. It's really really huge. Since it's also
creative-commons music it's quite interesting to have such a huge and neatly annotated 
library of music. That's mainly the cause I've written this tool.

Oh and thanks to @simcup (twitter: http://twitter.com/#!/simcup/status/115186326776717312) 
who pointed me to the possiblity of downloading this all at once.

Pre-Requisites
===============

You need some prerequisites to use this tool:

- Software:
	- an operating system compatible either with Microsoft .NET or MONO.
	- Microsoft .NET 4.0  OR Mono 2.8 installation
- Data:
	- a jamendo catalog xml file. How to obtain one of these can be read
	  here: http://developer.jamendo.com/en/wiki/NewDatabaseDumps
