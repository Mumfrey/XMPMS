Unreal 2 Expanded Multiplayer Master Server
===========================================

Once upon a time, Legend Entertainment made an awesome game called Unreal 2 Expanded Mulitplayer (U2XMP), the master server was run by Atari and was about as reliable as British Rail on wet tuesday afternoon. It would frequently go down for long periods and after the termination of Legend Entertainment in January of 2004 it got progressively worse.

Some of us worried that the master server would eventually go down for good and had the common sense to make Ethereal logs of the master server protocol to give us a fighting chance of engineering a new Master Server should the official ones ever go down for good. This turned out to be remarkably prescient.

The original replacement Master Server was written by ]HoC[Omen using my Ethereal Traces and ran for quite some time but didn't fully implement the protocol and was prone to crashing after receiving too many queries or when less than two servers were active - which rapidly became the case. Thus, around 2010 - six years after the demise of Legend Entertainment - I revisited my original Ethereal logs and engineered a somewhat sturdier master server in C# (Omen's original effort having been written in VB and the sources lost in the mists of time) and this is what you see here.

I freely admit that this is a pretty dreadful implementation of a master server, but it works quite happily with any ~2225 builds of the unreal engine which use the "stock" Epic protocol (for example, the much maligned UT 2003) and works with minor alterations with other games based on the same underlying engine version.

I'm posting this code mainly in the hope that it's of interest to somebody, I spent about 3 months developing this to the state it's in (which is when I lost interest - hence all the unfinished features) and to the best of my knowledge it implements all of the protocol (although maybe not correctly, but that's a different issue!) and works pretty well. I've been running this code as the current XMP master server for 4 years at time of writing with minimal crashing.

If this code is of interest to you, then I guess you probably need to get out more.

License
-------
This implementation was written from scratch using information gleaned from Ethereal logs of the master server protocol and is released under the GNU General Public License v3.

If any of this code causes your computer to explode, I hereby disclaim all liability and all that.