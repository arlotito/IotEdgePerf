```bash
dotnet run -- -n "<your-EH-name>" -c "<your-EH-conn-string>"
```
(as an alternative to command line options, you can us env vars EH_NAME and EH_CONN_STRING)

Example:
```bash
dotnet run -- -n "rate" -c "Endpoint=sb://arlotitoxyz.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=2az......bg="
```

![](./images/sample1.png)

# build
```bash
dotnet build -r linux-x64 
dotnet run -r linux-x64 
dotnet publish -r linux-x64 --configuration Release 
cp bin/Release/net5.0/linux-x64/publish/iotEdgePerf $HOME
```





