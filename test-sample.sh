export DEVICE_ID=__redacted__
export EH_NAME=__redacted__
export EH_CONN_STRING=__redacted__
export IOT_CONN_STRING=__redacted__
export IOT_HUB_NAME=__redacted__


# deploy transmitter
export MaxUpstreamBatchSize=200
./deploy-transmitter.sh $IOT_HUB_NAME $DEVICE_ID $MaxUpstreamBatchSize arlotito/iotedgeperf-transmitter:0.4.4

# do some tests
dotnet run -p ./source/IotEdgePerf -- --payload-length=1024 --burst-length=50000 --target-rate=10000 -l "yourlabel"
dotnet run -p ./source/IotEdgePerf -- --payload-length=2048 --burst-length=50000 --target-rate=10000 -l "yourlabel"
dotnet run -p ./source/IotEdgePerf -- --payload-length=4096 --burst-length=50000 --target-rate=10000 -l "yourlabel"
dotnet run -p ./source/IotEdgePerf -- --payload-length=8192 --burst-length=50000 --target-rate=10000 -l "yourlabel"
dotnet run -p ./source/IotEdgePerf -- --payload-length=16384 --burst-length=50000 --target-rate=10000 -l "yourlabel"
dotnet run -p ./source/IotEdgePerf -- --payload-length=32768 --burst-length=50000 --target-rate=10000 -l "yourlabel"