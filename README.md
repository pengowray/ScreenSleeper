![image](SleepScreenWPF/screensleeper-logo-128.png)

# ScreenSleeper

![image](https://github.com/pengowray/ScreenSleeper/assets/800133/e457f06a-12f9-4a50-b25e-5b32147e8099)

A simple MQTT client to turn off your screen and/or lock your PC when triggered by a MQTT message. For Windows 10; may work on other Windows versions but not yet tested.

Motivation: So turning off the light switch also turns off the PC's screen. Or automatically lock your PC when you leave the house. It's kind of satisfying to turn off everything at once if you need to leave your PC running.

This Windows MQTT client is designed for use with Home Assistance (HAOS), but will connect to any MQTT server.

If you don't know what MQTT is and you're not running Home Assistant already, then there might be a learning curve to get those things runnings first.

## Setting up ScreenSleeper

This is just a skeleton outline for now

* Compile and Install (no release version yet)
* Run for the first time
* Find config file (there's a "Find Config" button to make this easy)
* Edit and save config (`mqttConfig.json`)
* Press 'Connect'
* Auto start the app by making a shortcut in `shell:startup`

## Configure Home Assistant
* Install Home Assistant
* Install and configure Mosquito Broker
* Create a user in Mosquito (and reduce the user's access if you want to be security conscious)
* Create an automation,
  * Set what should trigger it under "When"
  * Under "Then do", add action "MQTT: Publish".
  * For topic: "my_pc/sleep/set"
  * For payload: "on"
  * Save automation
  * Note: You can customize the topic and payload to anything you like, just set them the same in ScreenSleeper's config (mqttConfig.json)
* Copy MQTT details to ScreenSleeper's config file
