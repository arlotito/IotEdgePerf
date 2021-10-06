export DEVICE_ID=__redacted__
export EH_NAME=__redacted__
export EH_CONN_STRING=__redacted__
export IOT_CONN_STRING=__redacted__
export IOT_HUB_NAME=__redacted__

./deploy-manifest.sh $IOT_HUB_NAME $DEVICE_ID 200

# round 1
./iotEdgePerf --payload-length=1024 --burst-length=50000 --target-rate=0 -l "yourlabel"
./iotEdgePerf --payload-length=2048 --burst-length=50000 --target-rate=0 -l "yourlabel"
./iotEdgePerf --payload-length=4096 --burst-length=50000 --target-rate=0 -l "yourlabel"
./iotEdgePerf --payload-length=8192 --burst-length=50000 --target-rate=0 -l "yourlabel"
./iotEdgePerf --payload-length=16384 --burst-length=50000 --target-rate=0 -l "yourlabel"
./iotEdgePerf --payload-length=32768 --burst-length=50000 --target-rate=0 -l "yourlabel"