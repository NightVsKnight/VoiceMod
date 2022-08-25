# VoiceMod

**Shameless plug:** Seriously, one way you can really help out this project is to subscribe to NightVsKnight's
[YouTube](https://www.youtube.com/channel/UCn8Ds6jeUzjxCPkMApg_koA) and/or [Twitch](https://www.twitch.tv/nightvsknight) channels.
I will be showing off this project there from time to time, and getting new subscribers gives me a little morale boost to help me continue this project.

**C# Voice Modulator that uses CSCore and NVIDIA Maxine AFX-SDK Denoiser/Noise-Suppression**

## Build Instruction

I'll improve these steps, but until then:
1. Checkout this repo and initialize its submodules
2. Open the cscore submodule
3. Open the cscore.sln solution in Visual Studio 2022
4. Build the entire cscore.sln solution
5. Close the cscore.sln solution
6. Open VoiceMod.sln solution in Visual Studio 2022
7. Remove NvAfxDotNet's reference to CSCore and then re-add
8. Build
