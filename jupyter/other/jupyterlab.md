```
docker run -i -t --rm -p 8888:8888 -v ~/dev-linux/edge-stress-tests/edge-benchmark/jupyter:/home --privileged rheartpython/azureml-jupyterlab:ubuntu18.04

docker run -i -t --rm -p 8888:8888 -v ~/dev-linux/edge-stress-tests/edge-benchmark/jupyter:/home/jovyan/work  jupyter/minimal-notebook
```

connect to http://127.0.0.1:8888/

install the plotly extension:
jupyter labextension install jupyterlab-plotly

