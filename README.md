# LightstripSyncClient
(working title lol)

A Windows Desktop Client to control Govee Lights.

Includes a mode to sync the colour of the lights with the desktop colour. 

Thanks to BeauJBurroughs https://github.com/BeauJBurroughs/Govee-H6127-Reverse-Engineering for doing a lot of the hard work and figuring out how the lights work with Bluetooth Low Energy.

### Current confirmed working models are 
H6141,  H6181.

If you have a different model from above, try testing it out and let me know if it works. From what I can tell, most Govee lights use the same UUIDs for BLE connections, so it should all work.

### How to use
Run the .exe in the release file, and the select your lightstrip device and click connect. The device name should start with "ihoment".

### Bugs
Sometimes the app fails to connect properly. If this happens, just relaunch the app. Also sometime it will lose connection if you don't change the lights for a while, so again, just relaunch. This shouldn't happen in sync mode though.

### Future Updates
Better error handling
Advanced sync options (Refresh rate/smooth speed/black/white filters/minimum values)

## To Govee
If you're not happy with the repo being available, let me know and I will remove it. I created this app as a side project to test my abilities and also be able to sync my lights with my PC and there isn't (yet) an official solution. 
Thanks for making affordable light strips so I don't have to shell out thousands to make my desk look like a space ship.

