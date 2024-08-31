# Angel-Island Core 6
64bit, .NET 8 Ultima Online free Server for Angel-Island and Siege Perilous

# Launch Parameters
-uosp       // launch as Siege Perilous<br/>
-uoai       // launch as Angel Island<br/>
-uols       // launch as Login Server<br/>
-usedb      // use SQLite common user password database (shares across servers)<br/>
-nopatch    // don't run startup patcher (patches world files. not needed for fresh installations.)<br/>
-uotc       // launch as test center. May be combiled with -uosp or -uoai<br/>

# Examples:
"GMN Core 3.0.exe" -uosp -usedb -nopatch         // launch Siege Perilous<br/>
"GMN Core 3.0.exe" -uoai -usedb -nopatch         // launch Angel Island<br/>
"GMN Core 3.0.exe" -uosp -uotc -usedb -nopatch   // launch Siege Perilous Test Center<br/>

# Basic Setup:
If you are using the Login Server (recommended) the standard connection port: 2593 is used.<br/>
If you wish to connect to the shards directly:<br/>
AngelIslandPort = 3593<br/>
TestCenterPort = 3594<br/>
SiegePerilousPort = 3595<br/>
LoginServerPort = 2593<br/>

Create folders for each server. Example: ShardAI, ShardSP, ShardTC, ShardLS<br/>
Launch each of the servers, launch your client, and connect the the Login Server at port:2593<br/>

# Administration:
The first administrator account will be AccessLevel.Owner. This is one level above AccessLevel.Administrator

# Optional Extras:
The Angel Island Core uses a handshake with the ClassicUO and RazorCE to ensure the players use an approved client configuration.
Both modified versions of ClassicUO and RazorCE are available here in their respective repos.

# World Files:
You'll also find the completely spawned Angel Island and Siege Perilous worlds here.<br/>
Angel Island is a customized OSI Publish 15 spawn.<br/>
Siege Perilous is a fairly accurate OSI Publish 13.6 spawn.<br/>
Both worlds contain the Custom Housing Preview area. This is an area where players can see and purchase any of our 56 Custom Houses. (you don't want to recreate this by hand.)<br/>
I also left all Townships intact so that your new staff can see, evaluate, and understand how elaborate they can be (before deleting, and/or awarding them in some way.)<br/>
Finally, these world files contain the complete collection of player-composed music with the Angel Island Music System. There are probably ~100 songs in the library. Note, AI & SP's music libraries likely differ.<br/>
You can get a feel for the music system as well as many other features on my YouTube channel
https://www.youtube.com/@UltimaOnlineFreeShard

# Getting Started Videos
https://youtu.be/0kauX-W5BLI <br/>
https://youtu.be/8zNv5ryd6_U

# Discord

https://discord.gg/GXcR2DsSnb