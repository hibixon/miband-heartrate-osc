# Mi Band Heartrate
[日本語](README.ja.md)

Enable and monitor heartrate with Mi Band device on Windows 10.

![miband heartrate](https://github.com/Eryux/miband-heartrate/raw/master/mibandheatrate-screen.png "Mi Band Heartrate screen")
![miband heartrate newer version](https://github.com/TayouVR/miband-heartrate-osc/assets/31988415/9f7684bb-03ce-4dff-960e-78cacb0a31b4)


### Supported devices

* Mi Band 2
* Mi Band 3
* Mi Band 4
* Mi Band 5


### Requirements

* .NET Framework 4.8 ([download](https://dotnet.microsoft.com/download/dotnet-framework/net48))
* Windows 10 1703+ (Creators Update)
* Bluetooth adapter supporting Bluetooth 4.0/4.2 BLE


### Usage

* Download and unzip the latest build from release section

#### For Mi Band 2 or Mi Band 3

* Un-pairing your Mi Band 2/3 from any devices

* Connect and pair your Mi Band 2/3 to your computer

* Launch `MiBand-Heartrate.exe`

* Click on `Manual Connect` button and select your device from device list and set model on `Mi Band 2` or `Mi Band 3` then click on `Connect`

* Once your device is successfully connected and authenticated, click on `Start` button

#### For Mi Band 4/5

* Get your authentication key of your device, visit ([freemyband.com](http://www.freemyband.com/)) for more information

* Connect and pair you Mi Band 4/5 to your computer

* Launch `MiBand-Heartrate.exe`

* Click on `Manual Connect` button and select your device from device list and set model on `Mi Band 4` then click on `Connect`

* A new window should appear, enter your authentication key then click on `Ok`

* Once your device is successfully connected and authenticated, click on `Start` button


### Automatic Connection

You can make an automatic connection by setting `MiBand-Heartrate-2.exe.config` in the same directory as `MiBand-Heartrate-2.exe`.

If you enable automatic connection, the `Manual Connect` button changes to a `Auto Connect` button.

|Key|Description|Note|
|-|-|-|
|useAutoConnect|Whether to connect automatically|`true` to enable automatic connection|
|autoConnectDeviceVersion|Device version|`4` for Mi Band 4,`5` for Mi Band 5|
|autoConnectDeviceName|Bluetooth device name|Basically `MiBand 5` for Mi Band 5|
|autoConnectDeviceAuthkey|32-characters authentication key|Example: `0123456789abcdef0123456789abcdef`|

Example
``` xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
    <configSections>
        <sectionGroup name="userSettings" type="System.Configuration.UserSettingsGroup, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089">
            <section name="MiBand_Heartrate.Properties.Settings" type="System.Configuration.ClientSettingsSection, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" allowExeDefinition="MachineToLocalUser" requirePermission="false" />
        </sectionGroup>
    </configSections>
    <userSettings>
        <MiBand_Heartrate.Properties.Settings>
            <setting name="useAutoConnect" serializeAs="String">
                <value>False</value>
            </setting>
            <setting name="autoConnectDeviceVersion" serializeAs="String">
                <value>3</value>
            </setting>
            <setting name="autoConnectDeviceName" serializeAs="String">
                <value>Mi Band 3</value>
            </setting>
            <setting name="autoConnectDeviceAuthKey" serializeAs="String">
                <value>0123456789abcdef0123456789abcdef</value>
            </setting>
            <setting name="continuousMode" serializeAs="String">
                <value>True</value>
            </setting>
            <setting name="fileOutput" serializeAs="String">
                <value>False</value>
            </setting>
            <setting name="csvOutput" serializeAs="String">
                <value>False</value>
            </setting>
            <setting name="oscOutput" serializeAs="String">
                <value>True</value>
            </setting>
            <setting name="tryConnectOnStartup" serializeAs="String">
                <value>False</value>
            </setting>
        </MiBand_Heartrate.Properties.Settings>
    </userSettings>
</configuration>
```


### Options

* **Export data in CSV file :** Log your heartrate data into a CSV file with date, time and heartrate value

* **Write realtime date in text file :** Continuously write heartrate value inside a text file

* **Send OSC to VRChat :** Send your heartbeat and heart rate using OSC. See the next section for detailed specifications.

* **Continuous mode :** Mi Band heartrate sensor can work in two different mode, one-shot and continuous. One-shot mode take 5 to 10 sec. to retrieve a heartrate value then stop. Continuous mode update heartrate value every 2 to 5 sec.


## VRChat
### Addresses

If "Send OSC to VRChat" is enabled, this app will continue to send the following messages

|Addresss|Value Type|Description|
|-|-|-|
|/avatar/parameters/HR/Int|Int|Heart rate per min [0, 255]|
|/avatar/parameters/HR/Float|Float|Normalized Heart rate ([0, 255] -> [-1, 1])|
|/avatar/parameters/HR/HalfFloat|Float|Normalized Heart rate ([0, 255] -> [0, 1]) <br> This is useful when controlling shape keys with Radial.|
|/avatar/parameters/HR/Beat|Int|1 : QRS Interval (Temporarily set it to 1/5 of the RR interval) <br> 0 : Other times|
|/avatar/parameters/HR/Pulse|Bool|True : QRS Interval (Temporarily set it to 1/5 of the RR interval) <br> False : Other times|
|/avatar/parameters/HR/BeatToggle|Bool|Reverses with each heartbeat|
|/avatar/parameters/HR/Connected|Bool|True if device is connected and sending data|
|/avatar/parameters/HR/Min|Int|Minimum heart rate per min in session [0, 255] (not implemented yet)|
|/avatar/parameters/HR/Max|Int|Maximum heart rate per min in session [0, 255] (not implemented yet)|

#### Old Addresses

|Addresss|Value Type|Description|
|-|-|-|
|/avatar/parameters/HeartRateInt|Int|Heart rate per min [0, 255]|
|/avatar/parameters/HeartRate3|Int|Same as HeartRateInt|
|/avatar/parameters/HeartRateFloat|Float|Normalized Heart rate ([0, 255] -> [-1, 1])|
|/avatar/parameters/HeartRate|Float|Same as HeartRateFloat|
|/avatar/parameters/floatHR|Float|Same as HeartRateFloat - for compatibility with other apps|
|/avatar/parameters/HeartRateFloat01|Float|Normalized Heart rate ([0, 255] -> [0, 1]) <br> This is useful when controlling shape keys with Radial.|
|/avatar/parameters/HeartRate2|Float|Same as HeartRateFloat01|
|/avatar/parameters/HeartBeatInt|Int|1 : QRS Interval (Temporarily set it to 1/5 of the RR interval) <br> 0 : Other times|
|/avatar/parameters/HeartBeatPulse|Bool|True : QRS Interval (Temporarily set it to 1/5 of the RR interval) <br> False : Other times|
|/avatar/parameters/HeartBeatToggle|Bool|Reverses with each heartbeat|
|/avatar/parameters/isHRConnected|Bool|True if device is connected and sending data - for compatibility with other apps|

### Build requirements

* Windows SDK 10.0.18362.1

  Download : [Windows SDK and emulator archive](https://developer.microsoft.com/en-US/windows/downloads/sdk-archive/)
* A suitable IDE, like Visual Studio 2022 or Jetbrains Rider
* .Net 7.0 or newer SDK


### Build

* Clone git repository

* Open `MiBand-Heartrate.sln` with Visual Studio

* Right-click on MiBand-Heartrate solution and select generate


### Useful links
* [huami-token](https://github.com/argrento/huami-token)
* [Microsoft GATT Documentation](https://docs.microsoft.com/fr-fr/windows/uwp/devices-sensors/bluetooth-low-energy-overview)
* [Mi Band 2 Authentication by leojrfs](https://leojrfs.github.io/writing/miband2-part1-auth/#reference), [python](https://github.com/leojrfs/miband2)
* https://github.com/creotiv/MiBand2
* [How I hacked my Xiaomi MiBand 2 fitness tracker — a step-by-step Linux guide by Andrey Nikishaev](https://medium.com/machine-learning-world/how-i-hacked-xiaomi-miband-2-to-control-it-from-linux-a5bd2f36d3ad)

VRChat
* [vard88508/vrc-osc-miband-hrm: Mi Band/Amazfit heart rate monitor with OSC integration for VRChat](https://github.com/vard88508/vrc-osc-miband-hrm)
* [ラジアルでのシェイプキー操作方法｜みかんねここ｜note](https://note.com/citron_vr/n/n7d54ebaebd83)
* [OSC HeartRateSendersドキュメントページ - おめが？日記_(2)](https://omega.hatenadiary.jp/entry/2022/02/27/035024)
* [【Mi スマートバンド(Mi Band) × VRChat OSC】自分の心拍数をアバターに表示する方法！ | Till0196のぼーびろく](https://till0196.com/post16907)


### Thirdparty licenses
OscCore | [MIT Licence](https://github.com/tilde-love/osc-core/blob/master/LICENSE)


### License

MIT License
