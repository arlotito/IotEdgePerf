
# vscode / wsl2
install pip3
sudo apt install python3-pip


```
docker run -i -t --rm -p 8888:8888 -v ~/dev-linux/edge-stress-tests/edge-benchmark/jupyter:/home --privileged rheartpython/azureml-jupyterlab:ubuntu18.04
```

jupyterlab (but plotly is not showing any chart)
```
docker run -i -t --rm -p 8888:8888 \
    -v ~/dev-linux/edge-stress-tests/edge-benchmark/jupyterlab:/home/jovyan/work \
    -e JUPYTER_ENABLE_LAB=yes \
    -e NB_UID=1000 \
    --name jupyterlab \
    jupyter/minimal-notebook
```

this is ok:
```
docker run -i -t --rm -p 8888:8888 \
    -v ~/dev-linux/edge-stress-tests/edge-benchmark/jupyterlab:/home/jovyan/work \
    -e NB_UID=1000 \
    --name jupyterlab \
    jupyter/minimal-notebook
```

connect to http://127.0.0.1:8888/

install the plotly extension:
jupyter labextension install jupyterlab-plotly

open terminal and:
jupyter trust low-rate-.ipynb

