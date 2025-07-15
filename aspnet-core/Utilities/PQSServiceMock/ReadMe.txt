The following command initiates the SEQ logger tool, which can be highly beneficial for development purposes.

docker run -d --restart unless-stopped --name SEQ_LOGGER_SIMULATOR -e ACCEPT_EULA=Y -v C:\Remove_Demos\Logs:/data -p 8096:80 datalust/seq:latest


docker build -t mock.service.image .
docker run -p 8777:80 mock.service.image