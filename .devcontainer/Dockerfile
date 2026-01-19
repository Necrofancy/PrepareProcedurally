FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim

# install mono and rider dependencies
RUN apt-get update && apt-get install -y \
    mono-devel \
    bash \
    git \
    procps 

USER root