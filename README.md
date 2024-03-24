![image](SleepScreenWPF/screensleeper-logo-128.png)

# ScreenSleeper

![image](https://github.com/pengowray/ScreenSleeper/assets/800133/e457f06a-12f9-4a50-b25e-5b32147e8099)

A simple MQTT client to turn off your screen and/or lock your PC when triggered by a MQTT message. For Windows 10; may work on other Windows versions but not yet tested.

Motivation: So turning off the light switch also turns off the PC's screen. Or automatically lock your PC when you leave the house. It's kind of satisfying to turn off everything at once if you need to leave your PC running.

This Windows MQTT client is designed for use with Home Assistance (HAOS), but will connect to any MQTT server.

If you don't know what MQTT is and you're not running Home Assistant already, then there might be a learning curve to get those things runnings first.

## Setting up ScreenSleeper

* Download and extract the [current release](https://github.com/pengowray/ScreenSleeper/releases)
* Run ScreenSleeper.exe for the first time
* Find config file (there's a "Find Config" button in the app to make this easy)
* Edit and save the config (There's a [detailed breakdown of the config file](https://github.com/pengowray/ScreenSleeper/wiki/config) in the wiki, if you need it)
* Click 'Connect'
* Set ScreenSleeper to auto start when you login by making a shortcut to it in shell:startup. (Hit `Win R` and type `shell:startup` enter; copy `ScreenSleeper.exe` from where you unzipped it and paste a shortcut into the Startup folder)

If you're using HA, [here's instructions to create an Automation to trigger ScreenSleeper](https://github.com/pengowray/ScreenSleeper/wiki/Home-Assistant-automation).
