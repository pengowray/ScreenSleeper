# ScreenSleeper

A simple MQTT client to turn off your screen and/or lock your PC when triggered by a MQTT message. For Windows 10; may work on other Windows versions but not yet tested.

Motivation: So turning off the light switch also turns off the PC's screen. Or automatically lock your PC when you leave the house. It's kind of satisfying to turn off everything at once if you need to leave your PC running.

This Windows MQTT client is designed for use with Home Assistance (HAOS), but will connect to any MQTT server.

If you don't know what MQTT is and you're not running Home Assistant already, then there might be a learning curve to get those things runnings first.

## Setting up ScreenSleeper

This is jsut a skeleton outline for now

* Compile and Install (no release version yet)
* Run for the first time
* Find config file
* Edit and save config
* Connect
* Auto start by making a shortcut in `shell:startup`

## Configure Home Assistant
* Install Home Assistant
* Install Mosquito Broker
* Create a user
* Secure the user
* Copy details to ScreenSleeper's configuration
