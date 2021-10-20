# System Metrics [![Codacy Badge](https://app.codacy.com/project/badge/Grade/abc00e146ba744e982ba0ddda65d1dd6)](https://www.codacy.com/gh/Pabszito/metrics/dashboard?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=Pabszito/metrics&amp;utm_campaign=Badge_Grade)
A Windows Service that submits data to a [statuspage.io](https://statuspage.io) metric. It can be seen [here](https://cosmictoasters.statuspage.io).
## Installing
Download the .msi installer from [here](https://github.com/Pabszito/metrics/releases). Then, navigate to `C:/Program Files (x86)/SystemMetrics/` and modify the `config.ini` file with your values. You're done. Your statuspage metric is now working.
## Building from source
Requeriments:

- .NET Framework 4.8
- An IDE like Visual Studio
- Git
- WiX for the .msi installer

Install the required dependencies, and then run the following commands:
```sh
git clone https://github.com/Pabszito/metrics # Clone the fucking repository
start ./SystemMetrics/SystemMetrics.sln # Opens the fucking solution with Visual Studio
```
Right-click the solution, and then click Build. If you see no errors at all, congrats, you compiled this shit.
## Notes
Don't be surprised if the code sucks, I just started learning C#.
