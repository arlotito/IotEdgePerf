# cd ~/dev-linux/edge-stress-tests/edge-simulation-framework/source
# dotnet publish -r linux-x64 --configuration Release -o ../..

export DEVICE_ID="standard-ds3-v2-edge-1-2-1631626847"
export EH_NAME="rate"
export EH_CONN_STRING="Endpoint=sb://arlotitoedgestresstest.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=2azS3G/SUgQUh6zxRZYQKYqLDj9bzePIBoUbHcaVxbg="
export IOT_CONN_STRING="HostName=arturol76-s1-benchmark.azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=bVKviSsuLXXksASS8oNnj5deG/gk0d4uj+lII9tAPjI="
export IOT_HUB_NAME="arturol76-s1-benchmark"

./deploy-manifest.sh $IOT_HUB_NAME $DEVICE_ID 200

./iotEdgePerf --payload-length=1024 --burst-length=50000 --burst-number=2 --target-rate=0 --burst-wait=10000 -l "200"
./iotEdgePerf --payload-length=2048 --burst-length=50000 --burst-number=2 --target-rate=0 --burst-wait=10000 -l "200"
./iotEdgePerf --payload-length=4096 --burst-length=50000 --burst-number=2 --target-rate=0 --burst-wait=10000 -l "200"
./iotEdgePerf --payload-length=8192 --burst-length=50000 --burst-number=2 --target-rate=0 --burst-wait=10000 -l "200"
./iotEdgePerf --payload-length=16384 --burst-length=50000 --burst-number=2 --target-rate=0 --burst-wait=10000 -l "200"