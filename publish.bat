cd SadFontsUtil\SadFontsUtil\
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:EnableCompressionInSingleFile=true /p:DebugType=None /p:DebugSymbols=false  /p:PublishTrimmed=true /p:TrimMode=partial -o "../../Publish/CLI"
cd ..\SadFontsUtilGUI
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:EnableCompressionInSingleFile=true /p:DebugType=None /p:DebugSymbols=false  -o "../../Publish/GUI"

pause